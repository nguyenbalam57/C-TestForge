using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Type of constraint on a variable
    /// </summary>
    public enum ConstraintType
    {
        MinValue,
        MaxValue,
        /// <summary>
        /// Enumeration constraint (set of allowed values)
        /// </summary>
        Enumeration,
        /// <summary>
        /// Range constraint (min-max)
        /// </summary>
        Range,
        Custom,
        /// <summary>
        /// Array size constraint
        /// </summary>
        ArraySize,
        /// <summary>
        /// Exact value constraint
        /// </summary>
        Exact,
        /// <summary>
        /// Pattern constraint (e.g., regex)
        /// </summary>
        Pattern
    }
}
