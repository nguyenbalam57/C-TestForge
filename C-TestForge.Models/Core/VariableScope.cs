using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Scope of a variable in C code
    /// </summary>
    public enum VariableScope
    {
        Global,
        Static,
        Local,
        Parameter,
        Rom
    }
}
