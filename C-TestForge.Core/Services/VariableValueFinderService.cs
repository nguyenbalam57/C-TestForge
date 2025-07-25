using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using C_TestForge.Core.Interfaces;
using C_TestForge.Models;

namespace C_TestForge.Core.Services
{
    public class VariableValueFinderService : IVariableValueFinderService
    {
        private readonly IZ3SolverService _z3SolverService;
        private readonly IParserService _parserService;

        public VariableValueFinderService(IZ3SolverService z3SolverService, IParserService parserService)
        {
            _z3SolverService = z3SolverService ?? throw new ArgumentNullException(nameof(z3SolverService));
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
        }

        public Dictionary<string, object> FindValuesForExpectedOutput(
            CFunction function,
            Dictionary<string, object> expectedOutput,
            List<CVariable> availableVariables)
        {
            // Extract constraints from function body
            List<string> constraints = ExtractConstraintsFromFunction(function);

            // Add output constraints based on expected output
            foreach (var output in expectedOutput)
            {
                string outputConstraint = $"{output.Key} == {FormatValue(output.Value)}";
                constraints.Add(outputConstraint);
            }

            // Get relevant variables (inputs to the function)
            var relevantVariables = GetRelevantVariables(function, availableVariables);

            // Use Z3 to find satisfying values
            return _z3SolverService.FindSatisfyingValues(relevantVariables, constraints);
        }

        public Dictionary<string, object> FindValuesForBranchCoverage(
            CFunction function,
            List<CVariable> availableVariables,
            string targetBranch)
        {
            // Extract constraints from function that lead to the target branch
            List<string> constraints = ExtractConstraintsForBranch(function, targetBranch);

            // Get relevant variables (inputs to the function)
            var relevantVariables = GetRelevantVariables(function, availableVariables);

            // Use Z3 to find satisfying values
            return _z3SolverService.FindSatisfyingValues(relevantVariables, constraints);
        }

        private List<string> ExtractConstraintsFromFunction(CFunction function)
        {
            List<string> constraints = new List<string>();

            // For a more robust implementation, this would involve analyzing the
            // function's AST to extract conditional statements and their conditions

            // Simple regex-based approach for illustration
            if (!string.IsNullOrEmpty(function.Body))
            {
                // Extract conditions from if statements
                var ifMatches = Regex.Matches(function.Body, @"if\s*\((.*?)\)");
                foreach (Match match in ifMatches)
                {
                    if (match.Groups.Count > 1)
                    {
                        string condition = match.Groups[1].Value.Trim();
                        constraints.Add(condition);
                    }
                }

                // Extract conditions from while statements
                var whileMatches = Regex.Matches(function.Body, @"while\s*\((.*?)\)");
                foreach (Match match in whileMatches)
                {
                    if (match.Groups.Count > 1)
                    {
                        string condition = match.Groups[1].Value.Trim();
                        constraints.Add(condition);
                    }
                }

                // Extract conditions from for statements
                var forMatches = Regex.Matches(function.Body, @"for\s*\((.*?);\s*(.*?);\s*(.*?)\)");
                foreach (Match match in forMatches)
                {
                    if (match.Groups.Count > 2)
                    {
                        string condition = match.Groups[2].Value.Trim();
                        if (!string.IsNullOrEmpty(condition))
                        {
                            constraints.Add(condition);
                        }
                    }
                }
            }

            return constraints;
        }

        private List<string> ExtractConstraintsForBranch(CFunction function, string targetBranch)
        {
            List<string> constraints = new List<string>();

            // This is a simplified approach. In a real implementation, you would need
            // to analyze the control flow graph and determine the path conditions
            // that lead to the target branch.

            // For now, we'll use a simplified approach where targetBranch is a condition
            // that we want to satisfy directly
            constraints.Add(targetBranch);

            return constraints;
        }

        private List<CVariable> GetRelevantVariables(CFunction function, List<CVariable> availableVariables)
        {
            // Start with function parameters
            var relevantVariables = function.Parameters.Select(p =>
                new CVariable
                {
                    Name = p.Name,
                    Type = p.Type,
                    // Other properties would be initialized based on type
                }).ToList();

            // Add any global variables that are used in the function
            if (!string.IsNullOrEmpty(function.Body))
            {
                foreach (var variable in availableVariables)
                {
                    // Simple check for variable usage in function body
                    // A more robust approach would involve analyzing the AST
                    if (function.Body.Contains(variable.Name))
                    {
                        // Only add if not already in the list
                        if (!relevantVariables.Any(v => v.Name == variable.Name))
                        {
                            relevantVariables.Add(variable);
                        }
                    }
                }
            }

            return relevantVariables;
        }

        private string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is bool boolValue)
                return boolValue ? "true" : "false";

            if (value is string stringValue)
                return $"\"{stringValue}\"";

            if (value is float || value is double)
                return value.ToString().Replace(',', '.');

            return value.ToString();
        }
    }

    public interface IVariableValueFinderService
    {
        Dictionary<string, object> FindValuesForExpectedOutput(
            CFunction function,
            Dictionary<string, object> expectedOutput,
            List<CVariable> availableVariables);

        Dictionary<string, object> FindValuesForBranchCoverage(
            CFunction function,
            List<CVariable> availableVariables,
            string targetBranch);
    }
}