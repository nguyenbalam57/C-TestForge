using C_TestForge.Core.Interfaces;
using C_TestForge.Core.Services;
using C_TestForge.Parser;
using C_TestForge.Solver;
using C_TestForge.TestCase.Repositories;
using C_TestForge.TestCase.Services;
using Microsoft.Extensions.DependencyInjection;
using Prism.Services.Dialogs;
using System;

namespace C_TestForge.UI.Services
{
    /// <summary>
    /// Provides extension methods for registering C-TestForge services with the dependency injection container
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers services for Phase 1 (Core Parsing and Analysis)
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <returns>The service collection for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if services is null</exception>
        public static IServiceCollection RegisterPhase1Services(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register parser services
            services.AddSingleton<IParserService, ClangSharpParserService>();
            services.AddSingleton<IPreprocessorService, PreprocessorService>();
            services.AddSingleton<ISourceCodeService, SourceCodeService>();

            // Register project management services
            services.AddSingleton<IProjectService, ProjectService>();
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<IConfigurationService, ConfigurationService>();

            // Register analysis services
            services.AddSingleton<IAnalysisService, AnalysisService>();
            services.AddSingleton<IFunctionAnalysisService, FunctionAnalysisService>();
            services.AddSingleton<IVariableAnalysisService, VariableAnalysisService>();
            services.AddSingleton<IMacroAnalysisService, MacroAnalysisService>();

            return services;
        }

        /// <summary>
        /// Registers services for Phase 2 (TestCase Management)
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <returns>The service collection for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if services is null</exception>
        public static IServiceCollection RegisterPhase2Services(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register TestCase management services
            services.AddSingleton<ITestCaseService, TestCaseService>();
            services.AddSingleton<ITestCaseRepository, TestCaseRepository>();
            services.AddSingleton<ITestCaseComparisonService, TestCaseComparisonService>();

            // Register import/export services
            services.AddSingleton<IImportService, ImportService>();
            services.AddSingleton<IExportService, ExportService>();
            services.AddSingleton<IExcelService, ExcelService>();
            services.AddSingleton<ICsvService, CsvService>();

            // Register UI related services
            services.AddSingleton<ITestCaseViewModelFactory, TestCaseViewModelFactory>();
            services.AddSingleton<ISourceCodeHighlightService, SourceCodeHighlightService>();

            return services;
        }

        /// <summary>
        /// Registers services for Phase 3 (Advanced Test Generation)
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <returns>The service collection for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if services is null</exception>
        public static IServiceCollection RegisterPhase3Services(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register Z3 Solver services
            services.AddSingleton<IZ3SolverService, Z3SolverService>();
            services.AddSingleton<IVariableValueFinderService, VariableValueFinderService>();

            // Register Test Generation services
            services.AddSingleton<IStubGeneratorService, StubGeneratorService>();
            services.AddSingleton<IUnitTestGeneratorService, UnitTestGeneratorService>();
            services.AddSingleton<IIntegrationTestGeneratorService, IntegrationTestGeneratorService>();
            services.AddSingleton<ITestCodeGeneratorService, TestCodeGeneratorService>();

            // Register advanced analysis services
            services.AddSingleton<IBranchAnalysisService, BranchAnalysisService>();
            services.AddSingleton<IFunctionCallGraphService, FunctionCallGraphService>();
            services.AddSingleton<ICodeCoverageService, CodeCoverageService>();

            return services;
        }

        /// <summary>
        /// Registers all services from all phases
        /// </summary>
        /// <param name="services">The service collection to add services to</param>
        /// <returns>The service collection for chaining</returns>
        /// <exception cref="ArgumentNullException">Thrown if services is null</exception>
        public static IServiceCollection RegisterAllServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register Phase 1 services
            services.RegisterPhase1Services();

            // Register Phase 2 services
            services.RegisterPhase2Services();

            // Register Phase 3 services
            services.RegisterPhase3Services();

            // Register common services used across all phases
            services.AddSingleton<ILogService, LogService>();
            services.AddSingleton<IErrorHandlingService, ErrorHandlingService>();
            services.AddSingleton<IUserSettingsService, UserSettingsService>();

            return services;
        }
    }
}