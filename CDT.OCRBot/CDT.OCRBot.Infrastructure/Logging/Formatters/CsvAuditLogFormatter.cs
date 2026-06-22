using Serilog.Events;
using Serilog.Formatting;
using System;
using System.Globalization;
using System.IO;

namespace CDT.OCRBot.Infrastructure.Logging.Formatters
{
    /// <summary>
    /// CSV formatter for audit logs optimized for Excel analysis
    /// </summary>
    public class CsvAuditLogFormatter : ITextFormatter
    {
        private static readonly string[] Headers = new[]
        {
            "Timestamp",
            "FileName",
            "FileSizeMB",
            "OutputFileSizeMB",
            "PageCount",
            "TextOnlyMode",
            "AutoTag",
            "Duration",
            "Status",
            "ApplicationVersion"
        };

        private bool _headerWritten = false;
        private readonly object _lock = new object();
        private readonly string _logFilePath;

        /// <summary>
        /// Creates a new CSV formatter
        /// </summary>
        /// <param name="logFilePath">Path template for the log file (used to check if file exists)</param>
        public CsvAuditLogFormatter(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        public void Format(LogEvent logEvent, TextWriter output)
        {
            lock (_lock)
            {
                // Write header only if not written yet AND stream is at beginning (new file)
                if (!_headerWritten)
                {
                    bool shouldWriteHeader = false;
                    
                    try
                    {
                        // Check if the underlying stream is at position 0 (new/empty file)
                        if (output is StreamWriter streamWriter && streamWriter.BaseStream != null)
                        {
                            shouldWriteHeader = streamWriter.BaseStream.Position == 0;
                        }
                        else
                        {
                            // Fallback: if we can't check stream position, write header to be safe
                            shouldWriteHeader = true;
                        }
                    }
                    catch
                    {
                        // If we can't check, write the header to be safe
                        shouldWriteHeader = true;
                    }
                    
                    if (shouldWriteHeader)
                    {
                        output.WriteLine(string.Join(",", Headers));
                    }
                    
                    _headerWritten = true;
                }

                // Extract and format property values
                var timestamp = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                var fileName = GetPropertyValue(logEvent, "FileName");
                var fileSizeBytes = GetPropertyValue(logEvent, "FileSizeBytes");
                var outputFileSizeBytes = GetPropertyValue(logEvent, "OutputFileSizeBytes");
                var pageCount = GetPropertyValue(logEvent, "PageCount");
                var textOnlyMode = GetPropertyValue(logEvent, "TextOnlyMode");
                var autoTag = GetPropertyValue(logEvent, "AutoTag");
                var durationMs = GetPropertyValue(logEvent, "DurationMs");
                var status = GetPropertyValue(logEvent, "Status");
                var applicationVersion = GetPropertyValue(logEvent, "ApplicationVersion");

                // Convert bytes to MB (with 2 decimal places)
                var fileSizeMB = FormatBytesToMB(fileSizeBytes);
                var outputFileSizeMB = FormatBytesToMB(outputFileSizeBytes);

                // Convert milliseconds to MM:SS format
                var durationFormatted = FormatMillisecondsToMMSS(durationMs);

                // Write CSV row with proper escaping
                var row = string.Join(",", new[]
                {
                    EscapeCsvValue(timestamp),
                    EscapeCsvValue(fileName),
                    fileSizeMB,
                    outputFileSizeMB,
                    pageCount,
                    textOnlyMode,
                    autoTag,
                    durationFormatted,
                    EscapeCsvValue(status),
                    EscapeCsvValue(applicationVersion)
                });

                output.WriteLine(row);
            }
        }

        private string GetPropertyValue(LogEvent logEvent, string propertyName)
        {
            if (logEvent.Properties.TryGetValue(propertyName, out var value))
            {
                // Remove Serilog's quote wrapping and return raw value
                var stringValue = value.ToString();
                
                // Remove surrounding quotes if present
                if (stringValue.StartsWith("\"") && stringValue.EndsWith("\""))
                {
                    stringValue = stringValue.Substring(1, stringValue.Length - 2);
                }

                return stringValue;
            }

            return string.Empty;
        }

        private string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            // Replace newlines with space for cleaner CSV
            value = value.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

            // Check if escaping is needed (contains comma, quote, or newline)
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                // Escape quotes by doubling them
                value = value.Replace("\"", "\"\"");
                
                // Wrap in quotes
                return $"\"{value}\"";
            }

            return value;
        }

        /// <summary>
        /// Converts bytes to megabytes with 2 decimal places
        /// </summary>
        private string FormatBytesToMB(string bytesStr)
        {
            if (string.IsNullOrEmpty(bytesStr) || !long.TryParse(bytesStr, out var bytes))
            {
                return "0.00";
            }

            var mb = bytes / (1024.0 * 1024.0);
            return mb.ToString("F2", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts milliseconds to MM:SS format
        /// </summary>
        private string FormatMillisecondsToMMSS(string millisecondsStr)
        {
            if (string.IsNullOrEmpty(millisecondsStr) || !long.TryParse(millisecondsStr, out var milliseconds))
            {
                return "00:00";
            }

            var totalSeconds = (int)(milliseconds / 1000);
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }

        /// <summary>
        /// Resolves the actual file path for a given date based on rolling interval
        /// </summary>
        private string ResolveActualFilePath(string pathTemplate, DateTimeOffset timestamp)
        {
            // Serilog uses this format for monthly rolling: path202602.txt
            var directory = Path.GetDirectoryName(pathTemplate) ?? string.Empty;
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(pathTemplate);
            var extension = Path.GetExtension(pathTemplate);
            
            // Remove trailing dash if present (from template "ocrbot-log-.txt")
            if (fileNameWithoutExt.EndsWith("-"))
            {
                fileNameWithoutExt = fileNameWithoutExt.TrimEnd('-');
            }
            
            // Use yyyyMM format for monthly rolling (e.g., 202602 for February 2026)
            var dateString = timestamp.ToString("yyyyMM");
            var actualFileName = $"{fileNameWithoutExt}{dateString}{extension}";
            
            return Path.Combine(directory, actualFileName);
        }
    }
}

