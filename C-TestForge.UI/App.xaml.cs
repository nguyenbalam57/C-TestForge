using AutoMapper;
using C_TestForge.Infrastructure;
using C_TestForge.Parser;
using C_TestForge.TestCase;
using C_TestForge.TestCase.Repositories;
using C_TestForge.TestCase.Services;
using C_TestForge.UI.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System;
using System.IO;
using System.Windows;


namespace C_TestForge.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : PrismApplication
{
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Register services from previous stages

        // Register TestCase services
        containerRegistry.RegisterSingleton<IMapper>(() =>
        {
            var config = new MapperConfiguration(cfg =>
            {
                // Configure AutoMapper mappings here
            });

            return config.CreateMapper();
        });

        containerRegistry.RegisterSingleton<ITestCaseRepository>(() =>
            new TestCaseRepository(
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "C-TestForge",
                    "TestCases.db"),
                Container.Resolve<IMapper>()));

        containerRegistry.RegisterSingleton<ITestCaseService, TestCaseService>();

        // Register dialogs
        containerRegistry.RegisterDialog<TestCaseEditorDialog, ViewModels.TestCaseEditorDialogViewModel>();
        containerRegistry.RegisterDialog<TestCaseComparisonDialog, ViewModels.TestCaseComparisonDialogViewModel>();
        containerRegistry.RegisterDialog<GenerateTestCaseDialog, ViewModels.GenerateTestCaseDialogViewModel>();
        containerRegistry.RegisterDialog<ConfirmationDialog, ViewModels.ConfirmationDialogViewModel>();
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // Đăng ký Infrastructure Module trước
        moduleCatalog.AddModule<InfrastructureModule>();

        // Đăng ký UI Module tiếp theo
        moduleCatalog.AddModule<UIModule>();

        // Đăng ký các module khác
        //moduleCatalog.AddModule<ParserModule>();
        moduleCatalog.AddModule<TestCaseModule>();
    }
}


