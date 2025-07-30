using C_TestForge.Models.Base;
using C_TestForge.Models.TestGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestExecution
{
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
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the execution run
        /// </summary>
        public string Description { get; set; } = string.Empty;

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

}
