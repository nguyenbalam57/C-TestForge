using C_TestForge.Models;
using C_TestForge.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Solver
{
    /// <summary>
    /// Interface for solving variable constraints
    /// </summary>
    public interface IConstraintSolverService
    {
        /// <summary>
        /// Converts C constraints to Z3 constraints
        /// </summary>
        /// <param name="constraints">C constraints</param>
        /// <returns>Z3 constraints</returns>
        Task<string> ConvertToZ3ConstraintsAsync(List<VariableConstraint> constraints);

        /// <summary>
        /// Extracts constraints from a function
        /// </summary>
        /// <param name="function">Function to analyze</param>
        /// <param name="variables">Variables to extract constraints for</param>
        /// <returns>List of constraints</returns>
        Task<List<VariableConstraint>> ExtractConstraintsFromFunctionAsync(CFunction function, List<CVariable> variables);

        /// <summary>
        /// Finds values for a specific variable
        /// </summary>
        /// <param name="variable">Variable to find values for</param>
        /// <param name="constraints">Constraints to satisfy</param>
        /// <param name="count">Number of values to find</param>
        /// <returns>List of values</returns>
        Task<List<string>> FindVariableValuesAsync(CVariable variable, List<VariableConstraint> constraints, int count);

        /// <summary>
        /// Finds values for a specific variable to achieve a target output
        /// </summary>
        /// <param name="variable">Variable to find values for</param>
        /// <param name="constraints">Constraints to satisfy</param>
        /// <param name="targetOutput">Target output value</param>
        /// <returns>List of values</returns>
        Task<List<string>> FindValuesForTargetOutputAsync(CVariable variable, List<VariableConstraint> constraints, string targetOutput);
    }
}
