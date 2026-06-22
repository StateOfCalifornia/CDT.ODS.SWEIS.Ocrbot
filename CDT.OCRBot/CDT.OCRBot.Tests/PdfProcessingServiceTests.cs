using CDT.OCRBot.Application.Services;
using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;
using Moq;

namespace CDT.OCRBot.Tests
{
    public class PdfProcessingServiceTests
    {
        private readonly Mock<IPdfProcessor> _mockPdfProcessor;
        private readonly Mock<IAppLogger> _mockLogger;
        private readonly Mock<IAuditLogger> _mockAuditLogger;
        private readonly PdfProcessingService _service;

        public PdfProcessingServiceTests()
        {
            _mockPdfProcessor = new Mock<IPdfProcessor>();
            _mockLogger = new Mock<IAppLogger>();
            _mockAuditLogger = new Mock<IAuditLogger>();
            _service = new PdfProcessingService(_mockPdfProcessor.Object, _mockLogger.Object, _mockAuditLogger.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidInputs_ReturnsSuccess()
        {
            // Arrange
            string inputPath = "input.pdf";
            string outputPath = "output.pdf";
            var options = new ProcessingOptions();

            _mockPdfProcessor.Setup(x => x.ProcessPdfAsync(
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<ProcessingOptions>(), 
                It.IsAny<IProgress<ProcessingProgress>>(), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(ProcessingResult.Success(outputPath, 100));

            // Act
            var result = await _service.ExecuteAsync(inputPath, outputPath, options);

            // Assert
            Assert.True(result.IsSuccess);
            _mockPdfProcessor.Verify(x => x.ProcessPdfAsync(inputPath, outputPath, options, null, default), Times.Once);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyInputPath_ReturnsFailure()
        {
            // Act
            var result = await _service.ExecuteAsync("", "output.pdf", new ProcessingOptions());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Input file path cannot be empty", result.Error?.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WithEmptyOutputPath_ReturnsFailure()
        {
            // Act
            var result = await _service.ExecuteAsync("input.pdf", "", new ProcessingOptions());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Contains("Output file path cannot be empty", result.Error?.Message);
        }

        [Fact]
        public async Task ExecuteAsync_WhenProcessorThrows_ReturnsFailure()
        {
            // Arrange
            _mockPdfProcessor.Setup(x => x.ProcessPdfAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<ProcessingOptions>(),
                It.IsAny<IProgress<ProcessingProgress>>(),
                It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Processing failed"));

            // Act
            var result = await _service.ExecuteAsync("input.pdf", "output.pdf", new ProcessingOptions());

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal("Processing failed", result.Error?.Message);
        }
    }
}
