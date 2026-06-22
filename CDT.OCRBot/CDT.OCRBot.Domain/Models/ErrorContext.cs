namespace CDT.OCRBot.Domain.Models
{
    /// <summary>
    /// Provides context information for exception classification and error reporting
    /// </summary>
    public class ErrorContext
    {
        /// <summary>
        /// The operation being performed when the error occurred
        /// </summary>
        public string Operation { get; init; } = string.Empty;

        /// <summary>
        /// The name of the file being processed (if applicable)
        /// </summary>
        public string? FileName { get; init; }

        /// <summary>
        /// The full path to the file being processed (if applicable)
        /// </summary>
        public string? FilePath { get; init; }

        /// <summary>
        /// The size of the file in bytes (if applicable)
        /// </summary>
        public long? FileSizeBytes { get; init; }

        /// <summary>
        /// The number of pages in the document (if applicable)
        /// </summary>
        public int? PageCount { get; init; }

        /// <summary>
        /// When the error occurred
        /// </summary>
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Additional provider-specific or operation-specific data
        /// </summary>
        public Dictionary<string, object> AdditionalData { get; init; } = new();

        /// <summary>
        /// Creates a formatted context string for error messages
        /// </summary>
        public string GetContextString()
        {
            if (!string.IsNullOrEmpty(FileName))
            {
                return $" for file '{FileName}'";
            }
            return string.Empty;
        }
    }
}
