using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses.Unions
{
    /// <summary>
    /// Types of union constraints
    /// </summary>
    public enum UnionConstraintType
    {
        /// <summary>
        /// Size-related constraint
        /// </summary>
        SizeConstraint,

        /// <summary>
        /// Alignment-related constraint
        /// </summary>
        AlignmentConstraint,

        /// <summary>
        /// Packing-related constraint
        /// </summary>
        PackingConstraint,

        /// <summary>
        /// Usage pattern constraint
        /// </summary>
        UsagePattern,

        /// <summary>
        /// Member count constraint
        /// </summary>
        MemberCount,

        /// <summary>
        /// Type safety constraint
        /// </summary>
        TypeSafety,

        /// <summary>
        /// Memory access constraint
        /// </summary>
        MemoryAccess
    }
}
