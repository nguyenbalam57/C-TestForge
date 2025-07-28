using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace C_TestForge.Models
{
    #region Project Models

    /// <summary>
    /// Type of source file
    /// </summary>
    public enum SourceFileType
    {
        Header,
        Implementation,
        Unknown
    }

    /// <summary>
    /// Represents a source file
    /// </summary>
    public class SourceFile
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Path to the source file
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Name of the source file
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Content of the source file
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Lines of the source file
        /// </summary>
        public List<string> Lines { get; set; } = new List<string>();

        /// <summary>
        /// Hash of the content for change detection
        /// </summary>
        public string ContentHash { get; set; }

        /// <summary>
        /// Type of the source file
        /// </summary>
        public SourceFileType FileType { get; set; }

        /// <summary>
        /// Dictionary of includes in the source file
        /// </summary>
        public Dictionary<string, string> Includes { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Last modified time of the file
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Parse result for this source file
        /// </summary>
        [JsonIgnore]
        public ParseResult ParseResult { get; set; }

        /// <summary>
        /// Whether the file has been modified since the last save
        /// </summary>
        [JsonIgnore]
        public bool IsDirty { get; set; }

        /// <summary>
        /// Get a string representation of the source file
        /// </summary>
        public override string ToString()
        {
            return $"{FileName} ({FileType})";
        }

        /// <summary>
        /// Update the content of the source file
        /// </summary>
        public void UpdateContent(string newContent)
        {
            Content = newContent;
            Lines = newContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            IsDirty = true;
        }

        /// <summary>
        /// Update a specific line in the source file
        /// </summary>
        public void UpdateLine(int lineNumber, string newContent)
        {
            if (lineNumber < 1 || lineNumber > Lines.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNumber));
            }

            Lines[lineNumber - 1] = newContent;
            Content = string.Join(Environment.NewLine, Lines);
            IsDirty = true;
        }

        /// <summary>
        /// Create a clone of the source file
        /// </summary>
        public SourceFile Clone()
        {
            return new SourceFile
            {
                Id = Id,
                FilePath = FilePath,
                FileName = FileName,
                Content = Content,
                Lines = Lines != null ? new List<string>(Lines) : new List<string>(),
                ContentHash = ContentHash,
                FileType = FileType,
                Includes = Includes != null ? new Dictionary<string, string>(Includes) : new Dictionary<string, string>(),
                LastModified = LastModified,
                IsDirty = IsDirty
            };
        }
    }

    /// <summary>
    /// Represents a project
    /// </summary>
    public class Project : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the project
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Path to the project file
        /// </summary>
        public string ProjectFilePath { get; set; }

        /// <summary>
        /// List of source files in the project
        /// </summary>
        public List<string> SourceFiles { get; set; } = new List<string>();

        /// <summary>
        /// List of include paths
        /// </summary>
        public List<string> IncludePaths { get; set; } = new List<string>();

        /// <summary>
        /// Dictionary of macro definitions
        /// </summary>
        public Dictionary<string, string> MacroDefinitions { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// List of configurations
        /// </summary>
        public List<Configuration> Configurations { get; set; } = new List<Configuration>();

        /// <summary>
        /// Active configuration name
        /// </summary>
        public string ActiveConfigurationName { get; set; }

        /// <summary>
        /// Last modified time of the project
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Project description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Custom project properties
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Get the active configuration
        /// </summary>
        [JsonIgnore]
        public Configuration ActiveConfiguration
        {
            get { return Configurations.FirstOrDefault(c => c.Name == ActiveConfigurationName); }
        }

        /// <summary>
        /// Get a string representation of the project
        /// </summary>
        public override string ToString()
        {
            return $"{Name} - {SourceFiles.Count} files, {Configurations.Count} configurations";
        }

        /// <summary>
        /// Create a clone of the project
        /// </summary>
        public Project Clone()
        {
            return new Project
            {
                Id = Id,
                Name = Name,
                ProjectFilePath = ProjectFilePath,
                SourceFiles = SourceFiles != null ? new List<string>(SourceFiles) : new List<string>(),
                IncludePaths = IncludePaths != null ? new List<string>(IncludePaths) : new List<string>(),
                MacroDefinitions = MacroDefinitions != null ? new Dictionary<string, string>(MacroDefinitions) : new Dictionary<string, string>(),
                Configurations = Configurations?.Select(c => c.Clone()).ToList() ?? new List<Configuration>(),
                ActiveConfigurationName = ActiveConfigurationName,
                LastModified = LastModified,
                Description = Description,
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }

    /// <summary>
    /// Represents a configuration for a project
    /// </summary>
    public class Configuration : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the configuration
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Dictionary of macro definitions
        /// </summary>
        public Dictionary<string, string> MacroDefinitions { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// List of include paths
        /// </summary>
        public List<string> IncludePaths { get; set; } = new List<string>();

        /// <summary>
        /// Additional command-line arguments
        /// </summary>
        public List<string> AdditionalArguments { get; set; } = new List<string>();

        /// <summary>
        /// Description of the configuration
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Custom configuration properties
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Get a string representation of the configuration
        /// </summary>
        public override string ToString()
        {
            return $"{Name} - {MacroDefinitions.Count} macros, {IncludePaths.Count} include paths";
        }

        /// <summary>
        /// Create a clone of the configuration
        /// </summary>
        public Configuration Clone()
        {
            return new Configuration
            {
                Id = Id,
                Name = Name,
                MacroDefinitions = MacroDefinitions != null ? new Dictionary<string, string>(MacroDefinitions) : new Dictionary<string, string>(),
                IncludePaths = IncludePaths != null ? new List<string>(IncludePaths) : new List<string>(),
                AdditionalArguments = AdditionalArguments != null ? new List<string>(AdditionalArguments) : new List<string>(),
                Description = Description,
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }

    #endregion
}
