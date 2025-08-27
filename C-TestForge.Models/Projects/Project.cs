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
        /// List of source files in the project, chỉ chứa Unit Under Tests
        /// Để rút gọn thời gian phân tích mã nguồn, chỉ lưu các file mã nguồn cần thiết cho việc kiểm thử
        /// </summary>
        public List<string> SourceFiles { get; set; } = new List<string>();

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
                Configurations = Configurations?.Select(c => c.Clone()).ToList() ?? new List<Configuration>(),
                ActiveConfigurationName = ActiveConfigurationName,
                LastModified = LastModified,
                Description = Description,
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }

        /// <summary>
        /// Hàm so sánh hai project xem có khác nhau không
        /// </summary>
        /// <param name="newProject"></param>
        /// <param name="oldProject"></param>
        /// <returns></returns>
        public static bool HasChanged(Project newProject, Project oldProject)
        {
            if (newProject == null || oldProject == null)
                return true;

            if (newProject.Name != oldProject.Name)
                return true;

            if (newProject.ProjectFilePath != oldProject.ProjectFilePath)
                return true;

            if (newProject.ActiveConfigurationName != oldProject.ActiveConfigurationName)
                return true;

            if (newProject.Description != oldProject.Description)
                return true;

            if (newProject.LastModified != oldProject.LastModified)
                return true;

            // So sánh SourceFiles
            if (!newProject.SourceFiles.SequenceEqual(oldProject.SourceFiles))
                return true;

            // So sánh Properties
            if (newProject.Properties.Count != oldProject.Properties.Count ||
                newProject.Properties.Except(oldProject.Properties).Any() ||
                oldProject.Properties.Except(newProject.Properties).Any())
                return true;

            // So sánh Configurations
            if (newProject.Configurations.Count != oldProject.Configurations.Count)
                return true;

            for (int i = 0; i < newProject.Configurations.Count; i++)
            {
                var newConfig = newProject.Configurations[i];
                var oldConfig = oldProject.Configurations[i];

                if (newConfig.Id != oldConfig.Id ||
                    newConfig.Name != oldConfig.Name ||
                    newConfig.Description != oldConfig.Description ||
                    !newConfig.MacroDefinitions.SequenceEqual(oldConfig.MacroDefinitions) ||
                    !newConfig.IncludePaths.SequenceEqual(oldConfig.IncludePaths) ||
                    !newConfig.AdditionalArguments.SequenceEqual(oldConfig.AdditionalArguments) ||
                    newConfig.Properties.Count != oldConfig.Properties.Count ||
                    newConfig.Properties.Except(oldConfig.Properties).Any() ||
                    oldConfig.Properties.Except(newConfig.Properties).Any())
                {
                    return true;
                }
            }

            return false;
        }
    }
}