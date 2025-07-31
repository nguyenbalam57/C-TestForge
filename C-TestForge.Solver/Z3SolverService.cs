using C_TestForge.Core.Interfaces.Solver;
using C_TestForge.Models;
using C_TestForge.Models.Core;
using Microsoft.Z3;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.SolverServices
{
    /// <summary>
    /// Implementation of IZ3SolverService using Z3 Theorem Prover
    /// </summary>
    public class Z3SolverService : IZ3SolverService, IDisposable
    {
        private readonly Context _context;
        private readonly ILogger<Z3SolverService> _logger;
        private bool _disposed = false;

        /// <summary>
        /// Constructor for Z3SolverService
        /// </summary>
        /// <param name="logger">Logger for logging solver operations</param>
        public Z3SolverService(ILogger<Z3SolverService> logger)
        {
            _logger = logger;

            // Configure Z3 parameters
            var config = new Dictionary<string, string>
            {
                { "model", "true" },
                { "proof", "false" }
            };

            // Create Z3 context
            _context = new Context(config);
            _logger.LogInformation("Z3SolverService initialized");
        }

        /// <summary>
        /// Finds variable values that satisfy the given constraints
        /// </summary>
        /// <param name="constraints">The constraints to satisfy</param>
        /// <param name="expectedOutputs">The expected outputs</param>
        /// <returns>Dictionary of variable names and their values</returns>
        public async Task<Dictionary<string, string>> FindVariableValuesAsync(
            Dictionary<string, VariableConstraint> constraints,
            Dictionary<string, string> expectedOutputs)
        {
            try
            {
                _logger.LogInformation("Finding variable values for {0} constraints with {1} expected outputs",
                    constraints.Count, expectedOutputs.Count);

                return await Task.Run(() =>
                {
                    using (var solver = _context.MkSolver())
                    {
                        // Create variables for each constraint
                        var variables = new Dictionary<string, Expr>();

                        foreach (var constraint in constraints)
                        {
                            try
                            {
                                // Create appropriate Z3 variable based on constraint type
                                Expr variable = CreateVariableFromConstraint(constraint.Key, constraint.Value);
                                variables.Add(constraint.Key, variable);

                                // Add constraint to the solver
                                AddConstraintToSolver(solver, constraint.Key, constraint.Value, variable);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing constraint for variable {0}", constraint.Key);
                            }
                        }

                        // Add constraints for expected outputs
                        foreach (var output in expectedOutputs)
                        {
                            try
                            {
                                if (variables.ContainsKey(output.Key))
                                {
                                    // If the output variable is already in our variables, add a constraint for its expected value
                                    var expectedValue = ParseValue(output.Value, variables[output.Key].Sort);
                                    solver.Add(_context.MkEq(variables[output.Key], expectedValue));
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing expected output for variable {0}", output.Key);
                            }
                        }

                        // Check if the constraints are satisfiable
                        if (solver.Check() == Status.SATISFIABLE)
                        {
                            _logger.LogInformation("Found satisfiable assignment");

                            var model = solver.Model;
                            var result = new Dictionary<string, string>();

                            // Extract values for each variable
                            foreach (var variable in variables)
                            {
                                try
                                {
                                    var value = model.Eval(variable.Value, true);
                                    result.Add(variable.Key, value.ToString());
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error evaluating model for variable {0}", variable.Key);
                                }
                            }

                            return result;
                        }
                        else
                        {
                            _logger.LogWarning("No satisfiable assignment found");
                            return new Dictionary<string, string>();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FindVariableValuesAsync");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Finds variable values that satisfy the given expression
        /// </summary>
        /// <param name="expression">The expression to satisfy</param>
        /// <param name="variableTypes">Dictionary of variable names and their types</param>
        /// <param name="constraints">The constraints to satisfy</param>
        /// <returns>Dictionary of variable names and their values</returns>
        public async Task<Dictionary<string, string>> FindVariableValuesForExpressionAsync(
            string expression,
            Dictionary<string, string> variableTypes,
            Dictionary<string, VariableConstraint> constraints)
        {
            try
            {
                _logger.LogInformation("Finding variable values for expression: {0}", expression);

                return await Task.Run(() =>
                {
                    using (var solver = _context.MkSolver())
                    {
                        // Create variables for each variable in the expression
                        var variables = new Dictionary<string, Expr>();

                        foreach (var variableType in variableTypes)
                        {
                            try
                            {
                                // Create appropriate Z3 variable based on type
                                Expr variable = CreateVariableFromType(variableType.Key, variableType.Value);
                                variables.Add(variableType.Key, variable);

                                // Add constraint if exists
                                if (constraints.ContainsKey(variableType.Key))
                                {
                                    AddConstraintToSolver(solver, variableType.Key, constraints[variableType.Key], variable);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing variable {0} of type {1}",
                                    variableType.Key, variableType.Value);
                            }
                        }

                        try
                        {
                            // Parse and add the expression as a constraint
                            var parsedExpression = ParseExpression(expression, variables);
                            solver.Add(_context.MkEq(parsedExpression, _context.MkBool(true)));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error parsing expression: {0}", expression);
                            return new Dictionary<string, string>();
                        }

                        // Check if the constraints are satisfiable
                        if (solver.Check() == Status.SATISFIABLE)
                        {
                            _logger.LogInformation("Found satisfiable assignment");

                            var model = solver.Model;
                            var result = new Dictionary<string, string>();

                            // Extract values for each variable
                            foreach (var variable in variables)
                            {
                                try
                                {
                                    var value = model.Eval(variable.Value, true);
                                    result.Add(variable.Key, value.ToString());
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(ex, "Error evaluating model for variable {0}", variable.Key);
                                }
                            }

                            return result;
                        }
                        else
                        {
                            _logger.LogWarning("No satisfiable assignment found");
                            return new Dictionary<string, string>();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FindVariableValuesForExpressionAsync");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// Finds variable values to achieve the specified code coverage
        /// </summary>
        /// <param name="functionAnalysis">The function analysis</param>
        /// <param name="variableTypes">Dictionary of variable names and their types</param>
        /// <param name="constraints">The constraints to satisfy</param>
        /// <param name="targetCoverage">The target coverage (0.0-1.0)</param>
        /// <returns>List of dictionaries of variable names and their values</returns>
        public async Task<List<Dictionary<string, string>>> FindVariableValuesForCoverageAsync(
            CFunction functionAnalysis,
            Dictionary<string, string> variableTypes,
            Dictionary<string, VariableConstraint> constraints,
            double targetCoverage = 0.9)
        {
            try
            {
                _logger.LogInformation("Finding variable values for coverage of function {0} with target coverage {1}",
                    functionAnalysis.Name, targetCoverage);

                return await Task.Run(() =>
                {
                    var result = new List<Dictionary<string, string>>();
                    var coveredPaths = new HashSet<string>();
                    var totalPaths = functionAnalysis.ControlFlowPaths?.Count ?? 0;

                    if (totalPaths == 0)
                    {
                        _logger.LogWarning("Function {0} has no control flow paths", functionAnalysis.Name);
                        return result;
                    }

                    // Calculate the number of paths needed to achieve the target coverage
                    int pathsNeeded = (int)Math.Ceiling(totalPaths * targetCoverage);

                    _logger.LogInformation("Function {0} has {1} paths, need to cover {2} paths to achieve {3}% coverage",
                        functionAnalysis.Name, totalPaths, pathsNeeded, targetCoverage * 100);

                    // Try to find values for each path
                    foreach (var path in functionAnalysis.ControlFlowPaths)
                    {
                        if (coveredPaths.Count >= pathsNeeded)
                        {
                            _logger.LogInformation("Achieved target coverage with {0} paths", coveredPaths.Count);
                            break;
                        }

                        try
                        {
                            using (var solver = _context.MkSolver())
                            {
                                // Create variables for each variable in the path
                                var variables = new Dictionary<string, Expr>();

                                foreach (var variableType in variableTypes)
                                {
                                    try
                                    {
                                        // Create appropriate Z3 variable based on type
                                        Expr variable = CreateVariableFromType(variableType.Key, variableType.Value);
                                        variables.Add(variableType.Key, variable);

                                        // Add constraint if exists
                                        if (constraints.ContainsKey(variableType.Key))
                                        {
                                            AddConstraintToSolver(solver, variableType.Key, constraints[variableType.Key], variable);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error processing variable {0} of type {1}",
                                            variableType.Key, variableType.Value);
                                    }
                                }

                                // Parse and add the path condition as a constraint
                                var pathCondition = path.Condition;
                                if (!string.IsNullOrEmpty(pathCondition))
                                {
                                    try
                                    {
                                        var parsedCondition = ParseExpression(pathCondition, variables);
                                        solver.Add(_context.MkEq(parsedCondition, _context.MkBool(true)));
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error parsing path condition: {0}", pathCondition);
                                        continue;
                                    }

                                    // Check if the constraints are satisfiable
                                    if (solver.Check() == Status.SATISFIABLE)
                                    {
                                        _logger.LogInformation("Found satisfiable assignment for path {0}", path.Id);

                                        var model = solver.Model;
                                        var pathValues = new Dictionary<string, string>();

                                        // Extract values for each variable
                                        foreach (var variable in variables)
                                        {
                                            try
                                            {
                                                var value = model.Eval(variable.Value, true);
                                                pathValues.Add(variable.Key, value.ToString());
                                            }
                                            catch (Exception ex)
                                            {
                                                _logger.LogError(ex, "Error evaluating model for variable {0}", variable.Key);
                                            }
                                        }

                                        result.Add(pathValues);
                                        coveredPaths.Add(path.Id);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("No satisfiable assignment found for path {0}", path.Id);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing path {0}", path.Id);
                        }
                    }

                    _logger.LogInformation("Found variable values for {0} out of {1} paths",
                        coveredPaths.Count, totalPaths);

                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in FindVariableValuesForCoverageAsync");
                return new List<Dictionary<string, string>>();
            }
        }

        #region Helper Methods

        /// <summary>
        /// Creates a Z3 variable from a constraint
        /// </summary>
        private Expr CreateVariableFromConstraint(string name, VariableConstraint constraint)
        {
            try
            {
                // Determine the appropriate sort based on constraint type
                if (constraint.Type == ConstraintType.Enumeration)
                {
                    // For enumerations, create a finite domain sort
                    return _context.MkIntConst(name);
                }
                else if (!string.IsNullOrEmpty(constraint.MinValue) || !string.IsNullOrEmpty(constraint.MaxValue))
                {
                    // For numeric constraints, create integer or real sort
                    if ((constraint.MinValue != null && constraint.MinValue.Contains(".")) ||
                        (constraint.MaxValue != null && constraint.MaxValue.Contains(".")))
                    {
                        return _context.MkRealConst(name);
                    }
                    else
                    {
                        return _context.MkIntConst(name);
                    }
                }
                else
                {
                    // Default to integer sort
                    return _context.MkIntConst(name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating variable from constraint for {0}", name);
                return _context.MkIntConst(name);
            }
        }

        /// <summary>
        /// Creates a Z3 variable from a type string
        /// </summary>
        private Expr CreateVariableFromType(string name, string type)
        {
            try
            {
                switch (type.ToLower())
                {
                    case "int":
                    case "int32":
                    case "int64":
                    case "long":
                    case "short":
                    case "byte":
                    case "sbyte":
                    case "uint":
                    case "uint32":
                    case "uint64":
                    case "ulong":
                    case "ushort":
                        return _context.MkIntConst(name);

                    case "float":
                    case "double":
                    case "decimal":
                        return _context.MkRealConst(name);

                    case "bool":
                        return _context.MkBoolConst(name);

                    case "char":
                        // For chars, use bit vector of size 8 (8 bits for ASCII)
                        return _context.MkBVConst(name, 8);

                    case "string":
                        // For strings, create a sequence sort
                        // This is a simplification, as Z3 has a special string theory
                        return _context.MkIntConst(name);

                    // Add bit vector types for C types
                    case "uint8_t":
                    case "uint8":
                        return _context.MkBVConst(name, 8);

                    case "uint16_t":
                    case "uint16":
                        return _context.MkBVConst(name, 16);

                    case "uint32_t":
                        return _context.MkBVConst(name, 32);

                    case "uint64_t":
                        return _context.MkBVConst(name, 64);

                    case "int8_t":
                    case "int8":
                        return _context.MkBVConst(name, 8);

                    case "int16_t":
                    case "int16":
                        return _context.MkBVConst(name, 16);

                    case "int32_t":
                        return _context.MkBVConst(name, 32);

                    case "int64_t":
                        return _context.MkBVConst(name, 64);

                    default:
                        // Default to integer
                        return _context.MkIntConst(name);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating variable from type for {0}", name);
                return _context.MkIntConst(name);
            }
        }

        /// <summary>
        /// Adds a constraint to the solver
        /// </summary>
        private void AddConstraintToSolver(Solver solver, string name, VariableConstraint constraint, Expr variable)
        {
            try
            {
                switch (constraint.Type)
                {
                    case ConstraintType.MinValue:
                        if (!string.IsNullOrEmpty(constraint.MinValue))
                        {
                            var minValue = ParseValue(constraint.MinValue, variable.Sort);

                            if (variable.Sort.Equals(_context.IntSort) || variable.Sort.Equals(_context.RealSort))
                            {
                                solver.Add(_context.MkGe((ArithExpr)variable, (ArithExpr)minValue));
                            }
                            else if (variable.Sort.SortKind == Z3_sort_kind.Z3_BV_SORT)
                            {
                                // For bit vectors, use unsigned greater or equal
                                solver.Add(_context.MkBVUGE((BitVecExpr)variable, (BitVecExpr)minValue));
                            }
                        }
                        break;

                    case ConstraintType.MaxValue:
                        if (!string.IsNullOrEmpty(constraint.MaxValue))
                        {
                            var maxValue = ParseValue(constraint.MaxValue, variable.Sort);

                            if (variable.Sort.Equals(_context.IntSort) || variable.Sort.Equals(_context.RealSort))
                            {
                                solver.Add(_context.MkLe((ArithExpr)variable, (ArithExpr)maxValue));
                            }
                            else if (variable.Sort.SortKind == Z3_sort_kind.Z3_BV_SORT)
                            {
                                // For bit vectors, use unsigned less or equal
                                solver.Add(_context.MkBVULE((BitVecExpr)variable, (BitVecExpr)maxValue));
                            }
                        }
                        break;

                    case ConstraintType.Range:
                        if (!string.IsNullOrEmpty(constraint.MinValue))
                        {
                            var minValue = ParseValue(constraint.MinValue, variable.Sort);

                            if (variable.Sort.Equals(_context.IntSort) || variable.Sort.Equals(_context.RealSort))
                            {
                                solver.Add(_context.MkGe((ArithExpr)variable, (ArithExpr)minValue));
                            }
                            else if (variable.Sort.SortKind == Z3_sort_kind.Z3_BV_SORT)
                            {
                                // For bit vectors, use unsigned greater or equal
                                solver.Add(_context.MkBVUGE((BitVecExpr)variable, (BitVecExpr)minValue));
                            }
                        }

                        if (!string.IsNullOrEmpty(constraint.MaxValue))
                        {
                            var maxValue = ParseValue(constraint.MaxValue, variable.Sort);

                            if (variable.Sort.Equals(_context.IntSort) || variable.Sort.Equals(_context.RealSort))
                            {
                                solver.Add(_context.MkLe((ArithExpr)variable, (ArithExpr)maxValue));
                            }
                            else if (variable.Sort.SortKind == Z3_sort_kind.Z3_BV_SORT)
                            {
                                // For bit vectors, use unsigned less or equal
                                solver.Add(_context.MkBVULE((BitVecExpr)variable, (BitVecExpr)maxValue));
                            }
                        }
                        break;

                    case ConstraintType.Enumeration:
                        if (constraint.AllowedValues != null && constraint.AllowedValues.Count > 0)
                        {
                            var allowedExprs = new List<BoolExpr>();

                            foreach (var value in constraint.AllowedValues)
                            {
                                var parsedValue = ParseValue(value, variable.Sort);
                                allowedExprs.Add(_context.MkEq(variable, parsedValue));
                            }

                            solver.Add(_context.MkOr(allowedExprs.ToArray()));
                        }
                        break;

                    case ConstraintType.Custom:
                        if (!string.IsNullOrEmpty(constraint.Expression))
                        {
                            // Parse and add custom expression
                            var variables = new Dictionary<string, Expr> { { name, variable } };
                            var parsedExpression = ParseExpression(constraint.Expression, variables);
                            solver.Add(_context.MkEq(parsedExpression, _context.MkBool(true)));
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding constraint to solver for variable {0}", name);
            }
        }

        /// <summary>
        /// Parses a value string to a Z3 expression of the appropriate sort
        /// </summary>
        private Expr ParseValue(string value, Sort sort)
        {
            try
            {
                if (sort.Equals(_context.IntSort))
                {
                    if (int.TryParse(value, out int intValue))
                    {
                        return _context.MkInt(intValue);
                    }
                    return _context.MkInt(0);
                }
                else if (sort.Equals(_context.RealSort))
                {
                    if (double.TryParse(value, out double doubleValue))
                    {
                        return _context.MkReal(doubleValue.ToString());
                    }
                    return _context.MkReal(0);
                }
                else if (sort.Equals(_context.BoolSort))
                {
                    if (bool.TryParse(value, out bool boolValue))
                    {
                        return _context.MkBool(boolValue);
                    }
                    return _context.MkBool(false);
                }
                else if (sort.SortKind == Z3_sort_kind.Z3_BV_SORT)
                {
                    // For bit vectors
                    if (int.TryParse(value, out int intValue))
                    {
                        // Get the size of the bit vector sort
                        uint size = ((BitVecSort)sort).Size;
                        return _context.MkBV(intValue, size);
                    }
                    // Default to 0 with the appropriate size
                    uint defaultSize = ((BitVecSort)sort).Size;
                    return _context.MkBV(0, defaultSize);
                }
                else
                {
                    // Default to integer
                    return _context.MkInt(0);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing value {0} for sort {1}", value, sort);
                return _context.MkInt(0);
            }
        }

        /// <summary>
        /// Parses an expression string to a Z3 expression
        /// </summary>
        private Expr ParseExpression(string expression, Dictionary<string, Expr> variables)
        {
            try
            {
                // Remove whitespace and handle parentheses
                expression = expression.Trim();

                // Handle expressions with parentheses
                if (expression.Contains("("))
                {
                    // Complex expression parsing would be implemented here
                    // This is a simplified implementation that works for basic expressions

                    // For now, we'll handle some common C expressions
                    // Replace C operators with Z3 operations
                    expression = expression
                        .Replace("&&", " and ")
                        .Replace("||", " or ")
                        .Replace("==", " = ")
                        .Replace("!=", " != ")
                        .Replace("!", " not ");

                    // Create a Z3 expression using the Z3 C# API's parsing capability
                    try
                    {
                        // We'll need to build expressions manually for complex cases
                        // This is just a placeholder for actual parsing logic
                        // For a real implementation, we would need to parse the expression
                        // into an abstract syntax tree and convert it to Z3 expressions

                        // For demonstration purposes, let's handle some simple cases
                        if (expression.Contains(">"))
                        {
                            var parts = expression.Split('>');
                            if (parts.Length == 2)
                            {
                                var left = ParseSimpleExpression(parts[0].Trim(), variables);
                                var right = ParseSimpleExpression(parts[1].Trim(), variables);

                                if (left.Sort.Equals(_context.IntSort) || left.Sort.Equals(_context.RealSort))
                                {
                                    return _context.MkGt((ArithExpr)left, (ArithExpr)right);
                                }
                                else if (left.Sort.SortKind == Z3_sort_kind.Z3_BV_SORT)
                                {
                                    return _context.MkBVUGT((BitVecExpr)left, (BitVecExpr)right);
                                }
                            }
                        }
                        else if (expression.Contains("<"))
                        {
                            var parts = expression.Split('<');
                            if (parts.Length == 2)
                            {
                                var left = ParseSimpleExpression(parts[0].Trim(), variables);
                                var right = ParseSimpleExpression(parts[1].Trim(), variables);

                                if (left.Sort.Equals(_context.IntSort) || left.Sort.Equals(_context.RealSort))
                                {
                                    return _context.MkLt((ArithExpr)left, (ArithExpr)right);
                                }
                                else if (left.Sort.SortKind == Z3_sort_kind.Z3_BV_SORT)
                                {
                                    return _context.MkBVULT((BitVecExpr)left, (BitVecExpr)right);
                                }
                            }
                        }
                        else if (expression.Contains("="))
                        {
                            var parts = expression.Split('=');
                            if (parts.Length == 2)
                            {
                                var left = ParseSimpleExpression(parts[0].Trim(), variables);
                                var right = ParseSimpleExpression(parts[1].Trim(), variables);
                                return _context.MkEq(left, right);
                            }
                        }

                        // Default case - return a true expression if we can't parse
                        _logger.LogWarning("Could not parse complex expression: {0}", expression);
                        return _context.MkBool(true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error parsing complex expression: {0}", expression);
                        return _context.MkBool(true);
                    }
                }
                else
                {
                    // Simple expression (variable or constant)
                    return ParseSimpleExpression(expression, variables);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ParseExpression for expression: {0}", expression);
                return _context.MkBool(true);
            }
        }

        /// <summary>
        /// Parses a simple expression (variable or constant) to a Z3 expression
        /// </summary>
        private Expr ParseSimpleExpression(string expression, Dictionary<string, Expr> variables)
        {
            expression = expression.Trim();

            // Check if expression is a variable
            if (variables.ContainsKey(expression))
            {
                return variables[expression];
            }

            // Check if expression is a boolean constant
            if (expression.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return _context.MkBool(true);
            }
            else if (expression.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return _context.MkBool(false);
            }

            // Check if expression is a numeric constant
            if (int.TryParse(expression, out int intValue))
            {
                return _context.MkInt(intValue);
            }
            else if (double.TryParse(expression, out double doubleValue))
            {
                return _context.MkReal(doubleValue.ToString());
            }

            // Default to boolean true
            _logger.LogWarning("Could not parse simple expression: {0}", expression);
            return _context.MkBool(true);
        }

        /// <summary>
        /// Evaluates a Z3 expression with the given variable values
        /// </summary>
        public string EvaluateExpression(string expression, Dictionary<string, string> variableValues, Dictionary<string, string> variableTypes)
        {
            try
            {
                _logger.LogInformation("Evaluating expression: {0}", expression);

                using (var solver = _context.MkSolver())
                {
                    // Create variables and set their values
                    var variables = new Dictionary<string, Expr>();

                    foreach (var variableValue in variableValues)
                    {
                        try
                        {
                            string type = variableTypes.ContainsKey(variableValue.Key) ?
                                variableTypes[variableValue.Key] : "int";

                            // Create appropriate Z3 variable based on type
                            Expr variable = CreateVariableFromType(variableValue.Key, type);
                            variables.Add(variableValue.Key, variable);

                            // Set variable value
                            var value = ParseValue(variableValue.Value, variable.Sort);
                            solver.Add(_context.MkEq(variable, value));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error processing variable value for {0}", variableValue.Key);
                        }
                    }

                    // Parse the expression
                    var parsedExpression = ParseExpression(expression, variables);

                    // Check if the constraints are satisfiable
                    if (solver.Check() == Status.SATISFIABLE)
                    {
                        var model = solver.Model;
                        var result = model.Eval(parsedExpression, true);
                        return result.ToString();
                    }
                    else
                    {
                        _logger.LogWarning("No satisfiable assignment found for expression evaluation");
                        return "Unknown";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EvaluateExpression for expression: {0}", expression);
                return "Error";
            }
        }

        /// <summary>
        /// Validates whether a constraint is satisfiable
        /// </summary>
        public async Task<bool> ValidateConstraintAsync(VariableConstraint constraint, string variableType = "int")
        {
            try
            {
                _logger.LogInformation("Validating constraint for variable {0}", constraint.VariableName);

                return await Task.Run(() =>
                {
                    using (var solver = _context.MkSolver())
                    {
                        // Create variable
                        Expr variable = CreateVariableFromType(constraint.VariableName, variableType);

                        // Add constraint
                        AddConstraintToSolver(solver, constraint.VariableName, constraint, variable);

                        // Check if the constraint is satisfiable
                        return solver.Check() == Status.SATISFIABLE;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateConstraintAsync for variable {0}", constraint.VariableName);
                return false;
            }
        }

        /// <summary>
        /// Generates sample values for a variable based on its constraints
        /// </summary>
        public async Task<List<string>> GenerateSampleValuesAsync(string variableName, string variableType, VariableConstraint constraint, int count = 10)
        {
            try
            {
                _logger.LogInformation("Generating {0} sample values for variable {1}", count, variableName);

                return await Task.Run(() =>
                {
                    var result = new List<string>();

                    using (var solver = _context.MkSolver())
                    {
                        // Create variable
                        Expr variable = CreateVariableFromType(variableName, variableType);

                        // Add constraint
                        if (constraint != null)
                        {
                            AddConstraintToSolver(solver, variableName, constraint, variable);
                        }

                        // Generate different values by adding exclusion constraints
                        for (int i = 0; i < count; i++)
                        {
                            if (solver.Check() == Status.SATISFIABLE)
                            {
                                var model = solver.Model;
                                var value = model.Eval(variable, true);
                                result.Add(value.ToString());

                                // Add constraint to exclude this value in next iterations
                                solver.Add(_context.MkNot(_context.MkEq(variable, value)));
                            }
                            else
                            {
                                _logger.LogInformation("No more satisfiable assignments for variable {0}", variableName);
                                break;
                            }
                        }
                    }

                    return result;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GenerateSampleValuesAsync for variable {0}", variableName);
                return new List<string>();
            }
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected dispose method
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    _context?.Dispose();
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~Z3SolverService()
        {
            Dispose(false);
        }

        #endregion
    }
}