using C_TestForge.Models.Base;
using C_TestForge.Models.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace C_TestForge.Models.TestCase
{
    /// <summary>
    /// Represents a test suite containing multiple test cases
    /// </summary>
    public class TestSuite : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the test suite
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Description of the test suite
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// List of test cases in the suite
        /// </summary>
        public List<TestCases.TestCase> TestCases { get; set; } = new List<TestCases.TestCase>();

        /// <summary>
        /// Tags for the test suite
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Custom properties for the test suite
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Get a string representation of the test suite
        /// </summary>
        public override string ToString()
        {
            return $"{Name} - {TestCases.Count} test cases";
        }

        /// <summary>
        /// Get a summary of test case statuses
        /// </summary>
        [JsonIgnore]
        public Dictionary<TestCaseStatus, int> StatusSummary
        {
            get
            {
                var summary = new Dictionary<TestCaseStatus, int>();
                foreach (var status in Enum.GetValues(typeof(TestCaseStatus)).Cast<TestCaseStatus>())
                {
                    summary[status] = TestCases.Count(t => t.Status == status);
                }
                return summary;
            }
        }

        /// <summary>
        /// Create a clone of the test suite
        /// </summary>
        public TestSuite Clone()
        {
            return new TestSuite
            {
                Id = Id,
                Name = Name,
                Description = Description,
                TestCases = TestCases?.Select(t => t.Clone()).ToList() ?? new List<TestCases.TestCase>(),
                Tags = Tags != null ? new List<string>(Tags) : new List<string>(),
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }
}