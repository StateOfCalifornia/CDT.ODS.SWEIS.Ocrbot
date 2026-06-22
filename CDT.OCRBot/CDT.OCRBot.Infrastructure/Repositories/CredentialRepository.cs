using CDT.OCRBot.Domain.Configuration;
using CDT.OCRBot.Domain.Interfaces;
using Meziantou.Framework.Win32;

namespace CDT.OCRBot.Infrastructure.Repositories
{
    /// <summary>
    /// Repository for securely storing and retrieving credentials using Windows Credential Manager
    /// </summary>
    public class CredentialRepository : ICredentialRepository
    {
        private readonly IAppLogger _logger;
        private const string TargetPrefix = "OCRBot_";

        // Credential keys for Azure services
        private const string DocIntelEndpoint = "DocumentIntelligence_Endpoint";
        private const string DocIntelKey = "DocumentIntelligence_ApiKey";
        private const string OpenAiEndpoint = "OpenAI_Endpoint";
        private const string OpenAiKey = "OpenAI_ApiKey";
        private const string OpenAiDeployment = "OpenAI_DeploymentName";

        // Credential keys for directory configuration
        private const string DefaultInputDir = "DefaultInputDirectory";
        private const string DefaultOutputDir = "DefaultOutputDirectory";

        // Credential keys for feature configuration
        private const string FeatureEnableTextDump = "Feature_EnableTextDump";
        private const string FeatureEnableAutoTag = "Feature_EnableAutoTag";

        /// <summary>
        /// Initializes a new instance of CredentialRepository
        /// </summary>
        /// <param name="logger">Application logger</param>
        public CredentialRepository(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogDebug("CredentialRepository initialized");
        }

        /// <inheritdoc/>
        public Task<bool> SaveAzureCredentialsAsync(AzureConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            try
            {
                _logger.LogInformation("Saving Azure credentials to Windows Credential Manager");

                SaveCredential(DocIntelEndpoint, config.DocumentIntelligenceEndpoint);
                SaveCredential(DocIntelKey, config.DocumentIntelligenceApiKey);
                SaveCredential(OpenAiEndpoint, config.OpenAiEndpoint);
                SaveCredential(OpenAiKey, config.OpenAiApiKey);
                SaveCredential(OpenAiDeployment, config.OpenAiDeploymentName);

                _logger.LogInformation("Azure credentials saved successfully");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save Azure credentials: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public Task<AzureConfig?> LoadAzureCredentialsAsync()
        {
            try
            {
                _logger.LogDebug("Loading Azure credentials from Windows Credential Manager");

                var config = new AzureConfig
                {
                    DocumentIntelligenceEndpoint = LoadCredential(DocIntelEndpoint),
                    DocumentIntelligenceApiKey = LoadCredential(DocIntelKey),
                    OpenAiEndpoint = LoadCredential(OpenAiEndpoint),
                    OpenAiApiKey = LoadCredential(OpenAiKey),
                    OpenAiDeploymentName = LoadCredential(OpenAiDeployment)
                };

                bool isValid = config.IsValid();
                if (isValid)
                {
                    _logger.LogInformation("Azure credentials loaded successfully");
                }
                else
                {
                    _logger.LogWarning("Azure credentials loaded but incomplete or invalid");
                }

                return Task.FromResult<AzureConfig?>(config);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load Azure credentials: {ex.Message}");
                return Task.FromResult<AzureConfig?>(null);
            }
        }

        /// <inheritdoc/>
        public Task<bool> AreAzureCredentialsConfiguredAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                _logger.LogDebug("Checking if Azure credentials are configured");

                var hasEndpoint = !string.IsNullOrEmpty(LoadCredential(DocIntelEndpoint));
                var hasKey = !string.IsNullOrEmpty(LoadCredential(DocIntelKey));

                // Only require Document Intelligence credentials for basic configuration
                // OpenAI credentials are optional (for auto-tagging feature)
                bool isConfigured = hasEndpoint && hasKey;

                if (isConfigured)
                {
                    _logger.LogInformation("Azure Document Intelligence credentials are configured");
                }
                else
                {
                    _logger.LogWarning("Azure Document Intelligence credentials are not configured");
                }

                return Task.FromResult(isConfigured);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error checking Azure credentials configuration: {ex.Message}", ex);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public Task ClearAzureCredentialsAsync()
        {
            try
            {
                _logger.LogInformation("Clearing Azure credentials from Windows Credential Manager");

                DeleteCredential(DocIntelEndpoint);
                DeleteCredential(DocIntelKey);
                DeleteCredential(OpenAiEndpoint);
                DeleteCredential(OpenAiKey);
                DeleteCredential(OpenAiDeployment);

                _logger.LogInformation("Azure credentials cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error clearing Azure credentials: {ex.Message}", ex);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> SaveDirectoryConfigAsync(DirectoryConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            try
            {
                _logger.LogInformation("Saving directory configuration to Windows Credential Manager");
                _logger.LogDebug($"Input directory: {config.DefaultInputDirectory ?? "(none)"}");
                _logger.LogDebug($"Output directory: {config.DefaultOutputDirectory}");

                SaveCredential(DefaultInputDir, config.DefaultInputDirectory ?? string.Empty);
                SaveCredential(DefaultOutputDir, config.DefaultOutputDirectory ?? string.Empty);

                _logger.LogInformation("Directory configuration saved successfully");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save directory configuration: {ex.Message}", ex);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public Task<DirectoryConfig?> LoadDirectoryConfigAsync()
        {
            try
            {
                _logger.LogDebug("Loading directory configuration from Windows Credential Manager");

                var config = new DirectoryConfig
                {
                    DefaultInputDirectory = LoadCredential(DefaultInputDir),
                    DefaultOutputDirectory = LoadCredential(DefaultOutputDir)
                };

                if (!string.IsNullOrWhiteSpace(config.DefaultOutputDirectory))
                {
                    _logger.LogInformation("Directory configuration loaded successfully");
                    _logger.LogDebug($"Input directory: {config.DefaultInputDirectory ?? "(none)"}");
                    _logger.LogDebug($"Output directory: {config.DefaultOutputDirectory}");
                }
                else
                {
                    _logger.LogWarning("Directory configuration loaded but output directory is not set");
                }

                return Task.FromResult<DirectoryConfig?>(config);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load directory configuration: {ex.Message}", ex);
                return Task.FromResult<DirectoryConfig?>(null);
            }
        }

        /// <inheritdoc/>
        public Task ClearDirectoryConfigAsync()
        {
            try
            {
                _logger.LogInformation("Clearing directory configuration from Windows Credential Manager");

                DeleteCredential(DefaultInputDir);
                DeleteCredential(DefaultOutputDir);

                _logger.LogInformation("Directory configuration cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error clearing directory configuration: {ex.Message}", ex);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> SaveFeatureConfigAsync(FeatureConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            try
            {
                _logger.LogInformation("Saving feature configuration to Windows Credential Manager");
                _logger.LogDebug($"Enable Text Dump: {config.EnableTextDump}");
                _logger.LogDebug($"Enable Auto Tag: {config.EnableAutoTag}");

                SaveCredential(FeatureEnableTextDump, config.EnableTextDump.ToString());
                SaveCredential(FeatureEnableAutoTag, config.EnableAutoTag.ToString());

                _logger.LogInformation("Feature configuration saved successfully");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save feature configuration: {ex.Message}", ex);
                return Task.FromResult(false);
            }
        }

        /// <inheritdoc/>
        public Task<FeatureConfig?> LoadFeatureConfigAsync()
        {
            try
            {
                _logger.LogDebug("Loading feature configuration from Windows Credential Manager");

                var enableTextDumpStr = LoadCredential(FeatureEnableTextDump);
                var enableAutoTagStr = LoadCredential(FeatureEnableAutoTag);

                var config = new FeatureConfig
                {
                    EnableTextDump = bool.TryParse(enableTextDumpStr, out bool enableTextDump) && enableTextDump,
                    EnableAutoTag = bool.TryParse(enableAutoTagStr, out bool enableAutoTag) && enableAutoTag
                };

                _logger.LogInformation("Feature configuration loaded successfully");
                _logger.LogDebug($"Enable Text Dump: {config.EnableTextDump}");
                _logger.LogDebug($"Enable Auto Tag: {config.EnableAutoTag}");

                return Task.FromResult<FeatureConfig?>(config);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load feature configuration: {ex.Message}", ex);
                return Task.FromResult<FeatureConfig?>(null);
            }
        }

        /// <inheritdoc/>
        public Task ClearFeatureConfigAsync()
        {
            try
            {
                _logger.LogInformation("Clearing feature configuration from Windows Credential Manager");

                DeleteCredential(FeatureEnableTextDump);
                DeleteCredential(FeatureEnableAutoTag);

                _logger.LogInformation("Feature configuration cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error clearing feature configuration: {ex.Message}", ex);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task<bool> SaveAllSettingsAsync(AzureConfig azureConfig, DirectoryConfig directoryConfig, FeatureConfig featureConfig)
        {
            _logger.LogInformation("Saving all settings (Azure credentials, directory configuration, and features)");

            var azureResult = await SaveAzureCredentialsAsync(azureConfig);
            var directoryResult = await SaveDirectoryConfigAsync(directoryConfig);
            var featureResult = await SaveFeatureConfigAsync(featureConfig);

            bool success = azureResult && directoryResult && featureResult;

            if (success)
            {
                _logger.LogInformation("All settings saved successfully");
            }
            else
            {
                _logger.LogWarning($"Settings save completed with issues - Azure: {azureResult}, Directory: {directoryResult}, Features: {featureResult}");
            }

            return success;
        }

        /// <inheritdoc/>
        public async Task<(AzureConfig? azureConfig, DirectoryConfig? directoryConfig, FeatureConfig? featureConfig)> LoadAllSettingsAsync()
        {
            _logger.LogDebug("Loading all settings (Azure credentials, directory configuration, and features)");

            var azureConfig = await LoadAzureCredentialsAsync();
            var directoryConfig = await LoadDirectoryConfigAsync();
            var featureConfig = await LoadFeatureConfigAsync();

            bool azureLoaded = azureConfig != null && azureConfig.IsValid();
            bool directoryLoaded = directoryConfig != null && directoryConfig.IsValid();
            bool featureLoaded = featureConfig != null;

            if (azureLoaded && directoryLoaded && featureLoaded)
            {
                _logger.LogInformation("All settings loaded successfully");
            }
            else
            {
                _logger.LogWarning($"Settings load completed - Azure valid: {azureLoaded}, Directory valid: {directoryLoaded}, Features loaded: {featureLoaded}");
            }

            return (azureConfig, directoryConfig, featureConfig);
        }

        /// <inheritdoc/>
        public async Task ClearAllSettingsAsync()
        {
            _logger.LogInformation("Clearing all settings (Azure credentials, directory configuration, and features)");

            await ClearAzureCredentialsAsync();
            await ClearDirectoryConfigAsync();
            await ClearFeatureConfigAsync();

            _logger.LogInformation("All settings cleared successfully");
        }

        #region Private Helper Methods

        /// <summary>
        /// Saves a single credential to Windows Credential Manager
        /// </summary>
        private void SaveCredential(string key, string value)
        {
            try
            {
                // Determine persistence based on whether it's a secret (Azure) or config (Directory/Features)
                // For simplicity and backward compatibility, we'll use LocalMachine for everything as before
                // Note: The original library used 'PersistanceType.LocalComputer' which maps to 'CredentialPersistence.LocalMachine'
                
                CredentialManager.WriteCredential(
                    applicationName: TargetPrefix + key,
                    userName: "OCRBot",
                    secret: value ?? string.Empty,
                    persistence: CredentialPersistence.LocalMachine);

                _logger.LogDebug($"Credential saved: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to save credential '{key}': {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Loads a single credential from Windows Credential Manager
        /// </summary>
        private string LoadCredential(string key)
        {
            try
            {
                var credential = CredentialManager.ReadCredential(applicationName: TargetPrefix + key);

                if (credential != null)
                {
                    _logger.LogDebug($"Credential loaded: {key}");
                    return credential.Password ?? string.Empty;
                }
                else
                {
                    _logger.LogDebug($"Credential not found: {key}");
                    return string.Empty;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load credential '{key}': {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// Deletes a single credential from Windows Credential Manager
        /// </summary>
        private void DeleteCredential(string key)
        {
            try
            {
                CredentialManager.DeleteCredential(applicationName: TargetPrefix + key);
                _logger.LogInformation($"Credential deleted successfully (or did not exist): {key}");
            }
            catch (Exception ex)
            {
                // Log the error but don't throw
                _logger.LogWarning($"Error deleting credential '{key}': {ex.Message}");
            }
        }

        #endregion
    }
}


