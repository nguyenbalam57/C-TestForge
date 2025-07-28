using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Type of constraint on a variable
    /// </summary>
    public enum ConstraintType
    {
        MinValue,
        MaxValue,
        Enumeration,
        Range,
        Custom
    }
}
