
using CDT.OCRBot.Domain.Configuration;

using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for managing Azure credentials and application configuration
    /// Implementations should use secure storage (e.g., Windows Credential Manager)
    /// </summary>
    public interface ICredentialRepository
    {
        /// <summary>
        /// Saves Azure service credentials securely
        /// </summary>
        /// <param name="config">Azure configuration to save</param>
        /// <returns>True if saved successfully, false otherwise</returns>
        Task<bool> SaveAzureCredentialsAsync(AzureConfig config);

        /// <summary>
        /// Loads Azure service credentials from secure storage
        /// </summary>
        /// <returns>Azure configuration if found, null otherwise</returns>
        Task<AzureConfig?> LoadAzureCredentialsAsync();

        /// <summary>
        /// Checks if Azure credentials are configured
        /// </summary>
        /// <param name="cancellationToken">Optional cancellation token</param>
        /// <returns>True if credentials exist and are complete, false otherwise</returns>
        Task<bool> AreAzureCredentialsConfiguredAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Clears all stored Azure credentials
        /// </summary>
        Task ClearAzureCredentialsAsync();

        /// <summary>
        /// Saves default directory configuration
        /// </summary>
        /// <param name="config">Directory configuration to save</param>
        /// <returns>True if saved successfully, false otherwise</returns>
        Task<bool> SaveDirectoryConfigAsync(DirectoryConfig config);

        /// <summary>
        /// Loads default directory configuration
        /// </summary>
        /// <returns>Directory configuration if found, null otherwise</returns>
        Task<DirectoryConfig?> LoadDirectoryConfigAsync();

        /// <summary>
        /// Clears stored directory configuration
        /// </summary>
        Task ClearDirectoryConfigAsync();

        /// <summary>
        /// Saves feature configuration
        /// </summary>
        /// <param name="config">Feature configuration to save</param>
        /// <returns>True if saved successfully, false otherwise</returns>
        Task<bool> SaveFeatureConfigAsync(FeatureConfig config);

        /// <summary>
        /// Loads feature configuration
        /// </summary>
        /// <returns>Feature configuration if found, null otherwise</returns>
        Task<FeatureConfig?> LoadFeatureConfigAsync();

        /// <summary>
        /// Clears stored feature configuration
        /// </summary>
        Task ClearFeatureConfigAsync();

        /// <summary>
        /// Saves all settings (Azure credentials, directory configuration, and features) in one operation
        /// </summary>
        /// <param name="azureConfig">Azure configuration</param>
        /// <param name="directoryConfig">Directory configuration</param>
        /// <param name="featureConfig">Feature configuration</param>
        /// <returns>True if all saved successfully, false otherwise</returns>
        Task<bool> SaveAllSettingsAsync(AzureConfig azureConfig, DirectoryConfig directoryConfig, FeatureConfig featureConfig);

        /// <summary>
        /// Loads all settings (Azure credentials, directory configuration, and features) in one operation
        /// </summary>
        /// <returns>Tuple containing all configurations (null if not found)</returns>
        Task<(AzureConfig? azureConfig, DirectoryConfig? directoryConfig, FeatureConfig? featureConfig)> LoadAllSettingsAsync();

        /// <summary>
        /// Clears all stored settings (credentials and directory config)
        /// </summary>
        Task ClearAllSettingsAsync();
    }
}






