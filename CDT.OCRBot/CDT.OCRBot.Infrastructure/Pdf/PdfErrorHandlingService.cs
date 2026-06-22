using Azure;
using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;

namespace CDT.OCRBot.Infrastructure.Pdf
{
    /// <summary>
    /// Service for consistent error handling patterns across the application
    /// </summary>
    /// <summary>
    /// Service for consistent PDF-related error handling patterns across the application
    /// </summary>
    public class PdfErrorHandlingService
    {
        private readonly IAppLogger _logger;

        public PdfErrorHandlingService(IAppLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Classifies an exception into a specific PDF error type with rich context
        /// </summary>
        /// <param name="ex">The exception to classify</param>
        /// <param name="fileName">The name of the file being processed (for context)</param>
        /// <returns>A PdfErrorInfo with categorized error information</returns>
        public PdfErrorInfo ClassifyException(Exception ex, string? fileName = null)
        {
            var context = new ErrorContext
            {
                Operation = "PDF Processing",
                FileName = fileName
            };

            return ClassifyExceptionInternal(ex, context);
        }

        private PdfErrorInfo ClassifyExceptionInternal(Exception ex, ErrorContext context)
        {
            var contextString = context.GetContextString();

            // Handle Azure RequestFailedException specifically
            if (ex is RequestFailedException azureEx)
            {
                return ClassifyRequestFailedException(azureEx, contextString);
            }

            // Handle timeout exceptions
            if (ex is TaskCanceledException || ex is OperationCanceledException)
            {
                if (ex.InnerException is TimeoutException || ex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                {
                    return PdfErrorInfo.Timeout(
                        $"Request timed out{contextString}: {ex.Message}",
                        ex);
                }
                return PdfErrorInfo.Unknown(
                    "Operation was cancelled.",
                    $"Operation cancelled{contextString}",
                    ex);
            }

            // Handle HTTP-related exceptions
            if (ex is HttpRequestException httpEx)
            {
                return PdfErrorInfo.NetworkError(
                    $"Network error{contextString}: {httpEx.Message}",
                    httpEx);
            }

            // Handle IO exceptions
            if (ex is System.IO.FileNotFoundException)
            {
                return PdfErrorInfo.FileNotFound(
                    $"File not found{contextString}: {ex.Message}",
                    ex);
            }

            if (ex is System.IO.IOException ioEx)
            {
                if (ioEx.Message.Contains("access", StringComparison.OrdinalIgnoreCase) ||
                    ioEx.Message.Contains("permission", StringComparison.OrdinalIgnoreCase))
                {
                    return PdfErrorInfo.RestrictedPermissions(
                        $"File access error{contextString}: {ioEx.Message}",
                        ioEx);
                }
                return PdfErrorInfo.Unknown(
                    "A file operation error occurred.",
                    $"IO error{contextString}: {ioEx.Message}",
                    ioEx);
            }

            // Default to unknown error
            return PdfErrorInfo.Unknown(
                null,
                $"Unclassified error{contextString}: {ex.Message}",
                ex);
        }

        /// <summary>
        /// Classifies an Azure RequestFailedException into specific error types
        /// </summary>
        private PdfErrorInfo ClassifyRequestFailedException(RequestFailedException azureEx, string fileContext)
        {
            var statusCode = azureEx.Status;
            var errorCode = azureEx.ErrorCode ?? "";
            var message = azureEx.Message ?? "";

            _logger.LogDebug($"Azure error - Status: {statusCode}, ErrorCode: {errorCode}, Message: {message}");

            // Check for specific error codes in the message/error code
            var lowerMessage = message.ToLowerInvariant();
            var lowerErrorCode = errorCode.ToLowerInvariant();

            // Password protected / encrypted detection
            if (lowerMessage.Contains("password") ||
                lowerMessage.Contains("encrypted") ||
                lowerMessage.Contains("protected") ||
                lowerErrorCode.Contains("passwordprotected") ||
                lowerErrorCode.Contains("encrypted"))
            {
                return PdfErrorInfo.PasswordProtected(
                    $"Azure returned password/encryption error{fileContext}: {message}",
                    azureEx);
            }

            // Unsupported content detection (this is what the user's error shows)
            if (lowerErrorCode.Contains("unsupportedcontent") ||
                lowerMessage.Contains("unsupported") ||
                lowerMessage.Contains("content is not supported"))
            {
                // Check if it mentions password protection in the details
                if (lowerMessage.Contains("password"))
                {
                    return PdfErrorInfo.PasswordProtected(
                        $"Azure returned unsupported content (password protected){fileContext}: {message}",
                        azureEx);
                }
                return PdfErrorInfo.UnsupportedContent(
                    $"Azure returned unsupported content error{fileContext}: {message}",
                    azureEx);
            }

            // Invalid request - could be various issues
            if (lowerErrorCode.Contains("invalidrequest") || statusCode == 400)
            {
                // Try to determine the specific cause from the message
                if (lowerMessage.Contains("password") || lowerMessage.Contains("protected"))
                {
                    return PdfErrorInfo.PasswordProtected(
                        $"Azure returned invalid request (likely password protected){fileContext}: {message}",
                        azureEx);
                }
                if (lowerMessage.Contains("corrupt") || lowerMessage.Contains("malformed"))
                {
                    return PdfErrorInfo.CorruptedFile(
                        $"Azure returned invalid request (corrupted file){fileContext}: {message}",
                        azureEx);
                }
                if (lowerMessage.Contains("empty") || lowerMessage.Contains("no content"))
                {
                    return PdfErrorInfo.EmptyOrNoContent(
                        $"Azure returned invalid request (empty/no content){fileContext}: {message}",
                        azureEx);
                }
                if (lowerMessage.Contains("size") || lowerMessage.Contains("too large"))
                {
                    return PdfErrorInfo.FileTooLarge(
                        $"Azure returned invalid request (file too large){fileContext}: {message}",
                        azureEx);
                }
                // Generic unsupported content for 400 errors
                return PdfErrorInfo.UnsupportedContent(
                    $"Azure returned invalid request{fileContext}: {message}",
                    azureEx);
            }

            // Authentication errors
            if (statusCode == 401 || statusCode == 403 ||
                lowerErrorCode.Contains("unauthorized") ||
                lowerErrorCode.Contains("forbidden"))
            {
                return PdfErrorInfo.AuthenticationFailed(
                    $"Azure authentication error{fileContext}: Status {statusCode}, {message}",
                    azureEx);
            }

            // Rate limiting
            if (statusCode == 429 || lowerErrorCode.Contains("ratelimit") || lowerErrorCode.Contains("throttl"))
            {
                return PdfErrorInfo.RateLimitExceeded(
                    $"Azure rate limit exceeded{fileContext}: {message}",
                    azureEx);
            }

            // Service unavailable / server errors
            if (statusCode >= 500 && statusCode < 600)
            {
                return PdfErrorInfo.NetworkError(
                    $"Azure service error{fileContext}: Status {statusCode}, {message}",
                    azureEx);
            }

            // Request timeout
            if (statusCode == 408 || lowerMessage.Contains("timeout"))
            {
                return PdfErrorInfo.Timeout(
                    $"Azure request timeout{fileContext}: {message}",
                    azureEx);
            }

            // Default for unrecognized Azure errors
            return PdfErrorInfo.Unknown(
                "An error occurred while analyzing the PDF with Azure Document Intelligence.",
                $"Azure error{fileContext}: Status {statusCode}, ErrorCode: {errorCode}, Message: {message}",
                azureEx);
        }

        /// <summary>
        /// Executes an action with standardized error handling and logging
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="onError">Optional callback when error occurs</param>
        /// <returns>True if successful, false if error occurred</returns>
        public bool ExecuteWithLogging(
            Action action,
            string operationName,
            Action<Exception>? onError = null)
        {
            try
            {
                _logger.LogDebug($"Starting operation: {operationName}");
                action();
                _logger.LogDebug($"Completed operation: {operationName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in {operationName}: {ex.Message}", ex);
                onError?.Invoke(ex);
                return false;
            }
        }

        /// <summary>
        /// Executes an async function with standardized error handling and logging
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="func">Async function to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="defaultValue">Default value to return on error</param>
        /// <param name="onError">Optional callback when error occurs</param>
        /// <returns>Result or default value if error occurred</returns>
        public async Task<T?> ExecuteWithLoggingAsync<T>(
            Func<Task<T>> func,
            string operationName,
            T? defaultValue = default,
            Action<Exception>? onError = null)
        {
            try
            {
                _logger.LogDebug($"Starting async operation: {operationName}");
                var result = await func();
                _logger.LogDebug($"Completed async operation: {operationName}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in {operationName}: {ex.Message}", ex);
                onError?.Invoke(ex);
                return defaultValue;
            }
        }

        /// <summary>
        /// Executes an action with retry logic
        /// </summary>
        /// <param name="action">Action to execute</param>
        /// <param name="operationName">Name of the operation for logging</param>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="delayMs">Delay between retries in milliseconds</param>
        /// <returns>True if successful, false if all retries failed</returns>
        public async Task<bool> ExecuteWithRetryAsync(
            Func<Task> action,
            string operationName,
            int maxRetries = 3,
            int delayMs = 1000)
        {
            int attempt = 0;

            while (attempt < maxRetries)
            {
                try
                {
                    attempt++;
                    _logger.LogDebug($"Attempt {attempt}/{maxRetries} for operation: {operationName}");

                    await action();

                    _logger.LogDebug($"Successfully completed operation: {operationName}");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Attempt {attempt}/{maxRetries} failed for {operationName}: {ex.Message}");

                    if (attempt >= maxRetries)
                    {
                        _logger.LogError($"All {maxRetries} attempts failed for {operationName}", ex);
                        return false;
                    }

                    // Wait before retry
                    await Task.Delay(delayMs);
                }
            }

            return false;
        }

        /// <summary>
        /// Safely disposes resources with logging
        /// </summary>
        /// <param name="disposable">Resource to dispose</param>
        /// <param name="resourceName">Name of the resource for logging</param>
        public void SafeDispose(IDisposable? disposable, string resourceName)
        {
            if (disposable == null)
                return;

            try
            {
                disposable.Dispose();
                _logger.LogDebug($"Successfully disposed: {resourceName}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error disposing {resourceName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates a user-friendly error message from an exception
        /// </summary>
        /// <param name="ex">Exception to convert</param>
        /// <param name="operationContext">Context of what was being done</param>
        /// <returns>User-friendly error message</returns>
        public string CreateUserFriendlyMessage(Exception ex, string operationContext)
        {
            return ex switch
            {
                ArgumentException => $"Invalid input for {operationContext}: {ex.Message}",
                UnauthorizedAccessException => $"Access denied while {operationContext}. Please check permissions.",
                System.IO.FileNotFoundException => $"File not found while {operationContext}: {ex.Message}",
                System.IO.IOException => $"File operation failed while {operationContext}: {ex.Message}",
                TimeoutException => $"Operation timed out while {operationContext}. Please try again.",
                OperationCanceledException => $"Operation cancelled: {operationContext}",
                _ => $"An error occurred while {operationContext}. Please check logs for details."
            };
        }
    }
}



