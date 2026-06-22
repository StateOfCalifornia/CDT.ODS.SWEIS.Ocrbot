
namespace CDT.OCRBot.Domain.Configuration
{
    /// <summary>
    /// Configuration for Azure services (Document Intelligence and OpenAI)
    /// </summary>
    public class AzureConfig
    {
        /// <summary>
        /// Azure Document Intelligence endpoint URL
        /// </summary>
        /// <example>https://your-resource.cognitiveservices.azure.com/</example>
        public string DocumentIntelligenceEndpoint { get; init; } = string.Empty;

        /// <summary>
        /// Azure Document Intelligence API key
        /// </summary>
        public string DocumentIntelligenceApiKey { get; init; } = string.Empty;

        /// <summary>
        /// Azure OpenAI endpoint URL
        /// </summary>
        /// <example>https://your-openai.openai.azure.com/</example>
        public string OpenAiEndpoint { get; init; } = string.Empty;

        /// <summary>
        /// Azure OpenAI API key
        /// </summary>
        public string OpenAiApiKey { get; init; } = string.Empty;

        /// <summary>
        /// Azure OpenAI deployment name
        /// </summary>
        /// <example>gpt-4</example>
        public string OpenAiDeploymentName { get; init; } = string.Empty;

        /// <summary>
        /// Validates Document Intelligence credentials (always required for OCR)
        /// </summary>
        public bool HasValidDocumentIntelligence()
        {
            return !string.IsNullOrWhiteSpace(DocumentIntelligenceEndpoint) &&
                   !string.IsNullOrWhiteSpace(DocumentIntelligenceApiKey);
        }

        /// <summary>
        /// Validates OpenAI credentials (only required when auto-tagging is enabled)
        /// </summary>
        public bool HasValidOpenAI()
        {
            return !string.IsNullOrWhiteSpace(OpenAiEndpoint) &&
                   !string.IsNullOrWhiteSpace(OpenAiApiKey) &&
                   !string.IsNullOrWhiteSpace(OpenAiDeploymentName);
        }

        /// <summary>
        /// Validates that all Azure credentials are configured (backward compatibility)
        /// </summary>
        /// <returns>True if all credentials are valid</returns>
        public bool IsValid()
        {
            return HasValidDocumentIntelligence() && HasValidOpenAI();
        }

        /// <summary>
        /// Returns a list of validation errors for missing or invalid fields
        /// </summary>
        /// <param name="requireOpenAiConfig">Whether OpenAI fields are required (based on EnableAutoTag setting)</param>
        /// <returns>List of error messages, empty if valid</returns>
        public List<string> GetValidationErrors(bool requireOpenAiConfig = true)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(DocumentIntelligenceEndpoint))
                errors.Add("Document Intelligence Endpoint is required");

            if (string.IsNullOrWhiteSpace(DocumentIntelligenceApiKey))
                errors.Add("Document Intelligence API Key is required");

            // Only validate OpenAI fields if Auto-Tagging is enabled
            if (requireOpenAiConfig)
            {
                if (string.IsNullOrWhiteSpace(OpenAiEndpoint))
                    errors.Add("OpenAI Endpoint is required");

                if (string.IsNullOrWhiteSpace(OpenAiApiKey))
                    errors.Add("OpenAI API Key is required");

                if (string.IsNullOrWhiteSpace(OpenAiDeploymentName))
                    errors.Add("OpenAI Deployment Name is required");
            }

            return errors;
        }
    }
}



