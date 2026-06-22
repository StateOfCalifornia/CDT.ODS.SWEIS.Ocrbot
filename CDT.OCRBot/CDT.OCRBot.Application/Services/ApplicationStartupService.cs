using CDT.OCRBot.Domain.Interfaces;

namespace CDT.OCRBot.Application.Services
{
    /// <summary>
    /// Use Case: Initialize all required services
    /// Orchestrates service initialization workflow
    /// </summary>
    public class ApplicationStartupService
    {
        private readonly IPdfProcessor _pdfProcessor;
        private readonly IAppLogger _logger;

        public ApplicationStartupService(IPdfProcessor pdfProcessor, IAppLogger logger)
        {
            _pdfProcessor = pdfProcessor;
            _logger = logger;
        }

        /// <summary>
        /// Executes the service initialization use case
        /// </summary>
        /// <returns>True if initialization successful, false otherwise</returns>
        public async Task<bool> ExecuteAsync()
        {
            _logger.LogInformation("Initializing services...");

            try
            {
                bool success = await _pdfProcessor.InitializeAsync();

                if (success)
                {
                    _logger.LogInformation("Services initialized successfully");
                }
                else
                {
                    _logger.LogError("Service initialization failed");
                }

                return success;
            }
            catch (System.Exception ex)
            {
                _logger.LogError($"Exception during service initialization: {ex.Message}", ex);
                return false;
            }
        }
    }
}
