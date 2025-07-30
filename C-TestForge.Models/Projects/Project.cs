using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using C_TestForge.Models.Base;

namespace C_TestForge.Models.Projects
{
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
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Path to the project file
        /// </summary>
        public string ProjectFilePath { get; set; } = string.Empty;

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
        public string ActiveConfigurationName { get; set; } = string.Empty;

        /// <summary>
        /// Last modified time of the project
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Project description
        /// </summary>
        public string Description { get; set; } = string.Empty;

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
}