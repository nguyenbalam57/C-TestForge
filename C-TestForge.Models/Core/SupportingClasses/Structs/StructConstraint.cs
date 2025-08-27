using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses.Structs
{
    /// <summary>
    /// Represents struct constraints
    /// </summary>
    public class StructConstraint
    {
        public string StructName { get; set; } = string.Empty;
        public StructConstraintType ConstraintType { get; set; }
        public string Value { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
    }

    /// <summary>
    /// Types of struct constraints
    /// </summary>
    public enum StructConstraintType
    {
        SizeConstraint,
        AlignmentConstraint,
        PackingConstraint,
        UsagePattern,
        FieldCount,
        BitFieldConstraint
    }
}
