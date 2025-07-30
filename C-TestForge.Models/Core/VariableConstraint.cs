using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a constraint on a variable
    /// </summary>
    public class VariableConstraint : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of the constraint
        /// </summary>
        public ConstraintType Type { get; set; }

        /// <summary>
        /// Name of the variable
        /// </summary>
        public string VariableName { get; set; } = string.Empty;

        /// <summary>
        /// Value for exact constraints
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Minimum value of the variable
        /// </summary>
        public string MinValue { get; set; } = string.Empty;

        /// <summary>
        /// Maximum value of the variable
        /// </summary>
        public string MaxValue { get; set; } = string.Empty;

        /// <summary>
        /// List of allowed values for an enumeration
        /// </summary>
        public List<string> AllowedValues { get; set; } = new List<string>();

        /// <summary>
        /// Custom constraint expression
        /// </summary>
        public string Expression { get; set; } = string.Empty;

        /// <summary>
        /// Source of the constraint (e.g., function name, line number)
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Get a string representation of the constraint
        /// </summary>
        public override string ToString()
        {
            switch (Type)
            {
                case ConstraintType.MinValue:
                    return $">= {MinValue}";
                case ConstraintType.MaxValue:
                    return $"<= {MaxValue}";
                case ConstraintType.Range:
                    return $"{MinValue} to {MaxValue}";
                case ConstraintType.Enumeration:
                    return $"One of: {string.Join(", ", AllowedValues)}";
                case ConstraintType.Custom:
                    return Expression;
                default:
                    return "Unknown constraint";
            }
        }

        /// <summary>
        /// Check if a value satisfies this constraint
        /// </summary>
        public bool IsSatisfied(string value)
        {
            // Implementation depends on the constraint type and value type
            // This is a placeholder for the actual implementation
            return true;
        }

        /// <summary>
        /// Create a clone of the constraint
        /// </summary>
        public VariableConstraint Clone()
        {
            return new VariableConstraint
            {
                Id = Id,
                Type = Type,
                MinValue = MinValue,
                MaxValue = MaxValue,
                AllowedValues = AllowedValues != null ? new List<string>(AllowedValues) : new List<string>(),
                Expression = Expression,
                Source = Source
            };
        }
    }
}
