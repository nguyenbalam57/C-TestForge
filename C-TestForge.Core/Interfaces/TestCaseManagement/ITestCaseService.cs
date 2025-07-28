using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for managing test cases
    /// </summary>
    public interface ITestCaseService
    {
        /// <summary>
        /// Gets all test cases
        /// </summary>
        Task<IEnumerable<TestCase>> GetAllTestCasesAsync();

        /// <summary>
        /// Gets test cases for a specific function
        /// </summary>
        Task<IEnumerable<TestCase>> GetTestCasesForFunctionAsync(string functionName);

        /// <summary>
        /// Adds a new test case
        /// </summary>
        Task<TestCase> AddTestCaseAsync(TestCase testCase);

        /// <summary>
        /// Updates an existing test case
        /// </summary>
        Task<bool> UpdateTestCaseAsync(TestCase testCase);

        /// <summary>
        /// Deletes a test case
        /// </summary>
        Task<bool> DeleteTestCaseAsync(int testCaseId);

        /// <summary>
        /// Executes a test case and returns the result
        /// </summary>
        Task<TestCaseResult> ExecuteTestCaseAsync(TestCase testCase);

        /// <summary>
        /// Validates a test case
        /// </summary>
        Task<TestCaseValidationResult> ValidateTestCaseAsync(TestCase testCase);
    }

    /// <summary>
    /// Represents a test result
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// Test case ID
        /// </summary>
        public string TestCaseId { get; set; }

        /// <summary>
        /// Test case name
        /// </summary>
        public string TestCaseName { get; set; }

        /// <summary>
        /// Status of the test
        /// </summary>
        public TestCaseStatus Status { get; set; }

        /// <summary>
        /// Message (e.g., error message)
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Actual return value
        /// </summary>
        public string ActualReturnValue { get; set; }

        /// <summary>
        /// Actual output variables
        /// </summary>
        public List<TestCaseVariable> ActualOutputVariables { get; set; } = new List<TestCaseVariable>();

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }

    /// <summary>
    /// Represents a validation issue
    /// </summary>
    public class ValidationIssue
    {
        /// <summary>
        /// Type of issue
        /// </summary>
        public string IssueType { get; set; }

        /// <summary>
        /// Description of the issue
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Severity of the issue
        /// </summary>
        public string Severity { get; set; }
    }
}
