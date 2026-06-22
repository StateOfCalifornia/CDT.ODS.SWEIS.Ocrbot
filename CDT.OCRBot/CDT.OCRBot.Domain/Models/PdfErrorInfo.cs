using CDT.OCRBot.Domain.Configuration;

namespace CDT.OCRBot.Domain.Models
{
    /// <summary>
    /// Types of PDF errors that can occur during processing
    /// </summary>
    public enum PdfErrorType
    {
        /// <summary>
        /// Unknown or unclassified error
        /// </summary>
        Unknown,

        /// <summary>
        /// PDF is password-protected or encrypted
        /// </summary>
        PasswordProtected,

        /// <summary>
        /// PDF has a digital signature that prevents processing
        /// </summary>
        DigitallySigned,

        /// <summary>
        /// PDF has restricted permissions (e.g., no copying allowed)
        /// </summary>
        RestrictedPermissions,

        /// <summary>
        /// PDF content is not supported (unusual encoding, format issues)
        /// </summary>
        UnsupportedContent,

        /// <summary>
        /// PDF file is corrupted or malformed
        /// </summary>
        CorruptedFile,

        /// <summary>
        /// PDF file is empty or has no extractable content
        /// </summary>
        EmptyOrNoContent,

        /// <summary>
        /// Azure service authentication failed
        /// </summary>
        AuthenticationFailed,

        /// <summary>
        /// Azure service rate limit exceeded
        /// </summary>
        RateLimitExceeded,

        /// <summary>
        /// Network or connectivity issue
        /// </summary>
        NetworkError,

        /// <summary>
        /// Request timed out
        /// </summary>
        Timeout,

        /// <summary>
        /// File is too large for processing
        /// </summary>
        FileTooLarge,

        /// <summary>
        /// File not found or inaccessible
        /// </summary>
        FileNotFound
    }

    /// <summary>
    /// Contains detailed information about a PDF processing error
    /// </summary>
    public class PdfErrorInfo
    {
        /// <summary>
        /// The type/category of the error
        /// </summary>
        public PdfErrorType ErrorType { get; init; }

        /// <summary>
        /// User-friendly error message
        /// </summary>
        public string UserMessage { get; init; } = string.Empty;

        /// <summary>
        /// Technical details for logging
        /// </summary>
        public string TechnicalDetails { get; init; } = string.Empty;

        /// <summary>
        /// Suggested action for the user
        /// </summary>
        public string SuggestedAction { get; init; } = string.Empty;

        /// <summary>
        /// Whether this error might be resolved by retrying
        /// </summary>
        public bool IsRetryable { get; init; }

        /// <summary>
        /// The original exception (if any)
        /// </summary>
        public Exception? OriginalException { get; init; }

        /// <summary>
        /// Creates a PdfErrorInfo for password-protected files
        /// </summary>
        public static PdfErrorInfo PasswordProtected(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.PasswordProtected,
            UserMessage = "This PDF is password-protected or encrypted.",
            TechnicalDetails = technicalDetails ?? "The file requires a password to open or has encryption that prevents processing.",
            SuggestedAction = "Please remove the password protection from the PDF and try again, or use a different file.",
            IsRetryable = false,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for digitally signed files
        /// </summary>
        public static PdfErrorInfo DigitallySigned(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.DigitallySigned,
            UserMessage = "This PDF has digital signatures that may prevent processing.",
            TechnicalDetails = technicalDetails ?? "Digital signatures can restrict document modification.",
            SuggestedAction = "Try removing the digital signature or use a copy of the document without signatures.",
            IsRetryable = false,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for restricted permissions
        /// </summary>
        public static PdfErrorInfo RestrictedPermissions(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.RestrictedPermissions,
            UserMessage = "This PDF has restricted permissions that prevent processing.",
            TechnicalDetails = technicalDetails ?? "The PDF has permission settings that block text extraction or copying.",
            SuggestedAction = "Contact the document owner to obtain an unrestricted version of the PDF.",
            IsRetryable = false,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for unsupported content
        /// </summary>
        public static PdfErrorInfo UnsupportedContent(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.UnsupportedContent,
            UserMessage = "This PDF contains content that cannot be processed.",
            TechnicalDetails = technicalDetails ?? "The PDF may have unusual encoding, non-standard formatting, or unsupported features.",
            SuggestedAction = "Try re-saving the PDF using a standard PDF creator, or convert it to a different format and back to PDF.",
            IsRetryable = false,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for corrupted files
        /// </summary>
        public static PdfErrorInfo CorruptedFile(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.CorruptedFile,
            UserMessage = "This PDF file appears to be corrupted or malformed.",
            TechnicalDetails = technicalDetails ?? "The PDF structure is invalid or contains errors.",
            SuggestedAction = "Try obtaining a fresh copy of the PDF, or use PDF repair tools to fix the file.",
            IsRetryable = false,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for empty or no content
        /// </summary>
        public static PdfErrorInfo EmptyOrNoContent(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.EmptyOrNoContent,
            UserMessage = "No text content could be extracted from this PDF.",
            TechnicalDetails = technicalDetails ?? "The PDF may be empty or contain only non-text elements.",
            SuggestedAction = "Verify that the PDF contains readable text content.",
            IsRetryable = false,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for authentication failures
        /// </summary>
        public static PdfErrorInfo AuthenticationFailed(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.AuthenticationFailed,
            UserMessage = "Azure service authentication failed.",
            TechnicalDetails = technicalDetails ?? "The API key or endpoint may be invalid or expired.",
            SuggestedAction = "Please check your Azure credentials in Settings and try again.",
            IsRetryable = false,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for rate limiting
        /// </summary>
        public static PdfErrorInfo RateLimitExceeded(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.RateLimitExceeded,
            UserMessage = "Azure service rate limit exceeded.",
            TechnicalDetails = technicalDetails ?? "Too many requests have been sent to the Azure service.",
            SuggestedAction = "Please wait a few minutes and try again.",
            IsRetryable = true,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for network errors
        /// </summary>
        public static PdfErrorInfo NetworkError(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.NetworkError,
            UserMessage = "A network error occurred while connecting to Azure services.",
            TechnicalDetails = technicalDetails ?? "Unable to reach Azure services due to network issues.",
            SuggestedAction = "Please check your internet connection and try again.",
            IsRetryable = true,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for timeout
        /// </summary>
        public static PdfErrorInfo Timeout(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.Timeout,
            UserMessage = "The operation timed out while processing the PDF.",
            TechnicalDetails = technicalDetails ?? "The Azure service did not respond within the expected time.",
            SuggestedAction = "The file may be too large or complex. Try processing a smaller file or try again later.",
            IsRetryable = true,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for files that are too large
        /// </summary>
        public static PdfErrorInfo FileTooLarge(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.FileTooLarge,
            UserMessage = "This PDF file is too large to process.",
            TechnicalDetails = technicalDetails ?? "The file exceeds the maximum size limit(500 MB) for processing.",
            SuggestedAction = "Try splitting the PDF into smaller files and process them separately.",
            IsRetryable = false,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for file not found
        /// </summary>
        public static PdfErrorInfo FileNotFound(string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.FileNotFound,
            UserMessage = "The PDF file could not be found or accessed.",
            TechnicalDetails = technicalDetails ?? "The file may have been moved, deleted, or is inaccessible.",
            SuggestedAction = "Please verify the file exists and you have permission to access it.",
            IsRetryable = false,
            OriginalException = ex
        };

        /// <summary>
        /// Creates a PdfErrorInfo for unknown errors
        /// </summary>
        public static PdfErrorInfo Unknown(string? userMessage = null, string? technicalDetails = null, Exception? ex = null) => new()
        {
            ErrorType = PdfErrorType.Unknown,
            UserMessage = userMessage ?? "An unexpected error occurred while processing the PDF.",
            TechnicalDetails = technicalDetails ?? ex?.Message ?? "Unknown error",
            SuggestedAction = "Please check the application logs for more details and try again.",
            IsRetryable = false,
            OriginalException = ex
        };

        /// <summary>
        /// Gets the log file directory path for user reference
        /// </summary>
        public static string GetLogDirectoryPath()
        {
            return AppConstants.Logging.GetLogDirectoryPath();
        }

        /// <summary>
        /// Formats a complete user-friendly error message including suggested action
        /// </summary>
        public string GetFullUserMessage()
        {
            var message = UserMessage;

            if (!string.IsNullOrEmpty(SuggestedAction))
            {
                message += $"\n\n{SuggestedAction}";
            }

            message += $"\n\nFor detailed information, please check the logs at:\n{GetLogDirectoryPath()}";

            return message;
        }
    }
}





