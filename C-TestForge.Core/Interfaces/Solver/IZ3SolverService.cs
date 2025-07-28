using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Solver
{
    /// <summary>
    /// Interface for using Z3 to solve constraints
    /// </summary>
    public interface IZ3SolverService
    {
        /// <summary>
        /// Finds values for variables that satisfy a set of constraints
        /// </summary>
        /// <param name="variables">Variables to find values for</param>
        /// <param name="constraints">Constraints to satisfy</param>
        /// <returns>Dictionary of variable names and their values</returns>
        Task<Dictionary<string, string>> SolveConstraintsAsync(List<CVariable> variables, List<VariableConstraint> constraints);

        /// <summary>
        /// Checks if a set of constraints is satisfiable
        /// </summary>
        /// <param name="constraints">Constraints to check</param>
        /// <returns>True if satisfiable, false otherwise</returns>
        Task<bool> CheckSatisfiabilityAsync(List<VariableConstraint> constraints);

        /// <summary>
        /// Finds values for variables that satisfy a Z3 expression
        /// </summary>
        /// <param name="variables">Variables to find values for</param>
        /// <param name="expression">Z3 expression</param>
        /// <returns>Dictionary of variable names and their values</returns>
        Task<Dictionary<string, string>> SolveExpressionAsync(List<CVariable> variables, string expression);

        /// <summary>
        /// Finds multiple sets of values for variables that satisfy a set of constraints
        /// </summary>
        /// <param name="variables">Variables to find values for</param>
        /// <param name="constraints">Constraints to satisfy</param>
        /// <param name="count">Number of value sets to find</param>
        /// <returns>List of dictionaries of variable names and their values</returns>
        Task<List<Dictionary<string, string>>> FindMultipleSolutionsAsync(List<CVariable> variables, List<VariableConstraint> constraints, int count);
    }
}
