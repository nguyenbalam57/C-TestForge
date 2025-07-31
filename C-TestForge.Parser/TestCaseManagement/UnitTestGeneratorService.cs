using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Models.TestCases;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Parser.TestCaseManagement
{
    /// <summary>
    /// Implementation of IUnitTestGeneratorService for generating unit tests
    /// </summary>
    public class UnitTestGeneratorService : IUnitTestGeneratorService
    {
        private readonly IParser _parser;
        private readonly ITestCodeGeneratorService _testCodeGeneratorService;

        public UnitTestGeneratorService(IParser parser, ITestCodeGeneratorService testCodeGeneratorService)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _testCodeGeneratorService = testCodeGeneratorService ?? throw new ArgumentNullException(nameof(testCodeGeneratorService));
        }

        /// <summary>
        /// Generates unit tests for the given function
        /// </summary>
        public async Task<List<TestCase>> GenerateUnitTestsAsync(
            string functionName,
            string filePath,
            double targetCoverage = 0.9)
        {
            // Parse source file to get function information
            var sourceFile = await _parser.ParseSourceFileAsync(filePath);
            var function = sourceFile.ParseResult.Functions.FirstOrDefault(f => f.Name == functionName);

            if (function == null)
            {
                throw new ArgumentException($"Function '{functionName}' not found in file '{filePath}'");
            }

            // Generate test cases based on function analysis
            var testCases = new List<TestCase>();

            // Create a basic test case
            var testCase = new TestCase
            {
                Name = $"Test_{functionName}_Basic",
                Description = $"Basic unit test for {functionName}",
                Type = TestCaseType.UnitTest,
                FunctionName = functionName,
                Status = TestCaseStatus.NotRun,
                CreationDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            // Add input variables based on function parameters
            foreach (var param in function.Parameters)
            {
                testCase.InputVariables.Add(new TestCaseVariableInput
                {
                    Name = param.Name,
                    Type = param.TypeName,
                    Value = GenerateDefaultValueForType(param.TypeName)
                });
            }

            // Add output variables if needed
            if (function.ReturnType != "void")
            {
                testCase.OutputVariables.Add(new TestCaseVariableOutput
                {
                    Name = "returnValue",
                    Type = function.ReturnType,
                    ExpectedValue = GenerateDefaultValueForType(function.ReturnType)
                });
            }

            testCases.Add(testCase);

            // In a real implementation, we would:
            // 1. Analyze function logic to identify branches and edge cases
            // 2. Use solver to find input values for each branch
            // 3. Generate multiple test cases to achieve target coverage

            return testCases;
        }

        /// <summary>
        /// Generates unit tests with specific inputs and outputs
        /// </summary>
        public async Task<TestCase> GenerateUnitTestWithValuesAsync(
            string functionName,
            string filePath,
            Dictionary<string, string> inputs,
            Dictionary<string, string> expectedOutputs)
        {
            // Parse source file to get function information
            var sourceFile = await _parser.ParseSourceFileAsync(filePath);
            var function = sourceFile.ParseResult.Functions.FirstOrDefault(f => f.Name == functionName);

            if (function == null)
            {
                throw new ArgumentException($"Function '{functionName}' not found in file '{filePath}'");
            }

            // Create a test case with the specified inputs and outputs
            var testCase = new TestCase
            {
                Name = $"Test_{functionName}_CustomValues",
                Description = $"Custom unit test for {functionName} with specific values",
                Type = TestCaseType.UnitTest,
                FunctionName = functionName,
                Status = TestCaseStatus.NotRun,
                CreationDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            // Add input variables based on function parameters
            foreach (var param in function.Parameters)
            {
                var value = inputs.ContainsKey(param.Name) ? inputs[param.Name] : GenerateDefaultValueForType(param.TypeName);

                testCase.InputVariables.Add(new TestCaseVariableInput
                {
                    Name = param.Name,
                    Type = param.TypeName,
                    Value = value
                });
            }

            // Add expected return value if applicable
            if (function.ReturnType != "void" && expectedOutputs.ContainsKey("returnValue"))
            {
                testCase.ExpectedReturnValue = expectedOutputs["returnValue"];

                testCase.OutputVariables.Add(new TestCaseVariableOutput
                {
                    Name = "returnValue",
                    Type = function.ReturnType,
                    ExpectedValue = expectedOutputs["returnValue"]
                });
            }

            // Add any other expected outputs
            foreach (var output in expectedOutputs.Where(o => o.Key != "returnValue"))
            {
                // In a real implementation, we would determine the output variable type
                testCase.OutputVariables.Add(new TestCaseVariableOutput
                {
                    Name = output.Key,
                    Type = "auto", // Placeholder, would be determined from context
                    ExpectedValue = output.Value
                });
            }

            return testCase;
        }

        /// <summary>
        /// Generates unit test code for the given test case
        /// </summary>
        public async Task<string> GenerateUnitTestCodeAsync(
            TestCase testCase,
            string filePath,
            string framework = "unity")
        {
            // Parse source file to get function information
            var sourceFile = await _parser.ParseSourceFileAsync(filePath);
            var function = sourceFile.ParseResult.Functions.FirstOrDefault(f => f.Name == testCase.FunctionName);

            if (function == null)
            {
                throw new ArgumentException($"Function '{testCase.FunctionName}' not found in file '{filePath}'");
            }

            // Generate test code for the test case using the test code generator service
            return await _testCodeGeneratorService.GenerateTestCodeAsync(
                new List<TestCase> { testCase },
                filePath,
                framework);
        }

        /// <summary>
        /// Generates a default value for a C type
        /// </summary>
        private string GenerateDefaultValueForType(string type)
        {
            type = type.Trim();

            if (type.Contains("*"))
                return "NULL";

            switch (type)
            {
                case "int":
                case "long":
                case "short":
                    return "0";
                case "unsigned int":
                case "unsigned long":
                case "unsigned short":
                case "size_t":
                    return "0";
                case "float":
                case "double":
                    return "0.0";
                case "char":
                    return "'a'";
                case "bool":
                    return "false";
                case "char*":
                case "const char*":
                    return "\"test\"";
                default:
                    return "0";
            }
        }
    }
}
