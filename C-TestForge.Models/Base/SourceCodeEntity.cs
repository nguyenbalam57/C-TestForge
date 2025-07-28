using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace C_TestForge.Models.Base
{
    /// <summary>
    /// Base class for all source code entities
    /// </summary>
    public abstract class SourceCodeEntity : IModelObject
    {
        /// <summary>
        /// Unique identifier for the entity
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the entity
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Line number in the source file
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Column number in the source file
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Source file where the entity is defined
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Get a location string in format "SourceFile:LineNumber:ColumnNumber"
        /// </summary>
        [JsonIgnore]
        public string Location => $"{SourceFile}:{LineNumber}:{ColumnNumber}";
    }
}
