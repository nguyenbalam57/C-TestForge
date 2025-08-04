using C_TestForge.Core.Extensions;
using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.Projects;
using C_TestForge.Core.Interfaces.Solver;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Core.Interfaces.UI;
using C_TestForge.Core.Logging;
using C_TestForge.Infrastructure;
using C_TestForge.Parser;
using C_TestForge.Parser.Analysis;
using C_TestForge.Parser.Projects;
using C_TestForge.Parser.TestCaseManagement;
using C_TestForge.Parser.UI;
using C_TestForge.SolverServices;
using C_TestForge.UI.Controls;
using C_TestForge.UI.Services;
using C_TestForge.UI.ViewModels;
using C_TestForge.UI.Views;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System;
using System.IO;
using System.Reflection;
using System.Windows;
using static Microsoft.WindowsAPICodePack.Shell.PropertySystem.SystemProperties.System;

namespace C_TestForge.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        /// <summary>
        /// Creates the shell window of the application
        /// </summary>
        /// <returns>The main window of the application</returns>
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        /// <summary>
        /// Registers types with the container
        /// </summary>
        /// <param name="containerRegistry">The container registry</param>
        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Configure and register logging services
            containerRegistry.AddLogging();

            // Register generic logger
            containerRegistry.RegisterGenericLogger();

            // Đăng ký TypeManager
            containerRegistry.RegisterSingleton<ITypeManager, TypeManager>();

            // Register core services
            containerRegistry.RegisterSingleton<IFileService, FileService>();
            containerRegistry.RegisterSingleton<IConfigurationService, ConfigurationService>();

            // Register parser services
            containerRegistry.RegisterSingleton<ISourceCodeService, SourceCodeService>();
            containerRegistry.RegisterSingleton<IPreprocessorService, PreprocessorService>();
            containerRegistry.RegisterSingleton<IParserService, ClangSharpParserService>();
            containerRegistry.RegisterSingleton<IClangSharpParserService, ClangSharpParserService>();

            // Register analysis services
            containerRegistry.RegisterSingleton<IFunctionAnalysisService, FunctionAnalysisService>();
            containerRegistry.RegisterSingleton<IVariableAnalysisService, VariableAnalysisService>();
            containerRegistry.RegisterSingleton<IMacroAnalysisService, MacroAnalysisService>();
            containerRegistry.RegisterSingleton<IAnalysisService, AnalysisService>();

            // Register solver services
            containerRegistry.RegisterSingleton<IZ3SolverService, Z3SolverService>();

            // Register project services
            containerRegistry.RegisterSingleton<IProjectService, ProjectService>();
            containerRegistry.RegisterSingleton<ISourceFileService, SourceFileService>();

            // Register test case services
            containerRegistry.RegisterSingleton<ITestCaseService, TestCaseService>();
            containerRegistry.RegisterSingleton<IUnitTestGeneratorService, UnitTestGeneratorService>();
            containerRegistry.RegisterSingleton<IIntegrationTestGeneratorService, IntegrationTestGeneratorService>();
            containerRegistry.RegisterSingleton<ITestCodeGeneratorService, TestCodeGeneratorService>();

            // Register dialogs
            containerRegistry.RegisterDialog<TestCaseEditorDialog, TestCaseEditorDialogViewModel>();
            containerRegistry.RegisterDialog<TestCaseComparisonDialog, TestCaseComparisonDialogViewModel>();
            containerRegistry.RegisterDialog<GenerateTestCaseDialog, GenerateTestCaseDialogViewModel>();
            containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();

            // Register ViewModels
            containerRegistry.RegisterDialog<TypeMappingManagerViewModel>();
            containerRegistry.RegisterSingleton<IDialogService, DialogService>();
            // Đăng ký SnackbarMessageQueue
            containerRegistry.RegisterSingleton<SnackbarMessageQueue>(provider =>
            {
                return new SnackbarMessageQueue(TimeSpan.FromSeconds(3));
            });

            // Đăng ký ISnackbarMessageQueue
            containerRegistry.RegisterSingleton<Core.Interfaces.UI.ISnackbarMessageQueue>(provider =>
            {
                var messageQueue = provider.Resolve<SnackbarMessageQueue>();
                return new SnackbarMessageQueueAdapter(messageQueue);
            });

            // Đăng ký IDialogService
            containerRegistry.RegisterSingleton<IDialogService, MaterialDialogService>();

            // Register views
            //containerRegistry.RegisterForNavigation<SourceCodeView, SourceCodeViewModel>();
            //containerRegistry.RegisterForNavigation<TestCaseView, TestCaseViewModel>();
            //containerRegistry.RegisterForNavigation<TestGenerationView, TestGenerationViewModel>();
            //containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
        }

        /// <summary>
        /// Configure the module catalog
        /// </summary>
        /// <param name="moduleCatalog">The module catalog</param>
        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            // Register Infrastructure Module first
            moduleCatalog.AddModule<InfrastructureModule>();

            // Register UI Module next
            moduleCatalog.AddModule<UIModule>();

            // Register other modules
            // You can uncomment these when these modules are implemented
            //moduleCatalog.AddModule<ParserModule>();
            //moduleCatalog.AddModule<TestCaseModule>();
        }

        /// <summary>
        /// Application startup event
        /// </summary>
        protected override void OnInitialized()
        {
            // Đăng ký encoding provider
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            base.OnInitialized();

            // Set up exception handling
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                LogUnhandledException((Exception)ex.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            // Create application data directory if it doesn't exist
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "C-TestForge");

            // Khởi tạo TypeManager
            var typeManager = Container.Resolve<ITypeManager>();
            InitializeTypeManagerAsync(typeManager);

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }
        }

        private async void InitializeTypeManagerAsync(ITypeManager typeManager)
        {
            await typeManager.InitializeAsync();
            // Có thể thực hiện các thao tác bổ sung sau khi TypeManager được khởi tạo
        }

        /// <summary>
        /// Logs an unhandled exception
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="source">Source of the exception</param>
        private void LogUnhandledException(Exception exception, string source)
        {
            // Get the logger
            var logger = Container.Resolve<Microsoft.Extensions.Logging.ILogger>();
            logger?.LogError(exception, $"Unhandled exception from {source}");

            // Show error message to user
            MessageBox.Show($"An unhandled exception occurred: {exception.Message}\r\n\r\nSource: {source}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Application exit event
        /// </summary>
        /// <param name="e">Exit event arguments</param>
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Flush and close the logger
            Serilog.Log.CloseAndFlush();
        }
    }
}