using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;
using C_TestForge.Models.TestCases;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Service for finding variable values
    /// </summary>
    public interface IVariableValueFinderService
    {
        /// <summary>
        /// Finds values for variables to satisfy the given test case outputs
        /// </summary>
        /// <param name="testCase">The test case</param>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <returns>Dictionary of variable names and their values</returns>
        Task<Dictionary<string, string>> FindValuesForTestCaseAsync(
            TestCase testCase,
            string functionName,
            string filePath);

        /// <summary>
        /// Finds values for variables to maximize code coverage
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="targetCoverage">The target coverage (0.0-1.0)</param>
        /// <returns>List of dictionaries of variable names and their values</returns>
        Task<List<Dictionary<string, string>>> FindValuesForMaxCoverageAsync(
            string functionName,
            string filePath,
            double targetCoverage = 0.9);

        /// <summary>
        /// Finds values that make the expression true
        /// </summary>
        /// <param name="expression">The expression</param>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <returns>Dictionary of variable names and their values</returns>
        Task<Dictionary<string, string>> FindValuesForExpressionAsync(
            string expression,
            string functionName,
            string filePath);
    }
}
