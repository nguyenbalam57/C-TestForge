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
    /// Interface for generating test cases
    /// </summary>
    public interface ITestGenerationService
    {
        /// <summary>
        /// Generates test cases for a function
        /// </summary>
        /// <param name="function">Function to generate test cases for</param>
        /// <param name="options">Generation options</param>
        /// <returns>List of generated test cases</returns>
        Task<List<TestCase>> GenerateTestCasesAsync(CFunction function, TestGenerationOptions options);

        /// <summary>
        /// Generates test cases for multiple functions
        /// </summary>
        /// <param name="functions">Functions to generate test cases for</param>
        /// <param name="options">Generation options</param>
        /// <returns>List of generated test cases</returns>
        Task<List<TestCase>> GenerateMultipleTestCasesAsync(List<CFunction> functions, TestGenerationOptions options);

        /// <summary>
        /// Generates integration test cases for a set of functions
        /// </summary>
        /// <param name="functions">Functions to generate test cases for</param>
        /// <param name="options">Generation options</param>
        /// <returns>List of generated test cases</returns>
        Task<List<TestCase>> GenerateIntegrationTestCasesAsync(List<CFunction> functions, TestGenerationOptions options);

        /// <summary>
        /// Generates values for a variable based on its constraints
        /// </summary>
        /// <param name="variable">Variable to generate values for</param>
        /// <param name="count">Number of values to generate</param>
        /// <returns>List of generated values</returns>
        Task<List<string>> GenerateVariableValuesAsync(CVariable variable, int count);

        /// <summary>
        /// Computes expected output values for a test case
        /// </summary>
        /// <param name="testCase">Test case to compute expected outputs for</param>
        /// <param name="function">Function to analyze</param>
        /// <returns>Updated test case with expected outputs</returns>
        Task<TestCase> ComputeExpectedOutputsAsync(TestCase testCase, CFunction function);
    }

    /// <summary>
    /// Options for test case generation
    /// </summary>
    public class TestGenerationOptions
    {
        /// <summary>
        /// Type of test cases to generate
        /// </summary>
        public TestCaseType TestCaseType { get; set; } = TestCaseType.UnitTest;

        /// <summary>
        /// Maximum number of test cases to generate
        /// </summary>
        public int MaxTestCases { get; set; } = 10;

        /// <summary>
        /// Whether to generate tests for edge cases
        /// </summary>
        public bool GenerateEdgeCases { get; set; } = true;

        /// <summary>
        /// Whether to generate tests for boundary values
        /// </summary>
        public bool GenerateBoundaryValues { get; set; } = true;

        /// <summary>
        /// Whether to generate tests for equivalence classes
        /// </summary>
        public bool GenerateEquivalenceClasses { get; set; } = true;

        /// <summary>
        /// Whether to use symbolic execution
        /// </summary>
        public bool UseSymbolicExecution { get; set; } = false;

        /// <summary>
        /// Whether to use Z3 for constraint solving
        /// </summary>
        public bool UseZ3Solver { get; set; } = true;

        /// <summary>
        /// Target code coverage percentage
        /// </summary>
        public int TargetCoveragePercentage { get; set; } = 80;
    }
}
