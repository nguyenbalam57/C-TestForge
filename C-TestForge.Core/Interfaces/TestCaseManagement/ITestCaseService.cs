using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Interface for managing test cases
    /// </summary>
    public interface ITestCaseService
    {
        /// <summary>
        /// Creates a new test case
        /// </summary>
        /// <param name="function">Function to test</param>
        /// <param name="testCaseType">Type of test case</param>
        /// <returns>The created test case</returns>
        Task<TestCaseModels> CreateTestCaseAsync(CFunction function, TestCaseType testCaseType);

        /// <summary>
        /// Loads test cases from a file
        /// </summary>
        /// <param name="filePath">Path to the test case file</param>
        /// <returns>List of test cases</returns>
        Task<List<TestCaseModels>> LoadTestCasesAsync(string filePath);

        /// <summary>
        /// Saves test cases to a file
        /// </summary>
        /// <param name="testCases">Test cases to save</param>
        /// <param name="filePath">Path to the test case file</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveTestCasesAsync(List<TestCaseModels> testCases, string filePath);

        /// <summary>
        /// Imports test cases from CSV
        /// </summary>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>List of imported test cases</returns>
        Task<List<TestCaseModels>> ImportFromCsvAsync(string filePath);

        /// <summary>
        /// Exports test cases to CSV
        /// </summary>
        /// <param name="testCases">Test cases to export</param>
        /// <param name="filePath">Path to the CSV file</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> ExportToCsvAsync(List<TestCaseModels> testCases, string filePath);

        /// <summary>
        /// Imports test cases from Excel
        /// </summary>
        /// <param name="filePath">Path to the Excel file</param>
        /// <returns>List of imported test cases</returns>
        Task<List<TestCaseModels>> ImportFromExcelAsync(string filePath);

        /// <summary>
        /// Exports test cases to Excel
        /// </summary>
        /// <param name="testCases">Test cases to export</param>
        /// <param name="filePath">Path to the Excel file</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> ExportToExcelAsync(List<TestCaseModels> testCases, string filePath);

        /// <summary>
        /// Runs a test case
        /// </summary>
        /// <param name="testCase">Test case to run</param>
        /// <param name="projectContext">Project context for the test</param>
        /// <returns>Test result</returns>
        Task<TestResult> RunTestCaseAsync(TestCaseModels testCase, Project projectContext);

        /// <summary>
        /// Generates code for a test case
        /// </summary>
        /// <param name="testCase">Test case to generate code for</param>
        /// <returns>Generated code</returns>
        Task<string> GenerateTestCodeAsync(TestCaseModels testCase);

        /// <summary>
        /// Validates a test case
        /// </summary>
        /// <param name="testCase">Test case to validate</param>
        /// <returns>List of validation issues</returns>
        Task<List<ValidationIssue>> ValidateTestCaseAsync(TestCaseModels testCase);
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
