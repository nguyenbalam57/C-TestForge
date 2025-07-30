using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Severity of a parse error
    /// </summary>
    public enum ErrorSeverity
    {
        /// <summary>
        /// Informational message
        /// </summary>
        Info = 0,

        /// <summary>
        /// Warning message
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error message
        /// </summary>
        Error = 2,

        /// <summary>
        /// Critical error message
        /// </summary>
        Critical = 3
    }
}
