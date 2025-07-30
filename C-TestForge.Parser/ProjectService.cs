using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Implementation of the project service
    /// </summary>
    public class ProjectService : IProjectService
    {
        private readonly ILogger<ProjectService> _logger;
        private readonly IFileService _fileService;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly IConfigurationService _configurationService;
        private readonly IAnalysisService _analysisService;

        public ProjectService(
            ILogger<ProjectService> logger,
            IFileService fileService,
            ISourceCodeService sourceCodeService,
            IConfigurationService configurationService,
            IAnalysisService analysisService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
        }

        /// <inheritdoc/>
        public async Task<Project> CreateProjectAsync(string projectName, string projectPath)
        {
            try
            {
                _logger.LogInformation($"Creating project: {projectName} at {projectPath}");

                if (string.IsNullOrEmpty(projectName))
                {
                    throw new ArgumentException("Project name cannot be null or empty", nameof(projectName));
                }

                if (string.IsNullOrEmpty(projectPath))
                {
                    throw new ArgumentException("Project path cannot be null or empty", nameof(projectPath));
                }

                // Create the project file path
                string projectFilePath = Path.Combine(projectPath, $"{projectName}.ctproj");

                // Check if the project file already exists
                if (_fileService.FileExists(projectFilePath))
                {
                    throw new InvalidOperationException($"Project file already exists: {projectFilePath}");
                }

                // Create the project directory if it doesn't exist
                string projectDirectory = Path.GetDirectoryName(projectFilePath);
                if (!string.IsNullOrEmpty(projectDirectory) && !Directory.Exists(projectDirectory))
                {
                    _logger.LogDebug($"Creating project directory: {projectDirectory}");
                    Directory.CreateDirectory(projectDirectory);
                }

                // Create a default configuration
                var defaultConfig = _configurationService.CreateDefaultConfiguration();

                // Create the project
                var project = new Project
                {
                    Name = projectName,
                    ProjectFilePath = projectFilePath,
                    SourceFiles = new List<string>(),
                    IncludePaths = new List<string>(),
                    MacroDefinitions = new Dictionary<string, string>(),
                    Configurations = new List<Configuration> { defaultConfig },
                    ActiveConfigurationName = defaultConfig.Name,
                    LastModified = DateTime.Now,
                    Description = $"C-TestForge project: {projectName}",
                    Properties = new Dictionary<string, string>
                    {
                        { "CreatedBy", Environment.UserName },
                        { "CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                    }
                };

                // Save the project
                await SaveProjectAsync(project);

                _logger.LogInformation($"Successfully created project: {projectName} at {projectFilePath}");

                return project;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating project: {projectName}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Project> LoadProjectAsync(string projectPath)
        {
            try
            {
                _logger.LogInformation($"Loading project from: {projectPath}");

                if (string.IsNullOrEmpty(projectPath))
                {
                    throw new ArgumentException("Project path cannot be null or empty", nameof(projectPath));
                }

                if (!_fileService.FileExists(projectPath))
                {
                    throw new FileNotFoundException($"Project file not found: {projectPath}");
                }

                // Read the project file
                string json = await _fileService.ReadFileAsync(projectPath);

                // Deserialize the project
                var project = JsonConvert.DeserializeObject<Project>(json);

                if (project == null)
                {
                    throw new InvalidOperationException($"Failed to deserialize project: {projectPath}");
                }

                // Set the project file path
                project.ProjectFilePath = projectPath;

                // Ensure the active configuration exists
                if (string.IsNullOrEmpty(project.ActiveConfigurationName) ||
                    !project.Configurations.Any(c => c.Name == project.ActiveConfigurationName))
                {
                    // Set the first configuration as active
                    if (project.Configurations.Count > 0)
                    {
                        project.ActiveConfigurationName = project.Configurations[0].Name;
                    }
                    else
                    {
                        // Create a default configuration
                        var defaultConfig = _configurationService.CreateDefaultConfiguration();

                        project.Configurations.Add(defaultConfig);
                        project.ActiveConfigurationName = defaultConfig.Name;
                    }
                }

                // Set the active configuration in the configuration service
                var activeConfig = project.Configurations.First(c => c.Name == project.ActiveConfigurationName);
                _configurationService.SetActiveConfiguration(activeConfig);

                _logger.LogInformation($"Successfully loaded project: {project.Name} from {projectPath}");

                return project;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading project: {projectPath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SaveProjectAsync(Project project)
        {
            try
            {
                _logger.LogInformation($"Saving project: {project.Name} to {project.ProjectFilePath}");

                if (project == null)
                {
                    throw new ArgumentNullException(nameof(project));
                }

                if (string.IsNullOrEmpty(project.ProjectFilePath))
                {
                    throw new ArgumentException("Project file path cannot be null or empty");
                }

                // Update last modified time
                project.LastModified = DateTime.Now;

                // Update properties
                if (!project.Properties.ContainsKey("LastModifiedBy"))
                {
                    project.Properties["LastModifiedBy"] = Environment.UserName;
                }

                project.Properties["LastModifiedDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Serialize the project
                string json = JsonConvert.SerializeObject(project, Newtonsoft.Json.Formatting.Indented);

                // Write the project file
                await _fileService.WriteFileAsync(project.ProjectFilePath, json);

                _logger.LogInformation($"Successfully saved project: {project.Name} to {project.ProjectFilePath}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving project: {project.Name}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<SourceFile> AddSourceFileAsync(Project project, string filePath)
        {
            try
            {
                _logger.LogInformation($"Adding source file {filePath} to project: {project.Name}");

                if (project == null)
                {
                    throw new ArgumentNullException(nameof(project));
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!_fileService.FileExists(filePath))
                {
                    throw new FileNotFoundException($"Source file not found: {filePath}");
                }

                // Check if the file is already in the project
                if (project.SourceFiles.Contains(filePath))
                {
                    _logger.LogWarning($"Source file {filePath} is already in project: {project.Name}");
                    return await _sourceCodeService.LoadSourceFileAsync(filePath);
                }

                // Add the file to the project
                project.SourceFiles.Add(filePath);

                // Save the project
                await SaveProjectAsync(project);

                // Load and return the source file
                var sourceFile = await _sourceCodeService.LoadSourceFileAsync(filePath);

                _logger.LogInformation($"Successfully added source file {filePath} to project: {project.Name}");

                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding source file {filePath} to project: {project.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<SourceFile>> GetSourceFilesAsync(Project project)
        {
            try
            {
                _logger.LogInformation($"Getting source files for project: {project.Name}");

                if (project == null)
                {
                    throw new ArgumentNullException(nameof(project));
                }

                var sourceFiles = new List<SourceFile>();

                // Load each source file
                foreach (var filePath in project.SourceFiles)
                {
                    try
                    {
                        if (_fileService.FileExists(filePath))
                        {
                            var sourceFile = await _sourceCodeService.LoadSourceFileAsync(filePath);
                            sourceFiles.Add(sourceFile);
                        }
                        else
                        {
                            _logger.LogWarning($"Source file not found: {filePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error loading source file: {filePath}");
                        // Continue with other files
                    }
                }

                _logger.LogInformation($"Successfully loaded {sourceFiles.Count} source files for project: {project.Name}");

                return sourceFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting source files for project: {project.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveSourceFileAsync(Project project, string filePath)
        {
            try
            {
                _logger.LogInformation($"Removing source file {filePath} from project: {project.Name}");

                if (project == null)
                {
                    throw new ArgumentNullException(nameof(project));
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                // Check if the file is in the project
                if (!project.SourceFiles.Contains(filePath))
                {
                    _logger.LogWarning($"Source file {filePath} is not in project: {project.Name}");
                    return false;
                }

                // Remove the file from the project
                project.SourceFiles.Remove(filePath);

                // Save the project
                await SaveProjectAsync(project);

                _logger.LogInformation($"Successfully removed source file {filePath} from project: {project.Name}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing source file {filePath} from project: {project.Name}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<Configuration> AddConfigurationAsync(Project project, Configuration configuration)
        {
            try
            {
                _logger.LogInformation($"Adding configuration {configuration.Name} to project: {project.Name}");

                if (project == null)
                {
                    throw new ArgumentNullException(nameof(project));
                }

                if (configuration == null)
                {
                    throw new ArgumentNullException(nameof(configuration));
                }

                if (string.IsNullOrEmpty(configuration.Name))
                {
                    throw new ArgumentException("Configuration name cannot be null or empty");
                }

                // Check if a configuration with the same name already exists
                if (project.Configurations.Any(c => c.Name == configuration.Name))
                {
                    throw new InvalidOperationException($"Configuration {configuration.Name} already exists in project: {project.Name}");
                }

                // Add the configuration to the project
                project.Configurations.Add(configuration);

                // Save the project
                await SaveProjectAsync(project);

                _logger.LogInformation($"Successfully added configuration {configuration.Name} to project: {project.Name}");

                return configuration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding configuration {configuration?.Name} to project: {project.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> RemoveConfigurationAsync(Project project, string configurationName)
        {
            try
            {
                _logger.LogInformation($"Removing configuration {configurationName} from project: {project.Name}");

                if (project == null)
                {
                    throw new ArgumentNullException(nameof(project));
                }

                if (string.IsNullOrEmpty(configurationName))
                {
                    throw new ArgumentException("Configuration name cannot be null or empty", nameof(configurationName));
                }

                // Find the configuration
                var configuration = project.Configurations.FirstOrDefault(c => c.Name == configurationName);

                if (configuration == null)
                {
                    _logger.LogWarning($"Configuration {configurationName} not found in project: {project.Name}");
                    return false;
                }

                // Check if it's the active configuration
                if (project.ActiveConfigurationName == configurationName)
                {
                    // Set another configuration as active
                    var otherConfiguration = project.Configurations.FirstOrDefault(c => c.Name != configurationName);
                    if (otherConfiguration != null)
                    {
                        project.ActiveConfigurationName = otherConfiguration.Name;
                        _configurationService.SetActiveConfiguration(otherConfiguration);
                    }
                    else
                    {
                        // Create a default configuration
                        var defaultConfig = _configurationService.CreateDefaultConfiguration();

                        project.Configurations.Add(defaultConfig);
                        project.ActiveConfigurationName = defaultConfig.Name;
                        _configurationService.SetActiveConfiguration(defaultConfig);
                    }
                }

                // Remove the configuration
                project.Configurations.Remove(configuration);

                // Save the project
                await SaveProjectAsync(project);

                _logger.LogInformation($"Successfully removed configuration {configurationName} from project: {project.Name}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing configuration {configurationName} from project: {project.Name}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SetActiveConfigurationAsync(Project project, string configurationName)
        {
            try
            {
                _logger.LogInformation($"Setting active configuration to {configurationName} for project: {project.Name}");

                if (project == null)
                {
                    throw new ArgumentNullException(nameof(project));
                }

                if (string.IsNullOrEmpty(configurationName))
                {
                    throw new ArgumentException("Configuration name cannot be null or empty", nameof(configurationName));
                }

                // Find the configuration
                var configuration = project.Configurations.FirstOrDefault(c => c.Name == configurationName);

                if (configuration == null)
                {
                    _logger.LogWarning($"Configuration {configurationName} not found in project: {project.Name}");
                    return false;
                }

                // Set as active
                project.ActiveConfigurationName = configurationName;

                // Save the project
                await SaveProjectAsync(project);

                // Update the active configuration in the configuration service
                _configurationService.SetActiveConfiguration(configuration);

                _logger.LogInformation($"Successfully set active configuration to {configurationName} for project: {project.Name}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error setting active configuration to {configurationName} for project: {project.Name}");
                return false;
            }
        }

        /// <inheritdoc/>
        public Configuration GetActiveConfiguration(Project project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            // Find the active configuration
            var configuration = project.Configurations.FirstOrDefault(c => c.Name == project.ActiveConfigurationName);

            if (configuration == null && project.Configurations.Count > 0)
            {
                // Use the first configuration if active not found
                configuration = project.Configurations[0];
                project.ActiveConfigurationName = configuration.Name;
            }
            else if (configuration == null)
            {
                // Create a default configuration if no configurations exist
                var defaultConfig = _configurationService.CreateDefaultConfiguration();
                project.Configurations.Add(defaultConfig);
                project.ActiveConfigurationName = defaultConfig.Name;
                configuration = defaultConfig;
            }

            return configuration;
        }

        /// <inheritdoc/>
        public async Task<AnalysisResult> AnalyzeProjectAsync(Project project, AnalysisOptions options)
        {
            try
            {
                _logger.LogInformation($"Analyzing project: {project.Name}");

                if (project == null)
                {
                    throw new ArgumentNullException(nameof(project));
                }

                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                // Use the analysis service to analyze the project
                var result = await _analysisService.AnalyzeProjectAsync(project, options);

                _logger.LogInformation($"Successfully analyzed project: {project.Name}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing project: {project.Name}");
                throw;
            }
        }
    }
}
