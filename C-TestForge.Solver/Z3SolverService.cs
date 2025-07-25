using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Z3;
using C_TestForge.Models;
using C_TestForge.Core.Interfaces;

namespace C_TestForge.Solver
{
    public class Z3SolverService : IZ3SolverService
    {
        private readonly Context _context;

        public Z3SolverService()
        {
            // Configure Z3 context
            Dictionary<string, string> config = new Dictionary<string, string>
            {
                { "model", "true" }
            };
            _context = new Context(config);
        }

        public Dictionary<string, object> FindSatisfyingValues(List<CVariable> variables, List<string> constraints)
        {
            Solver solver = _context.MkSolver();
            Dictionary<string, Expr> z3Vars = new Dictionary<string, Expr>();
            Dictionary<string, object> results = new Dictionary<string, object>();

            try
            {
                // Create Z3 variables for each CVariable
                foreach (var variable in variables)
                {
                    Expr z3Var = CreateZ3Variable(variable);
                    z3Vars.Add(variable.Name, z3Var);

                    // Add variable constraints (min/max values)
                    AddVariableConstraints(solver, variable, z3Var);
                }

                // Add additional constraints
                foreach (var constraint in constraints)
                {
                    BoolExpr constraintExpr = ParseConstraint(constraint, z3Vars);
                    if (constraintExpr != null)
                    {
                        solver.Add(constraintExpr);
                    }
                }

                // Check for satisfiability
                Status status = solver.Check();
                if (status == Status.SATISFIABLE)
                {
                    Model model = solver.Model;

                    // Extract results
                    foreach (var entry in z3Vars)
                    {
                        string varName = entry.Key;
                        Expr varExpr = entry.Value;

                        var variable = variables.FirstOrDefault(v => v.Name == varName);
                        if (variable != null)
                        {
                            var interpretation = model.Eval(varExpr, true);
                            results[varName] = ConvertZ3ResultToNativeType(interpretation, variable.Type);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log exception
                Console.WriteLine($"Z3 Solver error: {ex.Message}");
            }

            return results;
        }

        private Expr CreateZ3Variable(CVariable variable)
        {
            switch (variable.Type.ToLower())
            {
                case "int":
                case "int32_t":
                case "int16_t":
                case "int8_t":
                    return _context.MkIntConst(variable.Name);

                case "uint32_t":
                case "uint16_t":
                case "uint8_t":
                case "unsigned int":
                case "unsigned char":
                    return _context.MkIntConst(variable.Name); // Using Int with constraints

                case "float":
                case "double":
                    return _context.MkRealConst(variable.Name);

                case "bool":
                    return _context.MkBoolConst(variable.Name);

                default:
                    // For unsupported types, default to int
                    return _context.MkIntConst(variable.Name);
            }
        }

        private void AddVariableConstraints(Solver solver, CVariable variable, Expr z3Var)
        {
            // Handle min/max constraints for numeric types
            if (z3Var is IntExpr intVar)
            {
                // Add min constraint if exists
                if (variable.MinValue != null && int.TryParse(variable.MinValue.ToString(), out int minValue))
                {
                    solver.Add(_context.MkGe(intVar, _context.MkInt(minValue)));
                }

                // Add max constraint if exists
                if (variable.MaxValue != null && int.TryParse(variable.MaxValue.ToString(), out int maxValue))
                {
                    solver.Add(_context.MkLe(intVar, _context.MkInt(maxValue)));
                }

                // Handle unsigned types
                if (variable.Type.StartsWith("u") || variable.Type.StartsWith("unsigned"))
                {
                    solver.Add(_context.MkGe(intVar, _context.MkInt(0)));
                }
            }
            else if (z3Var is RealExpr realVar)
            {
                // Add min constraint if exists
                if (variable.MinValue != null && double.TryParse(variable.MinValue.ToString(), out double minValue))
                {
                    solver.Add(_context.MkGe(realVar, _context.MkReal(minValue.ToString())));
                }

                // Add max constraint if exists
                if (variable.MaxValue != null && double.TryParse(variable.MaxValue.ToString(), out double maxValue))
                {
                    solver.Add(_context.MkLe(realVar, _context.MkReal(maxValue.ToString())));
                }
            }

            // Handle enum constraints
            if (variable.EnumValues != null && variable.EnumValues.Any())
            {
                BoolExpr[] enumConstraints = variable.EnumValues
                    .Select(enumValue => _context.MkEq(z3Var, ConvertToZ3Value(enumValue, z3Var)))
                    .ToArray();

                solver.Add(_context.MkOr(enumConstraints));
            }
        }

        private Expr ConvertToZ3Value(object value, Expr z3Var)
        {
            if (z3Var is IntExpr)
            {
                if (int.TryParse(value.ToString(), out int intValue))
                {
                    return _context.MkInt(intValue);
                }
            }
            else if (z3Var is RealExpr)
            {
                if (double.TryParse(value.ToString(), out double doubleValue))
                {
                    return _context.MkReal(doubleValue.ToString());
                }
            }
            else if (z3Var is BoolExpr)
            {
                if (bool.TryParse(value.ToString(), out bool boolValue))
                {
                    return boolValue ? _context.MkTrue() : _context.MkFalse();
                }
            }

            // Default fallback
            return _context.MkInt(0);
        }

        private BoolExpr ParseConstraint(string constraint, Dictionary<string, Expr> variables)
        {
            try
            {
                // Simple parser for constraints like "a > b", "x == 5", etc.
                // In a real implementation, you would want a more robust parser

                string[] operators = { "==", "!=", ">=", "<=", ">", "<", "&&", "||" };
                string foundOperator = operators.FirstOrDefault(op => constraint.Contains(op));

                if (foundOperator != null)
                {
                    string[] parts = constraint.Split(new[] { foundOperator }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        string leftPart = parts[0].Trim();
                        string rightPart = parts[1].Trim();

                        Expr leftExpr = ParseExpression(leftPart, variables);
                        Expr rightExpr = ParseExpression(rightPart, variables);

                        if (leftExpr != null && rightExpr != null)
                        {
                            switch (foundOperator)
                            {
                                case "==": return _context.MkEq(leftExpr, rightExpr);
                                case "!=": return _context.MkNot(_context.MkEq(leftExpr, rightExpr));
                                case ">=": return _context.MkGe((ArithExpr)leftExpr, (ArithExpr)rightExpr);
                                case "<=": return _context.MkLe((ArithExpr)leftExpr, (ArithExpr)rightExpr);
                                case ">": return _context.MkGt((ArithExpr)leftExpr, (ArithExpr)rightExpr);
                                case "<": return _context.MkLt((ArithExpr)leftExpr, (ArithExpr)rightExpr);
                                case "&&": return _context.MkAnd((BoolExpr)leftExpr, (BoolExpr)rightExpr);
                                case "||": return _context.MkOr((BoolExpr)leftExpr, (BoolExpr)rightExpr);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing constraint: {constraint}. Error: {ex.Message}");
            }

            return null;
        }

        private Expr ParseExpression(string expression, Dictionary<string, Expr> variables)
        {
            expression = expression.Trim();

            // Check if it's a variable
            if (variables.ContainsKey(expression))
            {
                return variables[expression];
            }

            // Check if it's a numeric literal
            if (int.TryParse(expression, out int intValue))
            {
                return _context.MkInt(intValue);
            }

            if (double.TryParse(expression, out double doubleValue))
            {
                return _context.MkReal(doubleValue.ToString());
            }

            // Check if it's a boolean literal
            if (expression.ToLower() == "true")
            {
                return _context.MkTrue();
            }

            if (expression.ToLower() == "false")
            {
                return _context.MkFalse();
            }

            return null;
        }

        private object ConvertZ3ResultToNativeType(Expr result, string cType)
        {
            if (result is IntNum intNum)
            {
                int value = int.Parse(intNum.ToString());

                // Convert to appropriate C type
                switch (cType.ToLower())
                {
                    case "int8_t":
                    case "char":
                        return (sbyte)value;
                    case "uint8_t":
                    case "unsigned char":
                        return (byte)value;
                    case "int16_t":
                    case "short":
                        return (short)value;
                    case "uint16_t":
                    case "unsigned short":
                        return (ushort)value;
                    default:
                        return value;
                }
            }
            else if (result is RatNum ratNum)
            {
                if (double.TryParse(ratNum.ToString(), out double doubleValue))
                {
                    return cType.ToLower() == "float" ? (float)doubleValue : doubleValue;
                }
            }
            else if (result is BoolExpr boolExpr)
            {
                return boolExpr.ToString() == "true";
            }

            return null;
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
