using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace C_TestForge.Models
{
    #region Solver Models

    /// <summary>
    /// Type of constraint for the solver
    /// </summary>
    public enum SolverConstraintType
    {
        /// <summary>
        /// Equality constraint (a == b)
        /// </summary>
        Equality,

        /// <summary>
        /// Inequality constraint (a != b)
        /// </summary>
        Inequality,

        /// <summary>
        /// Less than constraint (a < b)
        /// </summary>
        LessThan,

        /// <summary>
        /// Less than or equal constraint (a <= b)
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// Greater than constraint (a > b)
        /// </summary>
        GreaterThan,

        /// <summary>
        /// Greater than or equal constraint (a >= b)
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// Range constraint (a <= x <= b)
        /// </summary>
        Range,

        /// <summary>
        /// Membership constraint (x in [a, b, c])
        /// </summary>
        Membership,

        /// <summary>
        /// Custom constraint (e.g. complex expression)
        /// </summary>
        Custom
    }

    /// <summary>
    /// Status of a solver operation
    /// </summary>
    public enum SolverStatus
    {
        /// <summary>
        /// Solver has not been run yet
        /// </summary>
        NotRun,

        /// <summary>
        /// Solver is running
        /// </summary>
        Running,

        /// <summary>
        /// Solver completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Solver found no solution
        /// </summary>
        Unsatisfiable,

        /// <summary>
        /// Solver timed out
        /// </summary>
        Timeout,

        /// <summary>
        /// Solver encountered an error
        /// </summary>
        Error,

        /// <summary>
        /// Solver was cancelled
        /// </summary>
        Cancelled
    }

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
        public string Name { get; set; }

        /// <summary>
        /// Type of the constraint
        /// </summary>
        public SolverConstraintType Type { get; set; }

        /// <summary>
        /// Left-hand side of the constraint (variable name or expression)
        /// </summary>
        public string LeftHandSide { get; set; }

        /// <summary>
        /// Right-hand side of the constraint (value or expression)
        /// </summary>
        public string RightHandSide { get; set; }

        /// <summary>
        /// Lower bound for range constraint
        /// </summary>
        public string LowerBound { get; set; }

        /// <summary>
        /// Upper bound for range constraint
        /// </summary>
        public string UpperBound { get; set; }

        /// <summary>
        /// List of allowed values for membership constraint
        /// </summary>
        public List<string> AllowedValues { get; set; } = new List<string>();

        /// <summary>
        /// Custom expression for complex constraints
        /// </summary>
        public string CustomExpression { get; set; }

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
        public string Source { get; set; }

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

    /// <summary>
    /// Represents a variable declaration for the Z3 solver
    /// </summary>
    public class SolverVariable : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the variable
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the variable in Z3 (Int, Real, Bool, etc.)
        /// </summary>
        public string Z3Type { get; set; }

        /// <summary>
        /// Original C type of the variable
        /// </summary>
        public string CType { get; set; }

        /// <summary>
        /// Whether the variable is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Size of the array (if IsArray is true)
        /// </summary>
        public int ArraySize { get; set; }

        /// <summary>
        /// Domain of the variable (for enumeration types)
        /// </summary>
        public List<string> Domain { get; set; } = new List<string>();

        /// <summary>
        /// Creates a clone of the solver variable
        /// </summary>
        public SolverVariable Clone()
        {
            return new SolverVariable
            {
                Id = Id,
                Name = Name,
                Z3Type = Z3Type,
                CType = CType,
                IsArray = IsArray,
                ArraySize = ArraySize,
                Domain = Domain != null ? new List<string>(Domain) : new List<string>()
            };
        }
    }

    /// <summary>
    /// Represents a query to the Z3 solver
    /// </summary>
    public class SolverQuery : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the query
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the query
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Variables involved in the query
        /// </summary>
        public List<SolverVariable> Variables { get; set; } = new List<SolverVariable>();

        /// <summary>
        /// Constraints in the query
        /// </summary>
        public List<SolverConstraint> Constraints { get; set; } = new List<SolverConstraint>();

        /// <summary>
        /// Target variables to solve for
        /// </summary>
        public List<string> TargetVariables { get; set; } = new List<string>();

        /// <summary>
        /// Maximum timeout for the solver in milliseconds
        /// </summary>
        public int TimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Whether to find all solutions
        /// </summary>
        public bool FindAllSolutions { get; set; } = false;

        /// <summary>
        /// Maximum number of solutions to find
        /// </summary>
        public int MaxSolutions { get; set; } = 1;

        /// <summary>
        /// Custom settings for the solver
        /// </summary>
        public Dictionary<string, string> SolverSettings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a clone of the solver query
        /// </summary>
        public SolverQuery Clone()
        {
            return new SolverQuery
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Variables = Variables?.Select(v => v.Clone()).ToList() ?? new List<SolverVariable>(),
                Constraints = Constraints?.Select(c => c.Clone()).ToList() ?? new List<SolverConstraint>(),
                TargetVariables = TargetVariables != null ? new List<string>(TargetVariables) : new List<string>(),
                TimeoutMs = TimeoutMs,
                FindAllSolutions = FindAllSolutions,
                MaxSolutions = MaxSolutions,
                SolverSettings = SolverSettings != null ? new Dictionary<string, string>(SolverSettings) : new Dictionary<string, string>()
            };
        }
    }

    /// <summary>
    /// Represents a solution from the Z3 solver
    /// </summary>
    public class SolverSolution : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Dictionary of variable assignments
        /// </summary>
        public Dictionary<string, string> Assignments { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Dictionary of array variable assignments
        /// </summary>
        public Dictionary<string, List<string>> ArrayAssignments { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// List of constraints that were satisfied
        /// </summary>
        public List<string> SatisfiedConstraints { get; set; } = new List<string>();

        /// <summary>
        /// List of constraints that were violated (for soft constraints)
        /// </summary>
        public List<string> ViolatedConstraints { get; set; } = new List<string>();

        /// <summary>
        /// Creates a clone of the solver solution
        /// </summary>
        public SolverSolution Clone()
        {
            var clone = new SolverSolution
            {
                Id = Id,
                Assignments = Assignments != null ? new Dictionary<string, string>(Assignments) : new Dictionary<string, string>(),
                SatisfiedConstraints = SatisfiedConstraints != null ? new List<string>(SatisfiedConstraints) : new List<string>(),
                ViolatedConstraints = ViolatedConstraints != null ? new List<string>(ViolatedConstraints) : new List<string>()
            };

            // Clone array assignments
            clone.ArrayAssignments = new Dictionary<string, List<string>>();
            if (ArrayAssignments != null)
            {
                foreach (var kvp in ArrayAssignments)
                {
                    clone.ArrayAssignments[kvp.Key] = new List<string>(kvp.Value);
                }
            }

            return clone;
        }
    }

    /// <summary>
    /// Represents a result from the Z3 solver
    /// </summary>
    public class SolverResult : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the result
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Query that produced this result
        /// </summary>
        public SolverQuery Query { get; set; }

        /// <summary>
        /// Status of the solver
        /// </summary>
        public SolverStatus Status { get; set; } = SolverStatus.NotRun;

        /// <summary>
        /// List of solutions found
        /// </summary>
        public List<SolverSolution> Solutions { get; set; } = new List<SolverSolution>();

        /// <summary>
        /// Error message if the solver encountered an error
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Start time of the solver
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the solver
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Duration of the solver operation
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Whether the solver found a solution
        /// </summary>
        [JsonIgnore]
        public bool HasSolution => Status == SolverStatus.Completed && Solutions.Count > 0;

        /// <summary>
        /// Creates a clone of the solver result
        /// </summary>
        public SolverResult Clone()
        {
            return new SolverResult
            {
                Id = Id,
                Name = Name,
                Query = Query?.Clone(),
                Status = Status,
                Solutions = Solutions?.Select(s => s.Clone()).ToList() ?? new List<SolverSolution>(),
                ErrorMessage = ErrorMessage,
                StartTime = StartTime,
                EndTime = EndTime
            };
        }
    }

    #endregion
}