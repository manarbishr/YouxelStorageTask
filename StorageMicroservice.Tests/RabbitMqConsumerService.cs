using NUnit.Framework;
using Moq;
using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StorageMicroservice.Models;
using StorageMicroservice.Services;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace StorageMicroservice.Tests
{
    [TestFixture]
    public class RabbitMqConsumerServiceTests
    {
        private Mock<IStorageService> _mockStorageService;
        private Mock<IConnection> _mockConnection;
        private Mock<IModel> _mockChannel;
        private Mock<ILogger<RabbitMqConsumerService>> _mockLogger;
        private RabbitMqConsumerService _rabbitMqConsumerService;

        [SetUp]
        public void Setup()
        {
            _mockStorageService = new Mock<IStorageService>();
            _mockConnection = new Mock<IConnection>();
            _mockChannel = new Mock<IModel>();
            _mockLogger = new Mock<ILogger<RabbitMqConsumerService>>();

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(x => x["RabbitMQ:HostName"]).Returns("localhost");
            configuration.Setup(x => x["RabbitMQ:UserName"]).Returns("guest");
            configuration.Setup(x => x["RabbitMQ:Password"]).Returns("guest");
            configuration.Setup(x => x["RabbitMQ:RequestQueueName"]).Returns("requestQueue");
            configuration.Setup(x => x["RabbitMQ:ResponseQueueName"]).Returns("responseQueue");

            _mockConnection.Setup(c => c.CreateModel()).Returns(_mockChannel.Object);

            _rabbitMqConsumerService = new RabbitMqConsumerService(configuration.Object, _mockStorageService.Object, _mockLogger.Object);
        }

        [Test]
        public async Task HandleUploadFileAsync_ShouldPublishSuccessResponse_WhenUploadSucceeds()
        {
            // Arrange
            var request = new FileOperationRequest
            {
                Action = "UploadFile",
                FileContentBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("dummy content")),
                FileName = "test.txt",
                ContentType = "text/plain"
            };

            var fileModel = new FileModel
            {
                Id = Guid.NewGuid(),
                Name = request.FileName,
                Size = 100,
                ContentType = request.ContentType
            };

            _mockStorageService.Setup(s => s.UploadFileAsync(It.IsAny<IFormFile>())).ReturnsAsync(fileModel);

            // Act
            await _rabbitMqConsumerService.HandleUploadFileAsync(request);

            // Assert
            _mockChannel.Verify(c => c.BasicPublish(
                It.Is<string>(e => e == ""),
                It.Is<string>(rk => rk == "responseQueue"),
                It.IsAny<IBasicProperties>(),
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b).Contains("File uploaded successfully"))),
                Times.Once);
        }

        [Test]
        public async Task HandleGetFileAsync_ShouldPublishSuccessResponse_WhenGetSucceeds()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var request = new FileOperationRequest
            {
                Action = "GetFile",
                FileId = fileId
            };

            var fileModel = new FileModel
            {
                Id = fileId,
                Name = "test.txt",
                Size = 100,
                ContentType = "text/plain"
            };

            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes("dummy content"));

            _mockStorageService.Setup(s => s.GetFileAsync(It.IsAny<Guid>())).ReturnsAsync((memoryStream, fileModel));

            // Act
            await _rabbitMqConsumerService.HandleGetFileAsync(request);

            // Assert
            _mockChannel.Verify(c => c.BasicPublish(
                It.Is<string>(e => e == ""),
                It.Is<string>(rk => rk == "responseQueue"),
                It.IsAny<IBasicProperties>(),
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b).Contains("File retrieved successfully"))),
                Times.Once);
        }

        [Test]
        public async Task HandleDeleteFileAsync_ShouldPublishSuccessResponse_WhenDeleteSucceeds()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var request = new FileOperationRequest
            {
                Action = "DeleteFile",
                FileId = fileId
            };

            _mockStorageService.Setup(s => s.DeleteFileAsync(It.IsAny<Guid>())).Returns(Task.CompletedTask);

            // Act
            await _rabbitMqConsumerService.HandleDeleteFileAsync(request);

            // Assert
            _mockChannel.Verify(c => c.BasicPublish(
                It.Is<string>(e => e == ""),
                It.Is<string>(rk => rk == "responseQueue"),
                It.IsAny<IBasicProperties>(),
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b).Contains("File deleted successfully"))),
                Times.Once);
        }
    }
}
