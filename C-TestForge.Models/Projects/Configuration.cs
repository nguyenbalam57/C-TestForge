using System;
using System.Collections.Generic;
using C_TestForge.Models.Base;

namespace C_TestForge.Models.Projects
{
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
        public string Name { get; set; } = string.Empty;

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
        public string Description { get; set; } = string.Empty;

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
}