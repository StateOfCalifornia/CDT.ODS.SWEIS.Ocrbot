using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;
using CDT.OCRBot.Domain.Configuration;
using System.Diagnostics;

namespace CDT.OCRBot.Application.Services
{
    /// <summary>
    /// Use Case: Process a single PDF file
    /// Orchestrates validation, processing, and result handling
    /// </summary>
    public class PdfProcessingService
    {
        private readonly IPdfProcessor _pdfProcessor;
        private readonly IAppLogger _logger;
        private readonly IAuditLogger _auditLogger;

        public PdfProcessingService(IPdfProcessor pdfProcessor, IAppLogger logger, IAuditLogger auditLogger)
        {
            _pdfProcessor = pdfProcessor;
            _logger = logger;
            _auditLogger = auditLogger;
        }

        /// <summary>
        /// Executes the PDF processing use case
        /// </summary>
        public async Task<ProcessingResult> ExecuteAsync(
            string inputFilePath,
            string outputFilePath,
            ProcessingOptions options,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation($"Starting PDF processing use case for: {inputFilePath}");

            var stopwatch = Stopwatch.StartNew();
            ProcessingResult? result = null;

            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(inputFilePath))
                {
                    return ProcessingResult.Failure(inputFilePath, new ArgumentException("Input file path cannot be empty"));
                }

                if (string.IsNullOrWhiteSpace(outputFilePath))
                {
                    return ProcessingResult.Failure(inputFilePath, new ArgumentException("Output file path cannot be empty"));
                }

                // Get file info for audit logging
                var fileInfo = new FileInfo(inputFilePath);
                var fileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0;

                // Execute processing
                result = await _pdfProcessor.ProcessPdfAsync(
                    inputFilePath,
                    outputFilePath,
                    options,
                    progress,
                    cancellationToken);

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    _logger.LogInformation($"Successfully processed: {inputFilePath}");
                }
                if (!result.IsSuccess)
                {
                    // Single log at application boundary with full context and exception stack
                    _logger.LogError(
                        $"PDF processing failed. File: {Path.GetFileName(inputFilePath)}, " +
                        $"Path: {inputFilePath}, Error: {result.Message}",
                        result.Error);  // Include exception for stack trace in error log
                }

                // Log audit entry
                LogAuditEntry(inputFilePath, fileSizeBytes, options, stopwatch.Elapsed, result);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError($"Exception in ProcessPdfUseCase: {ex.Message}", ex);
                
                result = ProcessingResult.Failure(inputFilePath, ex);
                
                // Log failed audit entry
                var fileInfo = new FileInfo(inputFilePath);
                LogAuditEntry(inputFilePath, fileInfo.Exists ? fileInfo.Length : 0, options, stopwatch.Elapsed, result);
                
                return result;
            }
        }

        /// <summary>
        /// Logs processing event to audit log
        /// </summary>
        private void LogAuditEntry(string fileName, long fileSizeBytes, ProcessingOptions options, TimeSpan duration, ProcessingResult result)
        {
            try
            {
                var auditEntry = new ProcessingAuditEntry
                {
                    Timestamp = DateTime.Now,
                    FileName = Path.GetFileName(fileName),
                    FileSizeBytes = fileSizeBytes,
                    PageCount = result.PageCount,
                    OutputFileSizeBytes = result.OutputFileSizeBytes,
                    Options = new ProcessingOptionsAudit
                    {
                        TextOnlyMode = options.TextOnly,
                        AutoTag = options.AddUATags,
                        EnableTextDump = options.EnableTextDump
                    },
                    Duration = duration,
                    Status = result.IsSuccess ? "Success" : "Failed",
                    ErrorMessage = result.IsSuccess ? null : result.Message,
                    ApplicationVersion = AppConstants.Application.Version
                };

                _auditLogger.LogProcessingEvent(auditEntry);
            }
            catch (Exception ex)
            {
                // Don't let audit logging failures break the main flow
                _logger.LogError($"Failed to write audit log: {ex.Message}", ex);
            }
        }
    }
}
