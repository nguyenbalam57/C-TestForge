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
    /// Service for solving constraints using Z3 Theorem Prover
    /// </summary>
    public interface IZ3SolverService
    {
        /// <summary>
        /// Finds variable values that satisfy the given constraints
        /// </summary>
        /// <param name="constraints">The constraints to satisfy</param>
        /// <param name="expectedOutputs">The expected outputs</param>
        /// <returns>Dictionary of variable names and their values</returns>
        Task<Dictionary<string, string>> FindVariableValuesAsync(
            Dictionary<string, VariableConstraint> constraints,
            Dictionary<string, string> expectedOutputs);

        /// <summary>
        /// Finds variable values that satisfy the given expression
        /// </summary>
        /// <param name="expression">The expression to satisfy</param>
        /// <param name="variableTypes">Dictionary of variable names and their types</param>
        /// <param name="constraints">The constraints to satisfy</param>
        /// <returns>Dictionary of variable names and their values</returns>
        Task<Dictionary<string, string>> FindVariableValuesForExpressionAsync(
            string expression,
            Dictionary<string, string> variableTypes,
            Dictionary<string, VariableConstraint> constraints);

        /// <summary>
        /// Finds variable values to achieve the specified code coverage
        /// </summary>
        /// <param name="functionAnalysis">The function analysis</param>
        /// <param name="variableTypes">Dictionary of variable names and their types</param>
        /// <param name="constraints">The constraints to satisfy</param>
        /// <param name="targetCoverage">The target coverage (0.0-1.0)</param>
        /// <returns>List of dictionaries of variable names and their values</returns>
        Task<List<Dictionary<string, string>>> FindVariableValuesForCoverageAsync(
            CFunction functionAnalysis,
            Dictionary<string, string> variableTypes,
            Dictionary<string, VariableConstraint> constraints,
            double targetCoverage = 0.9);
    }
}
