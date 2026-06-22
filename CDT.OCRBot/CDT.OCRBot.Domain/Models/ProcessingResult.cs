namespace CDT.OCRBot.Domain.Models
{
    /// <summary>
    /// Result of a PDF processing operation
    /// </summary>
    public class ProcessingResult
    {
        /// <summary>
        /// Indicates whether the processing was successful
        /// </summary>
        public bool IsSuccess { get; init; }

        /// <summary>
        /// Message describing the result (success or failure details)
        /// </summary>
        public string Message { get; init; } = string.Empty;

        /// <summary>
        /// Full path to the output file (if successful)
        /// </summary>
        public string OutputFilePath { get; init; } = string.Empty;

        /// <summary>
        /// Total processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; init; }

        /// <summary>
        /// Number of pages processed (0 if failed before page extraction)
        /// </summary>
        public int PageCount { get; init; }

        /// <summary>
        /// Size of the output file in bytes (0 if failed)
        /// </summary>
        public long OutputFileSizeBytes { get; init; }

        /// <summary>
        /// Exception that occurred during processing (if failed)
        /// </summary>
        public Exception? Error { get; init; }

        /// <summary>
        /// Creates a successful processing result
        /// </summary>
        /// <param name="outputPath">Path to the generated PDF file</param>
        /// <param name="processingTimeMs">Time taken to process</param>
        /// <param name="pageCount">Number of pages processed</param>
        /// <param name="outputFileSizeBytes">Size of output file in bytes</param>
        /// <param name="message">Optional success message</param>
        /// <returns>Success result</returns>
        public static ProcessingResult Success(string outputPath, long processingTimeMs, int pageCount = 0, long outputFileSizeBytes = 0, string? message = null)
        {
            return new ProcessingResult
            {
                IsSuccess = true,
                Message = message ?? "PDF processed successfully",
                OutputFilePath = outputPath,
                ProcessingTimeMs = processingTimeMs,
                PageCount = pageCount,
                OutputFileSizeBytes = outputFileSizeBytes
            };
        }

        /// <summary>
        /// Creates a failed processing result
        /// </summary>
        /// <param name="message">Error message</param>
        /// <param name="error">Exception that caused the failure</param>
        /// <param name="processingTimeMs">Time spent before failure</param>
        /// <returns>Failure result</returns>
        public static ProcessingResult Failure(string message, Exception? error = null, long processingTimeMs = 0)
        {
            return new ProcessingResult
            {
                IsSuccess = false,
                Message = message,
                Error = error,
                ProcessingTimeMs = processingTimeMs
            };
        }
    }
}



