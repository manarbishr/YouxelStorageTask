using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using StorageMicroservice.Models;

namespace StorageMicroservice.Services
{
    public class RabbitMqConsumerService : BackgroundService
    {
        private readonly IStorageService _storageService;
        private readonly string _requestQueueName;
        private readonly string _responseQueueName;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqConsumerService> _logger;


        public RabbitMqConsumerService(IConfiguration configuration, IStorageService storageService, ILogger<RabbitMqConsumerService> logger)
        {
            _storageService = storageService;
            var factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:HostName"],
                UserName = configuration["RabbitMQ:UserName"],
                Password = configuration["RabbitMQ:Password"]
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _requestQueueName = configuration["RabbitMQ:RequestQueueName"];
            _responseQueueName = configuration["RabbitMQ:ResponseQueueName"];

            _channel.QueueDeclare(queue: _requestQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueDeclare(queue: _responseQueueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var request = JsonConvert.DeserializeObject<FileOperationRequest>(message);

                switch (request.Action)
                {
                    case "UploadFile":
                        await HandleUploadFileAsync(request);
                        break;
                    case "GetFile":
                        await HandleGetFileAsync(request);
                        break;
                    case "DeleteFile":
                        await HandleDeleteFileAsync(request);
                        break;
                }
            };

            _channel.BasicConsume(queue: _requestQueueName, autoAck: true, consumer: consumer);

            return Task.CompletedTask;
        }

        public async Task HandleUploadFileAsync(FileOperationRequest request)
        {
            try
            {

                using var memoryStream = new MemoryStream(Convert.FromBase64String(request.FileContentBase64));
                var file = new FormFile(memoryStream, 0, memoryStream.Length, null, request.FileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = request.ContentType
                };

                var result = await _storageService.UploadFileAsync(file);

                var response = new FileOperationResponse
                {
                    Success = true,
                    Action = "UploadFile",
                    FileId = result.Id,
                    Message = "File uploaded successfully."
                };

                PublishResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message} , {Action}", ex.Message, "UploadFile");
                PublishResponse(new FileOperationResponse { Success = false, Action = "UploadFile", Message = ex.Message });
            }
        }

        public async Task HandleGetFileAsync(FileOperationRequest request)
        {
            try
            {
                var (content, fileInfo) = await _storageService.GetFileAsync(request.FileId);
                using var memoryStream = new MemoryStream();
                await content.CopyToAsync(memoryStream);

                var response = new FileOperationResponse
                {
                    Success = true,
                    Action = "GetFile",
                    FileId = fileInfo.Id,
                    FileName = fileInfo.Name,
                    FileContentBase64 = Convert.ToBase64String(memoryStream.ToArray()),
                    Message = "File retrieved successfully."
                };

                PublishResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message} , {Action}", ex.Message, "GetFile");
                PublishResponse(new FileOperationResponse { Success = false, Action = "GetFile", Message = ex.Message });
            }
        }

        public async Task HandleDeleteFileAsync(FileOperationRequest request)
        {
            try
            {
                await _storageService.DeleteFileAsync(request.FileId);

                var response = new FileOperationResponse
                {
                    Success = true,
                    Action = "DeleteFile",
                    FileId = request.FileId,
                    Message = "File deleted successfully."
                };

                PublishResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message} , {Action}", ex.Message, "DeleteFile");
                PublishResponse(new FileOperationResponse { Success = false, Action = "DeleteFile", Message = ex.Message });
            }
        }

        private void PublishResponse(FileOperationResponse response)
        {
            var responseJson = JsonConvert.SerializeObject(response);
            var responseBytes = Encoding.UTF8.GetBytes(responseJson);

            _channel.BasicPublish(exchange: "", routingKey: _responseQueueName, basicProperties: null, body: responseBytes);
        }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }


}