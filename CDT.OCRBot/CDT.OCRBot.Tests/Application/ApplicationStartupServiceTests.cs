using CDT.OCRBot.Application.Services;
using CDT.OCRBot.Domain.Interfaces;
using Moq;
using Xunit;

namespace CDT.OCRBot.Tests.Application
{
    public class ApplicationStartupServiceTests
    {
        private readonly Mock<IPdfProcessor> _mockPdfProcessor;
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly ApplicationStartupService _service;

        public ApplicationStartupServiceTests()
        {
            _mockPdfProcessor = new Mock<IPdfProcessor>();
            _mockLogger = new Mock<IAppLogger>();
            _service = new ApplicationStartupService(_mockPdfProcessor.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WhenInitializationSucceeds_ReturnsTrue()
        {
            // Arrange
            _mockPdfProcessor
                .Setup(x => x.InitializeAsync())
                .ReturnsAsync(true);

            // Act
            var result = await _service.ExecuteAsync();

            // Assert
            Assert.True(result);
            _mockLogger.Verify(x => x.LogInformation("Initializing services..."), Times.Once);
            _mockLogger.Verify(x => x.LogInformation("Services initialized successfully"), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenInitializationFails_ReturnsFalse()
        {
            // Arrange
            _mockPdfProcessor
                .Setup(x => x.InitializeAsync())
                .ReturnsAsync(false);

            // Act
            var result = await _service.ExecuteAsync();

            // Assert
            Assert.False(result);
            _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception?>()), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WhenExceptionThrown_ReturnsFalseAndLogsError()
        {
            // Arrange
            var exception = new Exception("Test exception");
            _mockPdfProcessor
                .Setup(x => x.InitializeAsync())
                .ThrowsAsync(exception);

            // Act
            var result = await _service.ExecuteAsync();

            // Assert
            Assert.False(result);
            _mockLogger.Verify(x => x.LogError(It.IsAny<string>(), It.IsAny<Exception?>()), Times.Once);
        }
    }
}
