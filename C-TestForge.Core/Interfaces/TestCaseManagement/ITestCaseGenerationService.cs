using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;
using C_TestForge.Models.TestCases;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Interface for test case generation service
    /// </summary>
    public interface ITestCaseGenerationService
    {
        /// <summary>
        /// Generates unit tests for a function
        /// </summary>
        Task<List<TestCase>> GenerateUnitTestsAsync(string functionName, string filePath, double targetCoverage = 0.9);

        /// <summary>
        /// Generates integration tests for a set of related functions
        /// </summary>
        Task<List<TestCase>> GenerateIntegrationTestsAsync(List<string> functionNames, string filePath, double targetCoverage = 0.9);
    }
}
