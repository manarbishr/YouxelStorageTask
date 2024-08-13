//using Moq;
//using Xunit;
//using FluentAssertions;
//using StorageMicroservice.Services; // Adjust the namespace according to your project
//using Amazon.S3; // AWS SDK for .NET
//using Amazon.S3.Model;
//using System.Threading.Tasks;

//namespace StorageMicroservice.Tests
//{
//    public class S3StorageServiceTests
//    {
//        private readonly Mock<IAmazonS3> _mockS3Client;
//        private readonly S3StorageService _s3StorageService;

//        public S3StorageServiceTests()
//        {
//            _mockS3Client = new Mock<IAmazonS3>();
//            _s3StorageService = new S3StorageService(_mockS3Client.Object);
//        }

//        [Fact]
//        public async Task UploadFile_ShouldReturnTrue_WhenUploadSucceeds()
//        {
//            // Arrange
//            var bucketName = "test-bucket";
//            var fileName = "test-file.txt";
//            var fileContent = new byte[] { 1, 2, 3 };

//            _mockS3Client.Setup(client => client.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
//                .ReturnsAsync(new PutObjectResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

//            // Act
//            var result = await _s3StorageService.UploadFileAsync(bucketName, fileName, fileContent);

//            // Assert
//            result.Should().BeTrue();
//        }

//        [Fact]
//        public async Task UploadFile_ShouldReturnFalse_WhenUploadFails()
//        {
//            // Arrange
//            var bucketName = "test-bucket";
//            var fileName = "test-file.txt";
//            var fileContent = new byte[] { 1, 2, 3 };

//            _mockS3Client.Setup(client => client.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
//                .ThrowsAsync(new AmazonS3Exception("Error"));

//            // Act
//            var result = await _s3StorageService.UploadFileAsync(bucketName, fileName, fileContent);

//            // Assert
//            result.Should().BeFalse();
//        }
//    }
//}
