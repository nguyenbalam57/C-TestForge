using C_TestForge.Infrastructure.Views;
using C_TestForge.Infrastructure.ViewModels;
using C_TestForge.UI.Dialogs;
using C_TestForge.UI.ViewModels;
using C_TestForge.UI.Views;
using Prism.Ioc;
using Prism.Modularity;
using Prism.Regions;

namespace C_TestForge.UI
{
    public class UIModule : IModule
    {
        private readonly IRegionManager _regionManager;

        public UIModule(IRegionManager regionManager)
        {
            _regionManager = regionManager;
        }

        public void OnInitialized(IContainerProvider containerProvider)
        {
            // Đăng ký các view với Region Manager
            _regionManager.RegisterViewWithRegion("MainRegion", typeof(DashboardView));
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            // Register new views for navigation
            containerRegistry.RegisterForNavigation<DashboardView>("DashboardView");
            containerRegistry.RegisterForNavigation<ProjectExplorerView>("ProjectExplorerView");
            containerRegistry.RegisterForNavigation<SourceAnalysisView>("SourceAnalysisView");
            containerRegistry.RegisterForNavigation<TestCasesView>("TestCasesView");
            containerRegistry.RegisterForNavigation<AboutView>("AboutView");

            // Register old views
            containerRegistry.Register<ITestCaseManagementView, TestCaseManagementView>();
            containerRegistry.Register<IDashboardView, DashboardView>();
            containerRegistry.Register<IProjectExplorerView, ProjectExplorerView>();
            containerRegistry.Register<ISourceAnalysisView, SourceAnalysisView>();
            containerRegistry.Register<ITestCasesView, TestCasesView>();
            containerRegistry.Register<IAboutView, AboutView>();

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
