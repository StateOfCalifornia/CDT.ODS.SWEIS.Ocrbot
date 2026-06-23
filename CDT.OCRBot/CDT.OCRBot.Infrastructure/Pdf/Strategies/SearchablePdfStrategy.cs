using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Infrastructure.Pdf.Strategies
{
    /// <summary>
    /// Strategy for creating searchable PDF (default mode)
    /// Overlays OCR text on original PDF images
    /// </summary>
    public class SearchablePdfStrategy : IPdfProcessingStrategy
    {
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly IAppLogger _logger;

        public SearchablePdfStrategy(IPdfGenerationService pdfGenerationService, IAppLogger logger)
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
            progress?.Report(ProcessingProgress.CreatingPdf(0, pages.Count, "Creating searchable PDF..."));
            var processedContent = _pdfGenerationService.CreateSearchablePdf(pages, originalPdfBytes);
            progress?.Report(ProcessingProgress.CreatingPdf(pages.Count, pages.Count, "PDF created"));

            _logger.LogDebug("Created searchable PDF");
            return Task.FromResult(processedContent);
        }
    }
}
