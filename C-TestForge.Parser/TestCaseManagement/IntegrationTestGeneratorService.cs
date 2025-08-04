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
    /// Implementation of IIntegrationTestGeneratorService for generating integration tests
    /// </summary>
    public class IntegrationTestGeneratorService : IIntegrationTestGeneratorService
    {
        private readonly IClangSharpParserService _parser;
        private readonly ITestCodeGeneratorService _testCodeGeneratorService;

        public IntegrationTestGeneratorService(IClangSharpParserService parser, ITestCodeGeneratorService testCodeGeneratorService)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _testCodeGeneratorService = testCodeGeneratorService ?? throw new ArgumentNullException(nameof(testCodeGeneratorService));
        }

        /// <summary>
        /// Generates integration tests for the given functions
        /// </summary>
        public async Task<List<TestCase>> GenerateIntegrationTestsAsync(
            List<string> functionNames,
            string filePath,
            double targetCoverage = 0.9)
        {
            if (functionNames == null || !functionNames.Any())
            {
                throw new ArgumentException("Function names list cannot be empty");
            }

            // Parse source file to get function information
            var sourceFile = await _parser.ParseSourceFileAsync(filePath);

            // Verify all functions exist in the source file
            foreach (var functionName in functionNames)
            {
                if (!sourceFile.ParseResult.Functions.Any(f => f.Name == functionName))
                {
                    throw new ArgumentException($"Function '{functionName}' not found in file '{filePath}'");
                }
            }

            // Generate test cases for integration testing
            var testCases = new List<TestCase>();

            // Create a basic integration test case
            var testCase = new TestCase
            {
                Name = $"IntegrationTest_{string.Join("_", functionNames)}",
                Description = $"Integration test for {string.Join(", ", functionNames)}",
                Type = TestCaseType.IntegrationTest,
                FunctionName = functionNames[0], // Primary function
                Status = TestCaseStatus.NotRun,
                CreationDate = DateTime.Now,
                LastModifiedDate = DateTime.Now
            };

            // Add input variables for the primary function
            var primaryFunction = sourceFile.ParseResult.Functions.First(f => f.Name == functionNames[0]);
            foreach (var param in primaryFunction.Parameters)
            {
                testCase.InputVariables.Add(new TestCaseVariableInput
                {
                    Name = param.Name,
                    Type = param.TypeName,
                    Value = GenerateDefaultValueForType(param.TypeName)
                });
            }

            // Add expected output for the primary function
            if (primaryFunction.ReturnType != "void")
            {
                testCase.OutputVariables.Add(new TestCaseVariableOutput
                {
                    Name = "returnValue",
                    Type = primaryFunction.ReturnType,
                    ExpectedValue = GenerateDefaultValueForType(primaryFunction.ReturnType)
                });
            }

            testCases.Add(testCase);

            // In a real implementation, we would:
            // 1. Analyze relationships between functions
            // 2. Identify data flow between functions
            // 3. Generate test cases that verify correct integration

            return testCases;
        }

        /// <summary>
        /// Generates integration tests for the function call graph
        /// </summary>
        public async Task<List<TestCase>> GenerateIntegrationTestsForCallGraphAsync(
            string rootFunctionName,
            string filePath,
            int depth = 3)
        {
            // Parse source file to get function information
            var sourceFile = await _parser.ParseSourceFileAsync(filePath);
            var rootFunction = sourceFile.ParseResult.Functions.FirstOrDefault(f => f.Name == rootFunctionName);

            if (rootFunction == null)
            {
                throw new ArgumentException($"Function '{rootFunctionName}' not found in file '{filePath}'");
            }

            // Build the call graph up to the specified depth
            var callGraph = new Dictionary<string, List<string>>();
            BuildCallGraph(rootFunctionName, sourceFile, callGraph, 0, depth);

            // Get all functions in the call graph
            var functionNames = callGraph.Keys.ToList();

            // Generate integration tests for these functions
            return await GenerateIntegrationTestsAsync(functionNames, filePath);
        }

        /// <summary>
        /// Generates integration test code for the given test case
        /// </summary>
        public async Task<string> GenerateIntegrationTestCodeAsync(
            TestCase testCase,
            string filePath,
            string framework = "unity")
        {
            if (testCase.Type != TestCaseType.IntegrationTest)
            {
                throw new ArgumentException("Test case must be an integration test");
            }

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
        /// Builds a call graph for the specified root function
        /// </summary>
        private void BuildCallGraph(
            string functionName,
            dynamic sourceFile,
            Dictionary<string, List<string>> callGraph,
            int currentDepth,
            int maxDepth)
        {
            // Stop if we've reached the maximum depth
            if (currentDepth >= maxDepth)
            {
                return;
            }

            // Add the function to the call graph if not already present
            if (!callGraph.ContainsKey(functionName))
            {
                callGraph[functionName] = new List<string>();

                // Get the function from the source file
                //var function = sourceFile.Functions.FirstOrDefault(f => f.Name == functionName);

                //if (function != null)
                //{
                //    // Add called functions to the call graph
                //    foreach (var calledFunction in function.CalledFunctions)
                //    {
                //        callGraph[functionName].Add(calledFunction);

                //        // Recursively build the call graph for called functions
                //        BuildCallGraph(calledFunction, sourceFile, callGraph, currentDepth + 1, maxDepth);
                //    }
                //}
            }
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
