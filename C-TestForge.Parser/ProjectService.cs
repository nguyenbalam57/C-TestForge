using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.CodeAnalysis;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using C_TestForge.Parser.Helpers;
using ClangSharp;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

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

        public ProjectService(
            ILogger<ProjectService> logger,
            IFileService fileService,
            ISourceCodeService sourceCodeService,
            IConfigurationService configurationService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        }

        /// <inheritdoc/>
        public async Task<Project> CreateProjectAsync(
            string projectName,
            string projectDescription,
            string projectPath,
            List<string>? macros = null,
            List<string>? includePaths = null,
            List<string>? cFiles = null)
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
                //var defaultConfig = _configurationService.CreateDefaultConfiguration();

                Configuration configuration = new Configuration
                {
                    Name = $"{projectName}_config",
                    MacroDefinitions = (macros != null && macros.Count > 0) ? macros : new List<string>(),
                    IncludePaths = includePaths != null ? new List<string>(includePaths) : new List<string>(),
                    Description = $"Config {projectName}",
                    Properties = new Dictionary<string, string> { 
                        { "CreatedBy", Environment.UserName },
                        { "CreatedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") } },

                };

                 // Create the project
                 var project = new Project
                {
                    Name = projectName,
                    ProjectFilePath = projectFilePath,
                    SourceFiles = cFiles != null ? cFiles : new List<string>(),
                    Configurations = new List<Configuration> { configuration },
                    ActiveConfigurationName = configuration.Name,
                    LastModified = DateTime.Now,
                    Description = projectDescription,
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
        public async Task<Project> EditProjectAsync(
    Project currentProject,
    string projectName,
    string projectDescription,
    string projectPath,
    List<string>? macros = null,
    List<string>? includePaths = null,
    List<string>? cFiles = null)
        {
            try
            {
                _logger.LogInformation($"Editing project: {projectName} at {projectPath}");

                if (string.IsNullOrEmpty(projectName))
                    throw new ArgumentException("Project name cannot be null or empty", nameof(projectName));
                if (string.IsNullOrEmpty(projectPath))
                    throw new ArgumentException("Project path cannot be null or empty", nameof(projectPath));

                // Đường dẫn file project cũ và mới
                string oldProjectFilePath = currentProject.ProjectFilePath;
                string newProjectFilePath = Path.Combine(projectPath, $"{projectName}.ctproj");

                // Đọc project cũ từ file (nếu tồn tại)
                Project? project = null;
                if (_fileService.FileExists(oldProjectFilePath))
                {
                    string json = await _fileService.ReadFileAsync(oldProjectFilePath);
                    project = JsonConvert.DeserializeObject<Project>(json);
                }
                // Nếu không đọc được thì dùng currentProject (trong bộ nhớ)
                if (project == null)
                {
                    project = currentProject.Clone();
                }

                // So sánh tên dự án và tên file
                bool isNameChanged = !string.Equals(project.Name, projectName, StringComparison.Ordinal);
                bool isFilePathChanged = !string.Equals(project.ProjectFilePath, newProjectFilePath, StringComparison.Ordinal);

                // Nếu đổi tên file, xóa file cũ sau khi lưu file mới
                bool needDeleteOldFile = isFilePathChanged && _fileService.FileExists(oldProjectFilePath);

                // Nếu đổi tên project, đổi tên cả thư mục chứa project và thư mục build/source liên quan
                if (isNameChanged)
                {
                    string oldProjectDir = Path.GetDirectoryName(oldProjectFilePath)!;
                    string newProjectDir = Path.GetDirectoryName(newProjectFilePath)!;

                    string oldProjectFolder = Path.Combine(oldProjectDir, currentProject.Name);
                    string newProjectFolder = Path.Combine(newProjectDir, projectName);

                    if (_fileService.DirectoryExists(oldProjectFolder))
                    {
                        // Đảm bảo không trùng tên thư mục mới
                        if (_fileService.DirectoryExists(newProjectFolder))
                        {
                            // Nếu thư mục mới đã tồn tại, xóa hoặc xử lý theo yêu cầu
                            await _fileService.DeleteDirectoryAsync(newProjectFolder);
                        }
                        Directory.Move(oldProjectFolder, newProjectFolder);
                        _logger.LogInformation($"Renamed project folder from {oldProjectFolder} to {newProjectFolder}");
                    }
                }

                var cof = project.Configurations.FirstOrDefault(n => n.Name == project.ActiveConfigurationName);
                if (cof != null)
                {
                    cof.MacroDefinitions = macros ?? new List<string>();
                    cof.IncludePaths = includePaths ?? new List<string>();
                    cof.Properties["LastModifiedBy"] = Environment.UserName;
                    cof.Properties["LastModifiedDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                }

                // Cập nhật thông tin
                project.Name = projectName;
                project.Description = projectDescription;
                project.ProjectFilePath = newProjectFilePath;
                project.SourceFiles = cFiles ?? new List<string>();
                project.LastModified = DateTime.Now;

                // Cập nhật thuộc tính
                if (project.Properties == null)
                    project.Properties = new Dictionary<string, string>();
                project.Properties["LastModifiedBy"] = Environment.UserName;
                project.Properties["LastModifiedDate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // Lưu lại project vào file mới
                await SaveProjectAsync(project);

                // Nếu đổi tên file, xóa file cũ
                if (needDeleteOldFile)
                {
                    await _fileService.DeleteFileAsync(oldProjectFilePath);
                    _logger.LogInformation($"Deleted old project file: {oldProjectFilePath}");
                }

                _logger.LogInformation($"Successfully edited project: {projectName} at {newProjectFilePath}");

                return project;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error editing project: {projectName}");
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

                // Bổ sung thêm phần tạo luôn thư mục nếu chưa có,
                // Khi đó sẽ xóa build
                // Mục đích là khi thay đổi gì thì build lại từ đầu.
                // Xác định thư mục build: <ProjectDir>\<ProjectName>\build\
                string projectDir = Path.GetDirectoryName(project.ProjectFilePath)!;
                string projectFolder = Path.Combine(projectDir, project.Name);
                string buildFolder = Path.Combine(projectFolder, "build");

                // Kiểm tra và tạo thư mục
                // Nếu tồn tại thì xóa hết nội dung bên trong
                if (_fileService.DirectoryExists(buildFolder))
                {
                    _fileService.DeleteDirectoryAsync(buildFolder);
                }

                _fileService.CreateDirectoryIfNotExists(projectFolder);

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
                var coffinal = project.Configurations.FirstOrDefault(c => c.Name == project.ActiveConfigurationName);

                // Đọc source file .h
                foreach (var includePath in coffinal.IncludePaths)
                {
                    try
                    {
                        if (_fileService.DirectoryExists(includePath))
                        {
                            // Tìm kiếm file .h trong thư mục include chỉ truy câp thư mục gốc
                            _logger.LogDebug($"Searching for header files in include path: {includePath}");
                            var files = _fileService.GetFiles(includePath, "h", false);
                            foreach (var file in files)
                            {
                                if (_fileService.FileExists(file))
                                {
                                    var sourceFile = await _sourceCodeService.LoadSourceFileAsync(file);
                                    sourceFiles.Add(sourceFile);
                                }
                                else
                                {
                                    _logger.LogWarning($"Header file not found: {file}");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"Include path not found: {includePath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error loading header files from: {includePath}");
                    }
                }

                // Load each source file .c
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

        #region Luu và đọc file build

        /// <summary>
        /// Lưu file build của dự án vào thư mục build (cùng thư mục với file project gốc).
        /// Tạo thư mục cùng tên với project, bên trong có thư mục build, lưu cả project và các source file.
        /// </summary>
        public async Task<bool> SaveBuildFilesAsync(Project project, List<SourceFile> sourceFiles)
        {
            try
            {
                if (project == null)
                    throw new ArgumentNullException(nameof(project));
                if (string.IsNullOrEmpty(project.Name))
                    throw new ArgumentException("Project name is required");
                if (string.IsNullOrEmpty(project.ProjectFilePath))
                    throw new ArgumentException("Project file path is required");

                // Xác định thư mục build: <ProjectDir>\<ProjectName>\build\
                string projectDir = Path.GetDirectoryName(project.ProjectFilePath)!;
                string projectFolder = Path.Combine(projectDir, project.Name);
                string buildFolder = Path.Combine(projectFolder, "build");

                // Kiểm tra và tạo thư mục
                // Nếu tồn tại thì xóa hết nội dung bên trong
                if (_fileService.DirectoryExists(buildFolder))
                {
                    _fileService.DeleteDirectoryAsync(buildFolder);
                }

                // Tạo thư mục nếu chưa có
                _fileService.CreateDirectoryIfNotExists(projectFolder);
                _fileService.CreateDirectoryIfNotExists(buildFolder);

                // Lưu Project vào buildFolder
                string buildProjectPath = Path.Combine(buildFolder, $"{project.Name}.ctproj");
                string projectJson = JsonConvert.SerializeObject(project, Formatting.Indented);
                await _fileService.WriteFileAsync(buildProjectPath, projectJson);

                // Lưu từng SourceFile vào buildFolder
                foreach (var sourceFile in sourceFiles)
                {
                    if(sourceFile == null || string.IsNullOrEmpty(sourceFile.FileName))
                        continue;

                    string fileName = Path.GetFileNameWithoutExtension(sourceFile.FileName);
                    string beforeDash = sourceFile.Id?.Split('-')[0]?.ToUpper();
                    string ext = sourceFile.Extension.Replace(".", "")?.ToUpper();
                    string buildSourceFilePath = Path.Combine(buildFolder, $"{fileName}_{beforeDash}_{ext}.json");
                    string sourceFileJson = JsonConvert.SerializeObject(sourceFile, Formatting.Indented);
                    await _fileService.WriteFileAsync(buildSourceFilePath, sourceFileJson);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving build files for project: {ProjectName}", project?.Name);
                return false;
            }
        }

        /// <summary>
        /// Đọc file build của dự án từ thư mục build.
        /// </summary>
        public async Task<(Project? project, List<SourceFile>? sourceFiles)> LoadBuildFilesAsync(string projectName, string projectDirectory)
        {
            try
            {
                if (string.IsNullOrEmpty(projectName) || string.IsNullOrEmpty(projectDirectory))
                    throw new ArgumentException("Project name and directory are required");

                string projectFolder = Path.Combine(projectDirectory, projectName);
                string buildFolder = Path.Combine(projectFolder, "build");
                string buildProjectPath = Path.Combine(buildFolder, $"{projectName}.ctproj");

                if (!_fileService.FileExists(buildProjectPath))
                    return (null, null);

                // Đọc Project
                string projectJson = await _fileService.ReadFileAsync(buildProjectPath);
                var project = JsonConvert.DeserializeObject<Project>(projectJson);

                // Đọc Project Chính
                string projectGoc = await _fileService.ReadFileAsync(Path.Combine(projectDirectory, $"{projectName}.ctproj"));
                var projectChinh = JsonConvert.DeserializeObject<Project>(projectGoc);

                // So sánh và kiểm tra thay đổi.
                // Nếu có thay đổi thì tiến hành Thông báo và build lại.
                if(IsBuildOutdated(projectChinh!, project!))
                {
                    _logger.LogWarning("Build files are outdated for project: {ProjectName}", projectName);
                    return (null, null);
                }

                // Đọc tất cả các file *.json (trừ file project) trong buildFolder
                var sourceFiles = new List<SourceFile>();
                var allJsonFiles = Directory.GetFiles(buildFolder, "*.json", SearchOption.TopDirectoryOnly)
                    .Where(f => !f.EndsWith($"{projectName}.ctproj", StringComparison.OrdinalIgnoreCase));
                foreach (var file in allJsonFiles)
                {
                    string json = await _fileService.ReadFileAsync(file);
                    var sourceFile = JsonConvert.DeserializeObject<SourceFile>(json);
                    if (sourceFile != null)
                        sourceFiles.Add(sourceFile);
                }

                return (project, sourceFiles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading build files for project: {ProjectName}", projectName);
                return (null, null);
            }
        }

        /// <summary>
        /// So sánh file Project ngoài thư mục và trong thư mục build để kiểm tra thay đổi.
        /// </summary>
        public bool IsBuildOutdated(Project project, Project buildProject)
        {
            // Sử dụng hàm HasChanged đã có trong Project
            return Project.HasChanged(project, buildProject);
        }

        #endregion

    }
}
