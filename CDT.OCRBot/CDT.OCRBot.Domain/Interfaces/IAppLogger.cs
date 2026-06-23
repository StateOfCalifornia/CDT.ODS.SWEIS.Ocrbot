using Microsoft.Extensions.Logging;

using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Domain.Interfaces
{
    /// <summary>
    /// Application-wide logging interface
    /// Wraps ILogger for consistent logging across the application
    /// </summary>
    public interface IAppLogger
    {
        /// <summary>
        /// Logs an informational message
        /// </summary>
        void LogInformation(string message);

        /// <summary>
        /// Logs a warning message
        /// </summary>
        void LogWarning(string message);

        /// <summary>
        /// Logs an error message with optional exception
        /// </summary>
        void LogError(string message, Exception? exception = null);

        /// <summary>
        /// Logs a debug message (only in development)
        /// </summary>
        void LogDebug(string message);

        /// <summary>
        /// Gets the path to the current log file
        /// </summary>
        string GetLogFilePath();

        /// <summary>
        /// Opens the log directory in File Explorer
        /// </summary>
        void OpenLogDirectory();
    }
}




