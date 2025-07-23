using C_TestForge.TestCase.Repositories;
using C_TestForge.TestCase.Services;
using C_TestForge.UI.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;
using System;
using System.IO;
using System.Reflection;

namespace C_TestForge.TestCase
{
    public class TestCaseModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public TestCaseModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            _regionManager.RegisterViewWithRegion("TestCaseManagementRegion", typeof(TestCaseManagementView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register repositories
            containerRegistry.RegisterSingleton<ITestCaseRepository>(() =>
                new TestCaseRepository(
                    Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "C-TestForge",
                        "TestCases.db"),
                    null)); // Replace with proper mapper registration

            // Register services
            containerRegistry.RegisterSingleton<ITestCaseService, TestCaseService>();

            // Register dialogs
            containerRegistry.RegisterDialog<TestCaseEditorDialog, TestCaseEditorDialogViewModel>();
            containerRegistry.RegisterDialog<TestCaseComparisonDialog, TestCaseComparisonDialogViewModel>();
            containerRegistry.RegisterDialog<GenerateTestCaseDialog, GenerateTestCaseDialogViewModel>();
            containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();
        }
    }
}