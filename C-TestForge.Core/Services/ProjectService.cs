using C_TestForge.Models;
using C_TestForge.Parser;
using LiteDB;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace C_TestForge.Core.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IParser _parser;
        private readonly ILogger<ProjectService> _logger;
        private readonly string _dbPath;
        private TestProject _currentProject;

        public ProjectService(IParser parser, ILogger<ProjectService> logger)
        {
            _parser = parser;
            _logger = logger;
            _dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "C-TestForge", "Projects.db");

            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(_dbPath));
        }

        public TestProject CurrentProject => _currentProject;

        public TestProject CreateProject(string name, string description, string sourceDirectory)
        {
            _logger.LogInformation($"Creating new project: {name}");

            var project = new TestProject
            {
                Id = Guid.NewGuid().ToString(),
                Name = name,
                Description = description,
                SourceDirectory = sourceDirectory,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            // Find all C source files in the directory
            if (Directory.Exists(sourceDirectory))
            {
                var sourceFiles = Directory.GetFiles(sourceDirectory, "*.c", SearchOption.AllDirectories);
                var headerFiles = Directory.GetFiles(sourceDirectory, "*.h", SearchOption.AllDirectories);

                project.SourceFiles.AddRange(sourceFiles);
                project.SourceFiles.AddRange(headerFiles);

                // Add include directories (assuming source and include directories are in the same parent directory)
                var parentDir = Directory.GetParent(sourceDirectory)?.FullName;
                if (parentDir != null)
                {
                    var includeDirs = Directory.GetDirectories(parentDir, "include", SearchOption.AllDirectories);
                    project.IncludeDirectories.AddRange(includeDirs);

                    // Also add the source directory itself for header files
                    project.IncludeDirectories.Add(sourceDirectory);
                }
            }

            SaveProject(project);
            _currentProject = project;
            return project;
        }

        public List<TestProject> GetAllProjects()
        {
            _logger.LogInformation("Getting all projects");

            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<TestProject>("projects");
            return collection.FindAll().ToList();
        }

        public TestProject GetProject(string id)
        {
            _logger.LogInformation($"Getting project with ID: {id}");

            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<TestProject>("projects");
            return collection.FindById(id);
        }

        public void SaveProject(TestProject project)
        {
            _logger.LogInformation($"Saving project: {project.Name}");

            project.ModifiedDate = DateTime.Now;

            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<TestProject>("projects");
            collection.Upsert(project);
        }

        public void DeleteProject(string id)
        {
            _logger.LogInformation($"Deleting project with ID: {id}");

            using var db = new LiteDatabase(_dbPath);
            var collection = db.GetCollection<TestProject>("projects");
            collection.Delete(id);

            if (_currentProject?.Id == id)
            {
                _currentProject = null;
            }
        }

        public TestProject LoadProject(string id)
        {
            _logger.LogInformation($"Loading project with ID: {id}");

            var project = GetProject(id);
            if (project != null)
            {
                _currentProject = project;
            }
            return project;
        }

        public Dictionary<string, CSourceFile> ParseProjectFiles(TestProject project)
        {
            _logger.LogInformation($"Parsing project files for: {project.Name}");

            var parsedFiles = new Dictionary<string, CSourceFile>();

            foreach (var sourceFile in project.SourceFiles)
            {
                try
                {
                    var parsedFile = _parser.ParseFile(
                        sourceFile,
                        project.IncludeDirectories,
                        project.PreprocessorDefinitions);

                    parsedFiles[sourceFile] = parsedFile;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error parsing file: {sourceFile}");
                }
            }

            return parsedFiles;
        }

        public void ExportProject(TestProject project, string filePath)
        {
            _logger.LogInformation($"Exporting project to: {filePath}");

            var json = JsonConvert.SerializeObject(project, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public TestProject ImportProject(string filePath)
        {
            _logger.LogInformation($"Importing project from: {filePath}");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Project file not found: {filePath}");
            }

            var json = File.ReadAllText(filePath);
            var project = JsonConvert.DeserializeObject<TestProject>(json);

            if (project != null)
            {
                // Generate a new ID to avoid conflicts
                project.Id = Guid.NewGuid().ToString();
                SaveProject(project);
                _currentProject = project;
            }

            return project;
        }
    }

    public interface IProjectService
    {
        TestProject CurrentProject { get; }
        TestProject CreateProject(string name, string description, string sourceDirectory);
        List<TestProject> GetAllProjects();
        TestProject GetProject(string id);
        void SaveProject(TestProject project);
        void DeleteProject(string id);
        TestProject LoadProject(string id);
        Dictionary<string, CSourceFile> ParseProjectFiles(TestProject project);
        void ExportProject(TestProject project, string filePath);
        TestProject ImportProject(string filePath);
    }
}
