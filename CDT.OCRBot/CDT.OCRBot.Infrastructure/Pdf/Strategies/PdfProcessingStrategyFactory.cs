using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Infrastructure.Pdf.Strategies
{
    /// <summary>
    /// Factory for creating PDF processing strategies based on processing options
    /// </summary>
    public class PdfProcessingStrategyFactory
    {
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly ITaggingService? _taggingService;
        private readonly IAppLogger _logger;

        public PdfProcessingStrategyFactory(
            IPdfGenerationService pdfGenerationService,
            ITaggingService? taggingService,
            IAppLogger logger)
        {
            _pdfGenerationService = pdfGenerationService ?? throw new ArgumentNullException(nameof(pdfGenerationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _taggingService = taggingService;
        }

        /// <summary>
        /// Creates the appropriate processing strategy based on options
        /// </summary>
        /// <param name="options">Processing options</param>
        /// <returns>Processing strategy instance</returns>
        public IPdfProcessingStrategy CreateStrategy(ProcessingOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            // Determine strategy based on options
            if (options.AddUATags && options.TextOnly)
            {
                // Mode: Tagged Text-Only PDF
                if (_taggingService == null)
                    throw new InvalidOperationException("Tagging service is required for tagged PDF modes");

                return new TaggedTextOnlyPdfStrategy(_pdfGenerationService, _taggingService, _logger);
            }
            else if (options.AddUATags)
            {
                // Mode: Tagged Searchable PDF
                if (_taggingService == null)
                    throw new InvalidOperationException("Tagging service is required for tagged PDF modes");

                return new TaggedPdfStrategy(_pdfGenerationService, _taggingService, _logger);
            }
            else if (options.TextOnly)
            {
                // Mode: Text-Only PDF
                return new TextOnlyPdfStrategy(_pdfGenerationService, _logger);
            }
            else
            {
                // Mode: Searchable PDF (default)
                return new SearchablePdfStrategy(_pdfGenerationService, _logger);
            }
        }
    }
}



