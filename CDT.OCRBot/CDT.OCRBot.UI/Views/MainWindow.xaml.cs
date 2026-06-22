using CDT.OCRBot.Application.Services;
using CDT.OCRBot.Application.Utils;
using CDT.OCRBot.Domain.Configuration;
using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Domain.Models;
using CDT.OCRBot.Infrastructure.Common;
using CDT.OCRBot.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;


namespace CDT.OCRBot.UI.Views
{
    /// <summary>
    /// Main window for PDF processing with professional no-scroll design
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PdfProcessingService _pdfProcessingService;
        private readonly ApplicationStartupService _startupService;
        private readonly ConfigurationLoadingService _configLoadingService;
        private readonly IDialogService _dialogService;
        private readonly IAppLogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly FileSelectionService _fileSelectionService;
        private readonly ProcessingStateManager _processingStateManager;
        private string _outputFolder = string.Empty;
        private string _lastBrowsedFolder = string.Empty;
        private bool _enableTextDump = false;
        private ObservableCollection<FileDisplayItem>? _fileDisplayItems = null;

        public MainWindow(
            PdfProcessingService pdfProcessingService,

            ApplicationStartupService startupService,

            ConfigurationLoadingService configLoadingService,
            FileSelectionService fileSelectionService,
            ProcessingStateManager processingStateManager,
            IDialogService dialogService,
            IAppLogger logger,
            IServiceProvider serviceProvider)
        {
            InitializeComponent();

            _pdfProcessingService = pdfProcessingService;
            _startupService = startupService;
            _configLoadingService = configLoadingService;
            _fileSelectionService = fileSelectionService;
            _processingStateManager = processingStateManager;
            _dialogService = dialogService;
            _logger = logger;
            _serviceProvider = serviceProvider;

            LoadDefaultDirectoriesAsync();
            InitializeServicesAsync();
        }

        #region Initialization

        private async void LoadDefaultDirectoriesAsync()
        {
            var config = await _configLoadingService.ExecuteAsync();
            var directoryConfig = config.DirectoryConfig;
            var featureConfig = config.FeatureConfig;

            if (directoryConfig != null)
            {
                if (!string.IsNullOrWhiteSpace(directoryConfig.DefaultOutputDirectory))
                {
                    _outputFolder = directoryConfig.DefaultOutputDirectory;
                }

                if (!string.IsNullOrWhiteSpace(directoryConfig.DefaultInputDirectory) &&
                    Directory.Exists(directoryConfig.DefaultInputDirectory))
                {
                    _lastBrowsedFolder = directoryConfig.DefaultInputDirectory;
                }
            }

            // Load feature flags
            if (featureConfig != null)
            {
                _enableTextDump = featureConfig.EnableTextDump;

                // Update Auto Tag checkbox visibility based on settings
                chkAddUATags.Visibility = featureConfig.EnableAutoTag ? Visibility.Visible : Visibility.Collapsed;
            }

            // Fallback to Documents if not configured
            if (string.IsNullOrWhiteSpace(_outputFolder))
            {
                _outputFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "OCRBot_Output");
            }

            if (string.IsNullOrWhiteSpace(_lastBrowsedFolder))
            {
                _lastBrowsedFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            System.Diagnostics.Debug.WriteLine($"Default directories loaded - Input: {_lastBrowsedFolder}, Output: {_outputFolder}");
        }

        private async void InitializeServicesAsync()
        {
            txtStatus.Text = AppConstants.UI.ReadyStatus;

            await _startupService.ExecuteAsync();

        }

        #endregion

        #region File Selection

        private async void BtnBrowseFiles_Click(object sender, RoutedEventArgs e)
        {
            // Disable button immediately
            btnBrowseFiles.IsEnabled = false;
            txtStatus.Text = AppConstants.UI.OpeningBrowser;

            try
            {
                // Check if already at max files
                if (_fileSelectionService.IsAtMaxFiles)
                {
                    _dialogService.ShowMessage(
                        $"Maximum {_fileSelectionService.MaxFiles} files allowed. Please remove some files first.",
                        AppConstants.UI.MaxFilesReachedTitle);
                    return;
                }

                // Remember last location
                string initialDirectory = _lastBrowsedFolder;
                int availableSlots = _fileSelectionService.AvailableSlots;

                var selectedFiles = await _dialogService.OpenFileDialogAsync(
                    $"Select PDF Files (Max {availableSlots} more)",
                    "PDF Files (*.pdf)|*.pdf",
                    true,
                    initialDirectory);

                // Update UI immediately if files selected
                if (selectedFiles != null && selectedFiles.Count > 0)
                {
                    // Validate and filter files
                    var (canAddAll, filesToAdd) = _fileSelectionService.ValidateAndFilterFiles(selectedFiles);

                    if (!canAddAll)
                    {
                        _dialogService.ShowMessage(
                            $"You can only add {_fileSelectionService.AvailableSlots} more file(s). Maximum {_fileSelectionService.MaxFiles} files allowed.\n\n" +
                            $"Selected {selectedFiles.Count} files, but only the first {_fileSelectionService.AvailableSlots} will be added.",
                            AppConstants.UI.TooManyFilesTitle,
                            AppConstants.UI.WarningTitle);
                    }

                    // Add files
                    _fileSelectionService.AddFiles(filesToAdd);

                    // Cache the folder for next time
                    if (_fileSelectionService.Count > 0)
                    {
                        try 
                        {
                            var folder = Path.GetDirectoryName(_fileSelectionService.SelectedFiles[0]);
                            if (folder != null) _lastBrowsedFolder = folder;
                        }
                        catch { /* Ignore path errors */ }
                    }

                    // Update UI
                    UpdateFileListDisplay();
                    UpdateProcessButtonState();
                    txtStatus.Text = $"Ready to process {_fileSelectionService.Count} file(s)";
                }
                else
                {
                    txtStatus.Text = AppConstants.UI.ReadyStatus;
                }
            }
            catch (Exception ex)
            {
                _dialogService.ShowMessage($"Error opening file browser: {ex.Message}", AppConstants.UI.ErrorTitle, AppConstants.UI.ErrorTitle);
                txtStatus.Text = "Error opening file browser";
            }
            finally
            {
                // Re-enable button immediately
                btnBrowseFiles.IsEnabled = true;
                btnChooseFolder.IsEnabled = true;
            }
        }

        private async void BtnChooseFolder_Click(object sender, RoutedEventArgs e)
        {
            // Disable button immediately
            btnChooseFolder.IsEnabled = false;
            txtStatus.Text = AppConstants.UI.OpeningFolderBrowser;

            try
            {
                // Check if already at max files
                if (_fileSelectionService.IsAtMaxFiles)
                {
                    _dialogService.ShowMessage(
                        $"Maximum {_fileSelectionService.MaxFiles} files allowed. Please remove some files first.",
                        AppConstants.UI.MaxFilesReachedTitle);
                    return;
                }

                // Remember last location
                string initialDirectory = _lastBrowsedFolder;

                var selectedFolder = await _dialogService.OpenFolderDialogAsync(
                    $"Select folder containing PDF files (max {AppConstants.Processing.MaxFilesPerBatch} files)",
                    initialDirectory);

                // Back on UI thread - update immediately
                if (!string.IsNullOrEmpty(selectedFolder))
                {
                    // Get all PDF files in the folder
                    var pdfFiles = Directory.GetFiles(selectedFolder, "*.pdf", SearchOption.TopDirectoryOnly)
                                           .OrderBy(f => f)
                                           .ToList();

                    // Validate folder contents
                    if (pdfFiles.Count == 0)
                    {
                        _dialogService.ShowMessage(
                            "No PDF Files Found",
                            AppConstants.UI.NoFilesFoundTitle);
                        return; // Don't cache folder if no PDFs found
                    }

                    if (pdfFiles.Count > _fileSelectionService.MaxFiles)
                    {
                        CustomMessageBox.Show(
                            $"The selected folder contains {pdfFiles.Count} PDF files.\n\n" +
                            $"Maximum {_fileSelectionService.MaxFiles} files allowed. Please choose a folder with {_fileSelectionService.MaxFiles} or fewer PDF files.",
                            AppConstants.UI.TooManyFilesTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        txtStatus.Text = AppConstants.UI.ReadyStatus;
                        return;
                    }

                    // Validate and filter files
                    var (canAddAll, filesToAdd) = _fileSelectionService.ValidateAndFilterFiles(pdfFiles);

                    if (!canAddAll)
                    {
                        CustomMessageBox.Show(
                            $"The folder contains {pdfFiles.Count} PDF files, but you can only add {_fileSelectionService.AvailableSlots} more file(s).\n\n" +
                            $"Maximum {_fileSelectionService.MaxFiles} files allowed total.\n\n" +
                            $"Only the first {_fileSelectionService.AvailableSlots} file(s) will be added.",
                            AppConstants.UI.TooManyFilesTitle,
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                    }

                    // Add files
                    int addedCount = _fileSelectionService.AddFiles(filesToAdd);

                    // Cache the folder for next time
                    _lastBrowsedFolder = selectedFolder;

                    // Update UI
                    UpdateFileListDisplay();
                    UpdateProcessButtonState();
                    txtStatus.Text = $"Added {addedCount} file(s) from folder - Ready to process {_fileSelectionService.Count} file(s)";

                    _logger.LogInformation($"Added {addedCount} files from folder: {selectedFolder}");
                }
                else
                {
                    txtStatus.Text = AppConstants.UI.ReadyStatus;
                }
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Error selecting folder: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Error selecting folder";
                _logger.LogError($"Error in BtnChooseFolder_Click: {ex.Message}", ex);
            }
            finally
            {
                // Re-enable button immediately
                btnChooseFolder.IsEnabled = true;
            }
        }

        private void RemoveFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string filePath)
            {
                _fileSelectionService.RemoveFile(filePath);
                UpdateFileListDisplay();
                UpdateProcessButtonState();
                txtStatus.Text = _fileSelectionService.Count > 0
                    ? $"Ready to process {_fileSelectionService.Count} file(s)"
                    : AppConstants.UI.ReadyStatus;

                System.Diagnostics.Debug.WriteLine($"Removed file: {Path.GetFileName(filePath)}");
            }
        }

        private void OpenFileFolder_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button button && button.Tag is string outputPath)
            {
                if (!string.IsNullOrEmpty(outputPath) && File.Exists(outputPath))
                {
                    try
                    {
                        // Open Windows Explorer and select the file
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{outputPath}\"");
                        _logger.LogInformation($"Opened folder for: {Path.GetFileName(outputPath)}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error opening folder: {ex.Message}", ex);
                        CustomMessageBox.Show($"Error opening folder: {ex.Message}", "Error",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            _fileSelectionService.ClearAll();
            UpdateFileListDisplay();
            UpdateProcessButtonState();
            txtStatus.Text = AppConstants.UI.ReadyStatus;

            System.Diagnostics.Debug.WriteLine("Cleared all files");
        }

        private void UpdateFileListDisplay()
        {
            if (_fileSelectionService.Count == 0)
            {
                emptyStatePanel.Visibility = Visibility.Visible;
                fileListPanel.Visibility = Visibility.Collapsed;
                _fileDisplayItems = null;
            }
            else
            {
                emptyStatePanel.Visibility = Visibility.Collapsed;
                fileListPanel.Visibility = Visibility.Visible;

                // Get current files from manager
                var currentFiles = _fileSelectionService.GetDisplayItems();

                // Initialize collection if first time or recreate if needed
                if (_fileDisplayItems == null)
                {
                    _fileDisplayItems = new ObservableCollection<FileDisplayItem>(currentFiles);
                    fileListControl.ItemsSource = _fileDisplayItems;
                }
                else
                {
                    // Update existing collection - find what was added or removed
                    var currentPaths = currentFiles.Select(f => f.FullPath).ToList();
                    var existingPaths = _fileDisplayItems.Select(f => f.FullPath).ToList();

                    // Remove items that are no longer in the selection
                    for (int i = _fileDisplayItems.Count - 1; i >= 0; i--)
                    {
                        if (!currentPaths.Contains(_fileDisplayItems[i].FullPath))
                        {
                            _fileDisplayItems.RemoveAt(i);
                        }
                    }

                    // Add new items
                    foreach (var newFile in currentFiles)
                    {
                        if (!existingPaths.Contains(newFile.FullPath))
                        {
                            _fileDisplayItems.Add(newFile);
                        }
                    }
                }

                // Update count with max indicator
                txtFileCount.Text = $"{_fileSelectionService.Count} files selected (Maximum {_fileSelectionService.MaxFiles} files)";

                // Disable clear button if no files
                btnClearAll.IsEnabled = _fileSelectionService.Count > 0;
            }
        }

        private void ProgressItem_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is Border border && border.DataContext is FileDisplayItem item)
            {
                // Find the icon TextBlock
                var iconTextBlock = FindVisualChild<TextBlock>(border, "iconTextBlock");
                if (iconTextBlock != null)
                {
                    // Subscribe to property changes
                    item.PropertyChanged += (s, args) =>
                    {
                        if (args.PropertyName == nameof(FileDisplayItem.Icon))
                        {
                            UpdateIconAnimation(iconTextBlock, item.Icon);
                        }
                    };

                    // Initial animation state
                    UpdateIconAnimation(iconTextBlock, item.Icon);
                }

                // Find and setup progress bar animation
                var progressBar = FindVisualChild<System.Windows.Controls.ProgressBar>(border, "progressBar");
                if (progressBar != null)
                {
                    double lastValue = 0;
                    item.PropertyChanged += (s, args) =>
                    {
                        if (args.PropertyName == nameof(FileDisplayItem.ProgressValue))
                        {
                            AnimateProgressBar(progressBar, lastValue, item.ProgressValue);
                            lastValue = item.ProgressValue;
                        }
                    };
                }
            }
        }

        private void UpdateIconAnimation(TextBlock iconTextBlock, string icon)
        {
            // Always create a new RotateTransform to avoid frozen object issues
            var rotateTransform = new RotateTransform(0);
            iconTextBlock.RenderTransform = rotateTransform;
            iconTextBlock.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);

            if (icon == "⟳")
            {
                // Start rotation animation
                var animation = new DoubleAnimation
                {
                    From = 0,
                    To = 360,
                    Duration = TimeSpan.FromSeconds(1.5),
                    RepeatBehavior = RepeatBehavior.Forever
                };
                rotateTransform.BeginAnimation(RotateTransform.AngleProperty, animation);
            }
        }

        private void AnimateProgressBar(System.Windows.Controls.ProgressBar progressBar, double fromValue, double toValue)
        {
            var animation = new DoubleAnimation
            {
                From = fromValue,
                To = toValue,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };
            progressBar.BeginAnimation(System.Windows.Controls.ProgressBar.ValueProperty, animation);
        }

        private T? FindVisualChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typedChild && (child as FrameworkElement)?.Name == childName)
                {
                    return typedChild;
                }

                var result = FindVisualChild<T>(child, childName);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        #endregion

        #region Processing Options




        #endregion

        #region Settings

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
            bool? result = settingsWindow.ShowDialog();

            if (result == true)
            {
                InitializeServicesAsync();
                LoadDefaultDirectoriesAsync();
            }
        }

        #endregion

        private void BtnViewLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.LogInformation("User opened logs directory");
                _logger.OpenLogDirectory();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to open logs directory: {ex.Message}", ex);
                CustomMessageBox.Show(
                    "Failed to open logs directory.\n\nPlease check application permissions.",
                    AppConstants.UI.ErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        #region Processing

        private void UpdateProcessButtonState()
        {
            // Check if we have files and at least one is not processed
            bool hasUnprocessedFiles = false;

            if (_fileDisplayItems != null && _fileDisplayItems.Count > 0)
            {
                hasUnprocessedFiles = _fileDisplayItems.Any(item => string.IsNullOrEmpty(item.OutputPath));
            }

            // Enable button only if we can start processing and have unprocessed files
            btnProcess.IsEnabled = _processingStateManager.CanStartProcessing(_fileSelectionService.Count) && hasUnprocessedFiles;
        }



        private async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (_processingStateManager.IsProcessing)
            {
                // Cancel processing
                _processingStateManager.CancelProcessing();
                return;
            }

            if (_fileSelectionService.Count == 0)
            {
                CustomMessageBox.Show("Please select at least one PDF file.", AppConstants.UI.NoFilesSelectedTitle,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrEmpty(_outputFolder))
            {
                CustomMessageBox.Show("Output folder is not configured.\nPlease configure it in Settings.", AppConstants.UI.NoOutputFolderTitle,
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var cancellationToken = _processingStateManager.StartProcessing();
            UpdateProcessButtonState();

            // Disable controls during processing
            DisableControlsDuringProcessing();

            // Update button to Cancel
            btnProcess.Content = "⏹ Cancel Processing";
            btnProcess.IsEnabled = true;

            var options = new ProcessingOptions
            {
                TextOnly = chkTextOnly.IsChecked == true,
                AddUATags = chkAddUATags.IsChecked == true,
                EnableTextDump = _enableTextDump
            };

            System.Diagnostics.Debug.WriteLine($"Starting processing - Mode: {options.GetDescription()}");

            txtStatus.Text = AppConstants.UI.ProcessingFiles;

            // Initialize progress in the existing file list (but don't reset already-processed files)
            var currentItems = fileListControl.ItemsSource as ObservableCollection<FileDisplayItem>;
            if (currentItems != null)
            {
                foreach (var item in currentItems)
                {
                    // Skip files that have already been processed
                    if (!string.IsNullOrEmpty(item.OutputPath))
                        continue;

                    item.Status = "Queued";
                    item.StatusColor = "#95A5A6";
                    item.ProgressValue = 0;
                }
            }

            try
            {
                var results = new List<ProcessingResult>();
                var stopwatch = Stopwatch.StartNew();

                for (int i = 0; i < _fileSelectionService.Count; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        txtStatus.Text = AppConstants.UI.ProcessingCancelled;
                        break;
                    }

                    // Skip already processed files
                    if (currentItems != null && i < currentItems.Count && !string.IsNullOrEmpty(currentItems[i].OutputPath))
                    {
                        _logger.LogInformation($"Skipping already processed file: {currentItems[i].Name}");
                        continue;
                    }

                    var inputFile = _fileSelectionService.SelectedFiles[i];
                    var fileName = Path.GetFileNameWithoutExtension(inputFile);
                    var outputFile = Path.Combine(_outputFolder, $"{fileName}_processed.pdf");
                    outputFile = Utils.GetUniqueFilePath(outputFile);

                    // Update current file progress
                    if (currentItems != null && i < currentItems.Count)
                    {
                        currentItems[i].Status = "Processing...";
                        currentItems[i].StatusColor = "#0288D1";
                        currentItems[i].Icon = "⟳";
                        currentItems[i].ProgressValue = 0;
                    }

                    var fileStopwatch = Stopwatch.StartNew();

                    // Create progress reporter that receives ProcessingProgress objects
                    var progress = new Progress<ProcessingProgress>(progressInfo =>
                    {
                        if (currentItems != null && i < currentItems.Count)
                        {
                            // Update status with phase-specific information
                            currentItems[i].Status = GetDisplayStatus(progressInfo);
                            // Update progress bar with overall percentage
                            currentItems[i].ProgressValue = progressInfo.OverallPercent;
                        }
                    });

                    var result = await _pdfProcessingService.ExecuteAsync(
                        inputFile,
                        outputFile,
                        options,
                        progress,
                        cancellationToken);

                    fileStopwatch.Stop();
                    results.Add(result);

                    // Update file status
                    if (result.IsSuccess)
                    {
                        if (currentItems != null && i < currentItems.Count)
                        {
                            currentItems[i].Status = $"Completed ({fileStopwatch.ElapsedMilliseconds / 1000.0:F1}s)";
                            currentItems[i].StatusColor = "#43A047";
                            currentItems[i].Icon = "✓";
                            currentItems[i].ProgressValue = 100;
                            currentItems[i].OutputPath = outputFile; // Set output path to show folder button
                        }

                        // Update button state after successful processing
                        UpdateProcessButtonState();
                    }
                    else
                    {
                        if (currentItems != null && i < currentItems.Count)
                        {
                            currentItems[i].Status = "Failed";
                            currentItems[i].StatusColor = "#E74C3C";
                            currentItems[i].Icon = "✗";
                            currentItems[i].ProgressValue = 0;
                        }

                        // Update button state after failure
                        UpdateProcessButtonState();

                        var continueProcessing = CustomMessageBox.Show(
                            $"Failed to process {Path.GetFileName(inputFile)}:\n{result.Message}\n\nContinue with remaining files?",
                            "Processing Error",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (continueProcessing == MessageBoxResult.No)
                            break;
                    }
                }

                stopwatch.Stop();


                // Show results
                ShowResults(results, stopwatch.ElapsedMilliseconds);
            }
            catch (OperationCanceledException)
            {
                txtStatus.Text = AppConstants.UI.ProcessingCancelled;
                CustomMessageBox.Show(AppConstants.UI.ProcessingCancelled, AppConstants.UI.CancelledTitle,
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show(
                    $"An unexpected error occurred:\n{ex.Message}",
                    AppConstants.UI.ErrorTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                txtStatus.Text = AppConstants.UI.ProcessingFailed;
                System.Diagnostics.Debug.WriteLine($"Processing error: {ex.Message}");
            }
            finally
            {
                _processingStateManager.CompleteProcessing();

                btnProcess.Content = "Start Processing";
                UpdateProcessButtonState();
                // Re-enable controls after processing
                EnableControlsAfterProcessing();
                // Keep progress panel visible after completion
            }
        }

        /// <summary>
        /// Gets a user-friendly display status from ProcessingProgress
        /// </summary>
        private static string GetDisplayStatus(ProcessingProgress progress)
        {
            // Use phase-specific status messages
            return progress.Phase switch
            {
                ProcessingPhase.Initializing => "Initializing...",
                ProcessingPhase.ReadingPdf => "Reading PDF...",
                ProcessingPhase.AnalyzingOcr => progress.Detail ?? "Analyzing OCR...",
                ProcessingPhase.AutoTagging => progress.Detail ?? "Auto Tagging...",
                ProcessingPhase.CreatingPdf => progress.Detail ?? "Creating PDF...",
                ProcessingPhase.Saving => "Saving...",
                ProcessingPhase.Complete => "Complete!",
                _ => progress.StatusMessage
            };
        }

        private void ShowResults(List<ProcessingResult> results, long totalTimeMs)
        {
            var successCount = results.Count(r => r.IsSuccess);
            var failCount = results.Count - successCount;
            var noOfSuccessFilesDisplay = string.Empty;
            var noOfFailsFilesDisplay = string.Empty;

            // Update status bar with results
            if (successCount > 0)
            {
                if (successCount == 1)
                {
                    noOfSuccessFilesDisplay = "1 file";
                }
                else
                {
                    noOfSuccessFilesDisplay = $"{successCount} files";
                }

                txtStatus.Text = failCount == 0
                    ? $"Successfully processed {noOfSuccessFilesDisplay} in {totalTimeMs / 1000.0:F1}s"
                    : $"Processed {successCount}/{results.Count} files. {failCount} failed.";

                System.Diagnostics.Debug.WriteLine($"Processing completed: {successCount} success, {failCount} failed");
            }
            else
            {
                txtStatus.Text = "Processing failed for all files";
                System.Diagnostics.Debug.WriteLine("Processing failed for all files");
            }
        }

        private void DisableControlsDuringProcessing()
        {
            btnClearAll.IsEnabled = false;
            btnBrowseFiles.IsEnabled = false;
            btnChooseFolder.IsEnabled = false;
            chkTextOnly.IsEnabled = false;
            chkAddUATags.IsEnabled = false;
            btnSettings.IsEnabled = false;

            if (_fileDisplayItems != null)
            {
                foreach (var item in _fileDisplayItems)
                {
                    item.IsRemoveEnabled = false;
                }
            }
        }

        private void EnableControlsAfterProcessing()
        {
            btnClearAll.IsEnabled = true;
            btnBrowseFiles.IsEnabled = true;
            btnChooseFolder.IsEnabled = true;
            chkTextOnly.IsEnabled = true;
            chkAddUATags.IsEnabled = true;
            btnSettings.IsEnabled = true;

            if (_fileDisplayItems != null)
            {
                foreach (var item in _fileDisplayItems)
                {
                    item.IsRemoveEnabled = true;
                }
            }
        }

        #endregion

        #region Helper Classes



        #endregion

        #region Help Menu Handlers

        private void BtnHelp_Click(object sender, RoutedEventArgs e)
        {
            helpPopup.IsOpen = true;
        }

        private void MenuUserGuide_Click(object sender, RoutedEventArgs e)
        {
            helpPopup.IsOpen = false;
            _logger.LogInformation("Opening User Guide");
            PdfViewerHelper.OpenEmbeddedPdf("UserGuide.pdf");
        }

        private void MenuReleaseNotes_Click(object sender, RoutedEventArgs e)
        {
            helpPopup.IsOpen = false;
            _logger.LogInformation("Opening Release Notes");
            PdfViewerHelper.OpenEmbeddedPdf("ReleaseNotes.pdf");
        }

        private void MenuPrivacyPolicy_Click(object sender, RoutedEventArgs e)
        {
            helpPopup.IsOpen = false;
            _logger.LogInformation("Opening Privacy Policy");
            PdfViewerHelper.OpenEmbeddedPdf("PrivacyPolicy.pdf");
        }

        private void MenuFAQs_Click(object sender, RoutedEventArgs e)
        {
            helpPopup.IsOpen = false;
            _logger.LogInformation("Opening FAQs");
            PdfViewerHelper.OpenEmbeddedPdf("FAQs.pdf");
        }

        private void MenuAbout_Click(object sender, RoutedEventArgs e)
        {
            helpPopup.IsOpen = false;
            _logger.LogInformation("Opening About dialog");
            var aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }

        #endregion
    }
}






