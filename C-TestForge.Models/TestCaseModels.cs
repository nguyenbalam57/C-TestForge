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
        public List<TestCaseVariableInput> InputVariables { get; set; } = new List<TestCaseVariableInput>();

        /// <summary>
        /// Output variables for the test case
        /// </summary>
        public List<TestCaseVariableOutput> OutputVariables { get; set; } = new List<TestCaseVariableOutput>();

        /// <summary>
        /// Additional expected outputs for the test case
        /// </summary>
        public List<TestCaseVariableOutput> ExpectedOutputs { get; set; } = new List<TestCaseVariableOutput>();

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
        /// Creator of the test case
        /// </summary>
        public string Creator { get; set; }

        /// <summary>
        /// Creation date of the test case
        /// </summary>
        public DateTime CreationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Last modified date of the test case
        /// </summary>
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Stubs for functions called by the function under test
        /// </summary>
        public List<FunctionStub> Stubs { get; set; } = new List<FunctionStub>();

        /// <summary>
        /// Setup code to run before the test case
        /// </summary>
        public string SetupCode { get; set; }

        /// <summary>
        /// Teardown code to run after the test case
        /// </summary>
        public string TeardownCode { get; set; }

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
                InputVariables = InputVariables?.Select(v => v.Clone()).ToList() ?? new List<TestCaseVariableInput>(),
                OutputVariables = OutputVariables?.Select(v => v.Clone()).ToList() ?? new List<TestCaseVariableOutput>(),
                ExpectedOutputs = ExpectedOutputs?.Select(v => v.Clone()).ToList() ?? new List<TestCaseVariableOutput>(),
                ExpectedReturnValue = ExpectedReturnValue,
                ActualReturnValue = ActualReturnValue,
                Status = Status,
                Tags = Tags != null ? new List<string>(Tags) : new List<string>(),
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>(),
                Creator = Creator,
                CreationDate = CreationDate,
                LastModifiedDate = LastModifiedDate,
                Stubs = Stubs?.Select(s => s.Clone()).ToList() ?? new List<FunctionStub>(),
                SetupCode = SetupCode,
                TeardownCode = TeardownCode
            };
        }

        /// <summary>
        /// Convert from legacy TestCase format (with old TestCaseVariable)
        /// </summary>
        public static TestCase FromLegacyFormat(TestCase legacyTestCase, List<TestCaseVariableInput> inputVars, List<TestCaseVariableOutput> outputVars)
        {
            var newTestCase = new TestCase
            {
                Id = legacyTestCase.Id,
                Name = legacyTestCase.Name,
                Description = legacyTestCase.Description,
                Type = legacyTestCase.Type,
                FunctionName = legacyTestCase.FunctionName,
                ExpectedOutputs = new List<TestCaseVariableOutput>(),
                ExpectedReturnValue = legacyTestCase.ExpectedReturnValue,
                ActualReturnValue = legacyTestCase.ActualReturnValue,
                Status = legacyTestCase.Status,
                Tags = legacyTestCase.Tags != null ? new List<string>(legacyTestCase.Tags) : new List<string>(),
                Properties = legacyTestCase.Properties != null ? new Dictionary<string, string>(legacyTestCase.Properties) : new Dictionary<string, string>(),
                Creator = legacyTestCase.Creator,
                CreationDate = legacyTestCase.CreationDate,
                LastModifiedDate = legacyTestCase.LastModifiedDate
            };

            // Convert input variables
            foreach (var inputVar in inputVars)
            {
                newTestCase.InputVariables.Add(new TestCaseVariableInput
                {
                    Id = inputVar.Id,
                    Name = inputVar.Name,
                    Type = inputVar.Type,
                    Value = inputVar.Value,
                    IsArray = inputVar.IsArray,
                    ArraySize = inputVar.ArraySize,
                    ArrayValues = inputVar.ArrayValues != null ? new List<string>(inputVar.ArrayValues) : new List<string>(),
                    IsPointer = inputVar.IsPointer,
                    IsByReference = inputVar.IsByReference,
                    Constraints = inputVar.Constraints?.Select(c => c.Clone()).ToList() ?? new List<VariableConstraint>()
                });
            }

            // Convert output variables
            foreach (var outputVar in outputVars)
            {
                newTestCase.OutputVariables.Add(new TestCaseVariableOutput
                {
                    Id = outputVar.Id,
                    Name = outputVar.Name,
                    Type = outputVar.Type,
                    ExpectedValue = outputVar.ExpectedValue,
                    IsArray = outputVar.IsArray,
                    ArraySize = outputVar.ArraySize,
                    ExpectedArrayValues = outputVar.ExpectedArrayValues != null ? new List<string>(outputVar.ExpectedArrayValues) : new List<string>(),
                    IsPointer = outputVar.IsPointer,
                    IsByReference = outputVar.IsByReference,
                    Constraints = outputVar.Constraints?.Select(c => c.Clone()).ToList() ?? new List<VariableConstraint>()
                });
            }

            return newTestCase;
        }
    }

    /// <summary>
    /// Base class for test case variables
    /// </summary>
    public abstract class TestCaseVariableBase : IModelObject
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
        /// Constraints on the variable
        /// </summary>
        public List<VariableConstraint> Constraints { get; set; } = new List<VariableConstraint>();

        /// <summary>
        /// Get a string representation of the test case variable
        /// </summary>
        public override string ToString()
        {
            string arrayPart = IsArray ? $"[{ArraySize}]" : "";
            string pointerPart = IsPointer ? "*" : "";
            string refPart = IsByReference ? "&" : "";

            return $"{Type}{pointerPart}{refPart} {Name}{arrayPart}";
        }
    }

    /// <summary>
    /// Represents an input variable in a test case
    /// </summary>
    public class TestCaseVariableInput : TestCaseVariableBase
    {
        /// <summary>
        /// Value of the input variable
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Array values (if IsArray is true)
        /// </summary>
        public List<string> ArrayValues { get; set; } = new List<string>();

        /// <summary>
        /// Whether the input value is generated by a solver
        /// </summary>
        public bool IsGeneratedValue { get; set; }

        /// <summary>
        /// Source of the generated value (e.g., solver query ID)
        /// </summary>
        public string GenerationSource { get; set; }

        /// <summary>
        /// Whether the input is a stub parameter
        /// </summary>
        public bool IsStubParameter { get; set; }

        /// <summary>
        /// Create a clone of the input variable
        /// </summary>
        public TestCaseVariableInput Clone()
        {
            return new TestCaseVariableInput
            {
                Id = Id,
                Name = Name,
                Type = Type,
                Value = Value,
                IsArray = IsArray,
                ArraySize = ArraySize,
                ArrayValues = ArrayValues != null ? new List<string>(ArrayValues) : new List<string>(),
                IsPointer = IsPointer,
                IsByReference = IsByReference,
                Constraints = Constraints?.Select(c => c.Clone()).ToList() ?? new List<VariableConstraint>(),
                IsGeneratedValue = IsGeneratedValue,
                GenerationSource = GenerationSource,
                IsStubParameter = IsStubParameter
            };
        }

        /// <summary>
        /// Get a string representation of the input variable
        /// </summary>
        public override string ToString()
        {
            string baseStr = base.ToString();
            return $"{baseStr} = {Value}";
        }
    }

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
        /// Creator of the test suite
        /// </summary>
        public string Creator { get; set; }

        /// <summary>
        /// Creation date of the test suite
        /// </summary>
        public DateTime CreationDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Last modified date of the test suite
        /// </summary>
        public DateTime LastModifiedDate { get; set; } = DateTime.Now;

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
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>(),
                Creator = Creator,
                CreationDate = CreationDate,
                LastModifiedDate = LastModifiedDate
            };
        }
    }

    #endregion
}
