using C_TestForge.Core.Services;
using C_TestForge.Models;
using C_TestForge.Models.TestCases;
using C_TestForge.Parser;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace C_TestForge.Tests.Core
{
    public class ProjectServiceTests
    {
        private readonly Mock<IParser> _mockParser;
        private readonly Mock<ILogger<ProjectService>> _mockLogger;
        private readonly ProjectService _projectService;
        private readonly string _tempFolder;

        public ProjectServiceTests()
        {
            _mockParser = new Mock<IParser>();
            _mockLogger = new Mock<ILogger<ProjectService>>();
            _projectService = new ProjectService(_mockParser.Object, _mockLogger.Object);

            _tempFolder = Path.Combine(Path.GetTempPath(), "C-TestForge-Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempFolder);
            Directory.CreateDirectory(Path.Combine(_tempFolder, "src"));
            Directory.CreateDirectory(Path.Combine(_tempFolder, "include"));

            // Create some test files
            File.WriteAllText(Path.Combine(_tempFolder, "src", "main.c"), "int main() { return 0; }");
            File.WriteAllText(Path.Combine(_tempFolder, "src", "utils.c"), "void utils_func() {}");
            File.WriteAllText(Path.Combine(_tempFolder, "include", "utils.h"), "#define VERSION 1.0");
        }

        [Fact]
        public void CreateProject_ShouldCreateProjectWithCorrectProperties()
        {
            // Arrange
            var name = "Test Project";
            var description = "Test Description";
            var sourceDir = Path.Combine(_tempFolder, "src");

            // Act
            var project = _projectService.CreateProject(name, description, sourceDir);

            // Assert
            project.Should().NotBeNull();
            project.Name.Should().Be(name);
            project.Description.Should().Be(description);
            project.SourceDirectory.Should().Be(sourceDir);
            project.SourceFiles.Should().HaveCount(2);
            project.SourceFiles.Should().Contain(f => f.EndsWith("main.c"));
            project.SourceFiles.Should().Contain(f => f.EndsWith("utils.c"));
            project.IncludeDirectories.Should().Contain(d => d.EndsWith("src"));
        }

        [Fact]
        public void ParseProjectFiles_ShouldCallParserForEachSourceFile()
        {
            // Arrange
            var project = new TestProject
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Test Project",
                SourceDirectory = Path.Combine(_tempFolder, "src"),
                SourceFiles = new List<string>
                {
                    Path.Combine(_tempFolder, "src", "main.c"),
                    Path.Combine(_tempFolder, "src", "utils.c")
                },
                IncludeDirectories = new List<string>
                {
                    Path.Combine(_tempFolder, "include")
                }
            };

            _mockParser.Setup(p => p.ParseFile(
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns((string path, IEnumerable<string> includes, IEnumerable<KeyValuePair<string, string>> defines) =>
                    new CSourceFile(path, $"// Content of {Path.GetFileName(path)}"));

            // Act
            var result = _projectService.ParseProjectFiles(project);

            // Assert
            result.Should().HaveCount(2);
            _mockParser.Verify(p => p.ParseFile(
                It.Is<string>(s => s.EndsWith("main.c")),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()), Times.Once);

            _mockParser.Verify(p => p.ParseFile(
                It.Is<string>(s => s.EndsWith("utils.c")),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<KeyValuePair<string, string>>>()), Times.Once);
        }

        [Fact]
        public void ExportAndImportProject_ShouldPreserveProjectData()
        {
            // Arrange
            var project = new TestProject
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Export Test Project",
                Description = "Project for testing export/import",
                SourceDirectory = Path.Combine(_tempFolder, "src"),
                SourceFiles = new List<string>
                {
                    Path.Combine(_tempFolder, "src", "main.c"),
                    Path.Combine(_tempFolder, "src", "utils.c")
                },
                IncludeDirectories = new List<string>
                {
                    Path.Combine(_tempFolder, "include")
                },
                PreprocessorDefinitions = new Dictionary<string, string>
                {
                    { "DEBUG", "1" },
                    { "VERSION", "\"1.0\"" }
                },
                TestCases = new List<TestCaseUser>
                {
                    new TestCaseUser
                    {
                        Id = Guid.NewGuid(),
                        Name = "Test Case 1",
                        FunctionName = "main",
                        Type = TestCaseType.UnitTest
                    }
                }
            };

            var exportPath = Path.Combine(_tempFolder, "exportedProject.json");

            // Act
            _projectService.ExportProject(project, exportPath);
            var importedProject = _projectService.ImportProject(exportPath);

            // Assert
            importedProject.Should().NotBeNull();
            importedProject.Name.Should().Be(project.Name);
            importedProject.Description.Should().Be(project.Description);
            importedProject.SourceDirectory.Should().Be(project.SourceDirectory);
            importedProject.SourceFiles.Should().BeEquivalentTo(project.SourceFiles);
            importedProject.IncludeDirectories.Should().BeEquivalentTo(project.IncludeDirectories);
            importedProject.PreprocessorDefinitions.Should().BeEquivalentTo(project.PreprocessorDefinitions);
            importedProject.TestCases.Should().HaveCount(1);
            importedProject.TestCases[0].Name.Should().Be("Test Case 1");
            importedProject.TestCases[0].FunctionName.Should().Be("main");
            importedProject.TestCases[0].Type.Should().Be(Models.TestCases.TestCaseType.UnitTest);
        }

        public void Dispose()
        {
            // Clean up temp files
            if (Directory.Exists(_tempFolder))
            {
                Directory.Delete(_tempFolder, true);
            }
        }
    }
}
