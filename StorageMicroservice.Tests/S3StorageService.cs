using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using StorageMicroservice.Models;
using StorageMicroservice.Services;
using Microsoft.AspNetCore.Http;

namespace StorageMicroservice.Tests
{
    [TestFixture]
    public class S3StorageServiceTests
    {
        private Mock<IAmazonS3> _mockS3Client;
        private Mock<ILogger<S3StorageService>> _mockLogger;
        private S3StorageService _s3StorageService;
        private string _bucketName = "test-bucket";

        [SetUp]
        public void Setup()
        {
            _mockS3Client = new Mock<IAmazonS3>();
            _mockLogger = new Mock<ILogger<S3StorageService>>();

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(x => x["AWS:Region"]).Returns("us-west-2");
            configuration.Setup(x => x["AWS:AccessKey"]).Returns("access-key");
            configuration.Setup(x => x["AWS:SecretKey"]).Returns("secret-key");
            configuration.Setup(x => x["AWS:BucketName"]).Returns(_bucketName);

            _s3StorageService = new S3StorageService(configuration.Object, _mockLogger.Object);
        }

        [Test]
        public async Task UploadFileAsync_ShouldReturnFileModel_WhenUploadSucceeds()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var fileName = "test.txt";
            var fileContent = "Hello, world!";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(fileContent);
            writer.Flush();
            ms.Position = 0;

            fileMock.Setup(f => f.OpenReadStream()).Returns(ms);
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(ms.Length);
            fileMock.Setup(f => f.ContentType).Returns("text/plain");

            var putObjectResponse = new PutObjectResponse();
            _mockS3Client.Setup(s3 => s3.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
                         .ReturnsAsync(putObjectResponse);

            // Act
            var result = await _s3StorageService.UploadFileAsync(fileMock.Object);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fileName, result.Name);
            Assert.AreEqual(ms.Length, result.Size);
            Assert.AreEqual("text/plain", result.ContentType);
            _mockS3Client.Verify(s3 => s3.PutObjectAsync(It.IsAny<PutObjectRequest>(), default), Times.Once);
        }

        [Test]
        public async Task GetFileAsync_ShouldReturnFileContent_WhenFileExists()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var fileContent = "Hello, world!";
            var getObjectResponse = new GetObjectResponse
            {
                Key = fileId.ToString(),
                ContentLength = fileContent.Length,
                Headers = { ContentType = "text/plain" },
                ResponseStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent))
            };
            getObjectResponse.Metadata.Add("x-amz-meta-uploaddate", DateTime.UtcNow.ToString("o"));

            _mockS3Client.Setup(s3 => s3.GetObjectAsync(It.IsAny<GetObjectRequest>(), default))
                         .ReturnsAsync(getObjectResponse);

            // Act
            var (content, fileInfo) = await _s3StorageService.GetFileAsync(fileId);

            // Assert
            Assert.IsNotNull(content);
            Assert.IsNotNull(fileInfo);
            Assert.AreEqual(fileId, fileInfo.Id);
            Assert.AreEqual(getObjectResponse.Key, fileInfo.Name);
            _mockS3Client.Verify(s3 => s3.GetObjectAsync(It.IsAny<GetObjectRequest>(), default), Times.Once);
        }

        [Test]
        public async Task DeleteFileAsync_ShouldInvokeDeleteObject_WhenFileExists()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var deleteObjectResponse = new DeleteObjectResponse(); // No additional properties needed for this test

            _mockS3Client.Setup(s3 => s3.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), default))
                         .ReturnsAsync(deleteObjectResponse);

            // Act
            await _s3StorageService.DeleteFileAsync(fileId);

            // Assert
            _mockS3Client.Verify(s3 => s3.DeleteObjectAsync(It.Is<DeleteObjectRequest>(req => req.Key == fileId.ToString() && req.BucketName == _bucketName), default), Times.Once);
        }
    }
}
