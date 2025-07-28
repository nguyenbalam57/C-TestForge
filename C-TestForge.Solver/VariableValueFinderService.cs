using C_TestForge.SolverServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;
using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Solver;
using C_TestForge.Core.Interfaces.Parser;

namespace C_TestForge.Solver
{
    /// <summary>
    /// Service for finding variable values
    /// </summary>
    public class VariableValueFinderService : IVariableValueFinderService
    {
        private readonly IParserService _parserService;
        private readonly IZ3SolverService _solverService;
        private readonly IVariableAnalysisService _variableAnalysisService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parserService">The parser service</param>
        /// <param name="solverService">The Z3 solver service</param>
        /// <param name="variableAnalysisService">The variable analysis service</param>
        public VariableValueFinderService(
            IParserService parserService,
            IZ3SolverService solverService,
            IVariableAnalysisService variableAnalysisService)
        {
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _solverService = solverService ?? throw new ArgumentNullException(nameof(solverService));
            _variableAnalysisService = variableAnalysisService ?? throw new ArgumentNullException(nameof(variableAnalysisService));
        }

        /// <summary>
        /// Finds values for variables to satisfy the given test case outputs
        /// </summary>
        public async Task<Dictionary<string, string>> FindValuesForTestCaseAsync(TestCaseModels testCase, string functionName, string filePath)
        {
            if (testCase == null)
                throw new ArgumentNullException(nameof(testCase));
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException(nameof(functionName));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            // Get variable constraints
            var constraints = await GetVariableConstraintsAsync(functionName, filePath);

            // Extract expected outputs
            var expectedOutputs = new Dictionary<string, string>();
            foreach (var output in testCase.OutputVariables)
            {
                expectedOutputs[output.ParameterName] = output.Value;
            }

            // Find variable values using Z3
            return await _solverService.FindVariableValuesAsync(constraints, expectedOutputs);
        }

        /// <summary>
        /// Finds values for variables to maximize code coverage
        /// </summary>
        public async Task<List<Dictionary<string, string>>> FindValuesForMaxCoverageAsync(string functionName, string filePath, double targetCoverage = 0.9)
        {
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException(nameof(functionName));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            // Get function analysis
            var functionAnalysis = await _parserService.AnalyzeFunctionAsync(functionName, filePath);
            if (functionAnalysis == null)
                throw new InvalidOperationException($"Function not found: {functionName}");

            // Get variable types
            var variableTypes = await GetVariableTypesAsync(functionName, filePath);

            // Get variable constraints
            var constraints = await GetVariableConstraintsAsync(functionName, filePath);

            // Find variable values using Z3
            return await _solverService.FindVariableValuesForCoverageAsync(
                functionAnalysis,
                variableTypes,
                constraints,
                targetCoverage);
        }

        /// <summary>
        /// Finds values that make the expression true
        /// </summary>
        public async Task<Dictionary<string, string>> FindValuesForExpressionAsync(string expression, string functionName, string filePath)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentNullException(nameof(expression));
            if (string.IsNullOrEmpty(functionName))
                throw new ArgumentNullException(nameof(functionName));
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("File not found", filePath);

            // Get variable types
            var variableTypes = await GetVariableTypesAsync(functionName, filePath);

            // Get variable constraints
            var constraints = await GetVariableConstraintsAsync(functionName, filePath);

            // Find variable values using Z3
            return await _solverService.FindVariableValuesForExpressionAsync(
                expression,
                variableTypes,
                constraints);
        }

        /// <summary>
        /// Gets variable types for a function
        /// </summary>
        private async Task<Dictionary<string, string>> GetVariableTypesAsync(string functionName, string filePath)
        {
            // Get function info
            var functions = await _parserService.ExtractFunctionsAsync(filePath);
            var function = functions.FirstOrDefault(f => f.Name == functionName);
            if (function == null)
                throw new InvalidOperationException($"Function not found: {functionName}");

            // Get variable types
            var variableTypes = new Dictionary<string, string>();

            // Add parameters
            foreach (var param in function.Parameters)
            {
                variableTypes[param.Name] = param.Type;
            }

            // Add local variables
            var variableAnalysisResult = await _variableAnalysisService.AnalyzeVariablesAsync(filePath);
            if (variableAnalysisResult.LocalVariablesByFunction.TryGetValue(functionName, out var localVars))
            {
                foreach (var localVar in localVars)
                {
                    variableTypes[localVar.Name] = localVar.DataType;
                }
            }

            return variableTypes;
        }

        /// <summary>
        /// Gets variable constraints for a function
        /// </summary>
        private async Task<Dictionary<string, VariableConstraint>> GetVariableConstraintsAsync(string functionName, string filePath)
        {
            // Get variable analysis
            var variableAnalysisResult = await _variableAnalysisService.AnalyzeVariablesAsync(filePath);

            // Create constraints dictionary
            var constraints = new Dictionary<string, VariableConstraint>();

            // Add constraints from variable analysis
            foreach (var kvp in variableAnalysisResult.VariableConstraints)
            {
                constraints[kvp.Key] = kvp.Value;
            }

            // Get function parameters
            var functions = await _parserService.ExtractFunctionsAsync(filePath);
            var function = functions.FirstOrDefault(f => f.Name == functionName);
            if (function == null)
                return constraints;

            // Add constraints for parameters if not already present
            foreach (var param in function.Parameters)
            {
                if (!constraints.ContainsKey(param.Name))
                {
                    constraints[param.Name] = new VariableConstraint
                    {
                        VariableName = param.Name
                    };
                }
            }

            return constraints;
        }
    }
}
