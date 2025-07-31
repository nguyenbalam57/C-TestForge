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
    /// Implementation of ITestCodeGeneratorService for generating test code
    /// </summary>
    public class TestCodeGeneratorService : ITestCodeGeneratorService
    {
        private readonly IParser _parser;

        public TestCodeGeneratorService(IParser parser)
        {
            _parser = parser ?? throw new ArgumentNullException(nameof(parser));
        }

        /// <summary>
        /// Generates test code for the given test cases
        /// </summary>
        public async Task<string> GenerateTestCodeAsync(
            IEnumerable<TestCase> testCases,
            string filePath,
            string framework = "unity")
        {
            if (testCases == null || !testCases.Any())
            {
                throw new ArgumentException("Test cases cannot be empty");
            }

            // Parse source file to get function information
            var sourceFile = await _parser.ParseSourceFileAsync(filePath);

            // Group test cases by function
            var testCasesByFunction = testCases.GroupBy(tc => tc.FunctionName);

            var testCode = new StringBuilder();

            // Add header
            AppendHeader(testCode, filePath, framework);

            // Generate test code for each function
            foreach (var functionGroup in testCasesByFunction)
            {
                var functionName = functionGroup.Key;
                var function = sourceFile.ParseResult.Functions.FirstOrDefault(f => f.Name == functionName);

                if (function == null)
                {
                    throw new ArgumentException($"Function '{functionName}' not found in file '{filePath}'");
                }

                // Generate test fixture for this function
                AppendTestFixture(testCode, function, functionGroup, framework);
            }

            // Add footer
            AppendFooter(testCode, framework);

            return testCode.ToString();
        }

        /// <summary>
        /// Generates test fixture code for the given function
        /// </summary>
        public async Task<string> GenerateTestFixtureAsync(
            string functionName,
            string filePath,
            string framework = "unity")
        {
            // Parse source file to get function information
            var sourceFile = await _parser.ParseSourceFileAsync(filePath);
            var function = sourceFile.ParseResult.Functions.FirstOrDefault(f => f.Name == functionName);

            if (function == null)
            {
                throw new ArgumentException($"Function '{functionName}' not found in file '{filePath}'");
            }

            var testFixture = new StringBuilder();

            // Generate empty test fixture
            switch (framework.ToLower())
            {
                case "unity":
                    AppendUnityTestFixture(testFixture, function, new List<TestCase>());
                    break;
                case "cunit":
                    AppendCUnitTestFixture(testFixture, function, new List<TestCase>());
                    break;
                default:
                    throw new ArgumentException($"Unsupported test framework: {framework}");
            }

            return testFixture.ToString();
        }

        /// <summary>
        /// Generates test runner code for the given test fixtures
        /// </summary>
        public async Task<string> GenerateTestRunnerAsync(
            IEnumerable<string> testFixtures,
            string framework = "unity")
        {
            if (testFixtures == null || !testFixtures.Any())
            {
                throw new ArgumentException("Test fixtures cannot be empty");
            }

            var testRunner = new StringBuilder();

            // Generate test runner based on framework
            switch (framework.ToLower())
            {
                case "unity":
                    AppendUnityTestRunner(testRunner, testFixtures);
                    break;
                case "cunit":
                    AppendCUnitTestRunner(testRunner, testFixtures);
                    break;
                default:
                    throw new ArgumentException($"Unsupported test framework: {framework}");
            }

            return testRunner.ToString();
        }

        #region Helper Methods

        /// <summary>
        /// Appends the header to the test code
        /// </summary>
        private void AppendHeader(StringBuilder sb, string filePath, string framework)
        {
            switch (framework.ToLower())
            {
                case "unity":
                    sb.AppendLine("/**");
                    sb.AppendLine(" * @file      test_" + System.IO.Path.GetFileName(filePath));
                    sb.AppendLine(" * @brief     Unit tests for " + System.IO.Path.GetFileName(filePath));
                    sb.AppendLine(" * @details   Generated by C-TestForge");
                    sb.AppendLine(" */");
                    sb.AppendLine();
                    sb.AppendLine("#include \"unity.h\"");
                    sb.AppendLine($"#include \"{System.IO.Path.GetFileName(filePath)}\"");
                    sb.AppendLine();
                    break;
                case "cunit":
                    sb.AppendLine("/**");
                    sb.AppendLine(" * @file      test_" + System.IO.Path.GetFileName(filePath));
                    sb.AppendLine(" * @brief     Unit tests for " + System.IO.Path.GetFileName(filePath));
                    sb.AppendLine(" * @details   Generated by C-TestForge");
                    sb.AppendLine(" */");
                    sb.AppendLine();
                    sb.AppendLine("#include <CUnit/CUnit.h>");
                    sb.AppendLine("#include <CUnit/Basic.h>");
                    sb.AppendLine($"#include \"{System.IO.Path.GetFileName(filePath)}\"");
                    sb.AppendLine();
                    break;
                default:
                    throw new ArgumentException($"Unsupported test framework: {framework}");
            }
        }

        /// <summary>
        /// Appends the footer to the test code
        /// </summary>
        private void AppendFooter(StringBuilder sb, string framework)
        {
            switch (framework.ToLower())
            {
                case "unity":
                    sb.AppendLine();
                    sb.AppendLine("int main(void)");
                    sb.AppendLine("{");
                    sb.AppendLine("    UNITY_BEGIN();");
                    sb.AppendLine("    // Run all tests");
                    sb.AppendLine("    RUN_TEST(test_placeholder);");
                    sb.AppendLine("    return UNITY_END();");
                    sb.AppendLine("}");
                    break;
                case "cunit":
                    sb.AppendLine();
                    sb.AppendLine("int main(void)");
                    sb.AppendLine("{");
                    sb.AppendLine("    CU_initialize_registry();");
                    sb.AppendLine("    // Add all test suites");
                    sb.AppendLine("    CU_basic_run_tests();");
                    sb.AppendLine("    CU_cleanup_registry();");
                    sb.AppendLine("    return CU_get_error();");
                    sb.AppendLine("}");
                    break;
                default:
                    throw new ArgumentException($"Unsupported test framework: {framework}");
            }
        }

        /// <summary>
        /// Appends a test fixture to the test code
        /// </summary>
        private void AppendTestFixture(
            StringBuilder sb,
            dynamic function,
            IGrouping<string, TestCase> testCases,
            string framework)
        {
            switch (framework.ToLower())
            {
                case "unity":
                    AppendUnityTestFixture(sb, function, testCases);
                    break;
                case "cunit":
                    AppendCUnitTestFixture(sb, function, testCases);
                    break;
                default:
                    throw new ArgumentException($"Unsupported test framework: {framework}");
            }
        }

        /// <summary>
        /// Appends a Unity test fixture to the test code
        /// </summary>
        private void AppendUnityTestFixture(
            StringBuilder sb,
            dynamic function,
            IEnumerable<TestCase> testCases)
        {
            string functionName = function.Name;

            sb.AppendLine($"// Test fixture for {functionName}");
            sb.AppendLine("void setUp(void)");
            sb.AppendLine("{");
            sb.AppendLine("    // Setup code");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("void tearDown(void)");
            sb.AppendLine("{");
            sb.AppendLine("    // Teardown code");
            sb.AppendLine("}");
            sb.AppendLine();

            // If no test cases, add a placeholder test
            if (!testCases.Any())
            {
                sb.AppendLine($"void test_placeholder(void)");
                sb.AppendLine("{");
                sb.AppendLine("    // TODO: Implement test");
                sb.AppendLine("    TEST_ASSERT_TRUE(1);");
                sb.AppendLine("}");
                sb.AppendLine();
                return;
            }

            // Generate a test for each test case
            foreach (var testCase in testCases)
            {
                sb.AppendLine($"void test_{testCase.Name}(void)");
                sb.AppendLine("{");

                // Declare variables for inputs
                foreach (var input in testCase.InputVariables)
                {
                    sb.AppendLine($"    {input.Type} {input.Name} = {input.Value};");
                }

                // Call the function
                if (function.ReturnType != "void" && testCase.OutputVariables.Any())
                {
                    var returnVarType = function.ReturnType;
                    sb.AppendLine($"    {returnVarType} actual = {functionName}({string.Join(", ", testCase.InputVariables.Select(i => i.Name))});");

                    // Add assertions for return value
                    var expectedReturn = testCase.OutputVariables.FirstOrDefault()?.ExpectedValue ?? "0";
                    sb.AppendLine($"    TEST_ASSERT_EQUAL({expectedReturn}, actual);");
                }
                else
                {
                    sb.AppendLine($"    {functionName}({string.Join(", ", testCase.InputVariables.Select(i => i.Name))});");

                    // Add assertions for output variables
                    foreach (var output in testCase.OutputVariables)
                    {
                        sb.AppendLine($"    TEST_ASSERT_EQUAL({output.ExpectedValue}, {output.Name});");
                    }
                }

                sb.AppendLine("}");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Appends a CUnit test fixture to the test code
        /// </summary>
        private void AppendCUnitTestFixture(
            StringBuilder sb,
            dynamic function,
            IEnumerable<TestCase> testCases)
        {
            string functionName = function.Name;

            sb.AppendLine($"// Test fixture for {functionName}");
            sb.AppendLine($"int init_{functionName}_suite(void)");
            sb.AppendLine("{");
            sb.AppendLine("    // Setup code");
            sb.AppendLine("    return 0;");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine($"int clean_{functionName}_suite(void)");
            sb.AppendLine("{");
            sb.AppendLine("    // Teardown code");
            sb.AppendLine("    return 0;");
            sb.AppendLine("}");
            sb.AppendLine();

            // If no test cases, add a placeholder test
            if (!testCases.Any())
            {
                sb.AppendLine($"void test_placeholder(void)");
                sb.AppendLine("{");
                sb.AppendLine("    // TODO: Implement test");
                sb.AppendLine("    CU_ASSERT(1);");
                sb.AppendLine("}");
                sb.AppendLine();
                return;
            }

            // Generate a test for each test case
            foreach (var testCase in testCases)
            {
                sb.AppendLine($"void test_{testCase.Name}(void)");
                sb.AppendLine("{");

                // Declare variables for inputs
                foreach (var input in testCase.InputVariables)
                {
                    sb.AppendLine($"    {input.Type} {input.Name} = {input.Value};");
                }

                // Call the function
                if (function.ReturnType != "void" && testCase.OutputVariables.Any())
                {
                    var returnVarType = function.ReturnType;
                    sb.AppendLine($"    {returnVarType} actual = {functionName}({string.Join(", ", testCase.InputVariables.Select(i => i.Name))});");

                    // Add assertions for return value
                    var expectedReturn = testCase.OutputVariables.FirstOrDefault()?.ExpectedValue ?? "0";
                    sb.AppendLine($"    CU_ASSERT_EQUAL({expectedReturn}, actual);");
                }
                else
                {
                    sb.AppendLine($"    {functionName}({string.Join(", ", testCase.InputVariables.Select(i => i.Name))});");

                    // Add assertions for output variables
                    foreach (var output in testCase.OutputVariables)
                    {
                        sb.AppendLine($"    CU_ASSERT_EQUAL({output.ExpectedValue}, {output.Name});");
                    }
                }

                sb.AppendLine("}");
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Appends a Unity test runner to the test code
        /// </summary>
        private void AppendUnityTestRunner(StringBuilder sb, IEnumerable<string> testFixtures)
        {
            sb.AppendLine("int main(void)");
            sb.AppendLine("{");
            sb.AppendLine("    UNITY_BEGIN();");

            // Add calls to run each test
            foreach (var fixture in testFixtures)
            {
                sb.AppendLine($"    RUN_TEST({fixture});");
            }

            sb.AppendLine("    return UNITY_END();");
            sb.AppendLine("}");
        }

        /// <summary>
        /// Appends a CUnit test runner to the test code
        /// </summary>
        private void AppendCUnitTestRunner(StringBuilder sb, IEnumerable<string> testFixtures)
        {
            sb.AppendLine("int main(void)");
            sb.AppendLine("{");
            sb.AppendLine("    CU_initialize_registry();");

            // Add test suites
            sb.AppendLine("    // Add all test suites");
            sb.AppendLine("    CU_pSuite pSuite = NULL;");
            sb.AppendLine();
            sb.AppendLine("    // Add tests to suites");
            foreach (var fixture in testFixtures)
            {
                sb.AppendLine($"    CU_add_test(pSuite, \"{fixture}\", {fixture});");
            }

            sb.AppendLine("    CU_basic_run_tests();");
            sb.AppendLine("    CU_cleanup_registry();");
            sb.AppendLine("    return CU_get_error();");
            sb.AppendLine("}");
        }

        #endregion
    }
}
