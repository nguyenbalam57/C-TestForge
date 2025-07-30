using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Type of conditional directive
    /// </summary>
    public enum ConditionalType
    {
        /// <summary>
        /// #if directive
        /// </summary>
        If,

        /// <summary>
        /// #ifdef directive
        /// </summary>
        IfDef,

        /// <summary>
        /// #ifndef directive
        /// </summary>
        IfNDef,

        /// <summary>
        /// #elif directive
        /// </summary>
        ElseIf,

        /// <summary>
        /// #else directive
        /// </summary>
        Else,

        /// <summary>
        /// #endif directive
        /// </summary>
        EndIf,

        /// <summary>
        /// #include directive
        /// </summary>
        Include
    }
}
