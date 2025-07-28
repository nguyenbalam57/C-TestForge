using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for generating integration tests
    /// </summary>
    public interface IIntegrationTestGeneratorService
    {
        /// <summary>
        /// Generates integration tests for the given functions
        /// </summary>
        /// <param name="functionNames">The function names</param>
        /// <param name="filePath">The file path</param>
        /// <param name="targetCoverage">The target coverage (0.0-1.0)</param>
        /// <returns>List of generated test cases</returns>
        Task<List<TestCaseModels>> GenerateIntegrationTestsAsync(
            List<string> functionNames,
            string filePath,
            double targetCoverage = 0.9);

        /// <summary>
        /// Generates integration tests for the function call graph
        /// </summary>
        /// <param name="rootFunctionName">The root function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="depth">The maximum call depth to include</param>
        /// <returns>List of generated test cases</returns>
        Task<List<TestCaseModels>> GenerateIntegrationTestsForCallGraphAsync(
            string rootFunctionName,
            string filePath,
            int depth = 3);

        /// <summary>
        /// Generates integration test code for the given test case
        /// </summary>
        /// <param name="testCase">The test case</param>
        /// <param name="filePath">The file path</param>
        /// <param name="framework">The test framework to use</param>
        /// <returns>The generated test code</returns>
        Task<string> GenerateIntegrationTestCodeAsync(
            TestCaseModels testCase,
            string filePath,
            string framework = "unity");
    }
}
