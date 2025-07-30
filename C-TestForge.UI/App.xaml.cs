
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.Solver;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Infrastructure;
using C_TestForge.Parser;
using C_TestForge.UI.Controls;
using C_TestForge.UI.Services;
using C_TestForge.UI.ViewModels;
using C_TestForge.UI.Views;
using Microsoft.Extensions.Logging;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System;
using System.IO;
using System.Reflection;
using System.Windows;

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
            // Register logger
            containerRegistry.RegisterSingleton<ILogger>(() =>
                LoggerFactory.Create(builder =>
                    builder.AddConsole().AddDebug()).CreateLogger("C-TestForge"));

            // Register TestCase services
            //containerRegistry.RegisterSingleton<IMapper>(() =>
            //{
            //    var config = new MapperConfiguration(cfg =>
            //    {
            //        // Configure AutoMapper mappings here
            //        // Add specific mappings as needed for your models
            //    });

            //    return config.CreateMapper();
            //});

            // Register repositories
            //containerRegistry.RegisterSingleton<ITestCaseRepository>(() =>
            //    new TestCaseRepository(
            //        Path.Combine(
            //            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            //            "C-TestForge",
            //            "TestCases.db"),
            //        Container.Resolve<IMapper>()));

            // Register services
            //containerRegistry.RegisterSingleton<ITestCaseService, TestCaseService>();
            //containerRegistry.RegisterSingleton<IClangParserService, ClangParserService>();
            //containerRegistry.RegisterSingleton<IZ3SolverService, Z3SolverService>();
            //containerRegistry.RegisterSingleton<IFileService, FileService>();

            // Register dialogs
            containerRegistry.RegisterDialog<TestCaseEditorDialog, TestCaseEditorDialogViewModel>();
            containerRegistry.RegisterDialog<TestCaseComparisonDialog, TestCaseComparisonDialogViewModel>();
            containerRegistry.RegisterDialog<GenerateTestCaseDialog, GenerateTestCaseDialogViewModel>();
            containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();

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
            //moduleCatalog.AddModule<ParserModule>();
            //moduleCatalog.AddModule<TestCaseModule>();
        }

        /// <summary>
        /// Application startup event
        /// </summary>
        /// <param name="e">Startup event arguments</param>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Set up exception handling
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
                LogUnhandledException((Exception)ex.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            // Create application data directory if it doesn't exist
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "C-TestForge");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

        }

        /// <summary>
        /// Logs an unhandled exception
        /// </summary>
        /// <param name="exception">The exception that occurred</param>
        /// <param name="source">Source of the exception</param>
        private void LogUnhandledException(Exception exception, string source)
        {
            // Get the logger
            var logger = Container.Resolve<ILogger>();
            logger?.LogError(exception, $"Unhandled exception from {source}");

            // Show error message to user
            MessageBox.Show($"An unhandled exception occurred: {exception.Message}\r\n\r\nSource: {source}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}