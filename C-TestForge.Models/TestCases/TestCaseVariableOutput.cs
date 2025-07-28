using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    /// <summary>
    /// Represents an output variable in a test case
    /// </summary>
    public class TestCaseVariableOutput : TestCaseVariableBase
    {
        /// <summary>
        /// Expected value of the output variable
        /// </summary>
        public string ExpectedValue { get; set; }

        /// <summary>
        /// Expected array values (if IsArray is true)
        /// </summary>
        public List<string> ExpectedArrayValues { get; set; } = new List<string>();

        /// <summary>
        /// Actual value after execution
        /// </summary>
        public string ActualValue { get; set; }

        /// <summary>
        /// Actual array values after execution (if IsArray is true)
        /// </summary>
        public List<string> ActualArrayValues { get; set; } = new List<string>();

        /// <summary>
        /// Whether to validate this output
        /// </summary>
        public bool ValidateOutput { get; set; } = true;

        /// <summary>
        /// Custom validation expression
        /// </summary>
        public string ValidationExpression { get; set; }

        /// <summary>
        /// Create a clone of the output variable
        /// </summary>
        public TestCaseVariableOutput Clone()
        {
            return new TestCaseVariableOutput
            {
                Id = Id,
                Name = Name,
                Type = Type,
                ExpectedValue = ExpectedValue,
                IsArray = IsArray,
                ArraySize = ArraySize,
                ExpectedArrayValues = ExpectedArrayValues != null ? new List<string>(ExpectedArrayValues) : new List<string>(),
                ActualValue = ActualValue,
                ActualArrayValues = ActualArrayValues != null ? new List<string>(ActualArrayValues) : new List<string>(),
                IsPointer = IsPointer,
                IsByReference = IsByReference,
                Constraints = Constraints?.Select(c => c.Clone()).ToList() ?? new List<VariableConstraint>(),
                ValidateOutput = ValidateOutput,
                ValidationExpression = ValidationExpression
            };
        }

        /// <summary>
        /// Get a string representation of the output variable
        /// </summary>
        public override string ToString()
        {
            string baseStr = base.ToString();
            return $"{baseStr} => {ExpectedValue}";
        }
    }
}
