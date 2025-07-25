using C_TestForge.Core.Interfaces;
using C_TestForge.Models;
using C_TestForge.Models.TestCases;
using C_TestForge.TestCase.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace C_TestForge.TestCase.Services
{
    public class IntegrationTestGeneratorService : IIntegrationTestGeneratorService
    {
        private readonly IParserService _parserService;
        private readonly IVariableValueFinderService _variableValueFinder;
        private readonly IUnitTestGeneratorService _unitTestGenerator;

        public IntegrationTestGeneratorService(
            IParserService parserService,
            IVariableValueFinderService variableValueFinder,
            IUnitTestGeneratorService unitTestGenerator)
        {
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _variableValueFinder = variableValueFinder ?? throw new ArgumentNullException(nameof(variableValueFinder));
            _unitTestGenerator = unitTestGenerator ?? throw new ArgumentNullException(nameof(unitTestGenerator));
        }

        public List<TestCase> GenerateIntegrationTests(List<string> functionNames, List<CVariable> availableVariables)
        {
            List<TestCase> testCases = new List<TestCase>();

            // 1. Load all functions
            var allFunctions = _parserService.GetFunctions();

            // 2. Filter functions to include only those in the list
            var functionsToTest = allFunctions
                .Where(f => functionNames.Contains(f.Name))
                .ToList();

            if (!functionsToTest.Any())
                return testCases;

            // 3. Build function dependency graph
            var dependencyGraph = BuildFunctionDependencyGraph(functionsToTest);

            // 4. Get entry points (functions that aren't called by others in the set)
            var entryPoints = GetEntryPoints(functionsToTest, dependencyGraph);

            // 5. Generate integration tests for each entry point
            foreach (var entryPoint in entryPoints)
            {
                var testCase = GenerateIntegrationTestForFunction(
                    entryPoint,
                    dependencyGraph,
                    availableVariables,
                    functionsToTest);

                if (testCase != null)
                {
                    testCases.Add(testCase);
                }
            }

            return testCases;
        }

        public string GenerateIntegrationTestCode(TestCase testCase, string templateFormat = "c")
        {
            StringBuilder code = new StringBuilder();

            switch (templateFormat.ToLower())
            {
                case "c":
                    code.AppendLine($"// Integration Test for {testCase.FunctionUnderTest}");
                    code.AppendLine($"// {testCase.Description}");
                    code.AppendLine();

                    // Include necessary headers
                    code.AppendLine("#include <stdio.h>");
                    code.AppendLine("#include <stdlib.h>");
                    code.AppendLine("#include <string.h>");
                    code.AppendLine("#include \"test_framework.h\"");
                    code.AppendLine("#include \"module_under_test.h\"");
                    code.AppendLine();

                    // Generate test function
                    code.AppendLine($"void {testCase.Name}(void)");
                    code.AppendLine("{");

                    // Setup variables for input parameters
                    code.AppendLine("    // Setup");
                    foreach (var input in testCase.InputParameters)
                    {
                        code.AppendLine($"    {input.Type} {input.Name} = {FormatValueForC(input.Value, input.Type)};");
                    }
                    code.AppendLine();

                    // Declare variables for expected outputs
                    if (testCase.ExpectedOutputs != null && testCase.ExpectedOutputs.Any())
                    {
                        code.AppendLine("    // Expected outputs");
                        foreach (var output in testCase.ExpectedOutputs)
                        {
                            code.AppendLine($"    {output.Type} expected_{output.Name} = {FormatValueForC(output.Value, output.Type)};");
                        }
                        code.AppendLine();
                    }

                    // Call the function under test
                    code.AppendLine("    // Call function under test");
                    string functionCall = $"{testCase.FunctionUnderTest}(";
                    functionCall += string.Join(", ", testCase.InputParameters.Select(p => p.Name));
                    functionCall += ")";

                    // Handle return value if any
                    var returnOutput = testCase.ExpectedOutputs?.FirstOrDefault(o => o.Name == "return_value");
                    if (returnOutput != null)
                    {
                        code.AppendLine($"    {returnOutput.Type} actual_return = {functionCall};");
                    }
                    else
                    {
                        code.AppendLine($"    {functionCall};");
                    }
                    code.AppendLine();

                    // Verify results
                    code.AppendLine("    // Verify results");
                    if (returnOutput != null)
                    {
                        code.AppendLine($"    TEST_ASSERT_EQUAL({FormatAssertValue(returnOutput)}, actual_return);");
                    }

                    // Check other outputs (parameters passed by reference)
                    foreach (var output in testCase.ExpectedOutputs ?? new List<TestParameter>())
                    {
                        if (output.Name != "return_value")
                        {
                            code.AppendLine($"    TEST_ASSERT_EQUAL({FormatAssertValue(output)}, {output.Name});");
                        }
                    }

                    code.AppendLine("}");
                    break;

                default:
                    code.AppendLine($"Unsupported template format: {templateFormat}");
                    break;
            }

            return code.ToString();
        }

        private Dictionary<string, List<string>> BuildFunctionDependencyGraph(List<CFunction> functions)
        {
            var graph = new Dictionary<string, List<string>>();

            foreach (var function in functions)
            {
                graph[function.Name] = new List<string>();

                if (!string.IsNullOrEmpty(function.Body))
                {
                    foreach (var potentiallyCalledFunction in functions)
                    {
                        // Skip self-calls
                        if (potentiallyCalledFunction.Name == function.Name)
                            continue;

                        // Check if function body contains a call to this function
                        string pattern = $@"{potentiallyCalledFunction.Name}\s*\(";
                        if (System.Text.RegularExpressions.Regex.IsMatch(function.Body, pattern))
                        {
                            graph[function.Name].Add(potentiallyCalledFunction.Name);
                        }
                    }
                }
            }

            return graph;
        }

        private List<CFunction> GetEntryPoints(List<CFunction> functions, Dictionary<string, List<string>> dependencyGraph)
        {
            var allCalledFunctions = new HashSet<string>(dependencyGraph.Values.SelectMany(v => v));

            return functions
                .Where(f => !allCalledFunctions.Contains(f.Name))
                .ToList();
        }

        private TestCase GenerateIntegrationTestForFunction(
            CFunction entryPoint,
            Dictionary<string, List<string>> dependencyGraph,
            List<CVariable> availableVariables,
            List<CFunction> allFunctions)
        {
            try
            {
                // Create a basic test case first
                var testCase = new TestCase
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = $"ITST_{entryPoint.Name}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                    Description = $"Integration test for {entryPoint.Name}",
                    FunctionUnderTest = entryPoint.Name,
                    Type = TestCaseType.IntegrationTest
                };

                // Generate input parameters using the variable finder
                var inputValues = _variableValueFinder.FindValuesForBranchCoverage(
                    entryPoint, availableVariables, "true");

                testCase.InputParameters = ConvertToTestParameters(inputValues, entryPoint);

                // For output parameters, we'll need placeholder values for now
                testCase.ExpectedOutputs = new List<TestParameter>();

                if (!string.IsNullOrEmpty(entryPoint.ReturnType) && entryPoint.ReturnType != "void")
                {
                    testCase.ExpectedOutputs.Add(new TestParameter
                    {
                        Name = "return_value",
                        Type = entryPoint.ReturnType,
                        Value = GetDefaultValue(entryPoint.ReturnType)
                    });
                }

                // Add additional data about function call chain
                var callChain = GetFunctionCallChain(entryPoint.Name, dependencyGraph, allFunctions);
                if (callChain.Any())
                {
                    testCase.Comments = $"Function call chain: {string.Join(" -> ", callChain)}";
                }

                return testCase;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating integration test for {entryPoint.Name}: {ex.Message}");
                return null;
            }
        }

        private List<string> GetFunctionCallChain(
            string functionName,
            Dictionary<string, List<string>> dependencyGraph,
            List<CFunction> allFunctions,
            HashSet<string> visited = null)
        {
            if (visited == null)
                visited = new HashSet<string>();

            if (visited.Contains(functionName))
                return new List<string>(); // Prevent cycles

            visited.Add(functionName);

            List<string> result = new List<string> { functionName };

            if (dependencyGraph.TryGetValue(functionName, out List<string> calledFunctions))
            {
                foreach (var calledFunction in calledFunctions)
                {
                    var subChain = GetFunctionCallChain(calledFunction, dependencyGraph, allFunctions, visited);
                    result.AddRange(subChain);
                }
            }

            return result;
        }

        private List<TestParameter> ConvertToTestParameters(Dictionary<string, object> values, CFunction function)
        {
            List<TestParameter> parameters = new List<TestParameter>();

            foreach (var param in function.Parameters)
            {
                if (values.TryGetValue(param.Name, out object value))
                {
                    parameters.Add(new TestParameter
                    {
                        Name = param.Name,
                        Type = param.Type,
                        Value = value
                    });
                }
                else
                {
                    // If no value was found, use a default value
                    parameters.Add(new TestParameter
                    {
                        Name = param.Name,
                        Type = param.Type,
                        Value = GetDefaultValue(param.Type)
                    });
                }
            }

            return parameters;
        }

        private object GetDefaultValue(string type)
        {
            // Return appropriate default values based on C type
            switch (type.ToLower())
            {
                case "int":
                case "int32_t":
                case "int16_t":
                case "int8_t":
                case "uint32_t":
                case "uint16_t":
                case "uint8_t":
                case "unsigned int":
                case "unsigned char":
                case "short":
                case "long":
                    return 0;

                case "float":
                case "double":
                    return 0.0;

                case "char":
                    return '\0';

                case "bool":
                    return false;

                case "char*":
                case "const char*":
                    return "";

                default:
                    if (type.EndsWith("*"))
                        return "NULL";
                    return 0;
            }
        }

        private string FormatValueForC(object value, string type)
        {
            if (value == null)
                return "NULL";

            // Format the value based on C type
            switch (type.ToLower())
            {
                case "char*":
                case "const char*":
                    return $"\"{value}\"";

                case "char":
                    if (value is string s && s.Length > 0)
                        return $"'{s[0]}'";
                    else if (value is char c)
                        return $"'{c}'";
                    return "'\\0'";

                case "float":
                    return $"{value}f";

                case "bool":
                    return (value is bool b && b) || (value is string str && (str == "true" || str == "1")) ? "true" : "false";

                default:
                    if (type.EndsWith("*") && !(value is string) && value.GetType().IsValueType)
                        return "NULL";
                    return value.ToString();
            }
        }

        private string FormatAssertValue(TestParameter parameter)
        {
            if (parameter.Type.ToLower() == "char*" || parameter.Type.ToLower() == "const char*")
            {
                return $"expected_{parameter.Name}";
            }
            else
            {
                return $"expected_{parameter.Name}";
            }
        }
    }

    public interface IIntegrationTestGeneratorService
    {
        List<TestCase> GenerateIntegrationTests(List<string> functionNames, List<CVariable> availableVariables);
        string GenerateIntegrationTestCode(TestCase testCase, string templateFormat = "c");
    }
}