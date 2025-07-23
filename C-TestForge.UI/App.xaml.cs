using C_TestForge.Core;
using C_TestForge.Core.Services;
using C_TestForge.UI.ViewModels;
using Microsoft.Extensions.Logging;
using System.Configuration;
using System.Data;
using System.Windows;

namespace C_TestForge.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure services
        AppBootstrapper.ConfigureServices();

        // Create main window
        var projectService = AppBootstrapper.GetService<IProjectService>();
        var testCaseService = AppBootstrapper.GetService<ITestCaseService>();
        var logger = AppBootstrapper.GetService<ILogger<MainWindowViewModel>>();

        var mainViewModel = new MainWindowViewModel(projectService, testCaseService, logger);
        var mainWindow = new MainWindow
        {
            DataContext = mainViewModel
        };

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Clean up
        AppBootstrapper.Shutdown();

        base.OnExit(e);
    }
}

