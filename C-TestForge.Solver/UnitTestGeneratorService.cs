using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;
using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Solver;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.TestCaseManagement;

namespace C_TestForge.Solver
{
    /// <summary>
    /// Service for generating unit tests
    /// </summary>
    public class UnitTestGeneratorService : IUnitTestGeneratorService
    {
        private readonly IClangSharpParserService _parserService;
        private readonly IVariableValueFinderService _variableValueFinderService;
        private readonly IStubGeneratorService _stubGeneratorService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parserService">The parser service</param>
        /// <param name="variableValueFinderService">The variable value finder service</param>
        /// <param name="stubGeneratorService">The stub generator service</param>
        public UnitTestGeneratorService(
            IClangSharpParserService parserService,
            IVariableValueFinderService variableValueFinderService,
            IStubGeneratorService stubGeneratorService)
        {
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _variableValueFinderService = variableValueFinderService ?? throw new ArgumentNullException(nameof(variableValueFinderService));
            _stubGeneratorService = stubGeneratorService ?? throw new ArgumentNullException(nameof(stubGeneratorService));
        }

        /// <summary>
        /// Generates unit tests for the given function
        /// </summary>
        public async Task<List<TestCaseModels>> GenerateUnitTestsAsync(string functionName, string filePath, double targetCoverage = 0.9)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException(nameof(functionName));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            // Get function info
            var functions = await _parserService.ExtractFunctionsAsync(filePath);
            var function = functions.FirstOrDefault(f => f.Name == functionName);
            if (function == null)
                throw new InvalidOperationException($"Function not found: {functionName}");

            // Find values for max coverage
            var valuesSets = await _variableValueFinderService.FindValuesForMaxCoverageAsync(
                functionName,
                filePath,
                targetCoverage);

            // Create test cases
            var testCases = new List<TestCase>();
            int testCaseId = 1;

            foreach (var values in valuesSets)
            {
                var testCase = new TestCase
                {
                    Id = testCaseId++,
                    Name = $"{functionName}_Test_{testCaseId}",
                    Description = $"Auto-generated unit test for {functionName}",
                    FunctionName = functionName,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "C-TestForge",
                    Type = TestCaseType.UnitTest,
                    Status = TestCaseStatus.Draft,
                    Inputs = new List<TestCaseInput>(),
                    ExpectedOutputs = new List<TestCaseOutput>()
                };

                // Add inputs
                foreach (var param in function.Parameters)
                {
                    testCase.Inputs.Add(new TestCaseInput
                    {
                        TestCaseId = testCase.Id,
                        ParameterName = param.Name,
                        VariableName = param.Name,
                        DataType = param.Type,
                        Value = values.TryGetValue(param.Name, out var value) ? value : "0",
                        IsArray = param.IsArray,
                        ArraySize = param.ArraySize
                    });
                }

                // Add return value as expected output
                testCase.ExpectedOutputs.Add(new TestCaseOutput
                {
                    TestCaseId = testCase.Id,
                    ParameterName = "return",
                    VariableName = "return",
                    DataType = function.ReturnType,
                    Value = "0", // Placeholder - in a real implementation, this would be calculated
                    IsReturnValue = true
                });

                testCases.Add(testCase);
            }

            return testCases;
        }

        /// <summary>
        /// Generates unit tests with specific inputs and outputs
        /// </summary>
        public async Task<TestCase> GenerateUnitTestWithValuesAsync(string functionName, string filePath, Dictionary<string, string> inputs, Dictionary<string, string> expectedOutputs)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException(nameof(functionName));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (inputs == null)
                throw new ArgumentNullException(nameof(inputs));
            if (expectedOutputs == null)
                throw new ArgumentNullException(nameof(expectedOutputs));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            // Get function info
            var functions = await _parserService.ExtractFunctionsAsync(filePath);
            var function = functions.FirstOrDefault(f => f.Name == functionName);
            if (function == null)
                throw new InvalidOperationException($"Function not found: {functionName}");

            // Create test case
            var testCase = new TestCase
            {
                Id = 1, // Placeholder
                Name = $"{functionName}_Test_Custom",
                Description = $"Unit test for {functionName} with custom values",
                FunctionName = functionName,
                CreatedDate = DateTime.Now,
                CreatedBy = "C-TestForge",
                Type = TestCaseType.UnitTest,
                Status = TestCaseStatus.Draft,
                Inputs = new List<TestCaseInput>(),
                ExpectedOutputs = new List<TestCaseOutput>()
            };

            // Add inputs
            foreach (var param in function.Parameters)
            {
                testCase.Inputs.Add(new TestCaseInput
                {
                    TestCaseId = testCase.Id,
                    ParameterName = param.Name,
                    VariableName = param.Name,
                    DataType = param.Type,
                    Value = inputs.TryGetValue(param.Name, out var value) ? value : "0",
                    IsArray = param.IsArray,
                    ArraySize = param.ArraySize
                });
            }

            // Add expected outputs
            foreach (var output in expectedOutputs)
            {
                var isReturnValue = output.Key == "return";

                testCase.ExpectedOutputs.Add(new TestCaseOutput
                {
                    TestCaseId = testCase.Id,
                    ParameterName = output.Key,
                    VariableName = output.Key,
                    DataType = isReturnValue ? function.ReturnType : "int", // Default to int for other outputs
                    Value = output.Value,
                    IsReturnValue = isReturnValue
                });
            }

            return testCase;
        }

        /// <summary>
        /// Generates unit test code for the given test case
        /// </summary>
        public async Task<string> GenerateUnitTestCodeAsync(TestCase testCase, string filePath, string framework = "unity")
        {
            if (testCase == null)
                throw new ArgumentNullException(nameof(testCase));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            // Get function info
            var functions = await _parserService.ExtractFunctionsAsync(filePath);
            var function = functions.FirstOrDefault(f => f.Name == testCase.FunctionName);
            if (function == null)
                throw new InvalidOperationException($"Function not found: {testCase.FunctionName}");

            var sb = new StringBuilder();

            switch (framework.ToLower())
            {
                case "unity":
                    return await GenerateUnityTestCodeAsync(testCase, function);
                case "cmocka":
                    return await GenerateCMockaTestCodeAsync(testCase, function);
                case "check":
                    return await GenerateCheckTestCodeAsync(testCase, function);
                default:
                    throw new ArgumentException($"Unsupported test framework: {framework}", nameof(framework));
            }
        }

        /// <summary>
        /// Generates Unity test code
        /// </summary>
        private async Task<string> GenerateUnityTestCodeAsync(TestCase testCase, CFunction function)
        {
            var sb = new StringBuilder();

            // Add includes
            sb.AppendLine("#include \"unity.h\"");
            sb.AppendLine("#include \"stubs.h\""); // Include stubs
            sb.AppendLine($"#include \"{Path.GetFileName(function.FilePath)}\""); // Include the file being tested
            sb.AppendLine();

            // Add test function
            sb.AppendLine($"void test_{testCase.Name}(void)");
            sb.AppendLine("{");

            // Declare variables for inputs
            foreach (var input in testCase.Inputs)
            {
                sb.Append($"    {input.DataType} {input.ParameterName} = ");

                // Handle different types
                if (input.DataType.Contains("char") && !input.DataType.Contains("*") && input.Value.Length == 1)
                {
                    sb.Append($"'{input.Value}'");
                }
                else if (input.DataType.Contains("char") && input.DataType.Contains("*"))
                {
                    sb.Append($"\"{input.Value}\"");
                }
                else
                {
                    sb.Append(input.Value);
                }

                sb.AppendLine(";");
            }

            sb.AppendLine();

            // Declare variable for return value if needed
            var returnOutput = testCase.ExpectedOutputs.FirstOrDefault(o => o.IsReturnValue);
            if (returnOutput != null && function.ReturnType != "void")
            {
                sb.AppendLine($"    {function.ReturnType} actual_result;");
                sb.AppendLine();
            }

            // Call the function
            if (function.ReturnType != "void")
            {
                sb.Append("    actual_result = ");
            }
            else
            {
                sb.Append("    ");
            }

            sb.Append($"{function.Name}(");

            // Add parameters
            for (int i = 0; i < testCase.Inputs.Count; i++)
            {
                var input = testCase.Inputs[i];
                sb.Append(input.ParameterName);

                // Add comma if not the last parameter
                if (i < testCase.Inputs.Count - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.AppendLine(");");
            sb.AppendLine();

            // Add assertions
            if (returnOutput != null && function.ReturnType != "void")
            {
                // Handle different types
                if (function.ReturnType.Contains("int") || function.ReturnType.Contains("long") ||
                    function.ReturnType.Contains("short") || function.ReturnType.Contains("byte"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL_INT({returnOutput.Value}, actual_result);");
                }
                else if (function.ReturnType.Contains("float"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL_FLOAT({returnOutput.Value}, actual_result);");
                }
                else if (function.ReturnType.Contains("double"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL_DOUBLE({returnOutput.Value}, actual_result);");
                }
                else if (function.ReturnType.Contains("bool") || function.ReturnType.Contains("boolean"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL({returnOutput.Value}, actual_result);");
                }
                else if (function.ReturnType.Contains("char") && !function.ReturnType.Contains("*"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL_CHAR('{returnOutput.Value}', actual_result);");
                }
                else if (function.ReturnType.Contains("char") && function.ReturnType.Contains("*"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL_STRING(\"{returnOutput.Value}\", actual_result);");
                }
                else
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL({returnOutput.Value}, actual_result);");
                }
            }

            // Add assertions for other outputs (e.g., out parameters)
            foreach (var output in testCase.ExpectedOutputs.Where(o => !o.IsReturnValue))
            {
                // Handle different types
                if (output.DataType.Contains("int") || output.DataType.Contains("long") ||
                    output.DataType.Contains("short") || output.DataType.Contains("byte"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL_INT({output.Value}, {output.ParameterName});");
                }
                else if (output.DataType.Contains("float"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL_FLOAT({output.Value}, {output.ParameterName});");
                }
                else if (output.DataType.Contains("double"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL_DOUBLE({output.Value}, {output.ParameterName});");
                }
                else if (output.DataType.Contains("bool") || output.DataType.Contains("boolean"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL({output.Value}, {output.ParameterName});");
                }
                else if (output.DataType.Contains("char") && !output.DataType.Contains("*"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL_CHAR('{output.Value}', {output.ParameterName});");
                }
                else if (output.DataType.Contains("char") && output.DataType.Contains("*"))
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL_STRING(\"{output.Value}\", {output.ParameterName});");
                }
                else
                {
                    sb.AppendLine($"    TEST_ASSERT_EQUAL({output.Value}, {output.ParameterName});");
                }
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Generates CMocka test code
        /// </summary>
        private async Task<string> GenerateCMockaTestCodeAsync(TestCase testCase, CFunction function)
        {
            var sb = new StringBuilder();

            // Add includes
            sb.AppendLine("#include <stdarg.h>");
            sb.AppendLine("#include <stddef.h>");
            sb.AppendLine("#include <setjmp.h>");
            sb.AppendLine("#include <cmocka.h>");
            sb.AppendLine("#include \"stubs.h\""); // Include stubs
            sb.AppendLine($"#include \"{Path.GetFileName(function.FilePath)}\""); // Include the file being tested
            sb.AppendLine();

            // Add test function
            sb.AppendLine($"static void {testCase.Name}(void **state)");
            sb.AppendLine("{");
            sb.AppendLine("    /* Unused parameter */");
            sb.AppendLine("    (void) state;");
            sb.AppendLine();

            // Declare variables for inputs
            foreach (var input in testCase.Inputs)
            {
                sb.Append($"    {input.DataType} {input.ParameterName} = ");

                // Handle different types
                if (input.DataType.Contains("char") && !input.DataType.Contains("*") && input.Value.Length == 1)
                {
                    sb.Append($"'{input.Value}'");
                }
                else if (input.DataType.Contains("char") && input.DataType.Contains("*"))
                {
                    sb.Append($"\"{input.Value}\"");
                }
                else
                {
                    sb.Append(input.Value);
                }

                sb.AppendLine(";");
            }

            sb.AppendLine();

            // Declare variable for return value if needed
            var returnOutput = testCase.ExpectedOutputs.FirstOrDefault(o => o.IsReturnValue);
            if (returnOutput != null && function.ReturnType != "void")
            {
                sb.AppendLine($"    {function.ReturnType} actual_result;");
                sb.AppendLine();
            }

            // Call the function
            if (function.ReturnType != "void")
            {
                sb.Append("    actual_result = ");
            }
            else
            {
                sb.Append("    ");
            }

            sb.Append($"{function.Name}(");

            // Add parameters
            for (int i = 0; i < testCase.Inputs.Count; i++)
            {
                var input = testCase.Inputs[i];
                sb.Append(input.ParameterName);

                // Add comma if not the last parameter
                if (i < testCase.Inputs.Count - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.AppendLine(");");
            sb.AppendLine();

            // Add assertions
            if (returnOutput != null && function.ReturnType != "void")
            {
                // Handle different types
                if (function.ReturnType.Contains("int") || function.ReturnType.Contains("long") ||
                    function.ReturnType.Contains("short") || function.ReturnType.Contains("byte"))
                {
                    sb.AppendLine($"    assert_int_equal({returnOutput.Value}, actual_result);");
                }
                else if (function.ReturnType.Contains("float") || function.ReturnType.Contains("double"))
                {
                    sb.AppendLine($"    assert_true(({returnOutput.Value} - actual_result) < 0.0001);");
                }
                else if (function.ReturnType.Contains("bool") || function.ReturnType.Contains("boolean"))
                {
                    sb.AppendLine($"    assert_true({returnOutput.Value} == actual_result);");
                }
                else if (function.ReturnType.Contains("char") && !function.ReturnType.Contains("*"))
                {
                    sb.AppendLine($"    assert_int_equal('{returnOutput.Value}', actual_result);");
                }
                else if (function.ReturnType.Contains("char") && function.ReturnType.Contains("*"))
                {
                    sb.AppendLine($"    assert_string_equal(\"{returnOutput.Value}\", actual_result);");
                }
                else
                {
                    sb.AppendLine($"    assert_int_equal({returnOutput.Value}, actual_result);");
                }
            }

            // Add assertions for other outputs (e.g., out parameters)
            foreach (var output in testCase.ExpectedOutputs.Where(o => !o.IsReturnValue))
            {
                // Handle different types
                if (output.DataType.Contains("int") || output.DataType.Contains("long") ||
                    output.DataType.Contains("short") || output.DataType.Contains("byte"))
                {
                    sb.AppendLine($"    assert_int_equal({output.Value}, {output.ParameterName});");
                }
                else if (output.DataType.Contains("float") || output.DataType.Contains("double"))
                {
                    sb.AppendLine($"    assert_true(({output.Value} - {output.ParameterName}) < 0.0001);");
                }
                else if (output.DataType.Contains("bool") || output.DataType.Contains("boolean"))
                {
                    sb.AppendLine($"    assert_true({output.Value} == {output.ParameterName});");
                }
                else if (output.DataType.Contains("char") && !output.DataType.Contains("*"))
                {
                    sb.AppendLine($"    assert_int_equal('{output.Value}', {output.ParameterName});");
                }
                else if (output.DataType.Contains("char") && output.DataType.Contains("*"))
                {
                    sb.AppendLine($"    assert_string_equal(\"{output.Value}\", {output.ParameterName});");
                }
                else
                {
                    sb.AppendLine($"    assert_int_equal({output.Value}, {output.ParameterName});");
                }
            }

            sb.AppendLine("}");
            sb.AppendLine();

            // Add main function
            sb.AppendLine("int main(void)");
            sb.AppendLine("{");
            sb.AppendLine("    const struct CMUnitTest tests[] = {");
            sb.AppendLine($"        cmocka_unit_test({testCase.Name}),");
            sb.AppendLine("    };");
            sb.AppendLine();
            sb.AppendLine("    return cmocka_run_group_tests(tests, NULL, NULL);");
            sb.AppendLine("}");

            return sb.ToString();
        }

        /// <summary>
        /// Generates Check test code
        /// </summary>
        private async Task<string> GenerateCheckTestCodeAsync(TestCase testCase, CFunction function)
        {
            var sb = new StringBuilder();

            // Add includes
            sb.AppendLine("#include <check.h>");
            sb.AppendLine("#include <stdlib.h>");
            sb.AppendLine("#include \"stubs.h\""); // Include stubs
            sb.AppendLine($"#include \"{Path.GetFileName(function.FilePath)}\""); // Include the file being tested
            sb.AppendLine();

            // Add test function
            sb.AppendLine($"START_TEST({testCase.Name})");
            sb.AppendLine("{");

            // Declare variables for inputs
            foreach (var input in testCase.Inputs)
            {
                sb.Append($"    {input.DataType} {input.ParameterName} = ");

                // Handle different types
                if (input.DataType.Contains("char") && !input.DataType.Contains("*") && input.Value.Length == 1)
                {
                    sb.Append($"'{input.Value}'");
                }
                else if (input.DataType.Contains("char") && input.DataType.Contains("*"))
                {
                    sb.Append($"\"{input.Value}\"");
                }
                else
                {
                    sb.Append(input.Value);
                }

                sb.AppendLine(";");
            }

            sb.AppendLine();

            // Declare variable for return value if needed
            var returnOutput = testCase.ExpectedOutputs.FirstOrDefault(o => o.IsReturnValue);
            if (returnOutput != null && function.ReturnType != "void")
            {
                sb.AppendLine($"    {function.ReturnType} actual_result;");
                sb.AppendLine();
            }

            // Call the function
            if (function.ReturnType != "void")
            {
                sb.Append("    actual_result = ");
            }
            else
            {
                sb.Append("    ");
            }

            sb.Append($"{function.Name}(");

            // Add parameters
            for (int i = 0; i < testCase.Inputs.Count; i++)
            {
                var input = testCase.Inputs[i];
                sb.Append(input.ParameterName);

                // Add comma if not the last parameter
                if (i < testCase.Inputs.Count - 1)
                {
                    sb.Append(", ");
                }
            }

            sb.AppendLine(");");
            sb.AppendLine();

            // Add assertions
            if (returnOutput != null && function.ReturnType != "void")
            {
                // Handle different types
                if (function.ReturnType.Contains("int") || function.ReturnType.Contains("long") ||
                    function.ReturnType.Contains("short") || function.ReturnType.Contains("byte"))
                {
                    sb.AppendLine($"    ck_assert_int_eq({returnOutput.Value}, actual_result);");
                }
                else if (function.ReturnType.Contains("float"))
                {
                    sb.AppendLine($"    ck_assert_float_eq({returnOutput.Value}, actual_result);");
                }
                else if (function.ReturnType.Contains("double"))
                {
                    sb.AppendLine($"    ck_assert_double_eq({returnOutput.Value}, actual_result);");
                }
                else if (function.ReturnType.Contains("bool") || function.ReturnType.Contains("boolean"))
                {
                    sb.AppendLine($"    ck_assert({returnOutput.Value} == actual_result);");
                }
                else if (function.ReturnType.Contains("char") && !function.ReturnType.Contains("*"))
                {
                    sb.AppendLine($"    ck_assert_int_eq('{returnOutput.Value}', actual_result);");
                }
                else if (function.ReturnType.Contains("char") && function.ReturnType.Contains("*"))
                {
                    sb.AppendLine($"    ck_assert_str_eq(\"{returnOutput.Value}\", actual_result);");
                }
                else
                {
                    sb.AppendLine($"    ck_assert_int_eq({returnOutput.Value}, actual_result);");
                }
            }

            // Add assertions for other outputs (e.g., out parameters)
            foreach (var output in testCase.ExpectedOutputs.Where(o => !o.IsReturnValue))
            {
                // Handle different types
                if (output.DataType.Contains("int") || output.DataType.Contains("long") ||
                    output.DataType.Contains("short") || output.DataType.Contains("byte"))
                {
                    sb.AppendLine($"    ck_assert_int_eq({output.Value}, {output.ParameterName});");
                }
                else if (output.DataType.Contains("float"))
                {
                    sb.AppendLine($"    ck_assert_float_eq({output.Value}, {output.ParameterName});");
                }
                else if (output.DataType.Contains("double"))
                {
                    sb.AppendLine($"    ck_assert_double_eq({output.Value}, {output.ParameterName});");
                }
                else if (output.DataType.Contains("bool") || output.DataType.Contains("boolean"))
                {
                    sb.AppendLine($"    ck_assert({output.Value} == {output.ParameterName});");
                }
                else if (output.DataType.Contains("char") && !output.DataType.Contains("*"))
                {
                    sb.AppendLine($"    ck_assert_int_eq('{output.Value}', {output.ParameterName});");
                }
                else if (output.DataType.Contains("char") && output.DataType.Contains("*"))
                {
                    sb.AppendLine($"    ck_assert_str_eq(\"{output.Value}\", {output.ParameterName});");
                }
                else
                {
                    sb.AppendLine($"    ck_assert_int_eq({output.Value}, {output.ParameterName});");
                }
            }

            sb.AppendLine("}");
            sb.AppendLine("END_TEST");
            sb.AppendLine();

            // Add test suite
            sb.AppendLine("Suite *create_test_suite(void)");
            sb.AppendLine("{");
            sb.AppendLine("    Suite *s;");
            sb.AppendLine("    TCase *tc_core;");
            sb.AppendLine();
            sb.AppendLine($"    s = suite_create(\"{function.Name}_tests\");");
            sb.AppendLine();
            sb.AppendLine("    /* Core test case */");
            sb.AppendLine("    tc_core = tcase_create(\"Core\");");
            sb.AppendLine();
            sb.AppendLine($"    tcase_add_test(tc_core, {testCase.Name});");
            sb.AppendLine("    suite_add_tcase(s, tc_core);");
            sb.AppendLine();
            sb.AppendLine("    return s;");
            sb.AppendLine("}");
            sb.AppendLine();

            // Add main function
            sb.AppendLine("int main(void)");
            sb.AppendLine("{");
            sb.AppendLine("    int number_failed;");
            sb.AppendLine("    Suite *s;");
            sb.AppendLine("    SRunner *sr;");
            sb.AppendLine();
            sb.AppendLine("    s = create_test_suite();");
            sb.AppendLine("    sr = srunner_create(s);");
            sb.AppendLine();
            sb.AppendLine("    srunner_run_all(sr, CK_NORMAL);");
            sb.AppendLine("    number_failed = srunner_ntests_failed(sr);");
            sb.AppendLine("    srunner_free(sr);");
            sb.AppendLine();
            sb.AppendLine("    return (number_failed == 0) ? EXIT_SUCCESS : EXIT_FAILURE;");
            sb.AppendLine("}");

            return sb.ToString();
        }
    }
}
