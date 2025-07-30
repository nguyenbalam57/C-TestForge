using System;
using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Projects;
using C_TestForge.Parser;
using C_TestForge.UI.Services;
using C_TestForge.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C_TestForge.UI.Services
{
    /// <summary>
    /// Service registration for the application
    /// </summary>
    public static class ServiceRegistration
    {
        /// <summary>
        /// Registers all services for the application
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <returns>Service collection</returns>
        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            // Register logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Register core services
            services.AddSingleton<IFileService, FileService>();
            services.AddSingleton<ISourceCodeService, SourceCodeService>();

            // Register parser services
            services.AddSingleton<IParserService, ClangSharpParserService>();
            services.AddSingleton<IPreprocessorService, PreprocessorService>();

            // Register analysis services
            services.AddSingleton<IAnalysisService, AnalysisService>();
            services.AddSingleton<IFunctionAnalysisService, FunctionAnalysisService>();
            services.AddSingleton<IVariableAnalysisService, VariableAnalysisService>();
            services.AddSingleton<IMacroAnalysisService, MacroAnalysisService>();

            // Register ViewModels
            services.AddSingleton<SourceAnalysisViewModel>();

            return services;
        }
    }
}