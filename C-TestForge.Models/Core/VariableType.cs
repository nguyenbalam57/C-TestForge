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
        Array,
        Pointer,
        Struct,
        Union,
        Enum
    }
}
