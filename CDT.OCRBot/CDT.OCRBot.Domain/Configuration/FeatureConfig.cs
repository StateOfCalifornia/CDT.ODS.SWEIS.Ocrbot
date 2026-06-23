namespace CDT.OCRBot.Domain.Configuration
{
    /// <summary>
    /// Configuration for optional application features
    /// </summary>
    public class FeatureConfig
    {
        /// <summary>
        /// Enable text extraction export to .txt files
        /// </summary>
        public bool EnableTextDump { get; init; } = false;

        /// <summary>
        /// Enable auto-tagging feature using Azure OpenAI
        /// </summary>
        public bool EnableAutoTag { get; init; } = false;
    }
}



