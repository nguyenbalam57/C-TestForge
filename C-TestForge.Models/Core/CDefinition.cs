using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a preprocessor definition in C code
    /// </summary>
    public class CDefinition : SourceCodeEntity
    {
        /// <summary>
        /// Value of the definition
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Whether the definition is a function-like macro
        /// </summary>
        public bool IsFunctionLike { get; set; }

        /// <summary>
        /// Parameters of a function-like macro
        /// </summary>
        public List<string> Parameters { get; set; } = new List<string>();

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

        /// <summary>
        /// Get a string representation of the definition
        /// </summary>
        public override string ToString()
        {
            if (IsFunctionLike)
            {
                string parameters = string.Join(", ", Parameters);
                return $"#define {Name}({parameters}) {Value}";
            }
            else
            {
                return $"#define {Name} {Value}";
            }
        }

        /// <summary>
        /// Create a clone of the definition
        /// </summary>
        public CDefinition Clone()
        {
            return new CDefinition
            {
                Id = Id,
                Name = Name,
                Value = Value,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                IsFunctionLike = IsFunctionLike,
                Parameters = Parameters != null ? new List<string>(Parameters) : new List<string>(),
                DefinitionType = DefinitionType,
                Dependencies = Dependencies != null ? new List<string>(Dependencies) : new List<string>(),
                IsEnabled = IsEnabled
            };
        }
    }
}
