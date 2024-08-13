using Moq;
using Xunit;
using System;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using StorageMicroservice.Services;
using StorageMicroservice.Models;
using Microsoft.Extensions.Configuration;

namespace StorageMicroservice.Tests
{
    public class BlobStorageServiceTests
    {
        private readonly Mock<BlobContainerClient> _mockContainerClient;
        private readonly Mock<BlobClient> _mockBlobClient;
        private readonly Mock<ILogger<S3StorageService>> _mockLogger;
        private readonly BlobStorageService _blobStorageService;

        public BlobStorageServiceTests()
        {
            _mockContainerClient = new Mock<BlobContainerClient>();
            _mockBlobClient = new Mock<BlobClient>();
            _mockLogger = new Mock<ILogger<S3StorageService>>();

            _mockContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                .Returns(_mockBlobClient.Object);

            var configuration = new Mock<IConfiguration>();
            configuration.Setup(x => x["Azure:BlobStorageConnectionString"]).Returns("UseDevelopmentStorage=true");
            configuration.Setup(x => x["Azure:ContainerName"]).Returns("test-container");

            _blobStorageService = new BlobStorageService(configuration.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task UploadFileAsync_ShouldReturnFileModel_WhenUploadSucceeds()
        {
            // Arrange
            var fileMock = new Mock<IFormFile>();
            var fileName = "test-file.txt";
            var contentType = "text/plain";
            var content = "Hello World";
            var fileStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));

            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.ContentType).Returns(contentType);
            fileMock.Setup(_ => _.Length).Returns(fileStream.Length);
            fileMock.Setup(_ => _.OpenReadStream()).Returns(fileStream);

          

            // Act
            var result = await _blobStorageService.UploadFileAsync(fileMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(fileName, result.Name);
            Assert.Equal(contentType, result.ContentType);
            Assert.Equal(fileStream.Length, result.Size);
        }

        [Fact]
        public async Task GetFileAsync_ShouldReturnFileInfo_WhenFileExists()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var downloadInfoMock = new Mock<BlobDownloadInfo>();
            downloadInfoMock.Setup(x => x.ContentLength).Returns(100);
            downloadInfoMock.Setup(x => x.ContentType).Returns("text/plain");
            downloadInfoMock.Setup(x => x.Content).Returns(new MemoryStream());

            _mockBlobClient.Setup(x => x.DownloadAsync(default))
                .ReturnsAsync(downloadInfoMock.Object);

            _mockBlobClient.Setup(x => x.GetPropertiesAsync(default))
                .ReturnsAsync(new BlobProperties
                {
                    Metadata = { { "UploadDate", DateTime.UtcNow.ToString("o") } }
                });

            // Act
            var (content, fileInfo) = await _blobStorageService.GetFileAsync(fileId);

            // Assert
            Assert.NotNull(content);
            Assert.NotNull(fileInfo);
            Assert.Equal(fileId, fileInfo.Id);
            Assert.Equal("text/plain", fileInfo.ContentType);
        }

        [Fact]
        public async Task DeleteFileAsync_ShouldNotThrowException_WhenFileExists()
        {
            // Arrange
            var fileId = Guid.NewGuid();

            _mockBlobClient.Setup(x => x.DeleteIfExistsAsync(default))
                .ReturnsAsync(true);

            // Act
            var exception = await Record.ExceptionAsync(() => _blobStorageService.DeleteFileAsync(fileId));

            // Assert
            Assert.Null(exception);
        }
    }
}
