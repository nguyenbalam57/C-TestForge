using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace C_TestForge.Models
{
    #region Test Case Models

    /// <summary>
    /// Type of test case
    /// </summary>
    public enum TestCaseType
    {
        UnitTest,
        IntegrationTest,
        SystemTest
    }

    /// <summary>
    /// Status of a test case
    /// </summary>
    public enum TestCaseStatus
    {
        NotRun,
        Passed,
        Failed,
        Error,
        Skipped
    }

    /// <summary>
    /// Represents a test case
    /// </summary>
    public class TestCase : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the test case
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the test case
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Type of the test case
        /// </summary>
        public TestCaseType Type { get; set; }

        /// <summary>
        /// Name of the function under test
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Input variables for the test case
        /// </summary>
        public List<TestCaseVariable> InputVariables { get; set; } = new List<TestCaseVariable>();

        /// <summary>
        /// Output variables for the test case
        /// </summary>
        public List<TestCaseVariable> OutputVariables { get; set; } = new List<TestCaseVariable>();

        /// <summary>
        /// Expected return value
        /// </summary>
        public string ExpectedReturnValue { get; set; }

        /// <summary>
        /// Actual return value
        /// </summary>
        public string ActualReturnValue { get; set; }

        /// <summary>
        /// Status of the test case
        /// </summary>
        public TestCaseStatus Status { get; set; } = TestCaseStatus.NotRun;

        /// <summary>
        /// Tags for the test case
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// Custom properties for the test case
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Get a string representation of the test case
        /// </summary>
        public override string ToString()
        {
            return $"{Name} - {FunctionName} ({Type})";
        }

        /// <summary>
        /// Create a clone of the test case
        /// </summary>
        public TestCase Clone()
        {
            return new TestCase
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Type = Type,
                FunctionName = FunctionName,
                InputVariables = InputVariables?.Select(v => v.Clone()).ToList() ?? new List<TestCaseVariable>(),
                OutputVariables = OutputVariables?.Select(v => v.Clone()).ToList() ?? new List<TestCaseVariable>(),
                ExpectedReturnValue = ExpectedReturnValue,
                ActualReturnValue = ActualReturnValue,
                Status = Status,
                Tags = Tags != null ? new List<string>(Tags) : new List<string>(),
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }

    /// <summary>
    /// Represents a variable in a test case
    /// </summary>
    public class TestCaseVariable : IModelObject
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
        /// Value of the variable
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Expected value of the variable (for output variables)
        /// </summary>
        public string ExpectedValue { get; set; }

        /// <summary>
        /// Whether the variable is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Size of the array (if IsArray is true)
        /// </summary>
        public int ArraySize { get; set; }

        /// <summary>
        /// Whether the variable is a pointer
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Whether the variable is passed by reference
        /// </summary>
        public bool IsByReference { get; set; }

        /// <summary>
        /// Get a string representation of the test case variable
        /// </summary>
        public override string ToString()
        {
            string arrayPart = IsArray ? $"[{ArraySize}]" : "";
            string pointerPart = IsPointer ? "*" : "";
            string refPart = IsByReference ? "&" : "";

            return $"{Type}{pointerPart}{refPart} {Name}{arrayPart} = {Value}";
        }

        /// <summary>
        /// Create a clone of the test case variable
        /// </summary>
        public TestCaseVariable Clone()
        {
            return new TestCaseVariable
            {
                Id = Id,
                Name = Name,
                Type = Type,
                Value = Value,
                ExpectedValue = ExpectedValue,
                IsArray = IsArray,
                ArraySize = ArraySize,
                IsPointer = IsPointer,
                IsByReference = IsByReference
            };
        }
    }

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
        public string Name { get; set; }

        /// <summary>
        /// Description of the test suite
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// List of test cases in the suite
        /// </summary>
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();

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
                TestCases = TestCases?.Select(t => t.Clone()).ToList() ?? new List<TestCase>(),
                Tags = Tags != null ? new List<string>(Tags) : new List<string>(),
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }

    #endregion
}
