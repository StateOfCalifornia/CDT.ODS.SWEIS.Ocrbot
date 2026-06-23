using CDT.OCRBot.Domain.Models;
using Xunit;

namespace CDT.OCRBot.Tests.Domain
{
    public class ProcessingOptionsTests
    {
        [Theory]
        [InlineData(false, false, "Searchable PDF (invisible text over original)")]
        [InlineData(true, false, "Text-only PDF (no images)")]
        [InlineData(false, true, "Tagged searchable PDF (PDF/UA compliant)")]
        [InlineData(true, true, "Tagged text-only PDF (PDF/UA compliant)")]
        public void GetDescription_ReturnsCorrectDescription(bool textOnly, bool addTags, string expected)
        {
            // Arrange
            var options = new ProcessingOptions
            {
                TextOnly = textOnly,
                AddUATags = addTags
            };

            // Act
            var description = options.GetDescription();

            // Assert
            Assert.Equal(expected, description);
        }

        [Theory]
        [InlineData(false, 1.0)]
        [InlineData(true, 3.5)]
        public void GetTimeMultiplier_ReturnsCorrectMultiplier(bool addTags, double expected)
        {
            // Arrange
            var options = new ProcessingOptions
            {
                AddUATags = addTags
            };

            // Act
            var multiplier = options.GetTimeMultiplier();

            // Assert
            Assert.Equal(expected, multiplier);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, true)]
        public void RequiresTaggingService_ReturnsCorrectValue(bool addTags, bool expected)
        {
            // Arrange
            var options = new ProcessingOptions
            {
                AddUATags = addTags
            };

            // Act & Assert
            Assert.Equal(expected, options.RequiresTaggingService);
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        [InlineData(true, true, true)]
        public void RequiresOriginalPdf_ReturnsCorrectValue(bool textOnly, bool addTags, bool expected)
        {
            // Arrange
            var options = new ProcessingOptions
            {
                TextOnly = textOnly,
                AddUATags = addTags
            };

            // Act & Assert
            Assert.Equal(expected, options.RequiresOriginalPdf);
        }
    }
}
