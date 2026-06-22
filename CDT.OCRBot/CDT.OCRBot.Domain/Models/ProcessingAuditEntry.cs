using System;

namespace CDT.OCRBot.Domain.Models
{
    /// <summary>
    /// Represents an audit log entry for PDF processing operations
    /// </summary>
    public class ProcessingAuditEntry
    {
        /// <summary>
        /// Timestamp of the processing event
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Name of the PDF file being processed
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Number of pages in the PDF
        /// </summary>
        public int PageCount { get; set; }

        /// <summary>
        /// Size of the output file in bytes (0 if failed)
        /// </summary>
        public long OutputFileSizeBytes { get; set; }

        /// <summary>
        /// Processing options used
        /// </summary>
        public ProcessingOptionsAudit Options { get; set; } = new();

        /// <summary>
        /// Duration of processing operation
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Status of the operation (Success/Failed)
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Error message if processing failed
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Application version that processed this file
        /// </summary>
        public string ApplicationVersion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Processing options for audit logging
    /// </summary>
    public class ProcessingOptionsAudit
    {
        /// <summary>
        /// Text-only mode enabled
        /// </summary>
        public bool TextOnlyMode { get; set; }

        /// <summary>
        /// Auto-tagging enabled
        /// </summary>
        public bool AutoTag { get; set; }

        /// <summary>
        /// Text dump export enabled
        /// </summary>
        public bool EnableTextDump { get; set; }
    }
}
