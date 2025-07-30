using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;
using C_TestForge.Models.CodeAnalysis.Coverage;
using C_TestForge.Models.TestCases;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for analyzing code coverage
    /// </summary>
    public interface ICodeCoverageService
    {
        /// <summary>
        /// Analyzes code coverage for the given test cases
        /// </summary>
        /// <param name="testCases">The test cases</param>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <returns>The code coverage result</returns>
        Task<CodeCoverageResult> AnalyzeCoverageAsync(
            IEnumerable<TestCase> testCases,
            string functionName,
            string filePath);

        /// <summary>
        /// Identifies uncovered code areas
        /// </summary>
        /// <param name="testCases">The test cases</param>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <returns>List of uncovered code areas</returns>
        Task<List<UncoveredCodeArea>> IdentifyUncoveredAreasAsync(
            IEnumerable<TestCase> testCases,
            string functionName,
            string filePath);

        /// <summary>
        /// Suggests test cases to improve coverage
        /// </summary>
        /// <param name="testCases">The existing test cases</param>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="targetCoverage">The target coverage (0.0-1.0)</param>
        /// <returns>List of suggested test cases</returns>
        Task<List<TestCase>> SuggestTestCasesForCoverageAsync(
            IEnumerable<TestCase> testCases,
            string functionName,
            string filePath,
            double targetCoverage = 0.9);
    }
}
