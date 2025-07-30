using C_TestForge.Models;
using C_TestForge.Models.Projects;
using C_TestForge.Models.TestCase;
using C_TestForge.Models.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Interface for managing test suites
    /// </summary>
    public interface ITestSuiteService
    {
        /// <summary>
        /// Creates a new test suite
        /// </summary>
        /// <param name="name">Name of the test suite</param>
        /// <returns>The created test suite</returns>
        TestSuite CreateTestSuite(string name);

        /// <summary>
        /// Loads a test suite from a file
        /// </summary>
        /// <param name="filePath">Path to the test suite file</param>
        /// <returns>The loaded test suite</returns>
        Task<TestSuite> LoadTestSuiteAsync(string filePath);

        /// <summary>
        /// Saves a test suite to a file
        /// </summary>
        /// <param name="testSuite">Test suite to save</param>
        /// <param name="filePath">Path to the test suite file</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveTestSuiteAsync(TestSuite testSuite, string filePath);

        /// <summary>
        /// Adds a test case to a test suite
        /// </summary>
        /// <param name="testSuite">Test suite to add to</param>
        /// <param name="testCase">Test case to add</param>
        /// <returns>Updated test suite</returns>
        TestSuite AddTestCase(TestSuite testSuite, TestCase testCase);

        /// <summary>
        /// Removes a test case from a test suite
        /// </summary>
        /// <param name="testSuite">Test suite to remove from</param>
        /// <param name="testCaseId">ID of the test case to remove</param>
        /// <returns>Updated test suite</returns>
        TestSuite RemoveTestCase(TestSuite testSuite, string testCaseId);

        /// <summary>
        /// Runs all test cases in a test suite
        /// </summary>
        /// <param name="testSuite">Test suite to run</param>
        /// <param name="projectContext">Project context for the tests</param>
        /// <returns>Test suite result</returns>
        Task<TestSuiteResult> RunTestSuiteAsync(TestSuite testSuite, Project projectContext);

        /// <summary>
        /// Generates a report for a test suite
        /// </summary>
        /// <param name="testSuite">Test suite to generate a report for</param>
        /// <param name="testResults">Test results</param>
        /// <param name="reportFormat">Format of the report</param>
        /// <returns>Generated report</returns>
        Task<string> GenerateReportAsync(TestSuite testSuite, List<TestResult> testResults, string reportFormat);
    }

    /// <summary>
    /// Represents a test suite result
    /// </summary>
    public class TestSuiteResult
    {
        /// <summary>
        /// Test suite ID
        /// </summary>
        public string TestSuiteId { get; set; }

        /// <summary>
        /// Test suite name
        /// </summary>
        public string TestSuiteName { get; set; }

        /// <summary>
        /// List of test results
        /// </summary>
        public List<TestResult> TestResults { get; set; } = new List<TestResult>();

        /// <summary>
        /// Total number of tests
        /// </summary>
        public int TotalTests => TestResults.Count;

        /// <summary>
        /// Number of passed tests
        /// </summary>
        public int PassedTests => TestResults.Count(r => r.Status == TestCaseStatus.Passed);

        /// <summary>
        /// Number of failed tests
        /// </summary>
        public int FailedTests => TestResults.Count(r => r.Status == TestCaseStatus.Failed);

        /// <summary>
        /// Number of error tests
        /// </summary>
        public int ErrorTests => TestResults.Count(r => r.Status == TestCaseStatus.Error);

        /// <summary>
        /// Number of skipped tests
        /// </summary>
        public int SkippedTests => TestResults.Count(r => r.Status == TestCaseStatus.Skipped);

        /// <summary>
        /// Total execution time in milliseconds
        /// </summary>
        public long TotalExecutionTimeMs { get; set; }
    }
}
