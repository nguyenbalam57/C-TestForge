using C_TestForge.Models.Base;
using C_TestForge.Models.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestExecution
{
    /// <summary>
    /// Result of executing a test case
    /// </summary>
    public class TestExecutionResult : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// ID of the test case that was executed
        /// </summary>
        public string TestCaseId { get; set; } = string.Empty;

        /// <summary>
        /// Name of the test case that was executed
        /// </summary>
        public string TestCaseName { get; set; } = string.Empty;

        /// <summary>
        /// Status of the test case after execution
        /// </summary>
        public TestCaseStatus Status { get; set; } = TestCaseStatus.NotRun;

        /// <summary>
        /// Status of the test execution
        /// </summary>
        public TestExecutionStatus ExecutionStatus { get; set; } = TestExecutionStatus.NotStarted;

        /// <summary>
        /// Start time of the test execution
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the test execution
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Error message if the test failed
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace if the test failed
        /// </summary>
        public string StackTrace { get; set; } = string.Empty;

        /// <summary>
        /// Actual return value of the function under test
        /// </summary>
        public string ActualReturnValue { get; set; } = string.Empty;

        /// <summary>
        /// Expected return value of the function under test
        /// </summary>
        public string ExpectedReturnValue { get; set; } = string.Empty;

        /// <summary>
        /// List of variable values after execution
        /// </summary>
        public List<TestVariableResult> VariableResults { get; set; } = new List<TestVariableResult>();

        /// <summary>
        /// List of stub function call records
        /// </summary>
        public List<StubCallRecord> StubCalls { get; set; } = new List<StubCallRecord>();

        /// <summary>
        /// Output captured during test execution
        /// </summary>
        public string CapturedOutput { get; set; } = string.Empty;

        /// <summary>
        /// Custom properties for the result
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Duration of the test execution
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Whether the test passed
        /// </summary>
        [JsonIgnore]
        public bool Passed => Status == TestCaseStatus.Passed;

        /// <summary>
        /// Creates a clone of the test execution result
        /// </summary>
        public TestExecutionResult Clone()
        {
            return new TestExecutionResult
            {
                Id = Id,
                TestCaseId = TestCaseId,
                TestCaseName = TestCaseName,
                Status = Status,
                ExecutionStatus = ExecutionStatus,
                StartTime = StartTime,
                EndTime = EndTime,
                ErrorMessage = ErrorMessage,
                StackTrace = StackTrace,
                ActualReturnValue = ActualReturnValue,
                ExpectedReturnValue = ExpectedReturnValue,
                VariableResults = VariableResults?.Select(v => v.Clone()).ToList() ?? new List<TestVariableResult>(),
                StubCalls = StubCalls?.Select(s => s.Clone()).ToList() ?? new List<StubCallRecord>(),
                CapturedOutput = CapturedOutput,
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }
}
