using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace C_TestForge.Core.Services
{
    #region ConfigurationService Implementation

    /// <summary>
    /// Implementation of the configuration service
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly IFileService _fileService;
        private Configuration _activeConfiguration;

        public ConfigurationService(
            ILogger<ConfigurationService> logger,
            IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));

            // Create a default configuration
            _activeConfiguration = CreateDefaultConfiguration();
        }

        /// <inheritdoc/>
        public async Task<Configuration> LoadConfigurationAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"Loading configuration from: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!_fileService.FileExists(filePath))
                {
                    throw new FileNotFoundException($"Configuration file not found: {filePath}");
                }

                // Read the configuration file
                string json = await _fileService.ReadFileAsync(filePath);

                // Deserialize the configuration
                var configuration = JsonConvert.DeserializeObject<Configuration>(json);

                if (configuration == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize configuration: {filePath}");
                }

                _logger.LogInformation($"Successfully loaded configuration: {configuration.Name} from {filePath}");

                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading configuration: {filePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SaveConfigurationAsync(Configuration configuration, string filePath)
        {
            try
            {
                _logger.LogInformation($"Saving configuration: {configuration.Name} to {filePath}");

                if (configuration == null)
                {
                    throw new ArgumentNullException(nameof(configuration));
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                // Serialize the configuration
                string json = JsonConvert.SerializeObject(configuration, Formatting.Indented);

                // Write the configuration file
                await _fileService.WriteFileAsync(filePath, json);

                _logger.LogInformation($"Successfully saved configuration: {configuration.Name} to {filePath}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving configuration: {configuration.Name} to {filePath}");
                return false;
            }
        }

        /// <inheritdoc/>
        public Configuration GetActiveConfiguration()
        {
            return _activeConfiguration;
        }

        /// <inheritdoc/>
        public void SetActiveConfiguration(Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            _logger.LogInformation($"Setting active configuration to: {configuration.Name}");
            _activeConfiguration = configuration;
        }

        /// <inheritdoc/>
        public ParseOptions CreateParseOptionsFromConfiguration(Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            return new ParseOptions
            {
                IncludePaths = new List<string>(configuration.IncludePaths),
                MacroDefinitions = new Dictionary<string, string>(configuration.MacroDefinitions),
                AdditionalClangArguments = new List<string>(configuration.AdditionalArguments),
                ParsePreprocessorDefinitions = true,
                AnalyzeVariables = true,
                AnalyzeFunctions = true
            };
        }

        /// <inheritdoc/>
        public async Task<List<Configuration>> ImportConfigurationsAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"Importing configurations from: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!_fileService.FileExists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                // Read the file
                string json = await _fileService.ReadFileAsync(filePath);

                // Try to deserialize as a list of configurations
                var configurations = JsonConvert.DeserializeObject<List<Configuration>>(json);

                if (configurations == null || configurations.Count == 0)
                {
                    // Try to deserialize as a single configuration
                    var configuration = JsonConvert.DeserializeObject<Configuration>(json);

                    if (configuration == null)
                    {
                        throw new InvalidOperationException($"Failed to deserialize configurations: {filePath}");
                    }

                    configurations = new List<Configuration> { configuration };
                }

                _logger.LogInformation($"Successfully imported {configurations.Count} configurations from {filePath}");

                return configurations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing configurations: {filePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExportConfigurationsAsync(List<Configuration> configurations, string filePath)
        {
            try
            {
                _logger.LogInformation($"Exporting {configurations.Count} configurations to: {filePath}");

                if (configurations == null || configurations.Count == 0)
                {
                    throw new ArgumentException("Configurations cannot be null or empty", nameof(configurations));
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                // Serialize the configurations
                string json = JsonConvert.SerializeObject(configurations, Formatting.Indented);

                // Write the file
                await _fileService.WriteFileAsync(filePath, json);

                _logger.LogInformation($"Successfully exported {configurations.Count} configurations to {filePath}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error exporting configurations to: {filePath}");
                return false;
            }
        }

        /// <inheritdoc/>
        public Configuration CreateDefaultConfiguration()
        {
            return new Configuration
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Default",
                MacroDefinitions = new Dictionary<string, string>(),
                IncludePaths = new List<string>(),
                AdditionalArguments = new List<string>(),
                Description = "Default configuration",
                Properties = new Dictionary<string, string>
                {
                    { "CreatedBy", Environment.UserName },
                    { "CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            };
        }

        /// <inheritdoc/>
        public Configuration CloneConfiguration(Configuration source, string newName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException("New configuration name cannot be null or empty", nameof(newName));
            }

            return new Configuration
            {
                Id = Guid.NewGuid().ToString(),
                Name = newName,
                MacroDefinitions = new Dictionary<string, string>(source.MacroDefinitions),
                IncludePaths = new List<string>(source.IncludePaths),
                AdditionalArguments = new List<string>(source.AdditionalArguments),
                Description = $"Clone of {source.Name}",
                Properties = new Dictionary<string, string>
                {
                    { "ClonedFrom", source.Name },
                    { "CreatedBy", Environment.UserName },
                    { "CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            };
        }

        /// <inheritdoc/>
        public void MergeConfigurations(Configuration target, Configuration source)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            // Merge macro definitions
            foreach (var macro in source.MacroDefinitions)
            {
                target.MacroDefinitions[macro.Key] = macro.Value;
            }

            // Merge include paths (avoid duplicates)
            foreach (var includePath in source.IncludePaths)
            {
                if (!target.IncludePaths.Contains(includePath))
                {
                    target.IncludePaths.Add(includePath);
                }
            }

            // Merge additional arguments (avoid duplicates)
            foreach (var arg in source.AdditionalArguments)
            {
                if (!target.AdditionalArguments.Contains(arg))
                {
                    target.AdditionalArguments.Add(arg);
                }
            }

            // Update description
            target.Description = $"Merged: {target.Name} + {source.Name}";

            // Update properties
            target.Properties["MergedWith"] = source.Name;
            target.Properties["LastModifiedBy"] = Environment.UserName;
            target.Properties["LastModifiedDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    #endregion
}
