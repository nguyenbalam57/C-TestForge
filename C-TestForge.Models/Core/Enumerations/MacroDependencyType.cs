using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.Enumerations
{
    /// <summary>
    /// Types of macro dependencies
    /// </summary>
    public enum MacroDependencyType
    {
        Direct,
        Indirect,
        Circular,
        Conditional
    }
}
