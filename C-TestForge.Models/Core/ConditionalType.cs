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
        If,
        IfDef,
        IfNDef,
        ElseIf,
        Else
    }
}
