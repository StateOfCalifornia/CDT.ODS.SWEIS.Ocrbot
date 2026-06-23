

using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Domain.Interfaces
{
    /// <summary>
    /// Service interface for PDF creation and manipulation using iText
    /// <summary>
    /// Service for generating PDF documents
    /// </summary>
    public interface IPdfGenerationService
    {
        byte[] CreateSearchablePdf(List<PageData> pages, byte[] originalPdfBytes);
        byte[] CreateTextOnlyPdf(List<PageData> pages);
        byte[] CreateTaggedPdf(List<PageData> pages, byte[] originalPdfBytes, Dictionary<string, object> taggingInfo);
    }
}






