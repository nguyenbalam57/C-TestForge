using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.Enumerations
{
    /// <summary>
    /// Types of definitions that can be found in C code
    /// </summary>
    public enum DefinitionType
    {
        /// <summary>
        /// Preprocessor macro definition
        /// </summary>
        Macro,

        /// <summary>
        /// Function-like macro definition
        /// </summary>
        FunctionMacro,

        /// <summary>
        /// Constant definition
        /// </summary>
        Constant,

        /// <summary>
        /// Enumeration value
        /// </summary>
        EnumValue,

        /// <summary>
        /// Type definition
        /// </summary>
        TypeDef,

        /// <summary>
        /// Conditional compilation directive
        /// </summary>
        Conditional
    }
}
