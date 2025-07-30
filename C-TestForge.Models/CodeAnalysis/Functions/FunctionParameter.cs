using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.Functions
{
    /// <summary>
    /// Represents a function parameter
    /// </summary>
    public class FunctionParameter
    {
        /// <summary>
        /// Gets or sets the parameter name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the parameter type
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the parameter is a pointer
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Gets or sets whether the parameter is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Gets or sets the default value (if any)
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;
    }
}
