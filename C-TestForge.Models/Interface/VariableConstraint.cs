using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Interface
{
    /// <summary>
    /// Represents a variable constraint
    /// </summary>
    public class VariableConstraint
    {
        /// <summary>
        /// Gets or sets the variable name
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// Gets or sets the minimum value
        /// </summary>
        public string MinValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum value
        /// </summary>
        public string MaxValue { get; set; }

        /// <summary>
        /// Gets or sets the enum values
        /// </summary>
        public List<string> EnumValues { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the allowed values
        /// </summary>
        public List<string> AllowedValues { get; set; } = new List<string>();
    }
}
