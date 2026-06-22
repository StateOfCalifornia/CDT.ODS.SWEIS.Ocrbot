using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;
using CDT.OCRBot.Infrastructure.Azure;
using CDT.OCRBot.Infrastructure.Common;
using System.Diagnostics;

namespace CDT.OCRBot.Infrastructure.Pdf
{
    /// <summary>
    /// Main processor that orchestrates the complete PDF processing workflow
    /// Coordinates credential management, OCR, tagging, and PDF generation services
    /// </summary>
    public class PdfProcessor : IPdfProcessor
    {
        private readonly ICredentialRepository _credentialRepository;
        private IOCRService? _ocrService;
        private ITaggingService? _taggingService;
        private readonly IPdfGenerationService _pdfGenerationService;
        private readonly IAppLogger _logger;
        private readonly PdfErrorHandlingService _errorHandlingService;

        /// <summary>
        /// Initializes a new instance of PdfProcessor
        /// </summary>
        /// <param name="credentialRepository">Repository for loading credentials</param>
        /// <param name="pdfGenerationService">Service for PDF generation</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="errorHandlingService">Service for handling PDF processing errors</param>
        public PdfProcessor(ICredentialRepository credentialRepository, IPdfGenerationService pdfGenerationService, IAppLogger logger, PdfErrorHandlingService errorHandlingService)
        {
            _credentialRepository = credentialRepository ?? throw new ArgumentNullException(nameof(credentialRepository));
            _pdfGenerationService = pdfGenerationService ?? throw new ArgumentNullException(nameof(pdfGenerationService));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));

            _logger.LogDebug("PdfProcessor initialized successfully.");
        }

        /// <summary>
        /// Constructor for dependency injection with pre-initialized services
        /// </summary>
        public PdfProcessor(ICredentialRepository credentialRepository, IOCRService ocrService, ITaggingService taggingService,
                           IPdfGenerationService pdfGenerationService, IAppLogger logger, PdfErrorHandlingService errorHandlingService)
        {
            _credentialRepository = credentialRepository ?? throw new ArgumentNullException(nameof(credentialRepository));
            _ocrService = ocrService ?? throw new ArgumentNullException(nameof(ocrService));
            _taggingService = taggingService ?? throw new ArgumentNullException(nameof(taggingService));
            _pdfGenerationService = pdfGenerationService ?? throw new ArgumentNullException(nameof(pdfGenerationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorHandlingService = errorHandlingService ?? throw new ArgumentNullException(nameof(errorHandlingService));

            System.Diagnostics.Debug.WriteLine("PdfProcessor created with pre-initialized services");
        }

        /// <inheritdoc/>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Initializing PdfProcessor services...");

                // Load Azure credentials
                var azureConfig = await _credentialRepository.LoadAzureCredentialsAsync();

                // Validate Document Intelligence credentials (required)
                if (azureConfig == null || !azureConfig.HasValidDocumentIntelligence())
                {
                    _logger.LogError("Document Intelligence credentials not configured or invalid");
                    System.Diagnostics.Debug.WriteLine("Document Intelligence credentials not configured or invalid");
                    return false;
                }

                // Initialize OCR service (required)
                _ocrService = new OCRService(
                    azureConfig.DocumentIntelligenceEndpoint,
                    azureConfig.DocumentIntelligenceApiKey,
                    _logger);

                _logger.LogDebug("OCR service initialized");

                // Initialize Tagging service only if OpenAI credentials are provided (optional)
                if (azureConfig.HasValidOpenAI())
                {
                    _taggingService = new TaggingService(
                        azureConfig.OpenAiEndpoint,
                        azureConfig.OpenAiApiKey,
                        azureConfig.OpenAiDeploymentName,
                        _logger);

                    _logger.LogDebug("Tagging service initialized");
                }
                else
                {
                    _taggingService = null;
                    _logger.LogDebug("Tagging service skipped (OpenAI credentials not configured)");
                    System.Diagnostics.Debug.WriteLine("Tagging service not initialized - OpenAI credentials not provided");
                }

                _logger.LogDebug("PdfProcessor initialization completed successfully");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize PdfProcessor: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<ProcessingResult> ProcessPdfAsync(
            string inputFilePath,
            string outputFilePath,
            ProcessingOptions options,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(inputFilePath))
                throw new ArgumentException("Input file path cannot be null or empty", nameof(inputFilePath));

            if (string.IsNullOrWhiteSpace(outputFilePath))
                throw new ArgumentException("Output file path cannot be null or empty", nameof(outputFilePath));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate input file
                if (!Utils.IsValidPdfFile(inputFilePath))
                {
                    return ProcessingResult.Failure(
                        "Input file not found or is not a valid PDF file",
                        processingTimeMs: stopwatch.ElapsedMilliseconds);
                }

                // Initialize services if needed (only OCR is required)
                if (_ocrService == null)
                {
                    progress?.Report(ProcessingProgress.Initializing(0));

                    if (!await InitializeAsync())
                    {
                        return ProcessingResult.Failure(
                            "Failed to initialize Azure services. Please check your credentials.",
                            processingTimeMs: stopwatch.ElapsedMilliseconds);
                    }

                    progress?.Report(ProcessingProgress.Initializing(100));
                }

                // Validate tagging service requirement
                if (options.RequiresTaggingService && _taggingService == null)
                {
                    return ProcessingResult.Failure(
                        "Auto-tagging feature is enabled but OpenAI credentials are not configured. " +
                        "Please configure OpenAI credentials in Settings or disable auto-tagging.",
                        processingTimeMs: stopwatch.ElapsedMilliseconds);
                }

                System.Diagnostics.Debug.WriteLine(
                    $"Starting PDF processing: {inputFilePath} -> {outputFilePath}");
                System.Diagnostics.Debug.WriteLine($"Processing mode: {options.GetDescription()}");

                // Step 1: Read original PDF
                progress?.Report(ProcessingProgress.ReadingPdf(0));
                byte[] originalPdfBytes = await Utils.ReadAllBytesAsync(inputFilePath, cancellationToken);
                progress?.Report(ProcessingProgress.ReadingPdf(100));

                System.Diagnostics.Debug.WriteLine($"Read {originalPdfBytes.Length} bytes from input file");

                // Step 2: OCR Analysis - progress is reported by OCRService
                progress?.Report(ProcessingProgress.AnalyzingOcr(0, 1, "Starting OCR analysis..."));
                List<PageData> pages;

                try
                {
                    pages = await _ocrService!.AnalyzeDocumentAsync(originalPdfBytes, progress, cancellationToken);

                    System.Diagnostics.Debug.WriteLine(
                        $"OCR completed: {pages.Count} pages, " +
                        $"{pages.Sum(p => p.Lines.Count)} lines, " +
                        $"{pages.Sum(p => p.Tables.Count)} tables");
                }
                catch (Exception ex)
                {
                    // Classify the error for user-friendly messaging
                    var fileName = Path.GetFileName(inputFilePath);
                    var errorInfo = _errorHandlingService.ClassifyException(ex, fileName);

                    // Return failure result - caller will log with full context
                    return ProcessingResult.Failure(
                        errorInfo.GetFullUserMessage(),
                        ex,
                        stopwatch.ElapsedMilliseconds);
                }

                if (pages == null || pages.Count == 0)
                {
                    return ProcessingResult.Failure(
                        "No pages extracted from PDF",
                        processingTimeMs: stopwatch.ElapsedMilliseconds);
                }

                // Step 3: Generate output PDF using strategy pattern
                // The strategy will report its own progress for tagging and PDF creation
                var strategyFactory = new Strategies.PdfProcessingStrategyFactory(_pdfGenerationService, _taggingService, _logger);
                var strategy = strategyFactory.CreateStrategy(options);

                byte[] processedContent = await strategy.ProcessAsync(
                    pages,
                    originalPdfBytes,
                    Path.GetFileName(inputFilePath),
                    outputFilePath,
                    progress,
                    cancellationToken);

                System.Diagnostics.Debug.WriteLine($"Created PDF using {strategy.GetType().Name}");

                // Dump OCR text if enabled (global - applies to all strategies)
                if (options.EnableTextDump)
                {
                    await DumpOcrTextAsync(pages, Path.GetFileName(inputFilePath), outputFilePath, cancellationToken);
                }

                // Step 4: Save output file
                progress?.Report(ProcessingProgress.Saving(0));

                await Utils.WriteAllBytesAsync(outputFilePath, processedContent, cancellationToken);

                progress?.Report(ProcessingProgress.Saving(100));

                stopwatch.Stop();

                System.Diagnostics.Debug.WriteLine(
                    $"Processing completed successfully in {stopwatch.ElapsedMilliseconds}ms");

                progress?.Report(ProcessingProgress.Complete());

                // Get output file size for audit logging
                var outputFileInfo = new FileInfo(outputFilePath);

                return ProcessingResult.Success(
                    outputFilePath,
                    stopwatch.ElapsedMilliseconds,
                    pages.Count,
                    outputFileInfo.Length,
                    "PDF processed successfully");
            }
            catch (OperationCanceledException)
            {
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine("Processing cancelled by user");

                return ProcessingResult.Failure(
                    "Processing was cancelled",
                    processingTimeMs: stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine($"Processing failed: {ex.Message}");

                return ProcessingResult.Failure(
                    $"Processing failed: {ex.Message}",
                    ex,
                    stopwatch.ElapsedMilliseconds);
            }
        }




        /// <summary>
        /// Dumps extracted OCR text to a text file alongside the processed PDF.
        /// This is a global operation that applies to all processing strategies.
        /// </summary>
        private async Task DumpOcrTextAsync(
            List<PageData> pages,
            string originalFileName,
            string outputFilePath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug($"Dumping OCR text for: {originalFileName}");

                var sb = new System.Text.StringBuilder();

                for (int i = 0; i < pages.Count; i++)
                {
                    var page = pages[i];
                    int pageNumber = i + 1;

                    sb.AppendLine($"========== Page {pageNumber} ==========");
                    sb.AppendLine();

                    if (page.Lines != null && page.Lines.Count > 0)
                    {
                        foreach (var line in page.Lines)
                        {
                            if (!string.IsNullOrWhiteSpace(line.Content))
                            {
                                sb.AppendLine(line.Content.Trim());
                            }
                        }
                    }

                    if (i < pages.Count - 1)
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                    }
                }

                string extractedText = sb.ToString();
                string directory = Path.GetDirectoryName(outputFilePath) ?? "";
                string baseFileName = Path.GetFileNameWithoutExtension(originalFileName);
                string textOutputPath = Path.Combine(directory, $"{baseFileName}.ocr.txt");
                textOutputPath = Common.Utils.GetUniqueFilePath(textOutputPath);

                await File.WriteAllTextAsync(textOutputPath, extractedText, System.Text.Encoding.UTF8, cancellationToken);
                _logger.LogInformation($"OCR text dumped to: {textOutputPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to dump OCR text: {ex.Message}", ex);
            }
        }

    }
}








