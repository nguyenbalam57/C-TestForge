using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Type of a variable in C code
    /// </summary>
    public enum VariableType
    {
        Primitive,
        /// <summary>
        /// Array type
        /// </summary>
        Array,
        /// <summary>
        /// Pointer type
        /// </summary>
        Pointer,
        /// <summary>
        /// Structure type
        /// </summary>
        Struct,
        /// <summary>
        /// Union type
        /// </summary>
        Union,
        /// <summary>
        /// Enumeration type
        /// </summary>
        Enum,
        /// <summary>
        /// Floating point type
        /// </summary>
        Float,
        /// <summary>
        /// Character type
        /// </summary>
        Char,
        /// <summary>
        /// Boolean type
        /// </summary>
        Bool,
        /// <summary>
        /// Integer type
        /// </summary>
        Integer,
        /// <summary>
        /// Unknown type
        /// </summary>
        Unknown
    }
}
