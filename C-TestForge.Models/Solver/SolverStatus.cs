using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Solver
{
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

}
