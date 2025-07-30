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
    /// Service for generating test code
    /// </summary>
    public interface ITestCodeGeneratorService
    {
        /// <summary>
        /// Generates test code for the given test cases
        /// </summary>
        /// <param name="testCases">The test cases</param>
        /// <param name="filePath">The file path</param>
        /// <param name="framework">The test framework to use</param>
        /// <returns>The generated test code</returns>
        Task<string> GenerateTestCodeAsync(
            IEnumerable<TestCase> testCases,
            string filePath,
            string framework = "unity");

        /// <summary>
        /// Generates test fixture code for the given function
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="framework">The test framework to use</param>
        /// <returns>The generated test fixture code</returns>
        Task<string> GenerateTestFixtureAsync(
            string functionName,
            string filePath,
            string framework = "unity");

        /// <summary>
        /// Generates test runner code for the given test fixtures
        /// </summary>
        /// <param name="testFixtures">The test fixtures</param>
        /// <param name="framework">The test framework to use</param>
        /// <returns>The generated test runner code</returns>
        Task<string> GenerateTestRunnerAsync(
            IEnumerable<string> testFixtures,
            string framework = "unity");
    }
}
