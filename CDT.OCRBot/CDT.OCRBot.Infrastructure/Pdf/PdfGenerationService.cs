using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Navigation;
using iText.Kernel.Pdf.Tagging;
using iText.Kernel.XMP;
using System.Text.Json;


namespace CDT.OCRBot.Infrastructure.Pdf
{
    /// <summary>
    /// PDF utility service with correct coordinate transformation and PDF/UA tagging.
    /// 
    /// Key features:
    /// 1. Correct Y-axis transformation (OCR top-left ? PDF bottom-left)
    /// 2. Proportional scaling to handle dimension mismatches
    /// 3. PDF/UA compliant structure tree
    /// 4. Proper marked content with MCID references
    /// </summary>
    public class PdfGenerationService : IPdfGenerationService
    {
        #region Fields and Constants

        private readonly IAppLogger _logger;
        private readonly CoordinateTransformationService _coordinateService;

        // Font sizing constants
        private const float BASELINE_ADJUSTMENT_FACTOR = 0.2f;
        private const float WIDTH_SAFETY_MARGIN = 0.98f; // Always applied to ensure text fits within bounds
        private const float HEIGHT_SIZE_FACTOR = 0.85f;

        // Page dimension constants
        private const float POINTS_PER_INCH = 72f;
        private const float DEFAULT_PAGE_WIDTH = 612f;  // Letter size width
        private const float DEFAULT_PAGE_HEIGHT = 792f; // Letter size height

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of PdfGenerationService
        /// </summary>
        /// <param name="logger">Application logger</param>
        public PdfGenerationService(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _coordinateService = new CoordinateTransformationService();
            _logger.LogDebug("PdfUtilityService initialized");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a searchable PDF with invisible text layer over original images
        /// </summary>
        public byte[] CreateSearchablePdf(List<PageData> pages, byte[] originalPdfBytes)
        {
            _logger.LogInformation($"Creating searchable PDF with {pages.Count} pages");

            using var outputStream = new MemoryStream();
            using var reader = new PdfReader(new MemoryStream(originalPdfBytes));
            using var writer = new PdfWriter(outputStream);
            using var pdfDoc = new PdfDocument(reader, writer);

            try
            {
                var font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                for (int i = 0; i < pages.Count; i++)
                {
                    try
                    {
                        ProcessSearchablePage(pdfDoc, pages[i], i + 1, font);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing page {i + 1}: {ex.Message}", ex);
                        continue; // Continue with next page
                    }
                }

                _logger.LogInformation("Searchable PDF created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateSearchablePdf: {ex.Message}", ex);
                throw;
            }
            finally
            {
                pdfDoc.Close();
            }

            return outputStream.ToArray();
        }

        /// <summary>
        /// Creates a text-only PDF without background images
        /// </summary>
        public byte[] CreateTextOnlyPdf(List<PageData> pages)
        {
            _logger.LogInformation($"Creating text-only PDF with {pages.Count} pages");

            using var outputStream = new MemoryStream();
            using var writer = new PdfWriter(outputStream);
            using var pdfDoc = new PdfDocument(writer);

            try
            {
                var font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                for (int i = 0; i < pages.Count; i++)
                {
                    try
                    {
                        ProcessTextOnlyPage(pdfDoc, pages[i], i + 1, font);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing page {i + 1}: {ex.Message}", ex);
                        continue; // Continue with next page
                    }
                }

                _logger.LogInformation("Text-only PDF created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateTextOnlyPdf: {ex.Message}", ex);
                throw;
            }
            finally
            {
                pdfDoc.Close();
            }

            return outputStream.ToArray();
        }

        /// <summary>
        /// Creates a PDF/UA compliant tagged PDF with accessibility structure
        /// </summary>
        public byte[] CreateTaggedPdf(List<PageData> pages, byte[] originalPdfBytes, Dictionary<string, object> taggingInfo)
        {
            _logger.LogInformation($"Creating tagged PDF with {pages.Count} pages");

            using var outputStream = new MemoryStream();
            using var reader = new PdfReader(new MemoryStream(originalPdfBytes));
            using var writer = new PdfWriter(outputStream);
            using var pdfDoc = new PdfDocument(reader, writer);
            pdfDoc.SetTagged();
            var doc = new iText.Layout.Document(pdfDoc);

            try
            {
                ConfigurePdfUaMetadata(pdfDoc);

                _logger.LogDebug($"Tagging Info: {JsonSerializer.Serialize(taggingInfo, new JsonSerializerOptions { WriteIndented = true })}");

                var renderedText = new HashSet<(string Content, float X, float Y)>();
                var outline = pdfDoc.GetOutlines(true);
                PdfOutline? currentOutline = outline;

                var tags = (List<Dictionary<string, object>>?)taggingInfo.GetValueOrDefault("tags") ?? new List<Dictionary<string, object>>();
                var font = PdfFontFactory.CreateFont(iText.IO.Font.Constants.StandardFonts.HELVETICA);

                for (int pageIndex = 0; pageIndex < pdfDoc.GetNumberOfPages(); pageIndex++)
                {
                    try
                    {
                        ProcessTaggedPage(pdfDoc, pages, pageIndex, tags, font, renderedText, ref currentOutline);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing page {pageIndex + 1}: {ex.Message}", ex);
                        continue; // Continue with next page
                    }
                }

                _logger.LogInformation($"Successfully completed tagged PDF creation with {renderedText.Count} text elements processed");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateTaggedPdf: {ex.Message}", ex);
                try
                {
                    doc.Close();
                    pdfDoc.Close();
                }
                catch (Exception closeEx)
                {
                    _logger.LogError($"Error closing PDF: {closeEx.Message}", closeEx);
                }
                return outputStream.ToArray();
            }

            doc.Close();
            pdfDoc.Close();
            return outputStream.ToArray();
        }

        #endregion

        #region Page Processing Methods

        /// <summary>
        /// Processes a single page for searchable PDF (invisible text)
        /// </summary>
        private void ProcessSearchablePage(PdfDocument pdfDoc, PageData pageData, int pageNumber, PdfFont font)
        {
            var pdfPage = pdfDoc.GetPage(pageNumber);
            var pageRect = pdfPage.GetPageSize();

            var context = CreateRenderingContext(
                pdfPage,
                font,
                pageRect.GetWidth(),
                pageRect.GetHeight(),
                pageData.Width,
                pageData.Height,
                pageData.Unit ?? "inch",
                isVisible: false
            );

            var sortedLines = GetSortedLines(pageData, context);

            foreach (var line in sortedLines)
            {
                RenderTextLine(context, line, pageNumber);
            }

            _logger.LogDebug($"Processed searchable page {pageNumber}: {sortedLines.Count} lines");
        }

        /// <summary>
        /// Processes a single page for text-only PDF (visible text)
        /// </summary>
        private void ProcessTextOnlyPage(PdfDocument pdfDoc, PageData pageData, int pageNumber, PdfFont font)
        {
            var (pageWidthPt, pageHeightPt) = CalculatePageDimensions(
                pageData.Width,
                pageData.Height,
                pageData.Unit ?? "inch"
            );

            var pageSize = new PageSize(pageWidthPt, pageHeightPt);
            pdfDoc.AddNewPage(pageSize);
            var pdfPage = pdfDoc.GetPage(pageNumber);

            var context = CreateRenderingContext(
                pdfPage,
                font,
                pageWidthPt,
                pageHeightPt,
                pageData.Width,
                pageData.Height,
                pageData.Unit ?? "inch",
                isVisible: true
            );

            context.Canvas.SetFillColor(ColorConstants.BLACK);

            var sortedLines = GetSortedLines(pageData, context);

            foreach (var line in sortedLines)
            {
                RenderTextLine(context, line, pageNumber);
            }

            _logger.LogDebug($"Processed text-only page {pageNumber}: {sortedLines.Count} lines");
        }

        /// <summary>
        /// Processes a single page for tagged PDF (with accessibility structure)
        /// </summary>
        private void ProcessTaggedPage(
            PdfDocument pdfDoc,
            List<PageData> pages,
            int pageIndex,
            List<Dictionary<string, object>> tags,
            PdfFont font,
            HashSet<(string Content, float X, float Y)> renderedText,
            ref PdfOutline currentOutline)
        {
            var pdfPage = pdfDoc.GetPage(pageIndex + 1);
            var pageRect = pdfPage.GetPageSize();
            float pageWidth = pageRect.GetWidth();
            float pageHeight = pageRect.GetHeight();
            float imageWidth = pages[pageIndex]?.Width ?? pageWidth / POINTS_PER_INCH;
            float imageHeight = pages[pageIndex]?.Height ?? pageHeight / POINTS_PER_INCH;
            string unit = pages[pageIndex]?.Unit ?? "inch";

            pdfPage.GetPdfObject().Put(PdfName.Tabs, PdfName.S);

            var pageStructElem = pdfDoc.GetStructTreeRoot().AddKid(new PdfStructElem(pdfDoc, PdfName.Sect));

            // Tag original page images
            TagPageImages(pdfPage, pageStructElem, pageIndex + 1);

            // Create tag lookup cache for O(1) performance
            var tagCache = new TagLookupCache(tags, _logger);

            var canvas = new PdfCanvas(pdfPage);
            var sortedLines = GetSortedLinesForPage(pages[pageIndex], pageWidth, pageHeight, imageWidth, imageHeight, unit);

            // Render invisible text with proper structure tagging
            foreach (var line in sortedLines)
            {
#pragma warning disable CS8601 // Possible null reference assignment - currentOutline is intentionally nullable
                RenderTaggedTextLine(
                    line,
                    canvas,
                    font,
                    pdfPage,
                    pageStructElem,
                    pageWidth,
                    pageHeight,
                    imageWidth,
                    imageHeight,
                    unit,
                    pageIndex + 1,
                    tagCache,
                    renderedText,
                    ref currentOutline
                );
#pragma warning restore CS8601
            }

            _logger.LogDebug($"Processed tagged page {pageIndex + 1}: {sortedLines.Count} lines");
        }

        #endregion

        #region Text Rendering Context

        /// <summary>
        /// Context for rendering text on a PDF page
        /// </summary>
        private class TextRenderingContext
        {
            public PdfCanvas Canvas { get; set; } = null!;
            public PdfFont Font { get; set; } = null!;
            public float PageWidth { get; set; }
            public float PageHeight { get; set; }
            public float OcrWidth { get; set; }
            public float OcrHeight { get; set; }
            public string Unit { get; set; } = string.Empty;
            public bool IsVisible { get; set; }
        }

        /// <summary>
        /// Creates a rendering context for a page
        /// </summary>
        private TextRenderingContext CreateRenderingContext(
            PdfPage pdfPage,
            PdfFont font,
            float pageWidth,
            float pageHeight,
            float ocrWidth,
            float ocrHeight,
            string unit,
            bool isVisible)
        {
            var canvas = new PdfCanvas(pdfPage);

            if (!isVisible)
            {
                canvas.SetTextRenderingMode(PdfCanvasConstants.TextRenderingMode.INVISIBLE);
            }

            return new TextRenderingContext
            {
                Canvas = canvas,
                Font = font,
                PageWidth = pageWidth,
                PageHeight = pageHeight,
                OcrWidth = ocrWidth,
                OcrHeight = ocrHeight,
                Unit = unit,
                IsVisible = isVisible
            };
        }

        #endregion

        #region Text Rendering Helpers

        /// <summary>
        /// Renders a single line of text with proper positioning and font sizing
        /// </summary>
        private void RenderTextLine(TextRenderingContext context, LineData lineData, int pageNumber)
        {
            var content = lineData?.Content;
            var boundingPolygon = lineData?.BoundingPolygon;

            if (!IsValidContent(content ?? string.Empty))
            {
                _logger.LogDebug($"Skipping invalid content on page {pageNumber}: '{content}'");
                return;
            }

            var rect = GetBoundingRectangle(
                boundingPolygon ?? new List<float>(),
                context.PageWidth,
                context.PageHeight,
                context.OcrWidth,
                context.OcrHeight,
                context.Unit
            );

            if (rect == null)
            {
                _logger.LogDebug($"Skipping line due to invalid bounding box: '{content}'");
                return;
            }

            float xPt = rect.GetX();
            float yPt = rect.GetY();
            float widthPt = rect.GetWidth();
            float heightPt = rect.GetHeight();

            float fontSizePt = ComputeFontSize(context.Font, content ?? string.Empty, widthPt, heightPt);
            float baselineAdjustment = fontSizePt * BASELINE_ADJUSTMENT_FACTOR;
            float yBaseline = yPt + baselineAdjustment;

            context.Canvas.BeginText();
            context.Canvas.SetFontAndSize(context.Font, fontSizePt);

            if (!context.IsVisible)
            {
                context.Canvas.SetTextRenderingMode(PdfCanvasConstants.TextRenderingMode.INVISIBLE);
            }

            context.Canvas.MoveText(xPt, yBaseline);
            context.Canvas.ShowText(content);
            context.Canvas.EndText();

            _logger.LogDebug($"Rendered {(context.IsVisible ? "visible" : "invisible")} text: '{content}' at ({xPt:F1}, {yBaseline:F1}), fontSize: {fontSizePt:F1}");
        }

        /// <summary>
        /// Renders a tagged text line with accessibility structure
        /// </summary>
        private void RenderTaggedTextLine(
            LineData line,
            PdfCanvas canvas,
            PdfFont font,
            PdfPage pdfPage,
            PdfStructElem pageStructElem,
            float pageWidth,
            float pageHeight,
            float imageWidth,
            float imageHeight,
            string unit,
            int pageNumber,
            TagLookupCache tagCache,
            HashSet<(string Content, float X, float Y)> renderedText,
            ref PdfOutline? currentOutline)
        {
            var content = line?.Content;
            var coordinates = line?.BoundingPolygon;

            if (!IsValidContent(content ?? string.Empty))
            {
                _logger.LogDebug($"Skipping invalid content on page {pageNumber}: '{content}'");
                return;
            }

            var scaledRect = GetBoundingRectangle(coordinates ?? new List<float>(), pageWidth, pageHeight, imageWidth, imageHeight, unit);
            if (scaledRect == null)
            {
                _logger.LogDebug($"Skipping line due to invalid bounding box: '{content}'");
                return;
            }

            float tagX = scaledRect.GetX();
            float tagY = Math.Max(0, Math.Min(scaledRect.GetY(), pageHeight - 10));
            float tagWidth = scaledRect.GetWidth();
            float tagHeight = scaledRect.GetHeight();
            float tagFontSize = ComputeFontSize(font, content ?? string.Empty, tagWidth, tagHeight);

            // Get tag role from taggingInfo using O(1) cache lookup
            var matchingTag = tagCache.FindTag(pageNumber, content ?? string.Empty);
            var role = matchingTag != null && matchingTag.ContainsKey("tag") ? (string)matchingTag["tag"] : "P";

            var textKey = (NormalizeContent(content ?? string.Empty), (float)Math.Round(tagX, 2), (float)Math.Round(tagY, 2));
            if (renderedText.Contains(textKey))
            {
                return; // Skip duplicates
            }

            // Create structure element
            var structElemName = role == "H1" || role == "H2" ? "H" : role;
            var contentStructElem = new PdfStructElem(pdfPage.GetDocument(), new PdfName(structElemName));
            contentStructElem.SetLang(new PdfString("en-US"));
            pageStructElem.AddKid(contentStructElem);

            var mcid = pdfPage.GetNextMcid();
            var markedContentDict = new PdfDictionary();
            markedContentDict.Put(PdfName.MCID, new PdfNumber(mcid));

            canvas.BeginMarkedContent(new PdfName(structElemName), markedContentDict);

            // Render invisible text
            float baselineAdjustment = tagFontSize * BASELINE_ADJUSTMENT_FACTOR;
            float yBaseline = tagY + baselineAdjustment;

            canvas.BeginText();
            canvas.SetFontAndSize(font, tagFontSize);
            canvas.SetTextRenderingMode(PdfCanvasConstants.TextRenderingMode.INVISIBLE);
            canvas.MoveText(tagX, yBaseline);
            canvas.ShowText(content);
            canvas.EndText();
            canvas.EndMarkedContent();

            // Link structure to marked content
            var mcrDict = new PdfDictionary();
            mcrDict.Put(PdfName.Type, PdfName.MCR);
            mcrDict.Put(PdfName.Pg, pdfPage.GetPdfObject());
            mcrDict.Put(PdfName.MCID, new PdfNumber(mcid));

            if (contentStructElem.GetPdfObject().GetAsArray(PdfName.K) == null)
            {
                contentStructElem.GetPdfObject().Put(PdfName.K, new PdfArray());
            }
            contentStructElem.GetPdfObject().GetAsArray(PdfName.K).Add(mcrDict);

            contentStructElem.SetActualText(new PdfString(content));

            // Handle special roles
            HandleSpecialTagRoles(contentStructElem, matchingTag, role, content ?? string.Empty, pdfPage, ref currentOutline);

            renderedText.Add(textKey);
            _logger.LogDebug($"Rendered and tagged text: '{content}' at ({tagX:F1}, {tagY:F1}) with MCID {mcid} as {structElemName}");
        }

        /// <summary>
        /// Gets lines sorted by Y position (top to bottom) for proper reading order
        /// </summary>
        private List<LineData> GetSortedLines(PageData page, TextRenderingContext context)
        {
            return GetSortedLinesForPage(
                page,
                context.PageWidth,
                context.PageHeight,
                context.OcrWidth,
                context.OcrHeight,
                context.Unit
            );
        }

        /// <summary>
        /// Gets lines sorted by Y position with explicit parameters
        /// </summary>
        private List<LineData> GetSortedLinesForPage(
            PageData page,
            float pageWidthPt,
            float pageHeightPt,
            float ocrWidth,
            float ocrHeight,
            string unit)
        {
            return page?.Lines
                ?.Select(line => new
                {
                    Line = line,
                    ScaledRect = GetBoundingRectangle(line?.BoundingPolygon ?? new List<float>(), pageWidthPt, pageHeightPt, ocrWidth, ocrHeight, unit)
                })
                ?.Where(x => x.ScaledRect != null && x.Line != null)
                ?.OrderByDescending(x => x.ScaledRect!.GetY())
                ?.Select(x => x.Line!)
                ?.ToList() ?? new List<LineData>();
        }

        #endregion

        #region PDF/UA Metadata and Structure

        /// <summary>
        /// Configures PDF/UA metadata for accessibility compliance
        /// </summary>
        private void ConfigurePdfUaMetadata(PdfDocument pdfDoc)
        {
            pdfDoc.GetCatalog().SetLang(new PdfString("en-US"));

            var info = pdfDoc.GetDocumentInfo();
            info.SetTitle("Accessible OCR Processed Document");
            info.SetAuthor("OCRBot");
            info.SetSubject("OCR processed PDF with accessibility tags");

            var viewerPrefs = new PdfDictionary();
            viewerPrefs.Put(PdfName.DisplayDocTitle, PdfBoolean.TRUE);
            pdfDoc.GetCatalog().Put(PdfName.ViewerPreferences, viewerPrefs);

            var xmpMeta = XMPMetaFactory.Create();
            xmpMeta.SetLocalizedText(XMPConst.NS_DC, "title", null, "x-default", "Accessible OCR Processed Document");
            xmpMeta.SetProperty(XMPConst.NS_PDFUA_ID, "part", "1");
            pdfDoc.SetXmpMetadata(xmpMeta);

            pdfDoc.GetCatalog().Put(PdfName.Tabs, PdfName.S);

            _logger.LogDebug("PDF/UA metadata configured");
        }

        /// <summary>
        /// Tags page images as Figure elements
        /// </summary>
        private void TagPageImages(PdfPage pdfPage, PdfStructElem pageStructElem, int pageNumber)
        {
            var resources = pdfPage.GetResources();
            var xObjects = resources?.GetResource(PdfName.XObject);

            if (xObjects == null) return;

            foreach (var xObjectName in xObjects.KeySet())
            {
                var xObject = xObjects.GetAsStream(xObjectName);
                if (xObject != null && xObject.Get(PdfName.Subtype)?.Equals(PdfName.Image) == true)
                {
                    var figureStructElem = new PdfStructElem(pdfPage.GetDocument(), PdfName.Figure);
                    pageStructElem.AddKid(figureStructElem);
                    figureStructElem.SetAlt(new PdfString("Original page image"));

                    var imageMcid = pdfPage.GetNextMcid();
                    var imageMcrDict = new PdfDictionary();
                    imageMcrDict.Put(PdfName.Type, PdfName.MCR);
                    imageMcrDict.Put(PdfName.Pg, pdfPage.GetPdfObject());
                    imageMcrDict.Put(PdfName.MCID, new PdfNumber(imageMcid));

                    if (figureStructElem.GetPdfObject().GetAsArray(PdfName.K) == null)
                    {
                        figureStructElem.GetPdfObject().Put(PdfName.K, new PdfArray());
                    }
                    figureStructElem.GetPdfObject().GetAsArray(PdfName.K).Add(imageMcrDict);

                    _logger.LogDebug($"Tagged image as Figure on page {pageNumber} with MCID {imageMcid}");
                }
            }
        }

        /// <summary>
        /// Handles special tag roles like links and headings
        /// </summary>
        private void HandleSpecialTagRoles(
            PdfStructElem contentStructElem,
            Dictionary<string, object>? matchingTag,
            string role,
            string content,
            PdfPage pdfPage,
            ref PdfOutline? currentOutline)
        {
            if (role == "Link" && matchingTag?.ContainsKey("url") == true)
            {
                var url = (string)matchingTag["url"];
                var actionDict = new PdfDictionary();
                actionDict.Put(PdfName.S, PdfName.URI);
                actionDict.Put(PdfName.URI, new PdfString(url));
                var attrDict = new PdfDictionary();
                attrDict.Put(PdfName.A, actionDict);
                contentStructElem.SetAttributes(attrDict);
            }
            else if (role == "H1" || role == "H2")
            {
                CreateBookmark(content, pdfPage, ref currentOutline, role);
            }

            if (matchingTag?.ContainsKey("lang") == true)
            {
                contentStructElem.SetLang(new PdfString((string)matchingTag["lang"]));
            }
        }

        /// <summary>
        /// Creates a bookmark for headings
        /// </summary>
        private void CreateBookmark(string content, PdfPage pdfPage, ref PdfOutline? currentOutline, string role)
        {
            if (currentOutline != null)
            {
                try
                {
                    var destination = PdfExplicitDestination.CreateFit(pdfPage);
                    var bookmark = currentOutline.AddOutline(content);
                    bookmark.AddDestination(destination);

                    if (role == "H1")
                    {
                        currentOutline = bookmark;
                    }

                    _logger.LogDebug($"Created bookmark for '{content}'");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error creating bookmark for '{content}': {ex.Message}", ex);
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validates that content is safe for PDF rendering
        /// </summary>
        private bool IsValidContent(string content)
        {
            return !string.IsNullOrWhiteSpace(content) && !content.Any(c => char.IsControl(c) && c != '\n');
        }

        /// <summary>
        /// Normalizes content for comparison (removes case and whitespace differences)
        /// </summary>
        private string NormalizeContent(string content)
        {
            if (string.IsNullOrEmpty(content)) return content;
            return content.Trim().ToLower();
        }

        /// <summary>
        /// Splits text into multiple lines if it exceeds width
        /// </summary>
        private List<string> SplitTextIntoLines(string text, PdfFont font, float fontSize, float maxWidth)
        {
            var lines = new List<string>();
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var currentLine = new System.Text.StringBuilder();
            float currentWidth = 0;

            foreach (var word in words)
            {
                float wordWidth = font.GetWidth(word + " ", fontSize);
                if (currentWidth + wordWidth <= maxWidth)
                {
                    currentLine.Append(word + " ");
                    currentWidth += wordWidth;
                }
                else
                {
                    if (currentLine.Length > 0)
                    {
                        lines.Add(currentLine.ToString().Trim());
                        currentLine.Clear();
                        currentWidth = 0;
                    }
                    if (wordWidth > maxWidth)
                    {
                        lines.Add(word);
                    }
                    else
                    {
                        currentLine.Append(word + " ");
                        currentWidth += wordWidth;
                    }
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString().Trim());
            }

            return lines;
        }

        /// <summary>
        /// Calculates page dimensions in points based on OCR dimensions and unit
        /// </summary>
        private (float width, float height) CalculatePageDimensions(float ocrWidth, float ocrHeight, string unit)
        {
            if (unit == "inch")
            {
                return (ocrWidth * POINTS_PER_INCH, ocrHeight * POINTS_PER_INCH);
            }
            else if (unit == "pixel")
            {
                return (ocrWidth, ocrHeight);
            }
            else
            {
                _logger.LogWarning($"Unsupported unit '{unit}', using default letter size");
                return (DEFAULT_PAGE_WIDTH, DEFAULT_PAGE_HEIGHT);
            }
        }

        /// <summary>
        /// Converts OCR bounding polygon to PDF rectangle with coordinate transformation
        /// </summary>
        private iText.Kernel.Geom.Rectangle? GetBoundingRectangle(
            List<float> boundingPolygon,
            float pageWidthPt,
            float pageHeightPt,
            float ocrWidth,
            float ocrHeight,
            string unit = "inch")
        {
            if (boundingPolygon == null || boundingPolygon.Count < 8)
            {
                _logger.LogWarning($"Invalid bounding polygon: {JsonSerializer.Serialize(boundingPolygon)}");
                return null;
            }

            // Validate coordinates
            for (int i = 0; i < boundingPolygon.Count; i += 2)
            {
                float x = boundingPolygon[i];
                float y = boundingPolygon[i + 1];

                if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y))
                {
                    _logger.LogWarning($"Invalid coordinate in bounding polygon: x={x}, y={y}");
                    return null;
                }
            }

            // Use coordinate transformation service
            var rect = _coordinateService.TransformOcrToPdfRectangle(
                boundingPolygon,
                pageWidthPt,
                pageHeightPt,
                ocrWidth,
                ocrHeight,
                unit);

            if (rect == null)
            {
                _logger.LogWarning("Coordinate transformation service returned null rectangle");
                return null;
            }

            return rect;
        }

        /// <summary>
        /// Calculate font size that fits text within the given width and height constraints
        /// </summary>
        private float ComputeFontSize(PdfFont font, string text, float boxWidth, float boxHeight)
        {
            if (string.IsNullOrEmpty(text) || boxWidth <= 0 || boxHeight <= 0)
                return 8f;

            // Start with height-based font size
            float fontSize = boxHeight * HEIGHT_SIZE_FACTOR;

            // Check if text fits within width
            float glyphWidth = font.GetWidth(text, fontSize);

            if (glyphWidth > boxWidth)
            {
                // Scale down to fit width
                float widthScale = boxWidth / glyphWidth;
                fontSize = fontSize * widthScale;
            }

            // Always apply safety margin to ensure text fits
            fontSize = fontSize * WIDTH_SAFETY_MARGIN;

            fontSize = Math.Max(1.0f, Math.Min(fontSize, 72f));
            fontSize = (float)Math.Floor(fontSize * 10f) / 10f;

            return fontSize;
        }

        #endregion
    }
}


