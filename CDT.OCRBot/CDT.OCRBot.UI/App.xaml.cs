using CDT.OCRBot.Application.Services;
using CDT.OCRBot.Domain.Configuration;
using CDT.OCRBot.Domain.Interfaces;
using CDT.OCRBot.Infrastructure.Azure;
using CDT.OCRBot.Infrastructure.Logging;
using CDT.OCRBot.Infrastructure.Pdf;
using CDT.OCRBot.Infrastructure.Repositories;
using CDT.OCRBot.UI.Services;
using CDT.OCRBot.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.IO;
using System.Windows;

namespace CDT.OCRBot.UI
{
    /// <summary>
    /// Application entry point with dependency injection and logging setup
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private ServiceProvider? _serviceProvider;

        /// <summary>
        /// Configures logging, dependency injection and shows appropriate window on startup
        /// </summary>
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set shutdown mode to manual control
            // This prevents the app from shutting down when SettingsWindow closes on first launch
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Configure Serilog before anything else
            ConfigureLogging();

            // Configure dependency injection
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Log application start
            var logger = _serviceProvider.GetRequiredService<IAppLogger>();
            logger.LogInformation("=== OCRBot Desktop Application Started ===");
            logger.LogInformation($"Version: {GetType().Assembly.GetName().Version}");
            logger.LogInformation($"OS: {Environment.OSVersion}");
            logger.LogInformation($"User: {Environment.UserName}");

            // Check if credentials are configured (with timeout)
            var credentialRepo = _serviceProvider.GetRequiredService<ICredentialRepository>();
            bool credentialsConfigured;

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(AppConstants.Azure.CredentialCheckTimeoutSeconds));
                
                var (azureConfig, dirConfig, _) = await credentialRepo.LoadAllSettingsAsync();
                
                // Only require Document Intelligence (OCR) for app launch
                // OpenAI (tagging) is optional and validated at processing time
                credentialsConfigured = azureConfig != null && azureConfig.HasValidDocumentIntelligence() && 
                                      dirConfig != null && dirConfig.IsValid();
            }
            catch (Exception ex)
            {
                logger.LogError($"Error checking credentials: {ex.Message}", ex);
                credentialsConfigured = false;
            }

            if (!credentialsConfigured)
            {
                // First time launch - show settings window
                logger.LogInformation("First launch detected - showing settings window");

                var settingsWindow = _serviceProvider.GetRequiredService<SettingsWindow>();
                bool? result = settingsWindow.ShowDialog();

                // If user cancelled settings, exit application
                if (result != true)
                {
                    logger.LogWarning("User cancelled initial configuration - application exiting");

                    CustomMessageBox.Show(
                        AppConstants.UI.Validation.AzureConfigRequired,
                        AppConstants.UI.ConfigurationRequired,
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Shutdown();
                    return;
                }

                logger.LogInformation("Initial configuration completed");
            }

            // Show main window
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow; // Set as the application's main window
            ShutdownMode = ShutdownMode.OnMainWindowClose; // Now shutdown when main window closes

            mainWindow.Show();
            mainWindow.Activate();
            mainWindow.Focus();

            logger.LogInformation("Main window displayed - application ready");
        }

        /// <summary>
        /// Configures Serilog logging with dual sinks: error logs and debug logs
        /// </summary>
        private void ConfigureLogging()
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OCRBot",
                "Logs");

            Directory.CreateDirectory(logDirectory);

            // Error log: daily rolling, errors only, 30-day retention
            var errorLogPath = Path.Combine(logDirectory, "ocrbot-error-.log");

            // Debug/main log: only in Debug mode
            var debugLogPath = Path.Combine(logDirectory, "ocrbot-debug-.log");

            var loggerConfig = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Debug(
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}");

            // Error log (both Debug and Release)
            loggerConfig.WriteTo.File(
                errorLogPath,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Error,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: AppConstants.Logging.LogRetentionDays,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: AppConstants.Logging.MaxLogFileSizeBytes,
                rollOnFileSizeLimit: true);

            // Debug log (Debug mode only)
#if DEBUG
            loggerConfig.WriteTo.File(
                debugLogPath,
                restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7, // Keep 7 days of debug logs
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext} - {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: AppConstants.Logging.MaxLogFileSizeBytes,
                rollOnFileSizeLimit: true);
#endif

            Log.Logger = loggerConfig.CreateLogger();
        }

        /// <summary>
        /// Configures dependency injection container
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddLogging(builder =>
            {
                builder.ClearProviders();
                builder.AddSerilog(dispose: true);
            });

            // Register app logger
            services.AddSingleton<IAppLogger, AppLogger>();

            // Register audit logger
            services.AddSingleton<IAuditLogger, AuditLogger>();

            // Register repositories WITH LOGGER
            services.AddSingleton<ICredentialRepository>(provider =>
            {
                var logger = provider.GetRequiredService<IAppLogger>();
                return new CredentialRepository(logger);
            });

            // Register utility service WITH LOGGER
            services.AddSingleton<IPdfGenerationService>(provider =>
            {
                var logger = provider.GetRequiredService<IAppLogger>();
                return new PdfGenerationService(logger);
            });

            // Register PDF Processor WITH LOGGER
            services.AddSingleton<IPdfProcessor>(provider =>
            {
                var credentialRepo = provider.GetRequiredService<ICredentialRepository>();
                var pdfGeneration = provider.GetRequiredService<IPdfGenerationService>();
                var logger = provider.GetRequiredService<IAppLogger>();
                var errorHandlingService = provider.GetRequiredService<PdfErrorHandlingService>();
                
                return new PdfProcessor(credentialRepo, pdfGeneration, logger, errorHandlingService);
            });

            // Register error handling service
            services.AddSingleton<PdfErrorHandlingService>();

            // Register common services
            services.AddSingleton<IDialogService, DialogService>();

            // Register Application Services
            services.AddTransient<PdfProcessingService>(provider =>
            {
                var pdfProcessor = provider.GetRequiredService<IPdfProcessor>();
                var logger = provider.GetRequiredService<IAppLogger>();
                var auditLogger = provider.GetRequiredService<IAuditLogger>();
                return new PdfProcessingService(pdfProcessor, logger, auditLogger);
            });

            services.AddTransient<ApplicationStartupService>();

            services.AddTransient<ConfigurationLoadingService>();

            // Register UI Services
            services.AddSingleton<FileSelectionService>(); // UI Service

            services.AddSingleton<ProcessingStateManager>();

            // Register windows
            services.AddTransient<MainWindow>(provider =>
            {
                var pdfProcessingService = provider.GetRequiredService<PdfProcessingService>();

                var startupService = provider.GetRequiredService<ApplicationStartupService>();

                var configService = provider.GetRequiredService<ConfigurationLoadingService>();
                var fileSelectionService = provider.GetRequiredService<FileSelectionService>();
                var processingStateManager = provider.GetRequiredService<ProcessingStateManager>();
                var dialogService = provider.GetRequiredService<IDialogService>();
                var appLogger = provider.GetRequiredService<IAppLogger>();

                return new MainWindow(
                    pdfProcessingService,

                    startupService,

                    configService,
                    fileSelectionService,
                    processingStateManager,
                    dialogService,
                    appLogger,
                    provider);
            });

            services.AddTransient<SettingsWindow>(provider =>
            {
                var credentialRepo = provider.GetRequiredService<ICredentialRepository>();
                var pdfProcessor = provider.GetRequiredService<IPdfProcessor>();
                var logger = provider.GetRequiredService<IAppLogger>();
                return new SettingsWindow(credentialRepo, pdfProcessor, logger);
            });
        }

        /// <summary>
        /// Cleanup on application exit
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                var logger = _serviceProvider?.GetService<IAppLogger>();
                logger?.LogInformation("=== OCRBot Desktop Application Shutting Down ===");
            }
            catch
            {
                // Ignore logging errors during shutdown
            }

            Log.CloseAndFlush();
            _serviceProvider?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Global exception handler
        /// </summary>
        private void Application_DispatcherUnhandledException(object sender,
            System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                var logger = _serviceProvider?.GetService<IAppLogger>();
                logger?.LogError("Unhandled exception occurred", e.Exception);
            }
            catch
            {
                // If logging fails, at least show the error
            }

            CustomMessageBox.Show(
                $"An unexpected error occurred:\n\n{e.Exception.Message}\n\nPlease check the logs for more details.",
                AppConstants.UI.UnexpectedError,
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true;
        }
    }
}

