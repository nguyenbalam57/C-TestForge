using C_TestForge.Models;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.ProjectManagement
{
    /// <summary>
    /// Interface for managing configurations
    /// </summary>
    public interface IConfigurationService
    {
        /// <summary>
        /// Loads configuration from a file
        /// </summary>
        /// <param name="filePath">Path to the configuration file</param>
        /// <returns>Configuration object</returns>
        Task<Configuration> LoadConfigurationAsync(string filePath);

        /// <summary>
        /// Saves configuration to a file
        /// </summary>
        /// <param name="configuration">Configuration to save</param>
        /// <param name="filePath">Path to the configuration file</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveConfigurationAsync(Configuration configuration, string filePath);

        /// <summary>
        /// Gets the active configuration
        /// </summary>
        /// <returns>Active configuration</returns>
        Configuration GetActiveConfiguration();

        /// <summary>
        /// Sets the active configuration
        /// </summary>
        /// <param name="configuration">Configuration to set as active</param>
        void SetActiveConfiguration(Configuration configuration);

        /// <summary>
        /// Creates parse options from a configuration
        /// </summary>
        /// <param name="configuration">Configuration to use</param>
        /// <returns>Parse options</returns>
        ParseOptions CreateParseOptionsFromConfiguration(Configuration configuration);

        /// <summary>
        /// Imports configurations from a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>List of imported configurations</returns>
        Task<List<Configuration>> ImportConfigurationsAsync(string filePath);

        /// <summary>
        /// Exports configurations to a file
        /// </summary>
        /// <param name="configurations">Configurations to export</param>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> ExportConfigurationsAsync(List<Configuration> configurations, string filePath);

        /// <summary>
        /// Creates a default configuration
        /// </summary>
        /// <returns>Default configuration</returns>
        Configuration CreateDefaultConfiguration();

        /// <summary>
        /// Clones a configuration
        /// </summary>
        /// <param name="source">Source configuration</param>
        /// <param name="newName">Name for the new configuration</param>
        /// <returns>Cloned configuration</returns>
        Configuration CloneConfiguration(Configuration source, string newName);

        /// <summary>
        /// Merges two configurations
        /// </summary>
        /// <param name="target">Target configuration</param>
        /// <param name="source">Source configuration</param>
        void MergeConfigurations(Configuration target, Configuration source);

        /// <summary>
        /// Gets a dictionary of macro values for a specific configuration
        /// </summary>
        /// <param name="configName">Name of the configuration</param>
        /// <returns>Dictionary of macro name to value</returns>
        Task<Dictionary<string, string>> GetConfigurationValuesAsync(string configName);

        /// <summary>
        /// Gets all available configuration names
        /// </summary>
        /// <returns>List of configuration names</returns>
        Task<List<string>> GetConfigurationNamesAsync();
    }
}