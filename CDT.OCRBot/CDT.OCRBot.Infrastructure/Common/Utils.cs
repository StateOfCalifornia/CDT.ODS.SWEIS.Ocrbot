using CDT.OCRBot.Domain.Configuration;

namespace CDT.OCRBot.Infrastructure.Common
{
    /// <summary>
    /// Consolidated utility class for file, PDF, and validation operations
    /// </summary>
    public static class Utils
    {
        #region File Operations

        /// <summary>
        /// Reads all bytes from a file asynchronously with error handling
        /// </summary>
        public static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("File path cannot be null or empty", nameof(path));

            if (!File.Exists(path))
                throw new FileNotFoundException($"File not found: {path}", path);

            try
            {
                return await File.ReadAllBytesAsync(path, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new IOException($"Failed to read file: {path}", ex);
            }
        }

        /// <summary>
        /// Writes all bytes to a file asynchronously with error handling
        /// </summary>
        public static async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("File path cannot be null or empty", nameof(path));

            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                {
                    EnsureDirectoryExists(directory);
                }

                await File.WriteAllBytesAsync(path, bytes, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new IOException($"Failed to write file: {path}", ex);
            }
        }

        /// <summary>
        /// Ensures a directory exists, creating it if necessary
        /// </summary>
        public static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Directory path cannot be null or empty", nameof(path));

            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    System.Diagnostics.Debug.WriteLine($"Created directory: {path}");
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Failed to create directory: {path}", ex);
            }
        }

        /// <summary>
        /// Generates a unique output file path by appending a number if file exists
        /// </summary>
        public static string GetUniqueFilePath(string desiredPath)
        {
            if (!File.Exists(desiredPath))
                return desiredPath;

            var directory = Path.GetDirectoryName(desiredPath) ?? string.Empty;
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(desiredPath);
            var extension = Path.GetExtension(desiredPath);

            int counter = 1;
            string newPath;

            do
            {
                var newFileName = $"{fileNameWithoutExtension}_{counter}{extension}";
                newPath = Path.Combine(directory, newFileName);
                counter++;
            }
            while (File.Exists(newPath));

            return newPath;
        }

        #endregion

        #region PDF Operations

        /// <summary>
        /// Creates a minimal test PDF for connection testing
        /// </summary>
        public static byte[] CreateMinimalTestPdf()
        {
            // Minimal valid PDF (empty page)
            var base64Pdf = "JVBERi0xLjQKJeLjz9MKMSAwIG9iago8PC9UeXBlL0NhdGFsb2cvUGFnZXMgMiAwIFI+PgplbmRvYmoKMiAwIG9iago8PC9UeXBlL1BhZ2VzL0NvdW50IDEvS2lkc1szIDAgUl0+PgplbmRvYmoKMyAwIG9iago8PC9UeXBlL1BhZ2UvTWVkaWFCb3hbMCAwIDMgM10vUGFyZW50IDIgMCBSL1Jlc291cmNlczw8Pj4+PgplbmRvYmoKeHJlZgowIDQKMDAwMDAwMDAwMCA2NTUzNSBmIAowMDAwMDAwMDE1IDAwMDAwIG4gCjAwMDAwMDAwNjQgMDAwMDAgbiAKMDAwMDAwMDEyMSAwMDAwMCBuIAp0cmFpbGVyCjw8L1NpemUgNC9Sb290IDEgMCBSPj4Kc3RhcnR4cmVmCjIwMwolJUVPRg==";
            return Convert.FromBase64String(base64Pdf);
        }

        /// <summary>
        /// Validates that a file path points to a valid PDF file
        /// </summary>
        public static bool IsValidPdfFile(string? filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            if (!File.Exists(filePath))
                return false;

            return Path.GetExtension(filePath).Equals(".pdf", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Validation Operations

        /// <summary>
        /// Validates that a URL is properly formatted
        /// </summary>
        public static bool IsValidUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uriResult))
                return false;

            return uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps;
        }

        /// <summary>
        /// Validates Azure configuration and returns list of errors
        /// </summary>
        public static List<string> ValidateAzureConfig(AzureConfig? config, bool requireOpenAiConfig = true)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("Configuration is null");
                return errors;
            }

            // Use the model's built-in validation
            errors.AddRange(config.GetValidationErrors(requireOpenAiConfig));

            // Additional validation for URL format
            if (!string.IsNullOrWhiteSpace(config.DocumentIntelligenceEndpoint) &&
                !IsValidUrl(config.DocumentIntelligenceEndpoint))
            {
                errors.Add("Document Intelligence Endpoint is not a valid URL");
            }

            // Only validate OpenAI URL format if Auto-Tagging is enabled and endpoint is provided
            if (requireOpenAiConfig &&
                !string.IsNullOrWhiteSpace(config.OpenAiEndpoint) &&
                !IsValidUrl(config.OpenAiEndpoint))
            {
                errors.Add("OpenAI Endpoint is not a valid URL");
            }

            return errors;
        }

        /// <summary>
        /// Validates directory configuration
        /// </summary>
        public static List<string> ValidateDirectoryConfig(DirectoryConfig? config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("Directory configuration is null");
                return errors;
            }

            // Use the model's built-in validation
            errors.AddRange(config.GetValidationErrors());

            return errors;
        }

        #endregion
    }
}
