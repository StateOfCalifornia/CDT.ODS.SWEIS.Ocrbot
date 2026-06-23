using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Infrastructure.Pdf.Strategies
{
    /// <summary>
    /// Strategy for creating text-only PDF
    /// Creates a new PDF with only the extracted text (no images)
    /// </summary>
    public class TextOnlyPdfStrategy : IPdfProcessingStrategy
    {
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly IAppLogger _logger;

        public TextOnlyPdfStrategy(IPdfGenerationService pdfGenerationService, IAppLogger logger)
        {
            _pdfGenerationService = pdfGenerationService ?? throw new ArgumentNullException(nameof(pdfGenerationService));
            _logger = logger;
        }

        public Task<byte[]> ProcessAsync(
            List<PageData> pages,
            byte[] originalPdfBytes,
            string fileName,
            string outputFilePath,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            progress?.Report(ProcessingProgress.CreatingPdf(0, pages.Count, "Creating text-only PDF..."));
            var processedContent = _pdfGenerationService.CreateTextOnlyPdf(pages);
            progress?.Report(ProcessingProgress.CreatingPdf(pages.Count, pages.Count, "PDF created"));

            _logger.LogDebug("Created text-only PDF");
            return Task.FromResult(processedContent);
        }
    }
}
