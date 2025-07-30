using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace C_TestForge.Models.Solver
{
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
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Query that produced this result
        /// </summary>
        public SolverQuery Query { get; set; } = new SolverQuery();

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
        public string ErrorMessage { get; set; } = string.Empty;

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
}
