using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Infrastructure.Pdf.Strategies
{
    /// <summary>
    /// Strategy for creating tagged text-only PDF
    /// Creates text-only PDF with accessibility tags
    /// </summary>
    public class TaggedTextOnlyPdfStrategy : IPdfProcessingStrategy
    {
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly ITaggingService _taggingService;
        private readonly IAppLogger _logger;

        public TaggedTextOnlyPdfStrategy(
            IPdfGenerationService pdfGenerationService,
            ITaggingService taggingService,
            IAppLogger logger)
        {
            _pdfGenerationService = pdfGenerationService ?? throw new ArgumentNullException(nameof(pdfGenerationService));
            _taggingService = taggingService;
            _logger = logger;
        }

        public async Task<byte[]> ProcessAsync(
            List<PageData> pages,
            byte[] originalPdfBytes,
            string fileName,
            string outputFilePath,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            // First create text-only PDF
            progress?.Report(ProcessingProgress.CreatingPdf(0, pages.Count, "Creating text-only PDF..."));
            var textOnlyPdf = _pdfGenerationService.CreateTextOnlyPdf(pages);

            // Then get tagging information
            progress?.Report(ProcessingProgress.AutoTagging(0, 1, "Starting auto-tagging..."));
            var taggingDict = await _taggingService.GetTaggingInfoAsync(
                originalPdfBytes,
                pages,
                fileName,
                progress,
                cancellationToken);

            // Create tagged PDF
            progress?.Report(ProcessingProgress.CreatingPdf(0, pages.Count, "Creating tagged PDF..."));
            var processedContent = _pdfGenerationService.CreateTaggedPdf(pages, originalPdfBytes, taggingDict);
            progress?.Report(ProcessingProgress.CreatingPdf(pages.Count, pages.Count, "PDF created"));

            _logger.LogDebug("Created tagged text-only PDF");
            return processedContent;
        }
    }
}
