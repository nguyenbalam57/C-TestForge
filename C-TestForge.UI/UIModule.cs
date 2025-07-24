using C_TestForge.Infrastructure.Views;
using C_TestForge.Infrastructure.ViewModels;
using C_TestForge.UI.Dialogs;
using C_TestForge.UI.ViewModels;
using C_TestForge.UI.Views;
using Prism.Ioc;
using Prism.Modularity;

namespace C_TestForge.UI
{
    public class UIModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            // Không cần làm gì ở đây
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register views
            containerRegistry.Register<ITestCaseManagementView, TestCaseManagementView>();

            // Register dialogs
            containerRegistry.RegisterDialog<TestCaseEditorDialog>();
            containerRegistry.RegisterDialog<TestCaseComparisonDialog>();
            containerRegistry.RegisterDialog<GenerateTestCaseDialog>();
            containerRegistry.RegisterDialog<ConfirmationDialog>();

            // Register view models
            containerRegistry.Register<ITestCaseEditorDialogViewModel, EditTestCaseDialogViewModel>();
            containerRegistry.Register<ITestCaseComparisonDialogViewModel, TestCaseComparisonDialogViewModel>();
            containerRegistry.Register<IGenerateTestCaseDialogViewModel, GenerateTestCaseDialogViewModel>();
            containerRegistry.Register<IConfirmationDialogViewModel, ConfirmationDialogViewModel>();
        }
    }
}
