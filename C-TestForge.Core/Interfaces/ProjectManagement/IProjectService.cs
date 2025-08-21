using C_TestForge.Models;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.ProjectManagement
{
    /// <summary>
    /// Interface for managing projects
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// Creates a new project
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <param name="projectPath">Path to the project</param>
        /// <returns>The created project</returns>
        Task<Project> CreateProjectAsync(
            string projectName, 
            string projectDescription, 
            string projectPath,
            List<string>? macros = null,
            List<string>? includePaths = null,
            List<string>? cFiles = null);

        /// <summary>
        /// Chỉnh sửa một project
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <param name="projectPath">Path to the project</param>
        /// <returns>The created project</returns>
        Task<Project> EditProjectAsync(
            Project currentProject,
            string projectName,
            string projectDescription,
            string projectPath,
            List<string>? macros = null,
            List<string>? includePaths = null,
            List<string>? cFiles = null);

        /// <summary>
        /// Loads a project from disk
        /// </summary>
        /// <param name="projectPath">Path to the project file</param>
        /// <returns>The loaded project</returns>
        Task<Project> LoadProjectAsync(string projectPath);

        /// <summary>
        /// Saves a project to disk
        /// </summary>
        /// <param name="project">Project to save</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveProjectAsync(Project project);

        /// <summary>
        /// Adds a source file to a project
        /// </summary>
        /// <param name="project">Project to add to</param>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>The added source file</returns>
        Task<SourceFile> AddSourceFileAsync(Project project, string filePath);

        /// <summary>
        /// Gets all source files in a project
        /// </summary>
        /// <param name="project">Project to get files from</param>
        /// <returns>List of source files</returns>
        Task<List<SourceFile>> GetSourceFilesAsync(Project project);

        /// <summary>
        /// Removes a source file from a project
        /// </summary>
        /// <param name="project">Project to remove from</param>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RemoveSourceFileAsync(Project project, string filePath);

        /// <summary>
        /// Adds a configuration to a project
        /// </summary>
        /// <param name="project">Project to add to</param>
        /// <param name="configuration">Configuration to add</param>
        /// <returns>The added configuration</returns>
        Task<Configuration> AddConfigurationAsync(Project project, Configuration configuration);

        /// <summary>
        /// Removes a configuration from a project
        /// </summary>
        /// <param name="project">Project to remove from</param>
        /// <param name="configurationName">Name of the configuration to remove</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> RemoveConfigurationAsync(Project project, string configurationName);

        /// <summary>
        /// Sets the active configuration for a project
        /// </summary>
        /// <param name="project">Project to update</param>
        /// <param name="configurationName">Name of the configuration to set as active</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SetActiveConfigurationAsync(Project project, string configurationName);

        /// <summary>
        /// Gets the active configuration for a project
        /// </summary>
        /// <param name="project">Project to get configuration for</param>
        /// <returns>The active configuration</returns>
        Configuration GetActiveConfiguration(Project project);

    }
}
