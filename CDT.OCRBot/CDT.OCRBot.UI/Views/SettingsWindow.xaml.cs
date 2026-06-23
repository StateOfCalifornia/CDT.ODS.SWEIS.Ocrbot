using CDT.OCRBot.Domain.Configuration;
using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Infrastructure.Azure;
using CDT.OCRBot.Infrastructure.Common;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using WinFormsDialogResult = System.Windows.Forms.DialogResult;

namespace CDT.OCRBot.UI.Views
{
    /// <summary>
    /// Settings window for configuring Azure credentials and default directories
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private readonly ICredentialRepository _credentialRepository;
        private readonly IPdfProcessor _pdfProcessor;
        private readonly IAppLogger _logger;    
        private string _defaultInputDir = string.Empty;
        private string _defaultOutputDir = string.Empty;

        /// <summary>
        /// Initializes a new instance of SettingsWindow
        /// </summary>
        public SettingsWindow(ICredentialRepository credentialRepository, IPdfProcessor pdfProcessor, IAppLogger logger)
        {
            InitializeComponent();

            _credentialRepository = credentialRepository ?? throw new ArgumentNullException(nameof(credentialRepository));
            _pdfProcessor = pdfProcessor ?? throw new ArgumentNullException(nameof(pdfProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            LoadExistingSettingsAsync();
        }

        /// <summary>
        /// Load existing settings from credential store
        /// </summary>
        private async void LoadExistingSettingsAsync()
        {
            try
            {
                var (azureConfig, directoryConfig, featureConfig) = await _credentialRepository.LoadAllSettingsAsync();

                // Load Azure settings - load any available values (don't require all fields to be valid)
                if (azureConfig != null)
                {
                    // Load Document Intelligence settings if available
                    if (!string.IsNullOrEmpty(azureConfig.DocumentIntelligenceEndpoint))
                        txtOCREndpoint.Text = azureConfig.DocumentIntelligenceEndpoint;

                    if (!string.IsNullOrEmpty(azureConfig.DocumentIntelligenceApiKey))
                        txtOCRApiKey.Password = azureConfig.DocumentIntelligenceApiKey;

                    // Load Azure OpenAI settings if available
                    if (!string.IsNullOrEmpty(azureConfig.OpenAiEndpoint))
                        txtTaggingEndpoint.Text = azureConfig.OpenAiEndpoint;

                    if (!string.IsNullOrEmpty(azureConfig.OpenAiApiKey))
                        txtTaggingApiKey.Password = azureConfig.OpenAiApiKey;

                    if (!string.IsNullOrEmpty(azureConfig.OpenAiDeploymentName))
                        txtDeploymentName.Text = azureConfig.OpenAiDeploymentName;

                        _logger.LogDebug("Azure settings loaded successfully");
                }
                else
                {
                    _logger.LogDebug("No Azure settings found - fields left empty");
                }

                // Load Directory settings
                if (directoryConfig != null && directoryConfig.IsValid())
                {
                    _defaultInputDir = directoryConfig.DefaultInputDirectory ?? string.Empty;
                    _defaultOutputDir = directoryConfig.DefaultOutputDirectory ?? string.Empty;

                    txtDefaultInputDirectory.Text = _defaultInputDir;
                    txtDefaultOutputDirectory.Text = _defaultOutputDir;

                    _logger.LogDebug("Directory settings loaded successfully");
                }
                else
                {
                    _logger.LogDebug("No valid directory settings found - fields left empty");
                }

                // Load Feature settings
                if (featureConfig != null)
                {
                    chkEnableTextDump.IsChecked = featureConfig.EnableTextDump;
                    chkEnableAutoTag.IsChecked = featureConfig.EnableAutoTag;

                    _logger.LogDebug("Feature settings loaded successfully");
                }

                // Update Azure OpenAI section visibility
                UpdateAzureOpenAIVisibility();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error loading settings: {ex.Message}", ex);
            }
        }

        private void BtnBrowseInputDirectory_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select Default Input Directory",
                SelectedPath = _defaultInputDir,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == WinFormsDialogResult.OK)
            {
                _defaultInputDir = dialog.SelectedPath;
                txtDefaultInputDirectory.Text = _defaultInputDir;
                _logger.LogDebug($"Input directory selected: {_defaultInputDir}");
            }
        }

        private void BtnBrowseOutputDirectory_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select Default Output Directory",
                SelectedPath = _defaultOutputDir,
                ShowNewFolderButton = true
            };

            if (dialog.ShowDialog() == WinFormsDialogResult.OK)
            {
                _defaultOutputDir = dialog.SelectedPath;
                txtDefaultOutputDirectory.Text = _defaultOutputDir;
                _logger.LogDebug($"Output directory selected: {_defaultOutputDir}");
            }
        }

        /// <summary>
        /// Tests the OCR (Document Intelligence) connection
        /// </summary>
        /// <param name="endpoint">Azure Document Intelligence endpoint</param>
        /// <param name="apiKey">API key</param>
        /// <returns>Tuple of (success, errorMessage). If success is true, errorMessage is null.</returns>
        private async Task<(bool success, string? errorMessage)> TestOCRConnectionAsync(string endpoint, string apiKey)
        {
            try
            {
                _logger.LogDebug("Testing OCR service connection...");

                var ocrService = new OCRService(endpoint, apiKey, _logger);
                bool success = await ocrService.TestConnectionAsync();

                if (success)
                {
                    _logger.LogDebug("OCR connection test: SUCCESS");
                    return (true, null);
                }
                else
                {
                    _logger.LogDebug("OCR connection test: FAILED");
                    return (false, AppConstants.UI.Validation.OcrConnectionError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"OCR connection test error: {ex.Message}", ex);
                return (false, $"Connection error:\n\n{ex.Message}\n\nPlease verify your credentials and try again.");
            }
        }

        /// <summary>
        /// Tests the Tagging (Azure OpenAI) connection
        /// </summary>
        /// <param name="endpoint">Azure OpenAI endpoint</param>
        /// <param name="apiKey">API key</param>
        /// <param name="deployment">Deployment name</param>
        /// <returns>Tuple of (success, errorMessage). If success is true, errorMessage is null.</returns>
        private async Task<(bool success, string? errorMessage)> TestTaggingConnectionAsync(string endpoint, string apiKey, string deployment)
        {
            try
            {
                _logger.LogDebug("Testing Tagging service connection...");

                var taggingService = new TaggingService(endpoint, apiKey, deployment, _logger);
                bool success = await taggingService.TestConnectionAsync();

                if (success)
                {
                    _logger.LogDebug("Tagging connection test: SUCCESS");
                    return (true, null);
                }
                else
                {
                    _logger.LogDebug("Tagging connection test: FAILED");
                    return (false, AppConstants.UI.Validation.TaggingConnectionError);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Tagging connection test error: {ex.Message}", ex);
                return (false, $"Connection error:\n\n{ex.Message}\n\nPlease verify your credentials and try again.");
            }
        }

        private async void BtnClearSettings_Click(object sender, RoutedEventArgs e)
        {
            var result = CustomMessageBox.Show(
                         AppConstants.UI.Settings.ClearSettingsConfirm,
                         AppConstants.UI.Settings.ClearSettingsTitle,
                         MessageBoxButton.YesNo,
                         MessageBoxImage.Warning,
                         this);

            if (result == MessageBoxResult.Yes)
            {
                // Disable buttons during operation
                btnSave.IsEnabled = false;
                btnClearSettings.IsEnabled = false;
                btnCancel.IsEnabled = false;

                try
                {
                    // Delete all stored settings from the machine
                    txtStatus.Text = AppConstants.UI.Settings.ClearingSettings;
                    await Task.Delay(100); // Allow UI to update

                    await _credentialRepository.ClearAllSettingsAsync();

                    // Clear UI fields
                    txtStatus.Text = AppConstants.UI.Settings.ClearingAzure;
                    await Task.Delay(100); // Allow UI to update

                    txtOCREndpoint.Text = string.Empty;
                    txtOCRApiKey.Password = string.Empty;
                    txtTaggingEndpoint.Text = string.Empty;
                    txtTaggingApiKey.Password = string.Empty;
                    txtDeploymentName.Text = string.Empty;

                    // Clear directories
                    txtStatus.Text = AppConstants.UI.Settings.ClearingDirs;
                    await Task.Delay(100); // Allow UI to update

                    txtDefaultInputDirectory.Text = string.Empty;
                    txtDefaultOutputDirectory.Text = string.Empty;
                    _defaultInputDir = string.Empty;
                    _defaultOutputDir = string.Empty;

                    // Clear checkboxes
                    txtStatus.Text = AppConstants.UI.Settings.ClearingFeatures;
                    await Task.Delay(100); // Allow UI to update

                    chkEnableTextDump.IsChecked = false;
                    chkEnableAutoTag.IsChecked = false;

                    // Update Azure OpenAI section visibility
                    UpdateAzureOpenAIVisibility();

                    txtStatus.Text = string.Empty;

                    txtStatus.Text = AppConstants.UI.Settings.ClosingApp;

                    CustomMessageBox.Show(
                       AppConstants.UI.Settings.SettingsClearedMessage,
                       AppConstants.UI.Settings.SettingsClearedTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information,
                        this);

                    _logger.LogInformation("All settings deleted from storage - closing main window");

                    // Close the main window since settings are required to process files
                    // The user must enter valid settings and save before the main window can be used
                    // Find MainWindow by type from all application windows (more reliable than Application.Current.MainWindow)
                    var mainWindow = System.Windows.Application.Current.Windows
                        .OfType<MainWindow>()
                        .FirstOrDefault();

                    if (mainWindow != null)
                    {
                        mainWindow.Close();
                    }

                    // Re-enable buttons so user can enter new settings
                    btnSave.IsEnabled = true;
                    btnClearSettings.IsEnabled = true;
                    btnCancel.IsEnabled = true;
                    txtStatus.Text = string.Empty;
                }
                catch (Exception ex)
                {
                    txtStatus.Text = "Error clearing settings";
                    CustomMessageBox.Show(
                        $"Error clearing settings:\n\n{ex.Message}",
                        AppConstants.UI.ErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        this);

                    _logger.LogError($"Error clearing settings: {ex.Message}", ex);

                    // Re-enable buttons on error
                    btnSave.IsEnabled = true;
                    btnClearSettings.IsEnabled = true;
                    btnCancel.IsEnabled = true;
                    txtStatus.Text = string.Empty;
                }
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // Check if Auto-Tagging is enabled
            bool autoTagEnabled = chkEnableAutoTag.IsChecked ?? false;

            // Build Azure config
            var azureConfig = new AzureConfig
            {
                DocumentIntelligenceEndpoint = txtOCREndpoint.Text.Trim(),
                DocumentIntelligenceApiKey = txtOCRApiKey.Password.Trim(),
                
                // Only save OpenAI credentials if Auto-Tagging is enabled
                // Otherwise, clear them to prevent confusion and ensure clean state
                OpenAiEndpoint = autoTagEnabled ? txtTaggingEndpoint.Text.Trim() : string.Empty,
                OpenAiApiKey = autoTagEnabled ? txtTaggingApiKey.Password.Trim() : string.Empty,
                OpenAiDeploymentName = autoTagEnabled ? txtDeploymentName.Text.Trim() : string.Empty
            };

            // Update fields from textboxes before validation
            _defaultInputDir = txtDefaultInputDirectory.Text.Trim();
            _defaultOutputDir = txtDefaultOutputDirectory.Text.Trim();

            // Build Directory config
            var directoryConfig = new DirectoryConfig
            {
                DefaultInputDirectory = _defaultInputDir,
                DefaultOutputDirectory = _defaultOutputDir
            };

            // Build Feature config
            var featureConfig = new FeatureConfig
            {
                EnableTextDump = chkEnableTextDump.IsChecked ?? false,
                EnableAutoTag = chkEnableAutoTag.IsChecked ?? false
            };

            // Clear all previous validation errors
            ClearValidationErrors();

            // Validate both Azure and Directory configs
            // Only require OpenAI config if Auto-Tagging is enabled
            var azureErrors = Utils.ValidateAzureConfig(azureConfig, featureConfig.EnableAutoTag);
            var directoryErrors = Utils.ValidateDirectoryConfig(directoryConfig);

            // Show inline validation errors and focus on first error (top to bottom)
            if (directoryErrors.Any() || azureErrors.Any())
            {
                ShowInlineValidationErrors(directoryErrors, azureErrors, featureConfig.EnableAutoTag);
                return;
            }

            // Test connections before saving
            btnSave.IsEnabled = false;
            btnClearSettings.IsEnabled = false;
            btnCancel.IsEnabled = false;

            try
            {
                // Test Document Intelligence connection
                txtStatus.Text = AppConstants.UI.Settings.TestingOcrConnection;
                await Task.Delay(100); // Allow UI to update

                var (ocrSuccess, ocrError) = await TestOCRConnectionAsync(
                    azureConfig.DocumentIntelligenceEndpoint,
                    azureConfig.DocumentIntelligenceApiKey);

                if (!ocrSuccess)
                {
                    txtStatus.Text = AppConstants.UI.Settings.OcrConnectionFailed;
                    CustomMessageBox.Show(
                        $"Azure Document Intelligence connection failed:\n\n{ocrError}",
                        AppConstants.UI.Settings.ConnectionTestFailed,
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        this);
                    return;
                }

                // Test Azure OpenAI connection only if Auto-Tagging is enabled
                if (featureConfig.EnableAutoTag)
                {
                    txtStatus.Text = AppConstants.UI.Settings.TestingTaggingConnection;
                    await Task.Delay(100); // Allow UI to update

                    var (taggingSuccess, taggingError) = await TestTaggingConnectionAsync(
                        azureConfig.OpenAiEndpoint,
                        azureConfig.OpenAiApiKey,
                        azureConfig.OpenAiDeploymentName);

                    if (!taggingSuccess)
                    {
                        txtStatus.Text = AppConstants.UI.Settings.TaggingConnectionFailed;
                        CustomMessageBox.Show(
                            $"Azure OpenAI connection failed:\n\n{taggingError}",
                            AppConstants.UI.Settings.ConnectionTestFailed,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning,
                            this);
                        return;
                    }
                }

                // All connection tests passed, now save settings
                txtStatus.Text = AppConstants.UI.Settings.SavingSettings;
                await Task.Delay(100); // Allow UI to update
                _logger.LogInformation("Saving settings...");

                bool success = await _credentialRepository.SaveAllSettingsAsync(azureConfig, directoryConfig, featureConfig);

                if (success)
                {
                    CustomMessageBox.Show(
                        AppConstants.UI.Settings.SaveSuccess,
                        AppConstants.UI.SuccessTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information,
                        this);

                    _logger.LogInformation("Settings saved successfully");

                    DialogResult = true;
                    Close();
                }
                else
                {
                    CustomMessageBox.Show(
                        AppConstants.UI.Settings.SaveFailure,
                        AppConstants.UI.ErrorTitle,
                        MessageBoxButton.OK,
                        MessageBoxImage.Error,
                        this);

                    _logger.LogWarning("Failed to save Settings");
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(
                    $"Error saving Settings:\n\n{ex.Message}",
                    AppConstants.UI.ErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error,
                    this);

                _logger.LogError($"Error saving Settings: {ex.Message}", ex);
            }
            finally
            {
                btnSave.IsEnabled = true;
                btnClearSettings.IsEnabled = true;
                btnCancel.IsEnabled = true;
                txtStatus.Text = string.Empty;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            _logger.LogDebug("Settings cancelled");
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Handles expander expansion to ensure only one section is expanded at a time
        /// </summary>
        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            var expandedExpander = sender as Expander;

            if (expandedExpander == null)
                return;

            // Collapse all other expanders (with null checks)
            if (expanderDirectories != null && expandedExpander != expanderDirectories)
                expanderDirectories.IsExpanded = false;

            if (expanderOCR != null && expandedExpander != expanderOCR)
                expanderOCR.IsExpanded = false;

            if (expanderTagging != null && expandedExpander != expanderTagging)
                expanderTagging.IsExpanded = false;
        }

        private void ChkEnableAutoTag_Changed(object sender, RoutedEventArgs e)
        {
            UpdateAzureOpenAIVisibility();
        }

        private void UpdateAzureOpenAIVisibility()
        {
            if (expanderTagging != null)
            {
                expanderTagging.Visibility = (chkEnableAutoTag.IsChecked ?? false)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            // If Save() already succeeded, DialogResult is true ? allow close
            if (DialogResult == true)
                return;

            // If Cancel button was pressed, DialogResult = false ? allow close
            if (DialogResult == false)
                return;

            // ----- Perform validation here -----

            bool enableAutoTag = chkEnableAutoTag.IsChecked ?? false;

            // Base required fields (always required)
            bool missingFields = string.IsNullOrWhiteSpace(txtOCREndpoint.Text)
                              || string.IsNullOrWhiteSpace(txtOCRApiKey.Password)
                              || string.IsNullOrWhiteSpace(_defaultInputDir)
                              || string.IsNullOrWhiteSpace(_defaultOutputDir);

            // Azure OpenAI fields only required if Auto-Tagging is enabled
            if (enableAutoTag)
            {
                missingFields = missingFields
                              || string.IsNullOrWhiteSpace(txtTaggingEndpoint.Text)
                              || string.IsNullOrWhiteSpace(txtTaggingApiKey.Password)
                              || string.IsNullOrWhiteSpace(txtDeploymentName.Text);
            }

            if (missingFields)
            {
                var result = CustomMessageBox.Show(
                             AppConstants.UI.Settings.UnsavedChangesMessage,
                             AppConstants.UI.Settings.UnsavedChangesTitle,
                             MessageBoxButton.YesNo,
                             MessageBoxImage.Warning,
                             this);

                if (result == MessageBoxResult.No)
                {
                    e.Cancel = true;   // Keeps window open
                }
            }
        }

        /// <summary>
        /// Clears all inline validation error messages
        /// </summary>
        private void ClearValidationErrors()
        {
            txtDefaultInputDirectoryError.Visibility = Visibility.Collapsed;
            txtDefaultOutputDirectoryError.Visibility = Visibility.Collapsed;
            txtOCREndpointError.Visibility = Visibility.Collapsed;
            txtOCRApiKeyError.Visibility = Visibility.Collapsed;
            txtTaggingEndpointError.Visibility = Visibility.Collapsed;
            txtTaggingApiKeyError.Visibility = Visibility.Collapsed;
            txtDeploymentNameError.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Shows inline validation errors and focuses on the first error field from top to bottom
        /// </summary>
        private void ShowInlineValidationErrors(List<string> directoryErrors, List<string> azureErrors, bool autoTagEnabled)
        {
            System.Windows.Controls.Control? firstErrorControl = null;
            Expander? firstErrorExpander = null;

            // Process directory errors (top section)
            foreach (var error in directoryErrors)
            {
                if (error.Contains("Input Directory"))
                {
                    txtDefaultInputDirectoryError.Text = error.Replace("? ", "");
                    txtDefaultInputDirectoryError.Visibility = Visibility.Visible;
                    if (firstErrorControl is null)
                    {
                        firstErrorControl = txtDefaultInputDirectory;
                        firstErrorExpander = expanderDirectories;
                    }
                }
                else if (error.Contains("Output Directory") || error.Contains("Input and Output"))
                {
                    txtDefaultOutputDirectoryError.Text = error.Replace("? ", "");
                    txtDefaultOutputDirectoryError.Visibility = Visibility.Visible;
                    if (firstErrorControl is null)
                    {
                        firstErrorControl = txtDefaultOutputDirectory;
                        firstErrorExpander = expanderDirectories;
                    }
                }
            }

            // Process Azure Document Intelligence errors (middle section)
            foreach (var error in azureErrors)
            {
                if (error.Contains("Document Intelligence Endpoint"))
                {
                    txtOCREndpointError.Text = error;
                    txtOCREndpointError.Visibility = Visibility.Visible;
                    if (firstErrorControl is null)
                    {
                        firstErrorControl = txtOCREndpoint;
                        firstErrorExpander = expanderOCR;
                    }
                }
                else if (error.Contains("Document Intelligence API Key"))
                {
                    txtOCRApiKeyError.Text = error;
                    txtOCRApiKeyError.Visibility = Visibility.Visible;
                    if (firstErrorControl is null)
                    {
                        firstErrorControl = txtOCRApiKey;
                        firstErrorExpander = expanderOCR;
                    }
                }
            }

            // Process Azure OpenAI errors (bottom section) - only if auto-tagging is enabled
            if (autoTagEnabled)
            {
                foreach (var error in azureErrors)
                {
                    if (error.Contains("OpenAI Endpoint"))
                    {
                        txtTaggingEndpointError.Text = error;
                        txtTaggingEndpointError.Visibility = Visibility.Visible;
                        if (firstErrorControl is null)
                        {
                            firstErrorControl = txtTaggingEndpoint;
                            firstErrorExpander = expanderTagging;
                        }
                    }
                    else if (error.Contains("OpenAI API Key"))
                    {
                        txtTaggingApiKeyError.Text = error;
                        txtTaggingApiKeyError.Visibility = Visibility.Visible;
                        if (firstErrorControl is null)
                        {
                            firstErrorControl = txtTaggingApiKey;
                            firstErrorExpander = expanderTagging;
                        }
                    }
                    else if (error.Contains("Deployment Name"))
                    {
                        txtDeploymentNameError.Text = error;
                        txtDeploymentNameError.Visibility = Visibility.Visible;
                        if (firstErrorControl is null)
                        {
                            firstErrorControl = txtDeploymentName;
                            firstErrorExpander = expanderTagging;
                        }
                    }
                }
            }

            // Expand the section with the first error and focus on it
            if (firstErrorExpander is not null && firstErrorControl is not null)
            {
                firstErrorExpander.IsExpanded = true;

                // Use Dispatcher to ensure the expander is fully expanded before focusing
                var controlToFocus = firstErrorControl;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    controlToFocus.Focus();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

    }
}






