using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Solver;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Solver
{
    /// <summary>
    /// Service for generating test cases automatically
    /// </summary>
    public class TestCaseGenerationService : ITestCaseGenerationService
    {
        private readonly IClangSharpParserService _parserService;
        private readonly IFunctionAnalysisService _functionAnalysisService;
        private readonly IVariableAnalysisService _variableAnalysisService;
        private readonly IZ3SolverService _solverService;

        public TestCaseGenerationService(
            IClangSharpParserService parserService,
            IFunctionAnalysisService functionAnalysisService,
            IVariableAnalysisService variableAnalysisService,
            IZ3SolverService solverService)
        {
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _functionAnalysisService = functionAnalysisService ?? throw new ArgumentNullException(nameof(functionAnalysisService));
            _variableAnalysisService = variableAnalysisService ?? throw new ArgumentNullException(nameof(variableAnalysisService));
            _solverService = solverService ?? throw new ArgumentNullException(nameof(solverService));
        }

        /// <summary>
        /// Generates unit tests for a function
        /// </summary>
        public async Task<List<TestCase>> GenerateUnitTestsAsync(string functionName, string filePath, double targetCoverage = 0.9)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException(nameof(functionName));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            try
            {
                // Analyze the function
                var functionAnalysis = await _functionAnalysisService.AnalyzeFunctionLogicAsync(functionName, filePath);
                if (functionAnalysis == null)
                {
                    throw new InvalidOperationException($"Failed to analyze function: {functionName}");
                }

                // Get function information
                var functions = await _parserService.ExtractFunctionsAsync(filePath);
                var function = functions.FirstOrDefault(f => f.Name == functionName);
                if (function == null)
                {
                    throw new InvalidOperationException($"Function not found: {functionName}");
                }

                // Get parameter types
                var variableTypes = new Dictionary<string, string>();
                foreach (var parameter in function.Parameters)
                {
                    variableTypes[parameter.Name] = parameter.Type;
                }

                // Get constraints for parameters
                var variableConstraints = await GetVariableConstraintsAsync(functionName, filePath);

                // Generate test cases for coverage
                var valuesSets = await _solverService.FindVariableValuesForCoverageAsync(
                    functionAnalysis,
                    variableTypes,
                    variableConstraints,
                    targetCoverage);

                if (valuesSets == null || !valuesSets.Any())
                {
                    throw new InvalidOperationException($"Failed to find variable values for function: {functionName}");
                }

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
                        Status = TestCaseStatus.Active,
                        Inputs = new List<TestCaseInput>(),
                        ExpectedOutputs = new List<TestCaseOutput>()
                    };

                    // Add inputs
                    foreach (var parameter in function.Parameters)
                    {
                        if (values.TryGetValue(parameter.Name, out var value))
                        {
                            testCase.Inputs.Add(new TestCaseInput
                            {
                                TestCaseId = testCase.Id,
                                ParameterName = parameter.Name,
                                VariableName = parameter.Name,
                                DataType = parameter.Type,
                                Value = value,
                                IsArray = parameter.IsArray,
                                ArraySize = parameter.ArraySize
                            });
                        }
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
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error generating unit tests: {ex.Message}");
                return new List<TestCase>();
            }
        }

        /// <summary>
        /// Generates integration tests for a set of related functions
        /// </summary>
        public async Task<List<TestCase>> GenerateIntegrationTestsAsync(List<string> functionNames, string filePath, double targetCoverage = 0.9)
        {
            if (functionNames == null || !functionNames.Any())
                throw new ArgumentNullException(nameof(functionNames));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));

            try
            {
                // Get function information
                var functions = await _parserService.ExtractFunctionsAsync(filePath);
                var selectedFunctions = functions.Where(f => functionNames.Contains(f.Name)).ToList();
                if (!selectedFunctions.Any())
                {
                    throw new InvalidOperationException("No functions found");
                }

                // Identify the main function (the one that calls others)
                var mainFunction = selectedFunctions.FirstOrDefault(f =>
                    f.CalledFunctions.Any(cf => functionNames.Contains(cf)));

                if (mainFunction == null)
                {
                    // If no clear main function, use the first function
                    mainFunction = selectedFunctions.First();
                }

                // Generate unit tests for the main function
                var unitTests = await GenerateUnitTestsAsync(mainFunction.Name, filePath, targetCoverage);

                // Convert unit tests to integration tests
                var integrationTests = new List<TestCase>();
                foreach (var unitTest in unitTests)
                {
                    var integrationTest = new TestCase
                    {
                        Id = unitTest.Id,
                        Name = unitTest.Name.Replace("_Test_", "_ITST_"),
                        Description = $"Auto-generated integration test for {string.Join(", ", functionNames)}",
                        FunctionName = mainFunction.Name,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "C-TestForge",
                        Type = TestCaseType.IntegrationTest,
                        Status = TestCaseStatus.Active,
                        Inputs = new List<TestCaseInput>(unitTest.Inputs),
                        ExpectedOutputs = new List<TestCaseOutput>(unitTest.ExpectedOutputs)
                    };

                    integrationTests.Add(integrationTest);
                }

                return integrationTests;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error generating integration tests: {ex.Message}");
                return new List<TestCase>();
            }
        }

        /// <summary>
        /// Gets constraints for variables used in a function
        /// </summary>
        private async Task<Dictionary<string, VariableConstraint>> GetVariableConstraintsAsync(string functionName, string filePath)
        {
            try
            {
                // Get variable constraints from the variable analysis service
                var variableAnalysisResult = await _variableAnalysisService.AnalyzeVariablesAsync(filePath);
                if (variableAnalysisResult == null)
                {
                    return new Dictionary<string, VariableConstraint>();
                }

                // Get local variables for the function
                var localVariables = variableAnalysisResult.LocalVariablesByFunction.TryGetValue(functionName, out var locals)
                    ? locals
                    : new List<CVariable>();

                // Create constraints
                var constraints = new Dictionary<string, VariableConstraint>();

                // Add constraints for local variables
                foreach (var variable in localVariables)
                {
                    if (variableAnalysisResult.VariableConstraints.TryGetValue(variable.Name, out var constraint))
                    {
                        constraints[variable.Name] = constraint;
                    }
                    else
                    {
                        constraints[variable.Name] = new VariableConstraint
                        {
                            VariableName = variable.Name,
                            MinValue = variable.MinValue,
                            MaxValue = variable.MaxValue,
                            EnumValues = variable.EnumValues?.ToList() ?? new List<string>()
                        };
                    }
                }

                // Get function information
                var functions = await _parserService.ExtractFunctionsAsync(filePath);
                var function = functions.FirstOrDefault(f => f.Name == functionName);
                if (function != null)
                {
                    // Add constraints for parameters
                    foreach (var parameter in function.Parameters)
                    {
                        if (!constraints.ContainsKey(parameter.Name))
                        {
                            constraints[parameter.Name] = new VariableConstraint
                            {
                                VariableName = parameter.Name
                            };
                        }
                    }
                }

                return constraints;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error getting variable constraints: {ex.Message}");
                return new Dictionary<string, VariableConstraint>();
            }
        }
    }
}
