using C_TestForge.Models.Base;
using C_TestForge.Models.TestGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
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
}
