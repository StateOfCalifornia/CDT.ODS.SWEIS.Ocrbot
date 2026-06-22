using CDT.OCRBot.Domain.Configuration;
using Rectangle = iText.Kernel.Geom.Rectangle;

namespace CDT.OCRBot.Infrastructure.Pdf
{
    /// <summary>
    /// Service for transforming coordinates between different coordinate systems (OCR to PDF)
    /// </summary>
    public class CoordinateTransformationService
    {
        /// <summary>
        /// Converts OCR coordinates to PDF coordinates and creates a bounding rectangle
        /// </summary>
        /// <param name="boundingPolygon">OCR bounding polygon (8 points: x1,y1,x2,y2,x3,y3,x4,y4)</param>
        /// <param name="pageWidthPt">PDF page width in points</param>
        /// <param name="pageHeightPt">PDF page height in points</param>
        /// <param name="ocrWidth">OCR coordinate space width</param>
        /// <param name="ocrHeight">OCR coordinate space height</param>
        /// <param name="unit">Unit of measurement (inch, pixel, etc.)</param>
        /// <returns>Rectangle in PDF coordinate space, or null if invalid</returns>
        public Rectangle? TransformOcrToPdfRectangle(
            List<float> boundingPolygon,
            float pageWidthPt,
            float pageHeightPt,
            float ocrWidth,
            float ocrHeight,
            string unit = "inch")
        {
            if (boundingPolygon == null || boundingPolygon.Count < 8)
                return null;

            // Convert OCR units to points
            float conversionFactor = GetConversionFactor(unit);
            float ocrWidthPt = ocrWidth * conversionFactor;
            float ocrHeightPt = ocrHeight * conversionFactor;

            // Calculate scale factors
            float scaleX = pageWidthPt / ocrWidthPt;
            float scaleY = pageHeightPt / ocrHeightPt;

            // Extract polygon points (top-left, top-right, bottom-right, bottom-left)
            float x1 = boundingPolygon[0];
            float y1 = boundingPolygon[1];
            float x2 = boundingPolygon[2];
            float y2 = boundingPolygon[3];
            float x3 = boundingPolygon[4];
            float y3 = boundingPolygon[5];
            float x4 = boundingPolygon[6];
            float y4 = boundingPolygon[7];

            // Find bounding box
            float minX = Math.Min(Math.Min(x1, x2), Math.Min(x3, x4));
            float maxX = Math.Max(Math.Max(x1, x2), Math.Max(x3, x4));
            float minY = Math.Min(Math.Min(y1, y2), Math.Min(y3, y4));
            float maxY = Math.Max(Math.Max(y1, y2), Math.Max(y3, y4));

            // Convert to points and scale
            float xPt = minX * conversionFactor * scaleX;
            float widthPt = (maxX - minX) * conversionFactor * scaleX;

            // Transform Y coordinate (OCR: top-left origin, PDF: bottom-left origin)
            // Use maxY because after flipping, the bottom of the OCR box becomes the bottom-left in PDF
            float yPt = TransformYCoordinate(maxY * conversionFactor, ocrHeightPt, pageHeightPt, scaleY);
            float heightPt = (maxY - minY) * conversionFactor * scaleY;

            // Validate dimensions
            if (widthPt <= 0 || heightPt <= 0 || xPt < 0 || yPt < 0)
                return null;

            return new Rectangle(xPt, yPt, widthPt, heightPt);
        }

        /// <summary>
        /// Transforms Y coordinate from OCR space (top-left origin) to PDF space (bottom-left origin)
        /// </summary>
        /// <param name="yOcr">Y coordinate in OCR space (points)</param>
        /// <param name="ocrHeightPt">OCR page height in points</param>
        /// <param name="pageHeightPt">PDF page height in points</param>
        /// <param name="scaleY">Y-axis scale factor</param>
        /// <returns>Y coordinate in PDF space</returns>
        public float TransformYCoordinate(float yOcr, float ocrHeightPt, float pageHeightPt, float scaleY)
        {
            // OCR has origin at top-left, PDF has origin at bottom-left
            // First flip the coordinate, then scale
            float yFlipped = ocrHeightPt - yOcr;
            return yFlipped * scaleY;
        }

        /// <summary>
        /// Gets the conversion factor from the specified unit to points
        /// </summary>
        /// <param name="unit">Unit name (inch, pixel, etc.)</param>
        /// <returns>Conversion factor to points</returns>
        public float GetConversionFactor(string unit)
        {
            return unit?.ToLower() switch
            {
                "inch" => AppConstants.Pdf.PointsPerInch,
                "pixel" => 1.0f, // Assume 72 DPI for pixels
                _ => AppConstants.Pdf.PointsPerInch
            };
        }

        /// <summary>
        /// Scales a rectangle to fit within page bounds while maintaining aspect ratio
        /// </summary>
        /// <param name="rect">Original rectangle</param>
        /// <param name="pageWidth">Page width</param>
        /// <param name="pageHeight">Page height</param>
        /// <param name="margin">Margin to maintain from edges</param>
        /// <returns>Scaled rectangle</returns>
        public Rectangle ScaleRectangleToPage(Rectangle rect, float pageWidth, float pageHeight, float margin = 10)
        {
            float x = Math.Max(margin, Math.Min(rect.GetX(), pageWidth - margin));
            float y = Math.Max(margin, Math.Min(rect.GetY(), pageHeight - margin));
            float width = Math.Min(rect.GetWidth(), pageWidth - x - margin);
            float height = Math.Min(rect.GetHeight(), pageHeight - y - margin);

            return new Rectangle(x, y, width, height);
        }

        /// <summary>
        /// Calculates the center point of a rectangle
        /// </summary>
        public (float x, float y) GetRectangleCenter(Rectangle rect)
        {
            return (
                rect.GetX() + rect.GetWidth() / 2,
                rect.GetY() + rect.GetHeight() / 2
            );
        }

        /// <summary>
        /// Checks if a point is within a rectangle
        /// </summary>
        public bool IsPointInRectangle(float x, float y, Rectangle rect)
        {
            return x >= rect.GetX() &&
                   x <= rect.GetX() + rect.GetWidth() &&
                   y >= rect.GetY() &&
                   y <= rect.GetY() + rect.GetHeight();
        }
    }
}



