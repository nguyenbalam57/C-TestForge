using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace C_TestForge.Models
{
    #region Test Execution Models

    /// <summary>
    /// Status of a test execution
    /// </summary>
    public enum TestExecutionStatus
    {
        /// <summary>
        /// Test execution has not started
        /// </summary>
        NotStarted,

        /// <summary>
        /// Test execution is in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Test execution completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Test execution failed
        /// </summary>
        Failed,

        /// <summary>
        /// Test execution was cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Test execution timed out
        /// </summary>
        Timeout
    }

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
        public string TestCaseId { get; set; }

        /// <summary>
        /// Name of the test case that was executed
        /// </summary>
        public string TestCaseName { get; set; }

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
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Stack trace if the test failed
        /// </summary>
        public string StackTrace { get; set; }

        /// <summary>
        /// Actual return value of the function under test
        /// </summary>
        public string ActualReturnValue { get; set; }

        /// <summary>
        /// Expected return value of the function under test
        /// </summary>
        public string ExpectedReturnValue { get; set; }

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
        public string CapturedOutput { get; set; }

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

    /// <summary>
    /// Result of a variable after test execution
    /// </summary>
    public class TestVariableResult : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the variable
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the variable
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Expected value of the variable
        /// </summary>
        public string ExpectedValue { get; set; }

        /// <summary>
        /// Actual value of the variable after execution
        /// </summary>
        public string ActualValue { get; set; }

        /// <summary>
        /// Whether the variable value matched the expected value
        /// </summary>
        public bool IsMatch { get; set; }

        /// <summary>
        /// Whether the variable is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Expected array values (if IsArray is true)
        /// </summary>
        public List<string> ExpectedArrayValues { get; set; } = new List<string>();

        /// <summary>
        /// Actual array values after execution (if IsArray is true)
        /// </summary>
        public List<string> ActualArrayValues { get; set; } = new List<string>();

        /// <summary>
        /// For arrays, indices where values did not match
        /// </summary>
        public List<int> MismatchIndices { get; set; } = new List<int>();

        /// <summary>
        /// Creates a clone of the test variable result
        /// </summary>
        public TestVariableResult Clone()
        {
            return new TestVariableResult
            {
                Id = Id,
                Name = Name,
                Type = Type,
                ExpectedValue = ExpectedValue,
                ActualValue = ActualValue,
                IsMatch = IsMatch,
                IsArray = IsArray,
                ExpectedArrayValues = ExpectedArrayValues != null ? new List<string>(ExpectedArrayValues) : new List<string>(),
                ActualArrayValues = ActualArrayValues != null ? new List<string>(ActualArrayValues) : new List<string>(),
                MismatchIndices = MismatchIndices != null ? new List<int>(MismatchIndices) : new List<int>()
            };
        }
    }

    /// <summary>
    /// Record of a stub function call during test execution
    /// </summary>
    public class StubCallRecord : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the stubbed function
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Sequence number of the call (1-based)
        /// </summary>
        public int CallSequence { get; set; }

        /// <summary>
        /// Timestamp of the call
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Parameter values passed to the function
        /// </summary>
        public List<ParameterValue> ParameterValues { get; set; } = new List<ParameterValue>();

        /// <summary>
        /// Return value from the stub
        /// </summary>
        public string ReturnValue { get; set; }

        /// <summary>
        /// Creates a clone of the stub call record
        /// </summary>
        public StubCallRecord Clone()
        {
            return new StubCallRecord
            {
                Id = Id,
                FunctionName = FunctionName,
                CallSequence = CallSequence,
                Timestamp = Timestamp,
                ParameterValues = ParameterValues?.Select(p => p.Clone()).ToList() ?? new List<ParameterValue>(),
                ReturnValue = ReturnValue
            };
        }
    }

    /// <summary>
    /// Represents a parameter value in a function call
    /// </summary>
    public class ParameterValue : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the parameter
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the parameter
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Value of the parameter
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Whether the parameter is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Array values (if IsArray is true)
        /// </summary>
        public List<string> ArrayValues { get; set; } = new List<string>();

        /// <summary>
        /// Creates a clone of the parameter value
        /// </summary>
        public ParameterValue Clone()
        {
            return new ParameterValue
            {
                Id = Id,
                Name = Name,
                Type = Type,
                Value = Value,
                IsArray = IsArray,
                ArrayValues = ArrayValues != null ? new List<string>(ArrayValues) : new List<string>()
            };
        }
    }

    /// <summary>
    /// Summary of test execution results
    /// </summary>
    public class TestExecutionSummary : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the execution run
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the execution run
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Status of the test execution
        /// </summary>
        public TestExecutionStatus Status { get; set; } = TestExecutionStatus.NotStarted;

        /// <summary>
        /// Start time of the execution run
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the execution run
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Total number of test cases
        /// </summary>
        public int TotalTestCases { get; set; }

        /// <summary>
        /// Number of passed test cases
        /// </summary>
        public int PassedTestCases { get; set; }

        /// <summary>
        /// Number of failed test cases
        /// </summary>
        public int FailedTestCases { get; set; }

        /// <summary>
        /// Number of skipped test cases
        /// </summary>
        public int SkippedTestCases { get; set; }

        /// <summary>
        /// Number of test cases with errors
        /// </summary>
        public int ErrorTestCases { get; set; }

        /// <summary>
        /// List of test execution results
        /// </summary>
        public List<TestExecutionResult> Results { get; set; } = new List<TestExecutionResult>();

        /// <summary>
        /// Coverage achieved during this execution run
        /// </summary>
        public TestCoverage Coverage { get; set; }

        /// <summary>
        /// Environment information for the execution run
        /// </summary>
        public Dictionary<string, string> EnvironmentInfo { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Custom properties for the summary
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Duration of the execution run
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Pass rate as a percentage
        /// </summary>
        [JsonIgnore]
        public double PassRate => TotalTestCases > 0 ? (double)PassedTestCases / TotalTestCases * 100 : 0;

        /// <summary>
        /// Creates a clone of the test execution summary
        /// </summary>
        public TestExecutionSummary Clone()
        {
            return new TestExecutionSummary
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Status = Status,
                StartTime = StartTime,
                EndTime = EndTime,
                TotalTestCases = TotalTestCases,
                PassedTestCases = PassedTestCases,
                FailedTestCases = FailedTestCases,
                SkippedTestCases = SkippedTestCases,
                ErrorTestCases = ErrorTestCases,
                Results = Results?.Select(r => r.Clone()).ToList() ?? new List<TestExecutionResult>(),
                Coverage = Coverage?.Clone(),
                EnvironmentInfo = EnvironmentInfo != null ? new Dictionary<string, string>(EnvironmentInfo) : new Dictionary<string, string>(),
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }

    #endregion
}