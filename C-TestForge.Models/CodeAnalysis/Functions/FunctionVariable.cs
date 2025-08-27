using C_TestForge.Models.Core;
using C_TestForge.Models.Core.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.Functions
{
    /// <summary>
    /// Represents a variable in a function
    /// </summary>
    public class FunctionVariable
    {
        /// <summary>
        /// Gets or sets the variable name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the variable type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the initial value (if any)
        /// </summary>
        public string InitialValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the line number where the variable is declared
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets whether the variable is a pointer
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Gets or sets whether the variable is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Gets or sets the scope of the variable
        /// </summary>
        public VariableScope Scope { get; set; }
    }
}
