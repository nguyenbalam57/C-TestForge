using System;
using C_TestForge.Models.Base;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a relationship between two functions
    /// </summary>
    public class FunctionRelationship : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the caller function
        /// </summary>
        public string CallerName { get; set; } = string.Empty;

        /// <summary>
        /// Name of the callee function
        /// </summary>
        public string CalleeName { get; set; } = string.Empty;

        /// <summary>
        /// Line number in the caller function
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Source file where the call occurs
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Get a string representation of the function relationship
        /// </summary>
        public override string ToString()
        {
            return $"{CallerName} calls {CalleeName} at {SourceFile}:{LineNumber}";
        }

        /// <summary>
        /// Create a clone of the function relationship
        /// </summary>
        public FunctionRelationship Clone()
        {
            return new FunctionRelationship
            {
                Id = Id,
                CallerName = CallerName,
                CalleeName = CalleeName,
                LineNumber = LineNumber,
                SourceFile = SourceFile
            };
        }
    }
}