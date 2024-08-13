using Amazon.S3;
using Amazon.S3.Model;
using StorageMicroservice.Models;

namespace StorageMicroservice.Services
{
    public class S3StorageService : IStorageService
    {

        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;
        private readonly ILogger<S3StorageService> _logger;


        public S3StorageService(IConfiguration configuration, ILogger<S3StorageService> logger)
        {
            var awsOptions = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(configuration["AWS:Region"])
            };
            _s3Client = new AmazonS3Client(configuration["AWS:AccessKey"], configuration["AWS:SecretKey"], awsOptions);

            _bucketName = configuration["AWS:BucketName"];
            _logger = logger;
        }

        public async Task<FileModel> UploadFileAsync(IFormFile file)
        {
            try
            {

                var key = Guid.NewGuid().ToString();
                var uploadDate = DateTime.UtcNow;

                using (var stream = file.OpenReadStream())
                {
                    var putRequest = new PutObjectRequest
                    {
                        InputStream = stream,
                        BucketName = _bucketName,
                        Key = key,
                        ContentType = file.ContentType,
                    };
                    putRequest.Metadata.Add("x-amz-meta-uploaddate", uploadDate.ToString("o"));

                    await _s3Client.PutObjectAsync(putRequest);
                }

                return new FileModel
                {
                    Id = Guid.Parse(key),
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
                var request = new GetObjectRequest
                {
                    BucketName = _bucketName,
                    Key = id.ToString()
                };

                var response = await _s3Client.GetObjectAsync(request);

                var uploadDateString = response.Metadata["x-amz-meta-uploaddate"];
                DateTime uploadDate = string.IsNullOrEmpty(uploadDateString)
                    ? DateTime.UtcNow
                    : DateTime.Parse(uploadDateString);

                var fileInfo = new FileModel
                {
                    Id = id,
                    Name = response.Key,
                    Size = response.ContentLength,
                    ContentType = response.Headers.ContentType,
                    UploadDate = uploadDate
                };

                return (response.ResponseStream, fileInfo);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error processing message: {Message} ", ex.Message);
                return (null, null);

            }
        }

        public async Task DeleteFileAsync(Guid id)
        {
            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = _bucketName,
                    Key = id.ToString()
                };

                await _s3Client.DeleteObjectAsync(deleteObjectRequest);
            }
            catch (Exception ex)
            {

                _logger.LogError(ex, "Error processing message: {Message} ", ex.Message);

            }
        }
    }
}