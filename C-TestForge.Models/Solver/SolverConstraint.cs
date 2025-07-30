using C_TestForge.Models.Base;

namespace C_TestForge.Models.Solver
{
    /// <summary>
    /// Represents a constraint for the Z3 solver
    /// </summary>
    public class SolverConstraint : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the constraint
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of the constraint
        /// </summary>
        public SolverConstraintType Type { get; set; }

        /// <summary>
        /// Left-hand side of the constraint (variable name or expression)
        /// </summary>
        public string LeftHandSide { get; set; } = string.Empty;

        /// <summary>
        /// Right-hand side of the constraint (value or expression)
        /// </summary>
        public string RightHandSide { get; set; } = string.Empty;

        /// <summary>
        /// Lower bound for range constraint
        /// </summary>
        public string LowerBound { get; set; } = string.Empty;

        /// <summary>
        /// Upper bound for range constraint
        /// </summary>
        public string UpperBound { get; set; } = string.Empty;

        /// <summary>
        /// List of allowed values for membership constraint
        /// </summary>
        public List<string> AllowedValues { get; set; } = new List<string>();

        /// <summary>
        /// Custom expression for complex constraints
        /// </summary>
        public string CustomExpression { get; set; } = string.Empty;

        /// <summary>
        /// Priority of the constraint (higher priority constraints are considered first)
        /// </summary>
        public int Priority { get; set; } = 0;

        /// <summary>
        /// Whether the constraint is hard (must be satisfied) or soft (can be violated)
        /// </summary>
        public bool IsHardConstraint { get; set; } = true;

        /// <summary>
        /// Weight of the constraint for soft constraints
        /// </summary>
        public double Weight { get; set; } = 1.0;

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
                case SolverConstraintType.Equality:
                    return $"{LeftHandSide} == {RightHandSide}";
                case SolverConstraintType.Inequality:
                    return $"{LeftHandSide} != {RightHandSide}";
                case SolverConstraintType.LessThan:
                    return $"{LeftHandSide} < {RightHandSide}";
                case SolverConstraintType.LessThanOrEqual:
                    return $"{LeftHandSide} <= {RightHandSide}";
                case SolverConstraintType.GreaterThan:
                    return $"{LeftHandSide} > {RightHandSide}";
                case SolverConstraintType.GreaterThanOrEqual:
                    return $"{LeftHandSide} >= {RightHandSide}";
                case SolverConstraintType.Range:
                    return $"{LowerBound} <= {LeftHandSide} <= {UpperBound}";
                case SolverConstraintType.Membership:
                    return $"{LeftHandSide} in [{string.Join(", ", AllowedValues)}]";
                case SolverConstraintType.Custom:
                    return CustomExpression;
                default:
                    return "Unknown constraint";
            }
        }

        /// <summary>
        /// Creates a clone of the solver constraint
        /// </summary>
        public SolverConstraint Clone()
        {
            return new SolverConstraint
            {
                Id = Id,
                Name = Name,
                Type = Type,
                LeftHandSide = LeftHandSide,
                RightHandSide = RightHandSide,
                LowerBound = LowerBound,
                UpperBound = UpperBound,
                AllowedValues = AllowedValues != null ? new List<string>(AllowedValues) : new List<string>(),
                CustomExpression = CustomExpression,
                Priority = Priority,
                IsHardConstraint = IsHardConstraint,
                Weight = Weight,
                Source = Source
            };
        }
    }
}