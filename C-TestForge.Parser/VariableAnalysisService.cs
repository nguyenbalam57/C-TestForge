using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClangSharp;
using ClangSharp.Interop;
using C_TestForge.Core;
using C_TestForge.Models;
using Microsoft.Extensions.Logging;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.Solver;

namespace C_TestForge.Parser
{
    #region VariableAnalysisService Implementation

    /// <summary>
    /// Implementation of the variable analysis service
    /// </summary>
    public class VariableAnalysisService : IVariableAnalysisService
    {
        private readonly ILogger<VariableAnalysisService> _logger;
        private readonly ISourceCodeService _sourceCodeService;

        public VariableAnalysisService(
            ILogger<VariableAnalysisService> logger,
            ISourceCodeService sourceCodeService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
        }

        /// <inheritdoc/>
        public CVariable ExtractVariable(CXCursor cursor)
        {
            try
            {
                if (cursor.Kind != CXCursorKind.CXCursor_VarDecl)
                {
                    return null;
                }

                string variableName = cursor.Spelling.ToString();
                _logger.LogDebug($"Extracting variable: {variableName}");

                // Get variable location
                var location = cursor.Location.GetFileLocation(out var file, out uint line, out uint column, out _);
                string sourceFile = file != null ? Path.GetFileName(file.Name.ToString()) : null;

                // Get variable type
                var type = cursor.Type;
                string typeName = type.Spelling.ToString();

                // Determine variable scope
                VariableScope scope = DetermineScope(cursor);

                // Determine variable type
                VariableType variableType = DetermineVariableType(type);

                // Check attributes
                bool isConst = typeName.Contains("const");
                bool isVolatile = typeName.Contains("volatile");
                bool isReadOnly = false;

                // Extract default value if available
                string defaultValue = null;
                cursor.VisitChildren((child, parent, clientData) =>
                {
                    if (child.Kind == CXCursorKind.CXCursor_IntegerLiteral ||
                        child.Kind == CXCursorKind.CXCursor_FloatingLiteral ||
                        child.Kind == CXCursorKind.CXCursor_StringLiteral ||
                        child.Kind == CXCursorKind.CXCursor_CharacterLiteral ||
                        child.Kind == CXCursorKind.CXCursor_InitListExpr)
                    {
                        defaultValue = GetLiteralValue(child);
                    }

                    return CXChildVisitResult.CXChildVisit_Continue;
                }, IntPtr.Zero);

                // Create variable object
                var variable = new CVariable
                {
                    Name = variableName,
                    TypeName = typeName,
                    VariableType = variableType,
                    Scope = scope,
                    DefaultValue = defaultValue,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    IsConst = isConst,
                    IsVolatile = isVolatile,
                    IsReadOnly = isReadOnly,
                    Constraints = new List<VariableConstraint>()
                };

                // Determine size of the variable
                variable.Size = DetermineVariableSize(type);

                // Add basic type constraints
                AddBasicTypeConstraints(variable);

                return variable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting variable from cursor: {cursor.Spelling}");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<List<VariableConstraint>> AnalyzeVariablesAsync(List<CVariable> variables, List<CFunction> functions, List<CDefinition> definitions)
        {
            try
            {
                _logger.LogInformation($"Analyzing {variables.Count} variables");

                var allConstraints = new List<VariableConstraint>();

                // Analyze each variable for constraints
                foreach (var variable in variables)
                {
                    _logger.LogDebug($"Analyzing variable: {variable.Name}");

                    // Add basic constraints based on type
                    var typeConstraints = GetTypeConstraints(variable);
                    allConstraints.AddRange(typeConstraints);
                    variable.Constraints.AddRange(typeConstraints);

                    // Check for enum constraints
                    if (variable.VariableType == VariableType.Enum)
                    {
                        var enumConstraints = GetEnumConstraints(variable, definitions);
                        allConstraints.AddRange(enumConstraints);
                        variable.Constraints.AddRange(enumConstraints);
                    }

                    // Look for constraints in function bodies
                    foreach (var function in functions)
                    {
                        if (function.UsedVariables.Contains(variable.Name))
                        {
                            _logger.LogDebug($"Variable {variable.Name} is used in function {function.Name}");
                            var functionConstraints = GetFunctionConstraints(variable, function);
                            allConstraints.AddRange(functionConstraints);
                            variable.Constraints.AddRange(functionConstraints);
                        }
                    }
                }

                _logger.LogInformation($"Found {allConstraints.Count} constraints for {variables.Count} variables");

                return allConstraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing variables");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<VariableConstraint>> ExtractConstraintsAsync(CVariable variable, SourceFile sourceFile)
        {
            try
            {
                _logger.LogInformation($"Extracting constraints for variable: {variable.Name}");

                if (variable == null)
                {
                    throw new ArgumentNullException(nameof(variable));
                }

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                var constraints = new List<VariableConstraint>();

                // Add basic type constraints
                constraints.AddRange(GetTypeConstraints(variable));

                // Extract constraints from the source code
                foreach (string line in sourceFile.Lines)
                {
                    // Skip comments
                    if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("/*"))
                    {
                        continue;
                    }

                    // Look for comparison constraints (e.g., if (x > 0))
                    ExtractComparisonConstraints(line, variable, constraints);

                    // Look for range checks (e.g., if (x >= min && x <= max))
                    ExtractRangeConstraints(line, variable, constraints);

                    // Look for assignment constraints (e.g., x = 5)
                    ExtractAssignmentConstraints(line, variable, constraints);
                }

                _logger.LogInformation($"Extracted {constraints.Count} constraints for variable: {variable.Name}");

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints for variable: {variable.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ValueRange> DetermineValueRangeAsync(CVariable variable)
        {
            try
            {
                _logger.LogInformation($"Determining value range for variable: {variable.Name}");

                if (variable == null)
                {
                    throw new ArgumentNullException(nameof(variable));
                }

                var range = new ValueRange
                {
                    VariableName = variable.Name
                };

                // Get constraints from the variable
                var minConstraint = variable.Constraints.FirstOrDefault(c => c.Type == ConstraintType.MinValue);
                var maxConstraint = variable.Constraints.FirstOrDefault(c => c.Type == ConstraintType.MaxValue);
                var rangeConstraint = variable.Constraints.FirstOrDefault(c => c.Type == ConstraintType.Range);
                var enumConstraint = variable.Constraints.FirstOrDefault(c => c.Type == ConstraintType.Enumeration);

                // Set min and max values from constraints
                if (minConstraint != null)
                {
                    range.MinValue = minConstraint.MinValue;
                    range.MustBePositive = int.TryParse(minConstraint.MinValue, out int minValue) && minValue > 0;
                }

                if (maxConstraint != null)
                {
                    range.MaxValue = maxConstraint.MaxValue;
                }

                if (rangeConstraint != null)
                {
                    range.MinValue = rangeConstraint.MinValue;
                    range.MaxValue = rangeConstraint.MaxValue;
                    range.MustBePositive = int.TryParse(rangeConstraint.MinValue, out int minValue) && minValue > 0;
                }

                if (enumConstraint != null)
                {
                    range.AllowedValues = enumConstraint.AllowedValues;
                }

                // Set defaults based on type if no constraints are found
                if (range.MinValue == null && range.MaxValue == null && range.AllowedValues == null)
                {
                    SetDefaultValueRange(variable, range);
                }

                _logger.LogInformation($"Determined value range for variable: {variable.Name}, Min: {range.MinValue}, Max: {range.MaxValue}, Allowed Values: {(range.AllowedValues != null ? range.AllowedValues.Count : 0)}");

                return range;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error determining value range for variable: {variable.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<VariableDataFlow> AnalyzeVariableDataFlowAsync(CVariable variable, CFunction function, SourceFile sourceFile)
        {
            try
            {
                _logger.LogInformation($"Analyzing data flow for variable: {variable.Name} in function: {function.Name}");

                if (variable == null)
                {
                    throw new ArgumentNullException(nameof(variable));
                }

                if (function == null)
                {
                    throw new ArgumentNullException(nameof(function));
                }

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                var dataFlow = new VariableDataFlow
                {
                    VariableName = variable.Name,
                    FunctionName = function.Name,
                    Assignments = new List<VariableAssignment>(),
                    Usages = new List<VariableUsage>()
                };

                // Extract the function body
                var functionBody = ExtractFunctionBody(function, sourceFile);

                if (functionBody.Count == 0)
                {
                    _logger.LogWarning($"Could not extract function body for {function.Name}");
                    return dataFlow;
                }

                // Analyze each line for assignments and usages
                for (int i = 0; i < functionBody.Count; i++)
                {
                    string line = functionBody[i];

                    // Skip comments
                    if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("/*"))
                    {
                        continue;
                    }

                    // Look for assignments
                    if (ContainsAssignment(line, variable.Name))
                    {
                        var assignment = ExtractAssignment(line, variable.Name);
                        if (assignment != null)
                        {
                            assignment.LineNumber = function.LineNumber + i;
                            dataFlow.Assignments.Add(assignment);
                        }
                    }

                    // Look for usages
                    if (ContainsVariableName(line, variable.Name))
                    {
                        var usage = new VariableUsage
                        {
                            LineNumber = function.LineNumber + i,
                            Context = line.Trim(),
                            UsageType = DetermineUsageType(line, variable.Name)
                        };

                        dataFlow.Usages.Add(usage);
                    }
                }

                _logger.LogInformation($"Analyzed data flow for variable: {variable.Name}, found {dataFlow.Assignments.Count} assignments and {dataFlow.Usages.Count} usages");

                return dataFlow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing data flow for variable: {variable.Name} in function: {function.Name}");
                throw;
            }
        }

        private VariableScope DetermineScope(CXCursor cursor)
        {
            // Check if the variable is a parameter
            if (cursor.SemanticParent.Kind == CXCursorKind.CXCursor_FunctionDecl ||
                cursor.SemanticParent.Kind == CXCursorKind.CXCursor_ParmDecl)
            {
                return VariableScope.Parameter;
            }

            // Check if the variable is local
            if (cursor.SemanticParent.Kind == CXCursorKind.CXCursor_CompoundStmt)
            {
                return VariableScope.Local;
            }

            // Check storage class
            bool isStatic = false;
            cursor.VisitChildren((child, parent, clientData) =>
            {
                if (child.Kind == CXCursorKind.CXCursor_StorageClass)
                {
                    string storage = child.Spelling.ToString();
                    if (storage == "static")
                    {
                        isStatic = true;
                    }
                }

                return CXChildVisitResult.CXChildVisit_Continue;
            }, IntPtr.Zero);

            if (isStatic)
            {
                return VariableScope.Static;
            }

            // Check if the variable is in read-only memory
            string typeName = cursor.Type.Spelling.ToString();
            if (typeName.Contains("const") && typeName.Contains("*") == false)
            {
                return VariableScope.Rom;
            }

            // Default to global
            return VariableScope.Global;
        }

        private VariableType DetermineVariableType(CXType type)
        {
            switch (type.Kind)
            {
                case CXTypeKind.CXType_ConstantArray:
                case CXTypeKind.CXType_VariableArray:
                case CXTypeKind.CXType_IncompleteArray:
                case CXTypeKind.CXType_DependentSizedArray:
                    return VariableType.Array;

                case CXTypeKind.CXType_Pointer:
                    return VariableType.Pointer;

                case CXTypeKind.CXType_Record:
                    return type.Spelling.ToString().Contains("struct") ? VariableType.Struct : VariableType.Union;

                case CXTypeKind.CXType_Enum:
                    return VariableType.Enum;

                default:
                    return VariableType.Primitive;
            }
        }

        private string GetLiteralValue(CXCursor cursor)
        {
            switch (cursor.Kind)
            {
                case CXCursorKind.CXCursor_IntegerLiteral:
                    // This is a simplified approach - would need proper evaluation
                    return "0"; // Placeholder

                case CXCursorKind.CXCursor_FloatingLiteral:
                    // This is a simplified approach - would need proper evaluation
                    return "0.0"; // Placeholder

                case CXCursorKind.CXCursor_StringLiteral:
                    // This is a simplified approach - would need proper evaluation
                    return "\"\""; // Placeholder

                case CXCursorKind.CXCursor_CharacterLiteral:
                    // This is a simplified approach - would need proper evaluation
                    return "'\\0'"; // Placeholder

                case CXCursorKind.CXCursor_InitListExpr:
                    // This is a simplified approach - would need proper evaluation
                    return "{}"; // Placeholder

                default:
                    return null;
            }
        }

        private int DetermineVariableSize(CXType type)
        {
            // This is a simplified approach - a real implementation would be more sophisticated
            switch (type.Kind)
            {
                case CXTypeKind.CXType_Bool:
                    return 1;

                case CXTypeKind.CXType_Char_U:
                case CXTypeKind.CXType_Char_S:
                case CXTypeKind.CXType_UChar:
                case CXTypeKind.CXType_SChar:
                    return 1;

                case CXTypeKind.CXType_UShort:
                case CXTypeKind.CXType_Short:
                    return 2;

                case CXTypeKind.CXType_UInt:
                case CXTypeKind.CXType_Int:
                case CXTypeKind.CXType_Float:
                    return 4;

                case CXTypeKind.CXType_ULong:
                case CXTypeKind.CXType_Long:
                case CXTypeKind.CXType_Double:
                    return 8;

                case CXTypeKind.CXType_LongLong:
                case CXTypeKind.CXType_ULongLong:
                case CXTypeKind.CXType_LongDouble:
                    return 8;

                case CXTypeKind.CXType_Pointer:
                    return 4; // Assume 32-bit pointers

                case CXTypeKind.CXType_ConstantArray:
                    long elementCount = type.GetArraySize();
                    var elementType = type.GetArrayElementType();
                    int elementSize = DetermineVariableSize(elementType);
                    return (int)(elementCount * elementSize);

                default:
                    return 0;
            }
        }

        private void AddBasicTypeConstraints(CVariable variable)
        {
            // Add basic constraints based on type
            if (variable.TypeName.Contains("unsigned") || variable.TypeName.Contains("uint"))
            {
                variable.Constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.MinValue,
                    MinValue = "0",
                    Source = $"Type constraint: {variable.TypeName}"
                });
            }
        }

        private List<VariableConstraint> GetTypeConstraints(CVariable variable)
        {
            var constraints = new List<VariableConstraint>();
            string typeName = variable.TypeName.ToLowerInvariant();

            if (typeName.Contains("unsigned") || typeName.Contains("uint"))
            {
                // Unsigned integer constraints
                if (typeName.Contains("char") || typeName.Contains("uint8"))
                {
                    constraints.Add(new VariableConstraint
                    {
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "255",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
                else if (typeName.Contains("short") || typeName.Contains("uint16"))
                {
                    constraints.Add(new VariableConstraint
                    {
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "65535",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
                else if (typeName.Contains("int") || typeName.Contains("uint32"))
                {
                    constraints.Add(new VariableConstraint
                    {
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "4294967295",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
                else if (typeName.Contains("long long") || typeName.Contains("uint64"))
                {
                    constraints.Add(new VariableConstraint
                    {
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "18446744073709551615",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
            }
            else if (typeName.Contains("char") || typeName.Contains("int8"))
            {
                // Signed char constraints
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Range,
                    MinValue = "-128",
                    MaxValue = "127",
                    Source = $"Type constraint: {variable.TypeName}"
                });
            }
            else if (typeName.Contains("short") || typeName.Contains("int16"))
            {
                // Signed short constraints
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Range,
                    MinValue = "-32768",
                    MaxValue = "32767",
                    Source = $"Type constraint: {variable.TypeName}"
                });
            }
            else if (typeName.Contains("int") || typeName.Contains("int32"))
            {
                // Signed int constraints
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Range,
                    MinValue = "-2147483648",
                    MaxValue = "2147483647",
                    Source = $"Type constraint: {variable.TypeName}"
                });
            }
            else if (typeName.Contains("long long") || typeName.Contains("int64"))
            {
                // Signed long long constraints
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Range,
                    MinValue = "-9223372036854775808",
                    MaxValue = "9223372036854775807",
                    Source = $"Type constraint: {variable.TypeName}"
                });
            }
            else if (typeName.Contains("bool"))
            {
                // Boolean constraints
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Enumeration,
                    AllowedValues = new List<string> { "0", "1", "false", "true" },
                    Source = $"Type constraint: {variable.TypeName}"
                });
            }

            return constraints;
        }

        private List<VariableConstraint> GetEnumConstraints(CVariable variable, List<CDefinition> definitions)
        {
            var constraints = new List<VariableConstraint>();

            // Find enum values that match the variable type
            var enumValues = definitions
                .Where(d => d.DefinitionType == DefinitionType.EnumValue)
                .ToList();

            if (enumValues.Any())
            {
                var allowedValues = enumValues
                    .Select(e => e.Name)
                    .ToList();

                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Enumeration,
                    AllowedValues = allowedValues,
                    Source = $"Enum constraint: {variable.TypeName}"
                });
            }

            return constraints;
        }

        private List<VariableConstraint> GetFunctionConstraints(CVariable variable, CFunction function)
        {
            var constraints = new List<VariableConstraint>();

            // Only add custom constraints for function parameters
            if (function.Parameters.Any(p => p.Name == variable.Name))
            {
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Custom,
                    Expression = $"Used in function {function.Name}",
                    Source = $"Function parameter in {function.Name}"
                });
            }

            return constraints;
        }

        private void ExtractComparisonConstraints(string line, CVariable variable, List<VariableConstraint> constraints)
        {
            // Look for comparison operators
            string variableName = variable.Name;

            // Check for greater than
            var greaterThanMatch = Regex.Match(line, $@"{Regex.Escape(variableName)}\s*>\s*([0-9.]+)");
            if (greaterThanMatch.Success)
            {
                string value = greaterThanMatch.Groups[1].Value;
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.MinValue,
                    MinValue = value,
                    Source = $"Comparison: {variableName} > {value}"
                });
            }

            // Check for greater than or equal
            var greaterThanEqualMatch = Regex.Match(line, $@"{Regex.Escape(variableName)}\s*>=\s*([0-9.]+)");
            if (greaterThanEqualMatch.Success)
            {
                string value = greaterThanEqualMatch.Groups[1].Value;
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.MinValue,
                    MinValue = value,
                    Source = $"Comparison: {variableName} >= {value}"
                });
            }

            // Check for less than
            var lessThanMatch = Regex.Match(line, $@"{Regex.Escape(variableName)}\s*<\s*([0-9.]+)");
            if (lessThanMatch.Success)
            {
                string value = lessThanMatch.Groups[1].Value;
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.MaxValue,
                    MaxValue = value,
                    Source = $"Comparison: {variableName} < {value}"
                });
            }

            // Check for less than or equal
            var lessThanEqualMatch = Regex.Match(line, $@"{Regex.Escape(variableName)}\s*<=\s*([0-9.]+)");
            if (lessThanEqualMatch.Success)
            {
                string value = lessThanEqualMatch.Groups[1].Value;
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.MaxValue,
                    MaxValue = value,
                    Source = $"Comparison: {variableName} <= {value}"
                });
            }

            // Check for equality
            var equalityMatch = Regex.Match(line, $@"{Regex.Escape(variableName)}\s*==\s*([0-9.]+|true|false)");
            if (equalityMatch.Success)
            {
                string value = equalityMatch.Groups[1].Value;
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Enumeration,
                    AllowedValues = new List<string> { value },
                    Source = $"Equality: {variableName} == {value}"
                });
            }
        }

        private void ExtractRangeConstraints(string line, CVariable variable, List<VariableConstraint> constraints)
        {
            string variableName = variable.Name;

            // Check for range checks (e.g., if (x >= min && x <= max))
            var rangeMatch = Regex.Match(line, $@"{Regex.Escape(variableName)}\s*>=\s*([0-9.]+).*&&.*{Regex.Escape(variableName)}\s*<=\s*([0-9.]+)");
            if (rangeMatch.Success)
            {
                string minValue = rangeMatch.Groups[1].Value;
                string maxValue = rangeMatch.Groups[2].Value;
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Range,
                    MinValue = minValue,
                    MaxValue = maxValue,
                    Source = $"Range check: {variableName} >= {minValue} && {variableName} <= {maxValue}"
                });
            }

            // Check for reversed range checks (e.g., if (min <= x && x <= max))
            var reversedRangeMatch = Regex.Match(line, $@"([0-9.]+)\s*<=\s*{Regex.Escape(variableName)}.*&&.*{Regex.Escape(variableName)}\s*<=\s*([0-9.]+)");
            if (reversedRangeMatch.Success)
            {
                string minValue = reversedRangeMatch.Groups[1].Value;
                string maxValue = reversedRangeMatch.Groups[2].Value;
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Range,
                    MinValue = minValue,
                    MaxValue = maxValue,
                    Source = $"Range check: {minValue} <= {variableName} && {variableName} <= {maxValue}"
                });
            }
        }

        private void ExtractAssignmentConstraints(string line, CVariable variable, List<VariableConstraint> constraints)
        {
            string variableName = variable.Name;

            // Check for direct assignments (e.g., x = 5)
            var assignmentMatch = Regex.Match(line, $@"\b{Regex.Escape(variableName)}\s*=\s*([0-9.]+|true|false)");
            if (assignmentMatch.Success)
            {
                string value = assignmentMatch.Groups[1].Value;
                constraints.Add(new VariableConstraint
                {
                    Type = ConstraintType.Custom,
                    Expression = $"{variableName} = {value}",
                    Source = $"Assignment: {variableName} = {value}"
                });
            }
        }

        private void SetDefaultValueRange(CVariable variable, ValueRange range)
        {
            string typeName = variable.TypeName.ToLowerInvariant();

            if (typeName.Contains("unsigned") || typeName.Contains("uint"))
            {
                range.MinValue = "0";
                range.MustBePositive = true;

                if (typeName.Contains("char") || typeName.Contains("uint8"))
                {
                    range.MaxValue = "255";
                }
                else if (typeName.Contains("short") || typeName.Contains("uint16"))
                {
                    range.MaxValue = "65535";
                }
                else if (typeName.Contains("int") || typeName.Contains("uint32"))
                {
                    range.MaxValue = "4294967295";
                }
                else if (typeName.Contains("long long") || typeName.Contains("uint64"))
                {
                    range.MaxValue = "18446744073709551615";
                }
            }
            else if (typeName.Contains("char") || typeName.Contains("int8"))
            {
                range.MinValue = "-128";
                range.MaxValue = "127";
            }
            else if (typeName.Contains("short") || typeName.Contains("int16"))
            {
                range.MinValue = "-32768";
                range.MaxValue = "32767";
            }
            else if (typeName.Contains("int") || typeName.Contains("int32"))
            {
                range.MinValue = "-2147483648";
                range.MaxValue = "2147483647";
            }
            else if (typeName.Contains("long long") || typeName.Contains("int64"))
            {
                range.MinValue = "-9223372036854775808";
                range.MaxValue = "9223372036854775807";
            }
            else if (typeName.Contains("bool"))
            {
                range.AllowedValues = new List<string> { "0", "1", "false", "true" };
            }
        }

        private List<string> ExtractFunctionBody(CFunction function, SourceFile sourceFile)
        {
            var bodyLines = new List<string>();

            // Find the function in the source file
            bool foundFunction = false;
            int braceCount = 0;

            for (int i = function.LineNumber - 1; i < sourceFile.Lines.Count; i++)
            {
                string line = sourceFile.Lines[i];

                if (!foundFunction)
                {
                    // Look for the function declaration
                    if (line.Contains(function.Name) && (line.Contains('(') || line.Contains(')')))
                    {
                        foundFunction = true;
                    }

                    continue;
                }

                // Count braces to find the function body
                for (int j = 0; j < line.Length; j++)
                {
                    if (line[j] == '{')
                    {
                        braceCount++;

                        if (braceCount == 1)
                        {
                            // Start of function body
                            bodyLines.Add(line.Substring(j));
                            break;
                        }
                    }
                    else if (line[j] == '}')
                    {
                        braceCount--;

                        if (braceCount == 0)
                        {
                            // End of function body
                            bodyLines.Add(line.Substring(0, j + 1));
                            return bodyLines;
                        }
                    }
                }

                if (braceCount > 0 && (!bodyLines.Contains(line)))
                {
                    bodyLines.Add(line);
                }
            }

            return bodyLines;
        }

        private bool ContainsAssignment(string line, string variableName)
        {
            // Check for direct assignments (e.g., x = 5)
            var assignmentRegex = new Regex($@"\b{Regex.Escape(variableName)}\s*=([^=])");
            return assignmentRegex.IsMatch(line);
        }

        private VariableAssignment ExtractAssignment(string line, string variableName)
        {
            // Extract the assignment expression
            var assignmentRegex = new Regex($@"\b{Regex.Escape(variableName)}\s*=\s*(.+?)(;|$)");
            var match = assignmentRegex.Match(line);

            if (match.Success)
            {
                string expression = match.Groups[1].Value.Trim();

                // Extract variables used in the expression
                var usedVariables = new List<string>();
                var variableRegex = new Regex(@"\b[a-zA-Z_][a-zA-Z0-9_]*\b");
                var variableMatches = variableRegex.Matches(expression);

                foreach (Match variableMatch in variableMatches)
                {
                    string usedVarName = variableMatch.Value;

                    // Skip keywords
                    if (usedVarName != "if" && usedVarName != "else" && usedVarName != "for" &&
                        usedVarName != "while" && usedVarName != "return" && usedVarName != "break" &&
                        usedVarName != "continue" && usedVarName != "switch" && usedVarName != "case" &&
                        usedVarName != "default" && usedVarName != "goto" && usedVarName != "sizeof" &&
                        usedVarName != "true" && usedVarName != "false" && usedVarName != "NULL")
                    {
                        usedVariables.Add(usedVarName);
                    }
                }

                return new VariableAssignment
                {
                    Expression = expression,
                    UsedVariables = usedVariables
                };
            }

            return null;
        }

        private bool ContainsVariableName(string line, string variableName)
        {
            // Check if the line contains the variable name as a whole word
            var regex = new Regex($@"\b{Regex.Escape(variableName)}\b");
            return regex.IsMatch(line);
        }

        private string DetermineUsageType(string line, string variableName)
        {
            // Check if the variable is being written to
            if (ContainsAssignment(line, variableName))
            {
                return "Write";
            }

            // Check if the variable is being incremented/decremented
            var incDecRegex = new Regex($@"\b{Regex.Escape(variableName)}\+\+|\+\+{Regex.Escape(variableName)}|\b{Regex.Escape(variableName)}--|--(Regex.Escape(variableName))");
            if (incDecRegex.IsMatch(line))
            {
                return "ReadWrite";
            }

            // Check for compound assignments
            var compoundAssignRegex = new Regex($@"\b{Regex.Escape(variableName)}\s*(\+=|-=|\*=|/=|%=|&=|\|=|\^=|<<=|>>=)");
            if (compoundAssignRegex.IsMatch(line))
            {
                return "ReadWrite";
            }

            // Default to read
            return "Read";
        }
    }

    #endregion
}
