using C_TestForge.Models.Base;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a C include directive
    /// </summary>
    public class CInclude : SourceCodeEntity
    {
        /// <summary>
        /// Path of the included file
        /// </summary>
        public string IncludePath { get; set; } = string.Empty;

        /// <summary>
        /// Whether this is a system include (angle brackets)
        /// </summary>
        public bool IsSystemInclude { get; set; }

        /// <summary>
        /// Whether the included file was found and successfully processed
        /// </summary>
        public bool IsResolved { get; set; }

        /// <summary>
        /// Full resolved path to the included file
        /// </summary>
        public string ResolvedPath { get; set; } = string.Empty;

        /// <summary>
        /// Whether this include is conditional (inside #ifdef, etc.)
        /// </summary>
        public bool IsConditional { get; set; }

        /// <summary>
        /// Include depth level (0 = top level, 1 = included by top level, etc.)
        /// </summary>
        public int IncludeDepth { get; set; }

        /// <summary>
        /// Whether this include creates a circular dependency
        /// </summary>
        public bool IsCircular { get; set; }

        /// <summary>
        /// Files that this include depends on
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        public override string ToString()
        {
            string bracket = IsSystemInclude ? "<>" : "\"\"";
            return $"#include {bracket[0]}{IncludePath}{bracket[1]}";
        }

        public CInclude Clone()
        {
            return new CInclude
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                IncludePath = IncludePath,
                IsSystemInclude = IsSystemInclude,
                IsResolved = IsResolved,
                ResolvedPath = ResolvedPath,
                IsConditional = IsConditional,
                IncludeDepth = IncludeDepth,
                IsCircular = IsCircular,
                Dependencies = new List<string>(Dependencies ?? new List<string>())
            };
        }
    }
}
