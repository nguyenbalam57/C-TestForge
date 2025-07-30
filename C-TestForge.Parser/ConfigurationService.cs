using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Implementation of the configuration service
    /// </summary>
    public class ConfigurationService : IConfigurationService
    {
        private readonly ILogger<ConfigurationService> _logger;
        private readonly IFileService _fileService;
        private Configuration _activeConfiguration;
        private readonly Dictionary<string, Configuration> _configurations;

        /// <summary>
        /// Constructor for ConfigurationService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="fileService">File service for reading/writing files</param>
        public ConfigurationService(ILogger<ConfigurationService> logger, IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _configurations = new Dictionary<string, Configuration>();
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

                string json = await _fileService.ReadFileAsync(filePath);
                var configuration = JsonSerializer.Deserialize<Configuration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (configuration == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize configuration from: {filePath}");
                }

                // Store the configuration in the dictionary
                if (!string.IsNullOrEmpty(configuration.Name))
                {
                    _configurations[configuration.Name] = configuration;
                }

                _logger.LogInformation($"Successfully loaded configuration: {configuration.Name}");

                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading configuration: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SaveConfigurationAsync(Configuration configuration, string filePath)
        {
            try
            {
                _logger.LogInformation($"Saving configuration to: {filePath}");

                if (configuration == null)
                {
                    throw new ArgumentNullException(nameof(configuration));
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                string json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await _fileService.WriteFileAsync(filePath, json);

                // Store the configuration in the dictionary
                if (!string.IsNullOrEmpty(configuration.Name))
                {
                    _configurations[configuration.Name] = configuration;
                }

                _logger.LogInformation($"Successfully saved configuration: {configuration.Name}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving configuration: {ex.Message}");
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

            _activeConfiguration = configuration;
            _logger.LogInformation($"Set active configuration to: {configuration.Name}");

            // Store the configuration in the dictionary if not already present
            if (!string.IsNullOrEmpty(configuration.Name) && !_configurations.ContainsKey(configuration.Name))
            {
                _configurations[configuration.Name] = configuration;
            }
        }

        /// <inheritdoc/>
        public ParseOptions CreateParseOptionsFromConfiguration(Configuration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            var parseOptions = new ParseOptions
            {
                AnalyzeFunctions = true,
                AnalyzeVariables = true,
                ParsePreprocessorDefinitions = true,
                IncludePaths = configuration.IncludePaths.ToList(),
                MacroDefinitions = configuration.MacroDefinitions.ToDictionary(kv => kv.Key, kv => kv.Value),
                AdditionalClangArguments = configuration.AdditionalArguments.ToList()
            };

            // Check if any properties affect parsing options
            if (configuration.Properties.TryGetValue("AnalyzeFunctions", out string analyzeFunctionsStr))
            {
                if (bool.TryParse(analyzeFunctionsStr, out bool analyzeFunctions))
                {
                    parseOptions.AnalyzeFunctions = analyzeFunctions;
                }
            }

            if (configuration.Properties.TryGetValue("AnalyzeVariables", out string analyzeVariablesStr))
            {
                if (bool.TryParse(analyzeVariablesStr, out bool analyzeVariables))
                {
                    parseOptions.AnalyzeVariables = analyzeVariables;
                }
            }

            if (configuration.Properties.TryGetValue("ParsePreprocessorDefinitions", out string parsePreprocessorStr))
            {
                if (bool.TryParse(parsePreprocessorStr, out bool parsePreprocessor))
                {
                    parseOptions.ParsePreprocessorDefinitions = parsePreprocessor;
                }
            }

            return parseOptions;
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
                    throw new FileNotFoundException($"Import file not found: {filePath}");
                }

                string json = await _fileService.ReadFileAsync(filePath);
                var configurations = JsonSerializer.Deserialize<List<Configuration>>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (configurations == null || configurations.Count == 0)
                {
                    throw new InvalidOperationException($"No configurations found in: {filePath}");
                }

                // Store the configurations in the dictionary
                foreach (var config in configurations)
                {
                    if (!string.IsNullOrEmpty(config.Name))
                    {
                        _configurations[config.Name] = config;
                    }
                }

                _logger.LogInformation($"Successfully imported {configurations.Count} configurations");

                return configurations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing configurations: {ex.Message}");
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
                    throw new ArgumentException("Configurations list cannot be null or empty", nameof(configurations));
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                string json = JsonSerializer.Serialize(configurations, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await _fileService.WriteFileAsync(filePath, json);

                _logger.LogInformation($"Successfully exported {configurations.Count} configurations");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error exporting configurations: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public Configuration CreateDefaultConfiguration()
        {
            var config = new Configuration
            {
                Name = "Default",
                Description = "Default configuration for C parsing",
                MacroDefinitions = new Dictionary<string, string>
                {
                    { "DEBUG", "1" }
                },
                IncludePaths = new List<string>
                {
                    "/usr/include",
                    "/usr/local/include"
                },
                AdditionalArguments = new List<string>
                {
                    "-std=c99"
                },
                Properties = new Dictionary<string, string>
                {
                    { "AnalyzeFunctions", "true" },
                    { "AnalyzeVariables", "true" },
                    { "ParsePreprocessorDefinitions", "true" }
                }
            };

            // Store the configuration in the dictionary
            _configurations[config.Name] = config;

            return config;
        }

        /// <inheritdoc/>
        public Configuration CloneConfiguration(Configuration source, string newName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var clone = source.Clone(newName);

            // Store the configuration in the dictionary
            if (!string.IsNullOrEmpty(clone.Name))
            {
                _configurations[clone.Name] = clone;
            }

            return clone;
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

            // Merge macro definitions (add any missing definitions from source)
            foreach (var define in source.MacroDefinitions)
            {
                if (!target.MacroDefinitions.ContainsKey(define.Key))
                {
                    target.MacroDefinitions[define.Key] = define.Value;
                }
            }

            // Merge include paths (add any missing paths from source)
            foreach (var path in source.IncludePaths)
            {
                if (!target.IncludePaths.Contains(path))
                {
                    target.IncludePaths.Add(path);
                }
            }

            // Merge additional arguments (add any missing arguments from source)
            foreach (var arg in source.AdditionalArguments)
            {
                if (!target.AdditionalArguments.Contains(arg))
                {
                    target.AdditionalArguments.Add(arg);
                }
            }

            // Merge properties (add any missing properties from source)
            foreach (var prop in source.Properties)
            {
                if (!target.Properties.ContainsKey(prop.Key))
                {
                    target.Properties[prop.Key] = prop.Value;
                }
            }

            // Optionally update description
            if (string.IsNullOrEmpty(target.Description) && !string.IsNullOrEmpty(source.Description))
            {
                target.Description = source.Description;
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, string>> GetConfigurationValuesAsync(string configName)
        {
            try
            {
                _logger.LogInformation($"Getting configuration values for: {configName}");

                if (string.IsNullOrEmpty(configName))
                {
                    throw new ArgumentException("Configuration name cannot be null or empty", nameof(configName));
                }

                // Check if the configuration exists in memory
                if (_configurations.TryGetValue(configName, out var config))
                {
                    return config.MacroDefinitions;
                }

                // Try to load the configuration from a standard location
                try
                {
                    string filePath = Path.Combine("Configurations", $"{configName}.json");
                    if (_fileService.FileExists(filePath))
                    {
                        var loadedConfig = await LoadConfigurationAsync(filePath);
                        return loadedConfig.MacroDefinitions;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not load configuration from standard location: {ex.Message}");
                }

                // Return empty dictionary if configuration not found
                _logger.LogWarning($"Configuration not found: {configName}");
                return new Dictionary<string, string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting configuration values: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> GetConfigurationNamesAsync()
        {
            try
            {
                _logger.LogInformation("Getting configuration names");

                // Add all configurations from memory
                var names = _configurations.Keys.ToList();

                // Try to load configurations from a standard location
                try
                {
                    string directoryPath = "Configurations";
                    if (_fileService.CreateDirectoryIfNotExists(directoryPath))
                    {
                        var files = _fileService.GetFilesInDirectory(directoryPath, ".json");
                        foreach (var file in files)
                        {
                            string configName = Path.GetFileNameWithoutExtension(file);
                            if (!names.Contains(configName) && !string.IsNullOrEmpty(configName))
                            {
                                names.Add(configName);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not load configurations from standard location: {ex.Message}");
                }

                await Task.CompletedTask;
                return names;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting configuration names: {ex.Message}");
                throw;
            }
        }
    }
}