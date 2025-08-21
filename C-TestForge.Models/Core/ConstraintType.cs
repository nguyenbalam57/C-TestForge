using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Types of constraints that can be applied to variables
    /// </summary>
    public enum ConstraintType
    {
        /// <summary>
        /// Minimum value constraint
        /// </summary>
        MinValue,

        /// <summary>
        /// Maximum value constraint
        /// </summary>
        MaxValue,

        /// <summary>
        /// Range constraint (min and max)
        /// </summary>
        Range,

        /// <summary>
        /// Enumeration of allowed values
        /// </summary>
        Enumeration,

        /// <summary>
        /// Array size constraint
        /// </summary>
        ArraySize,

        /// <summary>
        /// Custom constraint expression
        /// </summary>
        Custom,

        /// <summary>
        /// Exact value constraint
        /// </summary>
        ExactValue
    }
}
