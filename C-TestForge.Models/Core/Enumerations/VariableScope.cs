using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.Enumerations
{
    /// <summary>
    /// Scope of a variable in C code
    /// </summary>
    public enum VariableScope
    {
        /// <summary>
        /// Global variable
        /// </summary>
        Global,
        /// <summary>
        /// Static variable
        /// </summary>
        Static,
        /// <summary>
        /// Local variable
        /// </summary>
        Local,
        /// <summary>
        /// Function parameter
        /// </summary>
        Parameter,
        Register,
        Auto,
        Extern
    }
}
