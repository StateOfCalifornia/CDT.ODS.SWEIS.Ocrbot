using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Domain.Interfaces
{
    /// <summary>
    /// Interface for audit logging of PDF processing events
    /// </summary>
    public interface IAuditLogger
    {
        /// <summary>
        /// Logs a PDF processing event to the audit log
        /// </summary>
        /// <param name="entry">The audit entry containing processing details</param>
        void LogProcessingEvent(ProcessingAuditEntry entry);
    }
}
