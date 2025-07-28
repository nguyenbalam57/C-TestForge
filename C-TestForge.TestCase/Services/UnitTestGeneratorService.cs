using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using C_TestForge.Models;
using C_TestForge.Models.TestCases;

namespace C_TestForge.TestCase.Services
{
    public class UnitTestGeneratorService : IUnitTestGeneratorService
    {
        private readonly IVariableValueFinderService _variableValueFinder;
        private readonly IParserService _parserService;
        private readonly IStubGeneratorService _stubGenerator;

        public UnitTestGeneratorService(
            IVariableValueFinderService variableValueFinder,
            IParserService parserService,
            IStubGeneratorService stubGenerator)
        {
            _variableValueFinder = variableValueFinder ?? throw new ArgumentNullException(nameof(variableValueFinder));
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _stubGenerator = stubGenerator ?? throw new ArgumentNullException(nameof(stubGenerator));
        }

        public List<TestCaseUser> GenerateTestCasesForFunction(CFunction function, List<CVariable> availableVariables)
        {
            List<TestCaseUser> testCases = new List<TestCaseUser>();

            // 1. Analyze function to identify branches
            var branches = AnalyzeFunctionBranches(function);

            // 2. Generate test cases for branch coverage
            foreach (var branch in branches)
            {
                try
                {
                    // Find input values to cover this branch
                    var inputValues = _variableValueFinder.FindValuesForBranchCoverage(
                        function, availableVariables, branch);

                    if (inputValues.Any())
                    {
                        // Create a test case for this branch
                        var testCase = new TestCaseUser
                        {
                            Id = Guid.NewGuid(),
                            Name = $"Test_{function.Name}_{testCases.Count + 1}",
                            Description = $"Test case for {function.Name} - Branch: {branch}",
                            InputParameters = ConvertToTestParameters(inputValues, function),
                            FunctionUnderTest = function.Name
                        };

                        // For output parameters, we'll need to execute the function or use stub behavior
                        // Here we'll set placeholder values for now
                        testCase.ExpectedOutputs = new List<CVariable>();

                        if (!string.IsNullOrEmpty(function.ReturnType) && function.ReturnType != "void")
                        {
                            testCase.ExpectedOutputs.Add(new CVariable
                            {
                                Name = "return_value",
                                Type = function.ReturnType,
                                Value = GetDefaultValue(function.ReturnType)
                            });
                        }

                        testCases.Add(testCase);
                    }
                }
                catch (Exception ex)
                {
                    // Log the error and continue with next branch
                    Console.WriteLine($"Error generating test case for branch {branch}: {ex.Message}");
                }
            }

            // 3. Generate stubs for called functions
            var calledFunctions = AnalyzeFunctionCalls(function);
            var stubs = new List<StubFunction>();

            foreach (var calledFunction in calledFunctions)
            {
                var stub = _stubGenerator.GenerateStub(calledFunction);
                if (stub != null)
                {
                    stubs.Add(stub);
                }
            }

            // 4. Attach stubs to test cases
            foreach (var testCase in testCases)
            {
                testCase.RequiredStubs = stubs;
            }

            return testCases;
        }

        public string GenerateTestCode(TestCaseUser testCase, string templateFormat = "c")
        {
            StringBuilder code = new StringBuilder();

            switch (templateFormat.ToLower())
            {
                case "c":
                    code.AppendLine($"// Test case for {testCase.FunctionUnderTest}");
                    code.AppendLine($"// {testCase.Description}");
                    code.AppendLine();

                    // Include necessary headers
                    code.AppendLine("#include <stdio.h>");
                    code.AppendLine("#include <stdlib.h>");
                    code.AppendLine("#include <string.h>");
                    code.AppendLine("#include \"test_framework.h\"");
                    code.AppendLine("#include \"module_under_test.h\"");
                    code.AppendLine();

                    // Generate stub function implementations
                    if (testCase.RequiredStubs != null && testCase.RequiredStubs.Any())
                    {
                        code.AppendLine("// Stub implementations");
                        foreach (var stub in testCase.RequiredStubs)
                        {
                            code.Append(_stubGenerator.GenerateStubImplementation(stub, "c"));
                        }
                        code.AppendLine();
                    }

                    // Generate test function
                    code.AppendLine($"void {testCase.Name}(void)");
                    code.AppendLine("{");

                    // Declare variables for input parameters
                    code.AppendLine("    // Input parameters");
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
                    foreach (var output in testCase.ExpectedOutputs ?? new List<CVariable>())
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

        private List<string> AnalyzeFunctionBranches(CFunction function)
        {
            // In a real implementation, this would involve analyzing the AST and
            // identifying all possible execution paths through the function.

            // For simplicity, we'll use a basic approach to extract conditions
            List<string> branches = new List<string>();

            if (!string.IsNullOrEmpty(function.Body))
            {
                // Extract all if conditions
                var ifConditions = System.Text.RegularExpressions.Regex.Matches(function.Body, @"if\s*\((.*?)\)");
                foreach (System.Text.RegularExpressions.Match match in ifConditions)
                {
                    if (match.Groups.Count > 1)
                    {
                        string condition = match.Groups[1].Value.Trim();
                        branches.Add(condition);        // True branch
                        branches.Add($"!({condition})"); // False branch
                    }
                }

                // Add other control structures as needed (while, for, etc.)
            }

            // If no branches were found, add a default "true" branch
            if (!branches.Any())
            {
                branches.Add("true");
            }

            return branches;
        }

        private List<CFunction> AnalyzeFunctionCalls(CFunction function)
        {
            // In a real implementation, this would involve analyzing the AST to
            // identify all function calls made within the function body.

            List<CFunction> calledFunctions = new List<CFunction>();

            if (!string.IsNullOrEmpty(function.Body))
            {
                // This is a simplistic approach. A more robust solution would
                // parse the AST to find function calls accurately.

                // Get all functions from the parser
                var allFunctions = _parserService.GetFunctions();

                foreach (var potentiallyCalledFunction in allFunctions)
                {
                    // Skip the function itself
                    if (potentiallyCalledFunction.Name == function.Name)
                        continue;

                    // Check if the function body contains a call to this function
                    // This is a simple heuristic that might have false positives
                    string pattern = $@"{potentiallyCalledFunction.Name}\s*\(";
                    if (System.Text.RegularExpressions.Regex.IsMatch(function.Body, pattern))
                    {
                        calledFunctions.Add(potentiallyCalledFunction);
                    }
                }
            }

            return calledFunctions;
        }

        private List<CVariable> ConvertToTestParameters(Dictionary<string, object> values, CFunction function)
        {
            List<CVariable> parameters = new List<CVariable>();

            foreach (var param in function.Parameters)
            {
                if (values.TryGetValue(param.Name, out object value))
                {
                    parameters.Add(new CVariable
                    {
                        Name = param.Name,
                        Type = param.Type,
                        Value = value
                    });
                }
                else
                {
                    // If no value was found, use a default value
                    parameters.Add(new CVariable
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

        private string FormatAssertValue(CVariable parameter)
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

    public interface IUnitTestGeneratorService
    {
        List<TestCaseUser> GenerateTestCasesForFunction(CFunction function, List<CVariable> availableVariables);
        string GenerateTestCode(TestCaseUser testCase, string templateFormat = "c");
    }
}