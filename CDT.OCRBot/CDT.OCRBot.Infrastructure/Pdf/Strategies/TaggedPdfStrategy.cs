using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Infrastructure.Pdf.Strategies
{
    /// <summary>
    /// Strategy for creating tagged searchable PDF
    /// Adds accessibility tags to searchable PDF
    /// </summary>
    public class TaggedPdfStrategy : IPdfProcessingStrategy
    {
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly ITaggingService _taggingService;
        private readonly IAppLogger _logger;

        public TaggedPdfStrategy(
            IPdfGenerationService pdfGenerationService,
            ITaggingService taggingService,
            IAppLogger logger)
        {
            _pdfGenerationService = pdfGenerationService;
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
            progress?.Report(ProcessingProgress.AutoTagging(0, 1, "Starting auto-tagging..."));
            var taggingDict = await _taggingService.GetTaggingInfoAsync(
                originalPdfBytes,
                pages,
                fileName,
                progress,
                cancellationToken);

            progress?.Report(ProcessingProgress.CreatingPdf(0, pages.Count, "Generating tagged PDF..."));
            var processedContent = _pdfGenerationService.CreateTaggedPdf(pages, originalPdfBytes, taggingDict);

            _logger.LogDebug("Created tagged PDF");
            return processedContent;
        }
    }
}
