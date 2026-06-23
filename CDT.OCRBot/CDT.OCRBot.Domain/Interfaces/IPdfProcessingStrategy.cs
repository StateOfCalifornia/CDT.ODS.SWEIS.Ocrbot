
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Domain.Interfaces
{
    /// <summary>
    /// Strategy interface for different PDF processing modes
    /// </summary>
    public interface IPdfProcessingStrategy
    {
        /// <summary>
        /// Processes PDF pages according to the strategy's implementation
        /// </summary>
        /// <param name="pages">OCR-extracted page data</param>
        /// <param name="originalPdfBytes">Original PDF file bytes</param>
        /// <param name="fileName">Original file name</param>
        /// <param name="outputFilePath">Output file path for additional files</param>
        /// <param name="progress">Progress reporter for granular updates</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Processed PDF content as byte array</returns>
        Task<byte[]> ProcessAsync(
            List<PageData> pages,
            byte[] originalPdfBytes,
            string fileName,
            string outputFilePath,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default);
    }
}






