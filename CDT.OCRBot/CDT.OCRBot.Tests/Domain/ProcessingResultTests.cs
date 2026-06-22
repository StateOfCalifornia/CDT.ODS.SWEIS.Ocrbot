using CDT.OCRBot.Domain.Configuration;
using CDT.OCRBot.Domain.Models;
using Xunit;

namespace CDT.OCRBot.Tests.Domain
{
    public class ProcessingResultTests
    {
        [Fact]
        public void Success_CreatesSuccessResult()
        {
            // Arrange
            var outputPath = "C:\\output\\file.pdf";
            var processingTime = 1000L;
            var pageCount = 5;
            var outputFileSize = 50000L;
            var message = "Success";

            // Act
            var result = ProcessingResult.Success(outputPath, processingTime, pageCount, outputFileSize, message);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(outputPath, result.OutputFilePath);
            Assert.Equal(processingTime, result.ProcessingTimeMs);
            Assert.Equal(pageCount, result.PageCount);
            Assert.Equal(outputFileSize, result.OutputFileSizeBytes);
            Assert.Equal(message, result.Message);
            Assert.Null(result.Error);
        }

        [Fact]
        public void Success_WithDefaultMessage_UsesDefaultMessage()
        {
            // Act
            var result = ProcessingResult.Success("output.pdf", 1000);

            // Assert
            Assert.Equal("PDF processed successfully", result.Message);
        }

        [Fact]
        public void Failure_CreatesFailureResult()
        {
            // Arrange
            var errorMessage = "Processing failed";
            var exception = new Exception("Test error");
            var processingTime = 500L;

            // Act
            var result = ProcessingResult.Failure(errorMessage, exception, processingTime);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(errorMessage, result.Message);
            Assert.Equal(exception, result.Error);
            Assert.Equal(processingTime, result.ProcessingTimeMs);
            Assert.Empty(result.OutputFilePath);
        }

        [Fact]
        public void Failure_WithoutException_CreatesFailureResult()
        {
            // Arrange
            var errorMessage = "Processing failed";

            // Act
            var result = ProcessingResult.Failure(errorMessage);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(errorMessage, result.Message);
            Assert.Null(result.Error);
            Assert.Equal(0, result.ProcessingTimeMs);
        }
    }
}
