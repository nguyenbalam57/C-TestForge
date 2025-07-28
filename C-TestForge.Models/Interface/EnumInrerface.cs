using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Interface
{
    /// <summary>
    /// Represents the type of an uncovered code area
    /// </summary>
    public enum UncoveredAreaType
    {
        /// <summary>
        /// Uncovered line
        /// </summary>
        Line,

        /// <summary>
        /// Uncovered branch
        /// </summary>
        Branch,

        /// <summary>
        /// Uncovered path
        /// </summary>
        Path,

        /// <summary>
        /// Uncovered condition
        /// </summary>
        Condition,

        /// <summary>
        /// Uncovered function call
        /// </summary>
        FunctionCall
    }

    /// <summary>
    /// Represents the severity of code coverage issues
    /// </summary>
    public enum CodeCoverageSeverity
    {
        /// <summary>
        /// Low severity
        /// </summary>
        Low,

        /// <summary>
        /// Medium severity
        /// </summary>
        Medium,

        /// <summary>
        /// High severity
        /// </summary>
        High,

        /// <summary>
        /// Critical severity
        /// </summary>
        Critical
    }
}
