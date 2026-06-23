using CDT.OCRBot.Domain.Configuration;
using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;
using CDT.OCRBot.Infrastructure.Logging.Formatters;
using Serilog;
using System;
using System.IO;

namespace CDT.OCRBot.Infrastructure.Logging
{
    /// <summary>
    /// Audit logger implementation using Serilog with CSV formatting in text files
    /// </summary>
    public class AuditLogger : IAuditLogger
    {
        private readonly ILogger _auditLog;

        public AuditLogger()
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OCRBot",
                "Logs");

            Directory.CreateDirectory(logDirectory);

            var auditLogPath = Path.Combine(logDirectory, "ocrbot-log-.txt");

            // Create a separate logger instance specifically for audit logs
            // Uses CSV format in .txt file for manual Excel import when needed
            // Monthly rolling: one file per month (e.g., ocrbot-log-202602.txt)
            _auditLog = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    new CsvAuditLogFormatter(auditLogPath),
                    auditLogPath,
                    rollingInterval: RollingInterval.Month,
                    retainedFileCountLimit: 12)  // Keep 12 months of logs
                .CreateLogger();
        }

        /// <summary>
        /// Logs a PDF processing event to the CSV-formatted text audit log
        /// </summary>
        public void LogProcessingEvent(ProcessingAuditEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException(nameof(entry));

            // Flatten Options object and convert Duration to milliseconds for CSV format
            _auditLog.Information(
                "Processing {FileName} {FileSizeBytes} {OutputFileSizeBytes} {PageCount} " +
                "{TextOnlyMode} {AutoTag} {DurationMs} {Status} {ApplicationVersion}",
                entry.FileName,
                entry.FileSizeBytes,
                entry.OutputFileSizeBytes,
                entry.PageCount,
                entry.Options.TextOnlyMode,
                entry.Options.AutoTag,
                (long)entry.Duration.TotalMilliseconds,
                entry.Status,
                entry.ApplicationVersion);
                
        }
    }
}
