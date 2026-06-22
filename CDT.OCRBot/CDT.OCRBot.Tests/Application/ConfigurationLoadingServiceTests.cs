using CDT.OCRBot.Application.Services;
using CDT.OCRBot.Domain.Configuration;
using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;
using Moq;
using Xunit;

namespace CDT.OCRBot.Tests.Application
{
    public class ConfigurationLoadingServiceTests
    {
        private readonly Mock<ICredentialRepository> _mockCredentialRepository;
        private readonly ConfigurationLoadingService _service;

        public ConfigurationLoadingServiceTests()
        {
            _mockCredentialRepository = new Mock<ICredentialRepository>();
            _service = new ConfigurationLoadingService(_mockCredentialRepository.Object);
        }

        [Fact]
        public async Task ExecuteAsync_WithValidConfigs_ReturnsAppConfiguration()
        {
            // Arrange
            var directoryConfig = new DirectoryConfig
            {
                DefaultInputDirectory = "C:\\Input",
                DefaultOutputDirectory = "C:\\Output"
            };
            var featureConfig = new FeatureConfig
            {
                EnableTextDump = false,
                EnableAutoTag = true
            };

            _mockCredentialRepository
                .Setup(x => x.LoadDirectoryConfigAsync())
                .ReturnsAsync(directoryConfig);
            _mockCredentialRepository
                .Setup(x => x.LoadFeatureConfigAsync())
                .ReturnsAsync(featureConfig);

            // Act
            var result = await _service.ExecuteAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(directoryConfig, result.DirectoryConfig);
            Assert.Equal(featureConfig, result.FeatureConfig);
        }

        [Fact]
        public async Task ExecuteAsync_CallsRepositoryMethods()
        {
            // Arrange
            _mockCredentialRepository
                .Setup(x => x.LoadDirectoryConfigAsync())
                .ReturnsAsync(new DirectoryConfig());
            _mockCredentialRepository
                .Setup(x => x.LoadFeatureConfigAsync())
                .ReturnsAsync(new FeatureConfig());

            // Act
            await _service.ExecuteAsync();

            // Assert
            _mockCredentialRepository.Verify(x => x.LoadDirectoryConfigAsync(), Times.Once);
            _mockCredentialRepository.Verify(x => x.LoadFeatureConfigAsync(), Times.Once);
        }
    }
}
