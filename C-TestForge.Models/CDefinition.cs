using C_TestForge.Models.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models
{

    public class CDefinition
    {
        /// <summary>
        /// Name of the definition
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Value of the definition
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Line number in the source file
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Column number in the source file
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Source file where the definition is defined
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Whether the definition is a function-like macro
        /// </summary>
        public bool IsFunctionLike { get; set; }

        /// <summary>
        /// Parameters of a function-like macro
        /// </summary>
        public List<string> Parameters { get; set; }

        /// <summary>
        /// Type of the definition
        /// </summary>
        public DefinitionType DefinitionType { get; set; }

        /// <summary>
        /// List of definitions that this definition depends on
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Whether the definition is enabled in the current configuration
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }
}
