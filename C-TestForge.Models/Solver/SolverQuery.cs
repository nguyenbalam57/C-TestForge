using System.Collections.Generic;
using C_TestForge.Models.Base;

namespace C_TestForge.Models.Solver
{

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
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the query
        /// </summary>
        public string Description { get; set; } = string.Empty;

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
}