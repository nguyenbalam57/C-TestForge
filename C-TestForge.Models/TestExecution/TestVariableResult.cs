using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestExecution
{
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
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of the variable
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Expected value of the variable
        /// </summary>
        public string ExpectedValue { get; set; } = string.Empty;

        /// <summary>
        /// Actual value of the variable after execution
        /// </summary>
        public string ActualValue { get; set; } = string.Empty;

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
}
