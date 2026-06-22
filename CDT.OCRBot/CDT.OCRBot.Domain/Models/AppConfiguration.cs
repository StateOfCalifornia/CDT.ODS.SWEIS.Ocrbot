namespace CDT.OCRBot.Domain.Models
{
    /// <summary>
    /// Application configuration aggregate containing directory and feature settings
    /// </summary>
    public record AppConfiguration
    {
        /// <summary>
        /// Directory configuration for input/output paths
        /// </summary>
        public required Configuration.DirectoryConfig DirectoryConfig { get; init; }

        /// <summary>
        /// Feature flags and toggles
        /// </summary>
        public required Configuration.FeatureConfig FeatureConfig { get; init; }
    }
}
