using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using StorageMicroservice.Models;

namespace StorageMicroservice.Services
{
    public class BlobStorageService : IStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<S3StorageService> _logger;


        public BlobStorageService(IConfiguration configuration, ILogger<S3StorageService> logger)
        {
            string connectionString = configuration["Azure:BlobStorageConnectionString"];
            string containerName = configuration["Azure:ContainerName"];

            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            _containerClient.CreateIfNotExists(PublicAccessType.Blob);
            _logger = logger;
        }

        public async Task<FileModel> UploadFileAsync(IFormFile file)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(Guid.NewGuid().ToString());
                var uploadDate = DateTime.UtcNow;

                var metadata = new BlobHttpHeaders
                {
                    ContentType = file.ContentType
                };

                using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, metadata);
                    await blobClient.SetMetadataAsync(new System.Collections.Generic.Dictionary<string, string>
                {
                    { "UploadDate", uploadDate.ToString("o") }
                });
                }

                return new FileModel
                {
                    Id = Guid.Parse(blobClient.Name),
                    Name = file.FileName,
                    Size = file.Length,
                    ContentType = file.ContentType,
                    UploadDate = uploadDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message} ", ex.Message);
                return null;
            }
        }

        public async Task<(Stream content, FileModel fileInfo)> GetFileAsync(Guid id)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(id.ToString());
                BlobDownloadInfo download = await blobClient.DownloadAsync();

                var properties = await blobClient.GetPropertiesAsync();
                var uploadDate = properties.Value.Metadata.TryGetValue("UploadDate", out var date)
                    ? DateTime.Parse(date) : DateTime.UtcNow;

                var fileInfo = new FileModel
                {
                    Id = id,
                    Name = blobClient.Name,
                    Size = download.ContentLength,
                    ContentType = download.ContentType,
                    UploadDate = uploadDate
                };

                return (download.Content, fileInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message} ", ex.Message);
                return (null,null);
            }
        }

        public async Task DeleteFileAsync(Guid id)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(id.ToString());
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message: {Message} ", ex.Message);
            }
        }
    }
}