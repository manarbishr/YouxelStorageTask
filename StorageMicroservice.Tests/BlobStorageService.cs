using NUnit.Framework;
using Moq;
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
using Azure;

namespace StorageMicroservice.Tests
{
    [TestFixture]
    public class BlobStorageServiceTests
    {
        private Mock<BlobContainerClient> _mockContainerClient;
        private Mock<BlobClient> _mockBlobClient;
        private Mock<ILogger<S3StorageService>> _mockLogger;
        private BlobStorageService _blobStorageService;

        [SetUp]
        public void Setup()
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

        [Test]
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

            var blobContentInfo = BlobsModelFactory.BlobContentInfo(
                new ETag("test-etag"),
                DateTimeOffset.UtcNow,
                System.Text.Encoding.UTF8.GetBytes(content),
                "test-md5",
                "test-content-crc64",
                "test-request-id",
                0);

            _mockBlobClient.Setup(x => x.UploadAsync(It.IsAny<Stream>()))
                .ReturnsAsync(Response.FromValue(blobContentInfo, Mock.Of<Response>()));

            // Act
            var result = await _blobStorageService.UploadFileAsync(fileMock.Object);

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(fileName, result.Name);
            Assert.AreEqual(contentType, result.ContentType);
            Assert.AreEqual(fileStream.Length, result.Size);
        }

        [Test]
        public async Task GetFileAsync_ShouldReturnFileInfo_WhenFileExists()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var memoryStream = new MemoryStream();
            var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(
                content: memoryStream,
                contentType: "text/plain",
                contentLength: 100,
                contentHash: null,
                blobSequenceNumber: 0,
                blobType: BlobType.Block,
                leaseStatus: LeaseStatus.Unlocked,
                encryptionKeySha256: null,
                encryptionScope: null,
                metadata: new System.Collections.Generic.Dictionary<string, string>
                {
                    { "UploadDate", DateTime.UtcNow.ToString("o") }
                });

            _mockBlobClient.Setup(x => x.DownloadAsync(default))
                .ReturnsAsync(Response.FromValue(blobDownloadInfo, Mock.Of<Response>()));

            var propertiesResponseMock = new Mock<Response<BlobProperties>>();
            propertiesResponseMock.Setup(p => p.Value.Metadata).Returns(new System.Collections.Generic.Dictionary<string, string>
            {
                { "UploadDate", DateTime.UtcNow.ToString("o") }
            });
            _mockBlobClient.Setup(x => x.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), default))
                .ReturnsAsync(propertiesResponseMock.Object);

            // Act
            var (content, fileInfo) = await _blobStorageService.GetFileAsync(fileId);

            // Assert
            Assert.NotNull(content);
            Assert.NotNull(fileInfo);
            Assert.AreEqual(fileId, fileInfo.Id);
            Assert.AreEqual("text/plain", fileInfo.ContentType);
        }

        [Test]
        public async Task DeleteFileAsync_ShouldNotThrowException_WhenFileExists()
        {
            // Arrange
            var fileId = Guid.NewGuid();
            var responseMock = new Mock<Response<bool>>();
            responseMock.Setup(r => r.Value).Returns(true);

            _mockBlobClient.Setup(x => x.DeleteIfExistsAsync(DeleteSnapshotsOption.None, It.IsAny<BlobRequestConditions>(), default))
                .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _blobStorageService.DeleteFileAsync(fileId));
        }
    }
}
