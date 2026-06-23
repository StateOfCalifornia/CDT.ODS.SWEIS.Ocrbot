

using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Domain.Interfaces
{
    /// <summary>
    /// Main processor interface that orchestrates the complete PDF processing workflow
    /// Coordinates OCR, tagging, and PDF generation services
    /// </summary>
    public interface IPdfProcessor
    {
        /// <summary>
        /// Initializes all required services with stored credentials
        /// Must be called before processing operations
        /// </summary>
        /// <returns>True if initialization successful, false otherwise</returns>
        Task<bool> InitializeAsync();


        /// <summary>
        /// Processes a single PDF file with specified options
        /// </summary>
        /// <param name="inputFilePath">Path to input PDF file</param>
        /// <param name="outputFilePath">Path where processed PDF should be saved</param>
        /// <param name="options">Processing options (text-only, add tags)</param>
        /// <param name="progress">Optional progress reporter for granular status updates</param>
        /// <param name="cancellationToken">Cancellation token for async operation</param>
        /// <returns>ProcessingResult indicating success or failure with details</returns>
        /// <exception cref="ArgumentException">Thrown when file paths are invalid</exception>
        /// <exception cref="FileNotFoundException">Thrown when input file doesn't exist</exception>
        Task<ProcessingResult> ProcessPdfAsync(
            string inputFilePath,
            string outputFilePath,
            ProcessingOptions options,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default);


    }
}







