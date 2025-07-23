using AutoMapper;
using C_TestForge.Parser;
using C_TestForge.TestCase;
using C_TestForge.TestCase.Repositories;
using C_TestForge.TestCase.Services;
using C_TestForge.UI.ViewModels;
using C_TestForge.UI.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace C_TestForge.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
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
        containerRegistry.RegisterDialog<TestCaseEditorDialog, TestCaseEditorDialogViewModel>();
        containerRegistry.RegisterDialog<TestCaseComparisonDialog, TestCaseComparisonDialogViewModel>();
        containerRegistry.RegisterDialog<GenerateTestCaseDialog, GenerateTestCaseDialogViewModel>();
        containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // Register the Parser module from stage 1
        moduleCatalog.AddModule<ParserModule>();

        // Register the TestCase module from stage 2
        moduleCatalog.AddModule<TestCaseModule>();
    }
}


