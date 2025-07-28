using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Z3;
using C_TestForge.Models;
using C_TestForge.Core.Interfaces;

namespace C_TestForge.SolverServices
{
    /// <summary>
    /// Service for solving variable values using Z3 Theorem Prover
    /// </summary>
    public class Z3SolverService : IZ3SolverService
    {
        /// <summary>
        /// Finds variable values that satisfy the given constraints
        /// </summary>
        public async Task<Dictionary<string, string>> FindVariableValuesAsync(
            Dictionary<string, VariableConstraint> constraints,
            Dictionary<string, string> expectedOutputs)
        {
            if (constraints == null)
                throw new ArgumentNullException(nameof(constraints));
            if (expectedOutputs == null)
                throw new ArgumentNullException(nameof(expectedOutputs));

            using var context = new Context();
            var solver = context.MkSolver();
            var variables = new Dictionary<string, Expr>();
            var result = new Dictionary<string, string>();

            try
            {
                // Create Z3 variables and add constraints
                foreach (var constraint in constraints)
                {
                    var varName = constraint.Key;
                    var varConstraint = constraint.Value;

                    // Create variable
                    Expr variable;
                    if (IsIntegerType(varConstraint.VariableName))
                    {
                        variable = context.MkIntConst(varName);
                    }
                    else if (IsRealType(varConstraint.VariableName))
                    {
                        variable = context.MkRealConst(varName);
                    }
                    else if (IsBooleanType(varConstraint.VariableName))
                    {
                        variable = context.MkBoolConst(varName);
                    }
                    else
                    {
                        // Default to integer
                        variable = context.MkIntConst(varName);
                    }

                    variables[varName] = variable;

                    // Add min/max constraints
                    if (!string.IsNullOrEmpty(varConstraint.MinValue))
                    {
                        if (IsIntegerType(varConstraint.VariableName))
                        {
                            if (int.TryParse(varConstraint.MinValue, out int minValue))
                            {
                                solver.Add(context.MkGe((ArithExpr)variable, context.MkInt(minValue)));
                            }
                        }
                        else if (IsRealType(varConstraint.VariableName))
                        {
                            if (double.TryParse(varConstraint.MinValue, out double minValue))
                            {
                                solver.Add(context.MkGe((ArithExpr)variable, context.MkReal(minValue.ToString())));
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(varConstraint.MaxValue))
                    {
                        if (IsIntegerType(varConstraint.VariableName))
                        {
                            if (int.TryParse(varConstraint.MaxValue, out int maxValue))
                            {
                                solver.Add(context.MkLe((ArithExpr)variable, context.MkInt(maxValue)));
                            }
                        }
                        else if (IsRealType(varConstraint.VariableName))
                        {
                            if (double.TryParse(varConstraint.MaxValue, out double maxValue))
                            {
                                solver.Add(context.MkLe((ArithExpr)variable, context.MkReal(maxValue.ToString())));
                            }
                        }
                    }

                    // Add enum value constraints
                    if (varConstraint.EnumValues != null && varConstraint.EnumValues.Count > 0)
                    {
                        var enumConstraints = new List<BoolExpr>();
                        foreach (var enumValue in varConstraint.EnumValues)
                        {
                            // Extract the value from the enum (e.g., "VALUE = 1" -> "1")
                            var parts = enumValue.Split('=');
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int value))
                            {
                                enumConstraints.Add(context.MkEq((ArithExpr)variable, context.MkInt(value)));
                            }
                        }

                        if (enumConstraints.Count > 0)
                        {
                            solver.Add(context.MkOr(enumConstraints.ToArray()));
                        }
                    }

                    // Add allowed values constraints
                    if (varConstraint.AllowedValues != null && varConstraint.AllowedValues.Count > 0)
                    {
                        var allowedConstraints = new List<BoolExpr>();
                        foreach (var allowedValue in varConstraint.AllowedValues)
                        {
                            if (IsIntegerType(varConstraint.VariableName) && int.TryParse(allowedValue, out int intValue))
                            {
                                allowedConstraints.Add(context.MkEq((ArithExpr)variable, context.MkInt(intValue)));
                            }
                            else if (IsRealType(varConstraint.VariableName) && double.TryParse(allowedValue, out double doubleValue))
                            {
                                allowedConstraints.Add(context.MkEq((ArithExpr)variable, context.MkReal(doubleValue.ToString())));
                            }
                            else if (IsBooleanType(varConstraint.VariableName) && bool.TryParse(allowedValue, out bool boolValue))
                            {
                                allowedConstraints.Add(context.MkEq((BoolExpr)variable, context.MkBool(boolValue)));
                            }
                        }

                        if (allowedConstraints.Count > 0)
                        {
                            solver.Add(context.MkOr(allowedConstraints.ToArray()));
                        }
                    }
                }

                // Add expected output constraints
                foreach (var output in expectedOutputs)
                {
                    var outputName = output.Key;
                    var outputValue = output.Value;

                    if (variables.TryGetValue(outputName, out var variable))
                    {
                        if (IsIntegerType(outputName) && int.TryParse(outputValue, out int intValue))
                        {
                            solver.Add(context.MkEq((ArithExpr)variable, context.MkInt(intValue)));
                        }
                        else if (IsRealType(outputName) && double.TryParse(outputValue, out double doubleValue))
                        {
                            solver.Add(context.MkEq((ArithExpr)variable, context.MkReal(doubleValue.ToString())));
                        }
                        else if (IsBooleanType(outputName) && bool.TryParse(outputValue, out bool boolValue))
                        {
                            solver.Add(context.MkEq((BoolExpr)variable, context.MkBool(boolValue)));
                        }
                    }
                }

                // Check if the constraints are satisfiable
                var status = solver.Check();
                if (status == Status.SATISFIABLE)
                {
                    var model = solver.Model;
                    foreach (var variable in variables)
                    {
                        var interpretation = model.Eval(variable.Value, true);
                        result[variable.Key] = interpretation.ToString();
                    }
                }
                else
                {
                    // No solution found
                    return null;
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error finding variable values: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds variable values that satisfy the given expression
        /// </summary>
        public async Task<Dictionary<string, string>> FindVariableValuesForExpressionAsync(
            string expression,
            Dictionary<string, string> variableTypes,
            Dictionary<string, VariableConstraint> constraints)
        {
            if (string.IsNullOrEmpty(expression))
                throw new ArgumentNullException(nameof(expression));
            if (variableTypes == null)
                throw new ArgumentNullException(nameof(variableTypes));

            using var context = new Context();
            var solver = context.MkSolver();
            var variables = new Dictionary<string, Expr>();
            var result = new Dictionary<string, string>();

            try
            {
                // Create Z3 variables
                foreach (var variable in variableTypes)
                {
                    var varName = variable.Key;
                    var varType = variable.Value;

                    // Create variable
                    Expr z3Variable;
                    if (IsIntegerType(varType))
                    {
                        z3Variable = context.MkIntConst(varName);
                    }
                    else if (IsRealType(varType))
                    {
                        z3Variable = context.MkRealConst(varName);
                    }
                    else if (IsBooleanType(varType))
                    {
                        z3Variable = context.MkBoolConst(varName);
                    }
                    else
                    {
                        // Default to integer
                        z3Variable = context.MkIntConst(varName);
                    }

                    variables[varName] = z3Variable;

                    // Add constraints if available
                    if (constraints != null && constraints.TryGetValue(varName, out var constraint))
                    {
                        // Add min/max constraints
                        if (!string.IsNullOrEmpty(constraint.MinValue))
                        {
                            if (IsIntegerType(varType))
                            {
                                if (int.TryParse(constraint.MinValue, out int minValue))
                                {
                                    solver.Add(context.MkGe((ArithExpr)z3Variable, context.MkInt(minValue)));
                                }
                            }
                            else if (IsRealType(varType))
                            {
                                if (double.TryParse(constraint.MinValue, out double minValue))
                                {
                                    solver.Add(context.MkGe((ArithExpr)z3Variable, context.MkReal(minValue.ToString())));
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(constraint.MaxValue))
                        {
                            if (IsIntegerType(varType))
                            {
                                if (int.TryParse(constraint.MaxValue, out int maxValue))
                                {
                                    solver.Add(context.MkLe((ArithExpr)z3Variable, context.MkInt(maxValue)));
                                }
                            }
                            else if (IsRealType(varType))
                            {
                                if (double.TryParse(constraint.MaxValue, out double maxValue))
                                {
                                    solver.Add(context.MkLe((ArithExpr)z3Variable, context.MkReal(maxValue.ToString())));
                                }
                            }
                        }
                    }
                }

                // Parse and add the expression
                var parsedExpr = ParseExpression(context, expression, variables);
                if (parsedExpr != null)
                {
                    solver.Add(context.MkEq(parsedExpr, context.MkBool(true)));
                }

                // Check if the constraints are satisfiable
                var status = solver.Check();
                if (status == Status.SATISFIABLE)
                {
                    var model = solver.Model;
                    foreach (var variable in variables)
                    {
                        var interpretation = model.Eval(variable.Value, true);
                        result[variable.Key] = interpretation.ToString();
                    }
                }
                else
                {
                    // No solution found
                    return null;
                }

                return result;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error finding variable values for expression: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Finds variable values to achieve the specified code coverage
        /// </summary>
        public async Task<List<Dictionary<string, string>>> FindVariableValuesForCoverageAsync(
            CFunctionAnalysis functionAnalysis,
            Dictionary<string, string> variableTypes,
            Dictionary<string, VariableConstraint> constraints,
            double targetCoverage = 0.9)
        {
            if (functionAnalysis == null)
                throw new ArgumentNullException(nameof(functionAnalysis));
            if (variableTypes == null)
                throw new ArgumentNullException(nameof(variableTypes));

            var results = new List<Dictionary<string, string>>();

            try
            {
                // Get the paths to cover
                var pathsToCover = new HashSet<CPath>(functionAnalysis.Paths.Where(p => p.IsExecutable));
                var coveredPaths = new HashSet<CPath>();

                // Calculate how many paths we need to cover
                int totalPaths = pathsToCover.Count;
                int pathsNeeded = (int)Math.Ceiling(totalPaths * targetCoverage);

                using var context = new Context();
                var solver = context.MkSolver();
                var variables = new Dictionary<string, Expr>();

                // Create Z3 variables
                foreach (var variable in variableTypes)
                {
                    var varName = variable.Key;
                    var varType = variable.Value;

                    // Create variable
                    Expr z3Variable;
                    if (IsIntegerType(varType))
                    {
                        z3Variable = context.MkIntConst(varName);
                    }
                    else if (IsRealType(varType))
                    {
                        z3Variable = context.MkRealConst(varName);
                    }
                    else if (IsBooleanType(varType))
                    {
                        z3Variable = context.MkBoolConst(varName);
                    }
                    else
                    {
                        // Default to integer
                        z3Variable = context.MkIntConst(varName);
                    }

                    variables[varName] = z3Variable;

                    // Add constraints if available
                    if (constraints != null && constraints.TryGetValue(varName, out var constraint))
                    {
                        // Add min/max constraints
                        if (!string.IsNullOrEmpty(constraint.MinValue))
                        {
                            if (IsIntegerType(varType))
                            {
                                if (int.TryParse(constraint.MinValue, out int minValue))
                                {
                                    solver.Add(context.MkGe((ArithExpr)z3Variable, context.MkInt(minValue)));
                                }
                            }
                            else if (IsRealType(varType))
                            {
                                if (double.TryParse(constraint.MinValue, out double minValue))
                                {
                                    solver.Add(context.MkGe((ArithExpr)z3Variable, context.MkReal(minValue.ToString())));
                                }
                            }
                        }

                        if (!string.IsNullOrEmpty(constraint.MaxValue))
                        {
                            if (IsIntegerType(varType))
                            {
                                if (int.TryParse(constraint.MaxValue, out int maxValue))
                                {
                                    solver.Add(context.MkLe((ArithExpr)z3Variable, context.MkInt(maxValue)));
                                }
                            }
                            else if (IsRealType(varType))
                            {
                                if (double.TryParse(constraint.MaxValue, out double maxValue))
                                {
                                    solver.Add(context.MkLe((ArithExpr)z3Variable, context.MkReal(maxValue.ToString())));
                                }
                            }
                        }
                    }
                }

                // Find variable values for each path until we reach the target coverage
                while (coveredPaths.Count < pathsNeeded && pathsToCover.Any())
                {
                    // Reset solver for each path
                    solver.Reset();

                    // Add variable constraints
                    foreach (var variable in variableTypes)
                    {
                        var varName = variable.Key;
                        var varType = variable.Value;

                        // Add constraints if available
                        if (constraints != null && constraints.TryGetValue(varName, out var constraint))
                        {
                            // Add min/max constraints
                            if (!string.IsNullOrEmpty(constraint.MinValue))
                            {
                                if (IsIntegerType(varType))
                                {
                                    if (int.TryParse(constraint.MinValue, out int minValue))
                                    {
                                        solver.Add(context.MkGe((ArithExpr)variables[varName], context.MkInt(minValue)));
                                    }
                                }
                                else if (IsRealType(varType))
                                {
                                    if (double.TryParse(constraint.MinValue, out double minValue))
                                    {
                                        solver.Add(context.MkGe((ArithExpr)variables[varName], context.MkReal(minValue.ToString())));
                                    }
                                }
                            }

                            if (!string.IsNullOrEmpty(constraint.MaxValue))
                            {
                                if (IsIntegerType(varType))
                                {
                                    if (int.TryParse(constraint.MaxValue, out int maxValue))
                                    {
                                        solver.Add(context.MkLe((ArithExpr)variables[varName], context.MkInt(maxValue)));
                                    }
                                }
                                else if (IsRealType(varType))
                                {
                                    if (double.TryParse(constraint.MaxValue, out double maxValue))
                                    {
                                        solver.Add(context.MkLe((ArithExpr)variables[varName], context.MkReal(maxValue.ToString())));
                                    }
                                }
                            }
                        }
                    }

                    // Get a path to cover
                    var pathToCover = pathsToCover.First();
                    pathsToCover.Remove(pathToCover);

                    // Parse and add the path condition
                    var pathCondition = pathToCover.PathCondition;
                    if (!string.IsNullOrEmpty(pathCondition))
                    {
                        var parsedExpr = ParseExpression(context, pathCondition, variables);
                        if (parsedExpr != null)
                        {
                            solver.Add(context.MkEq(parsedExpr, context.MkBool(true)));
                        }
                    }

                    // Check if the constraints are satisfiable
                    var status = solver.Check();
                    if (status == Status.SATISFIABLE)
                    {
                        var model = solver.Model;
                        var result = new Dictionary<string, string>();

                        foreach (var variable in variables)
                        {
                            var interpretation = model.Eval(variable.Value, true);
                            result[variable.Key] = interpretation.ToString();
                        }

                        results.Add(result);
                        coveredPaths.Add(pathToCover);
                    }
                }

                return results;
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error finding variable values for coverage: {ex.Message}");
                return null;
            }
        }

        #region Helper Methods

        /// <summary>
        /// Determines if a type is an integer type
        /// </summary>
        private bool IsIntegerType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            string normalizedType = type.ToLower().Trim();
            return normalizedType == "int" || normalizedType == "int32" ||
                   normalizedType == "long" || normalizedType == "int64" ||
                   normalizedType == "short" || normalizedType == "int16" ||
                   normalizedType == "byte" || normalizedType == "uint8" ||
                   normalizedType == "sbyte" || normalizedType == "int8" ||
                   normalizedType == "uint" || normalizedType == "uint32" ||
                   normalizedType == "ulong" || normalizedType == "uint64" ||
                   normalizedType == "ushort" || normalizedType == "uint16";
        }

        /// <summary>
        /// Determines if a type is a real (floating-point) type
        /// </summary>
        private bool IsRealType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            string normalizedType = type.ToLower().Trim();
            return normalizedType == "float" || normalizedType == "single" ||
                   normalizedType == "double" || normalizedType == "decimal";
        }

        /// <summary>
        /// Determines if a type is a boolean type
        /// </summary>
        private bool IsBooleanType(string type)
        {
            if (string.IsNullOrEmpty(type))
                return false;

            string normalizedType = type.ToLower().Trim();
            return normalizedType == "bool" || normalizedType == "boolean";
        }

        /// <summary>
        /// Parses an expression string into a Z3 expression
        /// </summary>
        private Expr ParseExpression(Context context, string expression, Dictionary<string, Expr> variables)
        {
            if (string.IsNullOrEmpty(expression))
                return null;

            // This is a simplified parser that handles only basic expressions
            // A real implementation would need a more sophisticated parser

            // Check for simple equality
            if (expression.Contains("=="))
            {
                var parts = expression.Split(new[] { "==" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = ParseTerm(context, parts[0].Trim(), variables);
                    var right = ParseTerm(context, parts[1].Trim(), variables);
                    if (left != null && right != null)
                    {
                        return context.MkEq(left, right);
                    }
                }
            }

            // Check for inequality
            if (expression.Contains("!="))
            {
                var parts = expression.Split(new[] { "!=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = ParseTerm(context, parts[0].Trim(), variables);
                    var right = ParseTerm(context, parts[1].Trim(), variables);
                    if (left != null && right != null)
                    {
                        return context.MkNot(context.MkEq(left, right));
                    }
                }
            }

            // Check for less than
            if (expression.Contains("<") && !expression.Contains("<="))
            {
                var parts = expression.Split(new[] { "<" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = ParseTerm(context, parts[0].Trim(), variables);
                    var right = ParseTerm(context, parts[1].Trim(), variables);
                    if (left is ArithExpr leftArith && right is ArithExpr rightArith)
                    {
                        return context.MkLt(leftArith, rightArith);
                    }
                }
            }

            // Check for less than or equal
            if (expression.Contains("<="))
            {
                var parts = expression.Split(new[] { "<=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = ParseTerm(context, parts[0].Trim(), variables);
                    var right = ParseTerm(context, parts[1].Trim(), variables);
                    if (left is ArithExpr leftArith && right is ArithExpr rightArith)
                    {
                        return context.MkLe(leftArith, rightArith);
                    }
                }
            }

            // Check for greater than
            if (expression.Contains(">") && !expression.Contains(">="))
            {
                var parts = expression.Split(new[] { ">" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = ParseTerm(context, parts[0].Trim(), variables);
                    var right = ParseTerm(context, parts[1].Trim(), variables);
                    if (left is ArithExpr leftArith && right is ArithExpr rightArith)
                    {
                        return context.MkGt(leftArith, rightArith);
                    }
                }
            }

            // Check for greater than or equal
            if (expression.Contains(">="))
            {
                var parts = expression.Split(new[] { ">=" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = ParseTerm(context, parts[0].Trim(), variables);
                    var right = ParseTerm(context, parts[1].Trim(), variables);
                    if (left is ArithExpr leftArith && right is ArithExpr rightArith)
                    {
                        return context.MkGe(leftArith, rightArith);
                    }
                }
            }

            // Check for logical AND
            if (expression.Contains("&&"))
            {
                var parts = expression.Split(new[] { "&&" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = ParseExpression(context, parts[0].Trim(), variables);
                    var right = ParseExpression(context, parts[1].Trim(), variables);
                    if (left is BoolExpr leftBool && right is BoolExpr rightBool)
                    {
                        return context.MkAnd(leftBool, rightBool);
                    }
                }
            }

            // Check for logical OR
            if (expression.Contains("||"))
            {
                var parts = expression.Split(new[] { "||" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    var left = ParseExpression(context, parts[0].Trim(), variables);
                    var right = ParseExpression(context, parts[1].Trim(), variables);
                    if (left is BoolExpr leftBool && right is BoolExpr rightBool)
                    {
                        return context.MkOr(leftBool, rightBool);
                    }
                }
            }

            // If it's a simple variable, return it
            if (variables.TryGetValue(expression, out var variable))
            {
                return variable;
            }

            // If it's a boolean literal
            if (expression.ToLower() == "true")
            {
                return context.MkBool(true);
            }
            else if (expression.ToLower() == "false")
            {
                return context.MkBool(false);
            }

            // If it's a numeric literal
            if (int.TryParse(expression, out int intValue))
            {
                return context.MkInt(intValue);
            }
            else if (double.TryParse(expression, out double doubleValue))
            {
                return context.MkReal(doubleValue.ToString());
            }

            // If we can't parse it, return null
            return null;
        }

        /// <summary>
        /// Parses a term (variable or literal) into a Z3 expression
        /// </summary>
        private Expr ParseTerm(Context context, string term, Dictionary<string, Expr> variables)
        {
            if (string.IsNullOrEmpty(term))
                return null;

            // If it's a variable, return it
            if (variables.TryGetValue(term, out var variable))
            {
                return variable;
            }

            // If it's a boolean literal
            if (term.ToLower() == "true")
            {
                return context.MkBool(true);
            }
            else if (term.ToLower() == "false")
            {
                return context.MkBool(false);
            }

            // If it's a numeric literal
            if (int.TryParse(term, out int intValue))
            {
                return context.MkInt(intValue);
            }
            else if (double.TryParse(term, out double doubleValue))
            {
                return context.MkReal(doubleValue.ToString());
            }

            // Handle basic arithmetic expressions
            if (term.Contains("+"))
            {
                var parts = term.Split('+');
                if (parts.Length == 2)
                {
                    var left = ParseTerm(context, parts[0].Trim(), variables);
                    var right = ParseTerm(context, parts[1].Trim(), variables);
                    if (left is ArithExpr leftArith && right is ArithExpr rightArith)
                    {
                        return context.MkAdd(leftArith, rightArith);
                    }
                }
            }

            if (term.Contains("-"))
            {
                var parts = term.Split('-');
                if (parts.Length == 2)
                {
                    var left = ParseTerm(context, parts[0].Trim(), variables);
                    var right = ParseTerm(context, parts[1].Trim(), variables);
                    if (left is ArithExpr leftArith && right is ArithExpr rightArith)
                    {
                        return context.MkSub(leftArith, rightArith);
                    }
                }
            }

            if (term.Contains("*"))
            {
                var parts = term.Split('*');
                if (parts.Length == 2)
                {
                    var left = ParseTerm(context, parts[0].Trim(), variables);
                    var right = ParseTerm(context, parts[1].Trim(), variables);
                    if (left is ArithExpr leftArith && right is ArithExpr rightArith)
                    {
                        return context.MkMul(leftArith, rightArith);
                    }
                }
            }

            if (term.Contains("/"))
            {
                var parts = term.Split('/');
                if (parts.Length == 2)
                {
                    var left = ParseTerm(context, parts[0].Trim(), variables);
                    var right = ParseTerm(context, parts[1].Trim(), variables);
                    if (left is ArithExpr leftArith && right is ArithExpr rightArith)
                    {
                        return context.MkDiv(leftArith, rightArith);
                    }
                }
            }

            // If we can't parse it, return null
            return null;
        }

        #endregion
    }
}
