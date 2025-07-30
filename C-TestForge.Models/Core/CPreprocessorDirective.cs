using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a preprocessor directive in C code
    /// </summary>
    public class CPreprocessorDirective : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of the directive
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Value of the directive
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Line number in the source file
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Source file where the directive is defined
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;
    }
}
