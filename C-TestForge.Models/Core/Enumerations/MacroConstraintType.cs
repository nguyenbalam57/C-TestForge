using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.Enumerations
{
    /// <summary>
    /// Types of macro constraints
    /// </summary>
    public enum MacroConstraintType
    {
        UsageCount,
        NumericValue,
        StringValue,
        EnumValue,
        ConditionalUsage,
        ConditionalCompilation,
        ParameterCount,
        Custom
    }
}
