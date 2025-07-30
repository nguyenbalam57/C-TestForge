using System.Collections.Generic;
using C_TestForge.Models.Base;

namespace C_TestForge.Models.Solver
{
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
        /// Whether this solution is optimal according to the solver
        /// </summary>
        public bool IsOptimal { get; set; }

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
}