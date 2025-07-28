using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.Coverage
{
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
