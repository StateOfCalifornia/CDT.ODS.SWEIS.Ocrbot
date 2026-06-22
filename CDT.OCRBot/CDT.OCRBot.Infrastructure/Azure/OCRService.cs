using Azure;
using Azure.AI.DocumentIntelligence;
using CDT.OCRBot.Domain.Configuration;
using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;
using CDT.OCRBot.Infrastructure.Common;

namespace CDT.OCRBot.Infrastructure.Azure
{
    /// <summary>
    /// OCR service using Azure Document Intelligence for comprehensive document analysis.
    /// </summary>
    public class OCRService : IOCRService
    {
        private readonly DocumentIntelligenceClient _client;
        private readonly IAppLogger _logger;
        private readonly string _modelId;

        /// <summary>
        /// Initializes a new instance of OCRService with Azure Document Intelligence credentials.
        /// </summary>
        public OCRService(string endpoint, string apiKey, IAppLogger logger, string? modelId = null)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Endpoint cannot be null or empty", nameof(endpoint));

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("API Key cannot be null or empty", nameof(apiKey));

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _modelId = modelId ?? AppConstants.Azure.DocumentIntelligenceModelId;

            // Create client with timeout configuration
            var clientOptions = new DocumentIntelligenceClientOptions();
            clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(AppConstants.Azure.ApiTimeoutSeconds);

            _client = new DocumentIntelligenceClient(new Uri(endpoint), new AzureKeyCredential(apiKey), clientOptions);

            _logger.LogDebug($"OCRService initialized - Endpoint: {endpoint}, Model: {_modelId}, Timeout: {AppConstants.Azure.ApiTimeoutSeconds}s");
        }

        /// <summary>
        /// Analyzes a PDF document and extracts text, tables, and layout information.
        /// </summary>
        public async Task<List<PageData>> AnalyzeDocumentAsync(
            byte[] pdfBytes,
            IProgress<ProcessingProgress>? progress = null,
            CancellationToken cancellationToken = default)
        {
            if (pdfBytes == null)
                throw new ArgumentNullException(nameof(pdfBytes));

            if (pdfBytes.Length == 0)
                throw new ArgumentException("PDF bytes cannot be empty", nameof(pdfBytes));

            var pages = new List<PageData>();

            try
            {
                _logger.LogInformation($"Starting document analysis. File size: {pdfBytes.Length} bytes");

                // Report initial progress
                progress?.Report(ProcessingProgress.AnalyzingOcr(0, 1, "Analyzing document..."));

                var operation = await _client.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    _modelId,
                    BinaryData.FromBytes(pdfBytes),
                    cancellationToken: cancellationToken);

                var result = operation.Value;

                if (result?.Pages == null)
                {
                    _logger.LogWarning("No pages found in Document Intelligence response");
                    progress?.Report(ProcessingProgress.OcrComplete(0));
                    return pages;
                }

                var totalPages = result.Pages.Count;
                _logger.LogInformation($"Document Intelligence found {totalPages} pages");

                // Report that we received results and are processing pages
                progress?.Report(ProcessingProgress.AnalyzingOcr(0, totalPages, "Extracting page data..."));

                foreach (var page in result.Pages)
                {
                    var pageNumber = pages.Count + 1;

                    // Report progress for each page being processed
                    progress?.Report(ProcessingProgress.AnalyzingOcr(pageNumber, totalPages, $"Extracting page {pageNumber} of {totalPages}"));

                    if (page.Width == null || page.Height == null || page.Unit == null)
                    {
                        _logger.LogWarning(
                            $"Skipping page {pageNumber} with missing dimensions or unit: " +
                            $"Width={page.Width}, Height={page.Height}, Unit={page.Unit}");
                        continue;
                    }

                    var pageData = new PageData
                    {
                        Width = (float)page.Width,
                        Height = (float)page.Height,
                        Unit = page.Unit?.ToString().ToLower() ?? "inch"
                    };

                    // Extract text lines
                    var lineCount = 0;
                    foreach (var line in page.Lines ?? new List<DocumentLine>())
                    {
                        var lineData = new LineData
                        {
                            Content = line.Content ?? string.Empty,
                            BoundingPolygon = line.Polygon?.ToList() ?? new List<float>()
                        };
                        pageData.Lines.Add(lineData);
                        lineCount++;
                    }

                    // Extract tables
                    var tableCount = 0;
                    if (result.Tables != null)
                    {
                        foreach (var table in result.Tables)
                        {
                            // Only include tables that belong to this page
                            if (table.BoundingRegions?.Any(region => region.PageNumber == pageNumber) == true)
                            {
                                var tableData = new TableData
                                {
                                    RowCount = table.RowCount,
                                    ColumnCount = table.ColumnCount,
                                    BoundingPolygon = table.BoundingRegions?
                                        .SelectMany(r => r.Polygon)
                                        .ToList() ?? new List<float>()
                                };

                                var cellCount = 0;
                                foreach (var cell in table.Cells ?? new List<DocumentTableCell>())
                                {
                                    var cellData = new TableCellData
                                    {
                                        Content = cell.Content ?? string.Empty,
                                        RowIndex = cell.RowIndex,
                                        ColumnIndex = cell.ColumnIndex,
                                        RowSpan = cell.RowSpan,
                                        ColumnSpan = cell.ColumnSpan,
                                        IsHeader = cell.Kind == "columnHeader" || cell.Kind == "rowHeader",
                                        BoundingPolygon = cell.BoundingRegions?
                                            .SelectMany(r => r.Polygon)
                                            .ToList() ?? new List<float>()
                                    };
                                    tableData.Cells.Add(cellData);
                                    cellCount++;
                                }

                                pageData.Tables.Add(tableData);
                                tableCount++;

                                _logger.LogDebug(
                                    $"Extracted table {tableCount} on page {pageNumber}: " +
                                    $"{table.RowCount}x{table.ColumnCount} with {cellCount} cells");
                            }
                        }
                    }

                    pages.Add(pageData);

                    _logger.LogInformation(
                        $"Processed page {pageNumber}: {lineCount} lines, {tableCount} tables. " +
                        $"Dimensions: {pageData.Width}x{pageData.Height} {pageData.Unit}");
                }

                _logger.LogInformation(
                    $"Document analysis completed successfully. Processed {pages.Count} pages " +
                    $"with {pages.Sum(p => p.Lines.Count)} lines and {pages.Sum(p => p.Tables.Count)} tables");

                // Report OCR completion
                progress?.Report(ProcessingProgress.OcrComplete(pages.Count));
            }
            catch (Exception)
            {
                // Let caller handle logging with full context (file name, operation details)
                throw;
            }

            return pages;
        }

        /// <summary>
        /// Tests the connection to Azure Document Intelligence service.
        /// </summary>
        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Testing OCR service connection...");

                byte[] testPdf = Utils.CreateMinimalTestPdf();

                var operation = await _client.AnalyzeDocumentAsync(
                    WaitUntil.Completed,
                    _modelId,
                    BinaryData.FromBytes(testPdf),
                    cancellationToken: cancellationToken);

                bool success = operation.HasValue;

                _logger.LogDebug($"OCR connection test: {(success ? "SUCCESS" : "FAILED")}");

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError($"OCR connection test failed: {ex.Message}", ex);
                return false;
            }
        }
    }
}





