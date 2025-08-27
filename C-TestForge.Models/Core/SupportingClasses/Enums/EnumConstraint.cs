using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses.Enums
{
    /// <summary>
    /// Enum constraint information
    /// </summary>
    public class EnumConstraint
    {
        public string EnumName { get; set; }
        public string ValueName { get; set; }
        public EnumConstraintType ConstraintType { get; set; }
        public long? Value { get; set; }
        public long? MinValue { get; set; }
        public long? MaxValue { get; set; }
        public string Source { get; set; }
    }

    /// <summary>
    /// Types of enum constraints
    /// </summary>
    public enum EnumConstraintType
    {
        ValueRange,
        PositiveValues,
        UsageCount,
        ValueUsageCount,
        SwitchUsage,
        SequentialPattern,
        ZeroBased,
        BitFlagPattern,
        MaxBitPosition
    }
}
