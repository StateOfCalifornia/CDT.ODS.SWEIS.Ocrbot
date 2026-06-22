using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;

namespace CDT.OCRBot.Application.Utils
{
    /// <summary>
    /// Helper class for opening PDF help files
    /// </summary>
    public static class PdfViewerHelper
    {
        /// <summary>
        /// Opens an embedded PDF resource in the default PDF viewer
        /// </summary>
        /// <param name="resourceName">Name of the embedded resource (e.g., "UserGuide.pdf")</param>
        /// <returns>True if opened successfully, false otherwise</returns>
        public static bool OpenEmbeddedPdf(string resourceName)
        {
            try
            {
                // Get the assembly containing the embedded resources
                var assembly = Assembly.GetExecutingAssembly();
                var resourcePath = $"CDT.OCRBot.UI.Resources.HelpDocs.{resourceName}";

                // Check if resource exists
                using (var stream = assembly.GetManifestResourceStream(resourcePath))
                {
                    if (stream == null)
                    {
                        System.Windows.MessageBox.Show(
                            $"Help file '{resourceName}' not found.\n\nPlease ensure the file is included in the application.",
                            "File Not Found",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return false;
                    }

                    // Create temp directory for help files
                    var tempDir = Path.Combine(Path.GetTempPath(), "OCRBot", "Help");
                    Directory.CreateDirectory(tempDir);

                    // Create temp file path
                    var tempFilePath = Path.Combine(tempDir, resourceName);

                    // Extract to temp file
                    using (var fileStream = File.Create(tempFilePath))
                    {
                        stream.CopyTo(fileStream);
                    }

                    // Open with default PDF viewer
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = tempFilePath,
                        UseShellExecute = true,
                        Verb = "open"
                    };

                    Process.Start(startInfo);
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to open help file:\n\n{ex.Message}",
                    "Error Opening File",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Checks if an embedded PDF resource exists
        /// </summary>
        /// <param name="resourceName">Name of the embedded resource</param>
        /// <returns>True if exists, false otherwise</returns>
        public static bool EmbeddedPdfExists(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourcePath = $"OCRBot.Desktop.Resources.HelpDocs.{resourceName}";

            using (var stream = assembly.GetManifestResourceStream(resourcePath))
            {
                return stream != null;
            }
        }
    }
}






