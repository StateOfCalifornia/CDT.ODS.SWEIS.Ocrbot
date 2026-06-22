namespace CDT.OCRBot.Domain.Configuration
{
    /// <summary>
    /// Centralized application constants and configuration values
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// File processing limits
        /// </summary>
        public static class Processing
        {
            /// <summary>
            /// Maximum number of files that can be processed in a single batch
            /// </summary>
            public const int MaxFilesPerBatch = 6;

            /// <summary>
            /// Maximum number of pages to send to Azure OpenAI in a single request
            /// </summary>
            public const int MaxPagesPerAIBatch = 5;
        }

        /// <summary>
        /// PDF rendering and conversion constants
        /// </summary>
        public static class Pdf
        {
            /// <summary>
            /// Standard conversion factor for points per inch in PDF rendering
            /// </summary>
            public const float PointsPerInch = 72f;

            /// <summary>
            /// Default font size for text rendering
            /// </summary>
            public const float DefaultFontSize = 12f;

            /// <summary>
            /// Line spacing multiplier
            /// </summary>
            public const float LineSpacing = 1.2f;
        }

        /// <summary>
        /// Azure service configuration
        /// </summary>
        public static class Azure
        {
            /// <summary>
            /// Azure Document Intelligence model ID for layout analysis
            /// </summary>
            public const string DocumentIntelligenceModelId = "prebuilt-layout";

            /// <summary>
            /// Default timeout for Azure API calls (in seconds)
            /// </summary>
            public const int ApiTimeoutSeconds = 120;

            /// <summary>
            /// Timeout for credential check operations (in seconds)
            /// </summary>
            public const int CredentialCheckTimeoutSeconds = 10;
        }

        /// <summary>
        /// Logging configuration
        /// </summary>
        public static class Logging
        {
            /// <summary>
            /// Number of days to retain log files
            /// </summary>
            public const int LogRetentionDays = 30;

            /// <summary>
            /// Maximum log file size in bytes (10 MB)
            /// </summary>
            public const long MaxLogFileSizeBytes = 10_485_760;

            /// <summary>
            /// Application name used in log directory path
            /// </summary>
            public const string AppName = "OCRBot";

            /// <summary>
            /// Logs subdirectory name
            /// </summary>
            public const string LogsSubdirectory = "Logs";

            /// <summary>
            /// Gets the full path to the log directory
            /// </summary>
            public static string GetLogDirectoryPath()
            {
                return System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                    AppName,
                    LogsSubdirectory);
            }
        }

        /// <summary>
        /// UI color constants
        /// </summary>
        public static class Colors
        {
            public const string Primary = "#0288D1";
            public const string PrimaryLight = "#E3F2FD";
            public const string Border = "#CFD8DC";
            public const string Background = "#FAFAFA";
            public const string Success = "#43A047";
            public const string Warning = "#E74C3C";
            public const string Info = "#0288D1";
            public const string Waiting = "#95A5A6";
        }

        /// <summary>
        /// User Interface display strings
        /// </summary>
        public static class UI
        {
            public const string ReadyStatus = "Ready to process files";
            public const string OpeningBrowser = "Opening file browser...";
            public const string OpeningFolderBrowser = "Opening folder browser...";
            public const string ProcessingComplete = "Processing completed";
            public const string ProcessingFailed = "Processing failed";
            public const string Initializing = "Initializing...";
            public const string MaxFilesReachedTitle = "Maximum Files Reached";
            public const string ErrorTitle = "Error";
            public const string WarningTitle = "Warning";
            public const string TooManyFilesTitle = "Too Many Files";
            public const string NoFilesFoundTitle = "No PDF Files Found";
            public const string NoFilesSelectedTitle = "No Files Selected";
            public const string NoOutputFolderTitle = "No Output Folder";
            public const string CancelledTitle = "Cancelled";
            public const string ProcessingCancelled = "Processing cancelled";
            public const string ProcessingFiles = "Processing files...";
            public const string ConfigurationRequired = "Configuration Required";
            public const string UnexpectedError = "Unexpected Error";
            public const string SuccessTitle = "Success";

            public static class Settings
            {
                public const string ClearSettingsTitle = "Clear All Settings";
                public const string ClearSettingsConfirm = "Clear settings\n\nThis will:\n Delete stored directory configuration\n Delete all stored Azure credentials from your machine\n Reset all fields to empty\n Close the main window (valid settings required to process files)\n\nThis action cannot be undone.";
                public const string ClearingSettings = "Clearing all settings...";
                public const string ClearingAzure = "Clearing Azure credentials...";
                public const string ClearingDirs = "Clearing directory settings...";
                public const string ClearingFeatures = "Clearing feature settings...";
                public const string ClosingApp = "Closing application...";
                public const string SettingsClearedTitle = "Settings Cleared";
                public const string SettingsClearedMessage = "All settings have been cleared and deleted from your machine.\n\nPlease enter valid settings and save to continue using the application.";
                public const string SaveSuccess = "Settings saved successfully! \n\n Features, Default directories and Azure services have been configured.";
                public const string SaveFailure = "Failed to save Settings.\n\nPlease try again or check application logs.";
                public const string UnsavedChangesTitle = "Unsaved or Incomplete Settings";
                public const string UnsavedChangesMessage = "Some required settings are missing.\n\nAre you sure you want to close without saving?";
                public const string TestingOcrConnection = "Testing Azure Document Intelligence connection...";
                public const string TestingTaggingConnection = "Testing Azure OpenAI connection...";
                public const string ConnectionTestFailed = "Connection Test Failed";
                public const string OcrConnectionFailed = "Azure Document Intelligence connection failed";
                public const string TaggingConnectionFailed = "Azure OpenAI connection failed";
                public const string SavingSettings = "Saving settings...";
            }

            public static class Validation
            {
                public const string AzureCredentialsMissing = "Azure credentials not configured. Please update settings.";
                public const string DirectoryConfigMissing = "Directory settings not configured. Please update settings.";
                public const string AzureConfigRequired = "Azure configuration is required to use this application.\n\nPlease configure your Azure Document Intelligence and OpenAI credentials to continue.";
                public const string OcrConnectionError = "OCR Service connection failed.\n\nPlease check:\n Endpoint URL is correct\n API key is valid\n Service is active in Azure Portal";
                public const string TaggingConnectionError = "Tagging Service connection failed.\n\nPlease check:\n Endpoint URL is correct\n API key is valid\n Deployment name matches your Azure OpenAI deployment\n Service is active in Azure Portal";
            }
        }

        /// <summary>
        /// Application metadata
        /// </summary>
        public static class Application
        {
            /// <summary>
            /// Application version for audit logging
            /// </summary>
            public const string Version = "1.0.0";
        }
    }
}



