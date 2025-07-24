using C_TestForge.Infrastructure.Views;
using C_TestForge.TestCase.Repositories;
using C_TestForge.TestCase.Services;
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
            _regionManager.RegisterViewWithRegion("TestCaseManagementRegion", typeof(ITestCaseManagementView));
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
                    null));

            // Register services
            containerRegistry.RegisterSingleton<ITestCaseService, TestCaseService>();
        }
    }
}