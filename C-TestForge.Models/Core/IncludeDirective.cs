using System;
using C_TestForge.Models.Base;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents an include directive in C code
    /// </summary>
    public class IncludeDirective : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Path to the included file
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Line number in the source file
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Whether the include is a system include (<>) or a local include ("")
        /// </summary>
        public bool IsSystemInclude { get; set; }

        /// <summary>
        /// Source file where the directive is defined
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Get a string representation of the include directive
        /// </summary>
        public override string ToString()
        {
            if (IsSystemInclude)
            {
                return $"#include <{FilePath}>";
            }
            else
            {
                return $"#include \"{FilePath}\"";
            }
        }

        /// <summary>
        /// Create a clone of the include directive
        /// </summary>
        public IncludeDirective Clone()
        {
            return new IncludeDirective
            {
                Id = Id,
                FilePath = FilePath,
                LineNumber = LineNumber,
                IsSystemInclude = IsSystemInclude,
                SourceFile = SourceFile
            };
        }
    }
}