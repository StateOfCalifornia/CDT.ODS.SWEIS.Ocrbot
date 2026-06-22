using System.IO;

namespace CDT.OCRBot.Domain.Configuration
{
    /// <summary>
    /// Configuration for default input and output directories
    /// </summary>
    public class DirectoryConfig
    {
        /// <summary>
        /// Default directory for browsing input PDF files (required)
        /// </summary>
        public string DefaultInputDirectory { get; init; } = string.Empty;

        /// <summary>
        /// Default directory for saving processed PDF files (required)
        /// </summary>
        public string DefaultOutputDirectory { get; init; } = string.Empty;

        /// <summary>
        /// Validates that required directory configuration is present
        /// </summary>
        /// <returns>True if both directories are specified and different, false otherwise</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(DefaultInputDirectory) ||
                string.IsNullOrWhiteSpace(DefaultOutputDirectory))
            {
                return false;
            }

            // Ensure directories are not the same (case-insensitive, normalized path comparison)
            var normalizedInput = Path.GetFullPath(DefaultInputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var normalizedOutput = Path.GetFullPath(DefaultOutputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return !normalizedInput.Equals(normalizedOutput, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a list of validation errors
        /// </summary>
        /// <returns>List of error messages, empty if valid</returns>
        public List<string> GetValidationErrors()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(DefaultInputDirectory))
                errors.Add("• Default Input Directory is required");

            if (string.IsNullOrWhiteSpace(DefaultOutputDirectory))
                errors.Add("• Default Output Directory is required");

            // Check if directories are the same (only if both are provided)
            if (!string.IsNullOrWhiteSpace(DefaultInputDirectory) &&
                !string.IsNullOrWhiteSpace(DefaultOutputDirectory))
            {
                try
                {
                    var normalizedInput = Path.GetFullPath(DefaultInputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    var normalizedOutput = Path.GetFullPath(DefaultOutputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                    if (normalizedInput.Equals(normalizedOutput, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add("• Input and Output directories cannot be the same");
                    }
                }
                catch
                {
                    // If path normalization fails, skip this validation
                    // (invalid paths will be caught by other validators)
                }
            }

            return errors;
        }
    }
}



