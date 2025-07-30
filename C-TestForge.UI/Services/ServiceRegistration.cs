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

    /// <summary>
    /// Simple file service implementation
    /// </summary>
    public class FileService : IFileService
    {
        /// <summary>
        /// Checks if a file exists
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if the file exists, false otherwise</returns>
        public bool FileExists(string filePath)
        {
            return System.IO.File.Exists(filePath);
        }

        /// <summary>
        /// Gets the file name from a path
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>File name</returns>
        public string GetFileName(string filePath)
        {
            return System.IO.Path.GetFileName(filePath);
        }

        /// <summary>
        /// Reads a file asynchronously
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>File content</returns>
        public async Task<string> ReadFileAsync(string filePath)
        {
            return await System.IO.File.ReadAllTextAsync(filePath);
        }

        /// <summary>
        /// Writes a file asynchronously
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="content">Content to write</param>
        /// <returns>Task</returns>
        public async Task WriteFileAsync(string filePath, string content)
        {
            await System.IO.File.WriteAllTextAsync(filePath, content);
        }

        /// <summary>
        /// Gets the directory name from a path
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Directory name</returns>
        public string GetDirectoryName(string filePath)
        {
            return System.IO.Path.GetDirectoryName(filePath);
        }
    }
}

namespace C_TestForge.Core.Interfaces.ProjectManagement
{
    /// <summary>
    /// Interface for file operations
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Checks if a file exists
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if the file exists, false otherwise</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// Gets the file name from a path
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>File name</returns>
        string GetFileName(string filePath);

        /// <summary>
        /// Gets the directory name from a path
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Directory name</returns>
        string GetDirectoryName(string filePath);

        /// <summary>
        /// Reads a file asynchronously
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>File content</returns>
        Task<string> ReadFileAsync(string filePath);

        /// <summary>
        /// Writes a file asynchronously
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="content">Content to write</param>
        /// <returns>Task</returns>
        Task WriteFileAsync(string filePath, string content);
    }

}