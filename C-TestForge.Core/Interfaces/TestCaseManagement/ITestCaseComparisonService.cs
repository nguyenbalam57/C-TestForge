using C_TestForge.Models;
using C_TestForge.Models.Core;
using C_TestForge.Models.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for comparing test cases
    /// </summary>
    public interface ITestCaseComparisonService
    {
        ///// <summary>
        ///// Compares two test cases and returns the differences
        ///// </summary>
        //Task<TestCaseComparisonResult> CompareTestCasesAsync(TestCase testCase1, TestCase testCase2);

        ///// <summary>
        ///// Analyzes test case coverage for a function
        ///// </summary>
        //Task<TestCaseCoverageResult> AnalyzeCoverageAsync(string functionName, IEnumerable<TestCase> testCases);

        ///// <summary>
        ///// Checks if test cases use disabled variables
        ///// </summary>
        //Task<IEnumerable<TestCaseWarning>> CheckForDisabledVariablesAsync(IEnumerable<TestCase> testCases, IEnumerable<CVariable> disabledVariables);
    }
}
