using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for generating unit tests
    /// </summary>
    public interface IUnitTestGeneratorService
    {
        /// <summary>
        /// Generates unit tests for the given function
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="targetCoverage">The target coverage (0.0-1.0)</param>
        /// <returns>List of generated test cases</returns>
        Task<List<TestCaseModels>> GenerateUnitTestsAsync(
            string functionName,
            string filePath,
            double targetCoverage = 0.9);

        /// <summary>
        /// Generates unit tests with specific inputs and outputs
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="inputs">The input values</param>
        /// <param name="expectedOutputs">The expected output values</param>
        /// <returns>The generated test case</returns>
        Task<TestCaseModels> GenerateUnitTestWithValuesAsync(
            string functionName,
            string filePath,
            Dictionary<string, string> inputs,
            Dictionary<string, string> expectedOutputs);

        /// <summary>
        /// Generates unit test code for the given test case
        /// </summary>
        /// <param name="testCase">The test case</param>
        /// <param name="filePath">The file path</param>
        /// <param name="framework">The test framework to use</param>
        /// <returns>The generated test code</returns>
        Task<string> GenerateUnitTestCodeAsync(
            TestCaseModels testCase,
            string filePath,
            string framework = "unity");
    }
}
