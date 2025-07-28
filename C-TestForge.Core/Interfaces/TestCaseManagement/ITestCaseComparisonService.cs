using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models.Interface;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for comparing test cases
    /// </summary>
    public interface ITestCaseComparisonService
    {
        /// <summary>
        /// Compares two test cases and returns the differences
        /// </summary>
        Task<TestCaseComparisonResult> CompareTestCasesAsync(TestCaseModels testCase1, TestCaseModels testCase2);

        /// <summary>
        /// Analyzes test case coverage for a function
        /// </summary>
        Task<TestCaseCoverageResult> AnalyzeCoverageAsync(string functionName, IEnumerable<TestCaseModels> testCases);

        /// <summary>
        /// Checks if test cases use disabled variables
        /// </summary>
        Task<IEnumerable<TestCaseWarning>> CheckForDisabledVariablesAsync(IEnumerable<TestCaseModels> testCases, IEnumerable<CVariable> disabledVariables);
    }
}
