using CDT.OCRBot.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CDT.OCRBot.Infrastructure.Logging
{
    /// <summary>
    /// Application logger service that writes to file and debug output
    /// </summary>
    public class AppLogger : IAppLogger
    {
        private readonly ILogger<AppLogger> _logger;
        private readonly string _logDirectory;

        public AppLogger(ILogger<AppLogger> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Set up log directory in AppData
            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OCRBot",
                "Logs");

            Directory.CreateDirectory(_logDirectory);
        }

        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }

        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }

        public void LogError(string message, Exception? exception = null)
        {
            if (exception != null)
            {
                _logger.LogError(exception, message);
            }
            else
            {
                _logger.LogError(message);
            }
        }

        public void LogDebug(string message)
        {
            _logger.LogDebug(message);
        }

        public string GetLogFilePath()
        {
            // Return path to today's log file
            var today = DateTime.Now.ToString("yyyyMMdd");
            return Path.Combine(_logDirectory, $"ocrbot-{today}.log");
        }

        public void OpenLogDirectory()
        {
            try
            {
                if (Directory.Exists(_logDirectory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = _logDirectory,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open log directory");
            }
        }
    }
}


