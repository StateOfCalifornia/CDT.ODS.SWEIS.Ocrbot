
using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Domain.Interfaces
{

    public interface IOCRService
    {
        /// <summary>
        /// Analyzes a PDF document using Azure Document Intelligence OCR
        /// </summary>
        /// <param name="pdfBytes">PDF file bytes</param>
        /// <param name="progress">Optional progress reporter</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of page data with extracted text and layout information</returns>
        Task<List<PageData>> AnalyzeDocumentAsync(
            byte[] pdfBytes,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tests the connection to Azure Document Intelligence service
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>True if connection is successful</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    }
}




