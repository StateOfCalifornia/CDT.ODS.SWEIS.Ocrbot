using CDT.OCRBot.Domain.Configuration;
using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Application.Services
{
    public class ConfigurationLoadingService
    {
        private readonly ICredentialRepository _credentialRepository;

        public ConfigurationLoadingService(ICredentialRepository credentialRepository)
        {
            _credentialRepository = credentialRepository;
        }

        public async Task<AppConfiguration> ExecuteAsync()
        {
            var directoryConfig = await _credentialRepository.LoadDirectoryConfigAsync();
            var featureConfig = await _credentialRepository.LoadFeatureConfigAsync();

            return new AppConfiguration
            {
                DirectoryConfig = directoryConfig,
                FeatureConfig = featureConfig
            };
        }
    }
}
