

using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Domain.Interfaces
{
    /// <summary>
    /// Service interface for semantic tagging using Azure OpenAI
    /// Generates PDF/UA accessibility tags based on OCR data
    /// </summary>
    public interface ITaggingService
    {
        /// <summary>
        /// Generates semantic tags for PDF content using AI analysis
        /// </summary>
        /// <param name="originalPdfBytes">Original PDF file bytes (for context)</param>
        /// <param name="pages">OCR-extracted page data</param>
        /// <param name="fileName">Name of the file being processed (for logging)</param>
        /// <param name="progress">Optional progress reporter for batch-level updates</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>Dictionary containing tags array and imageDimensions array</returns>
        Task<Dictionary<string, object>> GetTaggingInfoAsync(
            byte[] originalPdfBytes,
            List<PageData> pages,
            string fileName,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the connection to Azure OpenAI service
        /// </summary>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    }
}





