using C_TestForge.Models.Base;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core.SupportingClasses.Unions
{
    /// <summary>
    /// Represents a constraint on a union
    /// </summary>
    public class UnionConstraint : SourceCodeEntity
    {
        /// <summary>
        /// Name of the union this constraint applies to
        /// </summary>
        public string UnionName { get; set; } = string.Empty;

        /// <summary>
        /// Type of constraint
        /// </summary>
        public UnionConstraintType ConstraintType { get; set; }

        /// <summary>
        /// Value or description of the constraint
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Source of the constraint (attribute, pragma, usage pattern, etc.)
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Severity of the constraint
        /// </summary>
        public ConstraintSeverity Severity { get; set; } = ConstraintSeverity.Info;

        public override string ToString()
        {
            return $"{UnionName}: {ConstraintType} = {Value}";
        }

        public UnionConstraint Clone()
        {
            return new UnionConstraint
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                UnionName = UnionName,
                ConstraintType = ConstraintType,
                Value = Value,
                Source = Source,
                Severity = Severity
            };
        }
    }
}
