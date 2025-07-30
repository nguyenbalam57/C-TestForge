using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;
using ClangSharp;
using ClangSharp.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Implementation of the variable analysis service
    /// </summary>
    public class VariableAnalysisService : IVariableAnalysisService
    {
        private readonly ILogger<VariableAnalysisService> _logger;
        private readonly ISourceCodeService _sourceCodeService;

        /// <summary>
        /// Constructor for VariableAnalysisService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="sourceCodeService">Source code service for reading source files</param>
        public VariableAnalysisService(
            ILogger<VariableAnalysisService> logger,
            ISourceCodeService sourceCodeService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
        }

        /// <inheritdoc/>
        public unsafe CVariable ExtractVariable(CXCursor cursor)
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
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceFile = file != null ? System.IO.Path.GetFileName(file.Name.ToString()) : null;

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
                }, default(CXClientData));

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
                    IsReadOnly = isReadOnly
                };

                // Determine size if possible
                variable.Size = DetermineSize(type);

                return variable;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting variable: {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<List<VariableConstraint>> AnalyzeVariablesAsync(
            List<CVariable> variables,
            List<CFunction> functions,
            List<CDefinition> definitions)
        {
            _logger.LogInformation($"Analyzing {variables.Count} variables");

            var constraints = new List<VariableConstraint>();

            try
            {
                // Analyze each variable
                foreach (var variable in variables)
                {
                    _logger.LogDebug($"Analyzing variable: {variable.Name}");

                    // Add constraints based on variable type
                    var typeConstraints = ExtractTypeConstraints(variable);
                    constraints.AddRange(typeConstraints);

                    // Find which functions use this variable
                    variable.UsedByFunctions.Clear();
                    foreach (var function in functions)
                    {
                        if (function.UsedVariables.Contains(variable.Name))
                        {
                            variable.UsedByFunctions.Add(function.Name);
                        }
                    }

                    // Look for enum constraints
                    if (variable.TypeName.Contains("enum"))
                    {
                        var enumConstraints = ExtractEnumConstraints(variable, definitions);
                        constraints.AddRange(enumConstraints);
                    }
                }

                // Analyze function bodies for additional constraints
                foreach (var function in functions)
                {
                    foreach (var variable in variables)
                    {
                        if (function.UsedVariables.Contains(variable.Name))
                        {
                            var usageConstraints = await ExtractUsageConstraintsAsync(variable, function);
                            constraints.AddRange(usageConstraints);
                        }
                    }
                }

                _logger.LogInformation($"Extracted {constraints.Count} variable constraints");

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing variables: {ex.Message}");
                return constraints;
            }
        }

        /// <inheritdoc/>
        public async Task<List<VariableConstraint>> ExtractConstraintsAsync(CVariable variable, SourceFile sourceFile)
        {
            _logger.LogInformation($"Extracting constraints for variable {variable.Name}");

            var constraints = new List<VariableConstraint>();

            try
            {
                // Add basic type constraints
                constraints.AddRange(ExtractTypeConstraints(variable));

                // Extract constraints from source code comments
                var commentConstraints = await ExtractConstraintsFromCommentsAsync(variable, sourceFile);
                constraints.AddRange(commentConstraints);

                // Extract constraints from code patterns
                var patternConstraints = await ExtractConstraintsFromPatternsAsync(variable, sourceFile);
                constraints.AddRange(patternConstraints);

                _logger.LogInformation($"Extracted {constraints.Count} constraints for variable {variable.Name}");

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints for variable {variable.Name}: {ex.Message}");
                return constraints;
            }
        }

        /// <summary>
        /// Determines the scope of a variable
        /// </summary>
        /// <param name="cursor">Variable cursor</param>
        /// <returns>Variable scope</returns>
        private unsafe VariableScope DetermineScope(CXCursor cursor)
        {
            // Check if the variable is a parameter
            if (cursor.Kind == CXCursorKind.CXCursor_ParmDecl)
            {
                return VariableScope.Parameter;
            }

            // Check if the variable is local
            var parent = clang.getCursorLexicalParent(cursor);
            if (parent.Kind == CXCursorKind.CXCursor_FunctionDecl ||
                parent.Kind == CXCursorKind.CXCursor_CXXMethod)
            {
                return VariableScope.Local;
            }

            // Check if the variable is static
            bool isStatic = false;
            cursor.VisitChildren((child, parent, clientData) =>
            {

                // Check spelling for static keyword
                string childText = child.Spelling.ToString();
                if (childText == "static")
                {
                    isStatic = true;
                }

                return CXChildVisitResult.CXChildVisit_Continue;
            }, default(CXClientData));

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

        /// <summary>
        /// Determines the variable type
        /// </summary>
        /// <param name="type">Clang type</param>
        /// <returns>Variable type</returns>
        private VariableType DetermineVariableType(CXType type)
        {
            switch (type.kind)
            {
                case CXTypeKind.CXType_ConstantArray:
                case CXTypeKind.CXType_VariableArray:
                case CXTypeKind.CXType_IncompleteArray:
                case CXTypeKind.CXType_DependentSizedArray:
                    return VariableType.Array;

                case CXTypeKind.CXType_Pointer:
                    return VariableType.Pointer;

                case CXTypeKind.CXType_Record:
                    return type.Spelling.ToString().Contains("struct") ?
                        VariableType.Struct : VariableType.Union;

                case CXTypeKind.CXType_Enum:
                    return VariableType.Enum;

                case CXTypeKind.CXType_Float:
                case CXTypeKind.CXType_Double:
                case CXTypeKind.CXType_LongDouble:
                    return VariableType.Float;

                case CXTypeKind.CXType_Bool:
                    return VariableType.Bool;

                case CXTypeKind.CXType_Char_S:
                case CXTypeKind.CXType_Char_U:
                case CXTypeKind.CXType_SChar:
                case CXTypeKind.CXType_UChar:
                    return VariableType.Char;

                default:
                    // Handle integer types
                    if (type.kind >= CXTypeKind.CXType_Short && type.kind <= CXTypeKind.CXType_ULongLong)
                    {
                        return VariableType.Integer;
                    }

                    return VariableType.Unknown;
            }
        }

        /// <summary>
        /// Gets the literal value from a Clang cursor
        /// </summary>
        /// <param name="cursor">Literal cursor</param>
        /// <returns>String representation of the literal value</returns>
        private string GetLiteralValue(CXCursor cursor)
        {
            var extent = cursor.Extent;
            return extent.ToString();
        }

        /// <summary>
        /// Determines the size of a variable type in bytes
        /// </summary>
        /// <param name="type">Clang type</param>
        /// <returns>Size in bytes or 0 if unknown</returns>
        private int DetermineSize(CXType type)
        {
            try
            {
                switch (type.kind)
                {
                    case CXTypeKind.CXType_Bool:
                        return 1;

                    case CXTypeKind.CXType_Char_S:
                    case CXTypeKind.CXType_Char_U:
                    case CXTypeKind.CXType_SChar:
                    case CXTypeKind.CXType_UChar:
                        return 1;

                    case CXTypeKind.CXType_Short:
                    case CXTypeKind.CXType_UShort:
                        return 2;

                    case CXTypeKind.CXType_Int:
                    case CXTypeKind.CXType_UInt:
                        return 4;

                    case CXTypeKind.CXType_Long:
                    case CXTypeKind.CXType_ULong:
                        return 4; // May be 8 on 64-bit platforms

                    case CXTypeKind.CXType_LongLong:
                    case CXTypeKind.CXType_ULongLong:
                        return 8;

                    case CXTypeKind.CXType_Float:
                        return 4;

                    case CXTypeKind.CXType_Double:
                        return 8;

                    case CXTypeKind.CXType_LongDouble:
                        return 16; // May vary by platform

                    case CXTypeKind.CXType_Pointer:
                        return 4; // May be 8 on 64-bit platforms

                    case CXTypeKind.CXType_ConstantArray:
                        unsafe
                        {
                            long arraySize = clang.getArraySize(type);
                            var elementType = clang.getArrayElementType(type);
                            int elementSize = DetermineSize(elementType);
                            return (int)(arraySize * elementSize);
                        }

                    default:
                        return 0;
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Extracts constraints based on the variable's type
        /// </summary>
        /// <param name="variable">Variable to analyze</param>
        /// <returns>List of type-based constraints</returns>
        private List<VariableConstraint> ExtractTypeConstraints(CVariable variable)
        {
            var constraints = new List<VariableConstraint>();

            string typeName = variable.TypeName.ToLower();

            // Add range constraints based on variable type
            if (typeName.Contains("bool"))
            {
                // Boolean constraints
                constraints.Add(new VariableConstraint
                {
                    VariableName = variable.Name,
                    Type = ConstraintType.Enumeration,
                    AllowedValues = new List<string> { "0", "1", "false", "true" },
                    Source = $"Type constraint: {variable.TypeName}"
                });
            }
            else if (typeName.Contains("char") && !typeName.Contains("*"))
            {
                // Character constraints
                if (typeName.Contains("unsigned") || typeName.Contains("uchar"))
                {
                    // Unsigned char constraints
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "255",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
                else
                {
                    // Signed char constraints
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Range,
                        MinValue = "-128",
                        MaxValue = "127",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
            }
            else if (typeName.Contains("short") || typeName.Contains("int16"))
            {
                if (typeName.Contains("unsigned") || typeName.Contains("ushort"))
                {
                    // Unsigned short constraints
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "65535",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
                else
                {
                    // Signed short constraints
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Range,
                        MinValue = "-32768",
                        MaxValue = "32767",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
            }
            else if (typeName.Contains("int") || typeName.Contains("int32"))
            {
                if (typeName.Contains("unsigned") || typeName.Contains("uint"))
                {
                    // Unsigned int constraints
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "4294967295",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
                else
                {
                    // Signed int constraints
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Range,
                        MinValue = "-2147483648",
                        MaxValue = "2147483647",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
            }
            else if (typeName.Contains("long long") || typeName.Contains("int64"))
            {
                if (typeName.Contains("unsigned") || typeName.Contains("uint64"))
                {
                    // Unsigned long long constraints
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Range,
                        MinValue = "0",
                        MaxValue = "18446744073709551615",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
                else
                {
                    // Signed long long constraints
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Range,
                        MinValue = "-9223372036854775808",
                        MaxValue = "9223372036854775807",
                        Source = $"Type constraint: {variable.TypeName}"
                    });
                }
            }
            else if (typeName.Contains("float"))
            {
                // Float constraints (approximate)
                constraints.Add(new VariableConstraint
                {
                    VariableName = variable.Name,
                    Type = ConstraintType.Range,
                    MinValue = "-3.4e38",
                    MaxValue = "3.4e38",
                    Source = $"Type constraint: {variable.TypeName}"
                });
            }
            else if (typeName.Contains("double"))
            {
                // Double constraints (approximate)
                constraints.Add(new VariableConstraint
                {
                    VariableName = variable.Name,
                    Type = ConstraintType.Range,
                    MinValue = "-1.7e308",
                    MaxValue = "1.7e308",
                    Source = $"Type constraint: {variable.TypeName}"
                });
            }

            // Check for array size constraints
            if (variable.VariableType == VariableType.Array)
            {
                // Extract array size from type name
                var match = Regex.Match(variable.TypeName, @"\[(\d+)\]");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int arraySize))
                {
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.ArraySize,
                        Value = arraySize.ToString(),
                        Source = $"Array size: {arraySize}"
                    });
                }
            }

            return constraints;
        }

        /// <summary>
        /// Extracts enum constraints for a variable
        /// </summary>
        /// <param name="variable">Variable to analyze</param>
        /// <param name="definitions">Available definitions</param>
        /// <returns>List of enum-based constraints</returns>
        private List<VariableConstraint> ExtractEnumConstraints(CVariable variable, List<CDefinition> definitions)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                // Look for enum definitions that match the variable type
                var enumValues = new List<string>();
                foreach (var definition in definitions)
                {
                    if (definition.DefinitionType == DefinitionType.EnumValue)
                    {
                        enumValues.Add(definition.Name);
                    }
                }

                if (enumValues.Count > 0)
                {
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Enumeration,
                        AllowedValues = enumValues,
                        Source = $"Enum values for {variable.TypeName}"
                    });
                }

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting enum constraints for variable {variable.Name}: {ex.Message}");
                return constraints;
            }
        }

        /// <summary>
        /// Extracts constraints from usage patterns in a function
        /// </summary>
        /// <param name="variable">Variable to analyze</param>
        /// <param name="function">Function to analyze</param>
        /// <returns>List of usage-based constraints</returns>
        private async Task<List<VariableConstraint>> ExtractUsageConstraintsAsync(CVariable variable, CFunction function)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                if (string.IsNullOrEmpty(function.Body))
                {
                    return constraints;
                }

                // Look for range checks
                var rangeChecks = ExtractRangeChecks(function.Body, variable.Name);
                constraints.AddRange(rangeChecks);

                // Look for equality checks
                var equalityChecks = ExtractEqualityChecks(function.Body, variable.Name);
                constraints.AddRange(equalityChecks);

                // Look for value assignments
                var assignmentConstraints = ExtractAssignmentConstraints(function.Body, variable.Name);
                constraints.AddRange(assignmentConstraints);

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting usage constraints for variable {variable.Name} in function {function.Name}: {ex.Message}");
                return constraints;
            }
        }

        /// <summary>
        /// Extracts constraints from source code comments
        /// </summary>
        /// <param name="variable">Variable to analyze</param>
        /// <param name="sourceFile">Source file</param>
        /// <returns>List of comment-based constraints</returns>
        private async Task<List<VariableConstraint>> ExtractConstraintsFromCommentsAsync(CVariable variable, SourceFile sourceFile)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                if (sourceFile == null || sourceFile.Lines == null || sourceFile.Lines.Count == 0)
                {
                    return constraints;
                }

                // Look for comments around the variable declaration
                int startLine = Math.Max(0, variable.LineNumber - 5);
                int endLine = Math.Min(sourceFile.Lines.Count - 1, variable.LineNumber + 5);

                for (int i = startLine; i <= endLine; i++)
                {
                    string line = sourceFile.Lines[i];

                    // Look for range comments: e.g., "// Range: 0-100" or "/* Valid values: 1, 2, 3 */"
                    var rangeMatch = Regex.Match(line, @"(?://|/\*)\s*(?:Range|Valid range|Value range):\s*(-?\d+(?:\.\d+)?)\s*(?:to|-)\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                    if (rangeMatch.Success)
                    {
                        string minValue = rangeMatch.Groups[1].Value;
                        string maxValue = rangeMatch.Groups[2].Value;

                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = minValue,
                            MaxValue = maxValue,
                            Source = $"Comment at line {i + 1}"
                        });
                    }

                    // Look for enumeration comments: e.g., "// Valid values: 1, 2, 3" or "/* Allowed: A, B, C */"
                    var enumMatch = Regex.Match(line, @"(?://|/\*)\s*(?:Valid values|Allowed|Allowed values|Valid|Values):\s*([\w\d\s,]+)", RegexOptions.IgnoreCase);
                    if (enumMatch.Success)
                    {
                        string valuesStr = enumMatch.Groups[1].Value;
                        var values = valuesStr.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                              .Select(v => v.Trim())
                                              .Where(v => !string.IsNullOrWhiteSpace(v))
                                              .ToList();

                        if (values.Count > 0)
                        {
                            constraints.Add(new VariableConstraint
                            {
                                VariableName = variable.Name,
                                Type = ConstraintType.Enumeration,
                                AllowedValues = values,
                                Source = $"Comment at line {i + 1}"
                            });
                        }
                    }

                    // Look for min/max comments: e.g., "// Min: 0" or "/* Maximum: 100 */"
                    var minMatch = Regex.Match(line, @"(?://|/\*)\s*(?:Min|Minimum|Lower bound):\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                    var maxMatch = Regex.Match(line, @"(?://|/\*)\s*(?:Max|Maximum|Upper bound):\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);

                    if (minMatch.Success || maxMatch.Success)
                    {
                        string minValue = minMatch.Success ? minMatch.Groups[1].Value : null;
                        string maxValue = maxMatch.Success ? maxMatch.Groups[1].Value : null;

                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = minValue,
                            MaxValue = maxValue,
                            Source = $"Comment at line {i + 1}"
                        });
                    }
                }

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints from comments for variable {variable.Name}: {ex.Message}");
                return constraints;
            }
        }

        /// <summary>
        /// Extracts constraints from code patterns
        /// </summary>
        /// <param name="variable">Variable to analyze</param>
        /// <param name="sourceFile">Source file</param>
        /// <returns>List of pattern-based constraints</returns>
        private async Task<List<VariableConstraint>> ExtractConstraintsFromPatternsAsync(CVariable variable, SourceFile sourceFile)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                if (sourceFile == null || string.IsNullOrEmpty(sourceFile.Content))
                {
                    return constraints;
                }

                string content = sourceFile.Content;

                // Look for range checks: e.g., "if (variable > min && variable < max)"
                var rangeChecks = ExtractRangeChecks(content, variable.Name);
                constraints.AddRange(rangeChecks);

                // Look for equality checks: e.g., "if (variable == value1 || variable == value2)"
                var equalityChecks = ExtractEqualityChecks(content, variable.Name);
                constraints.AddRange(equalityChecks);

                // Look for switch statements: e.g., "switch (variable) { case value1: ... }"
                var switchCases = ExtractSwitchCases(content, variable.Name);
                if (switchCases.Count > 0)
                {
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Enumeration,
                        AllowedValues = switchCases,
                        Source = "Switch statement cases"
                    });
                }

                // Look for array accesses: e.g., "array[variable]"
                var arrayAccesses = ExtractArrayAccessConstraints(content, variable.Name);
                constraints.AddRange(arrayAccesses);

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints from patterns for variable {variable.Name}: {ex.Message}");
                return constraints;
            }
        }

        /// <summary>
        /// Extracts range checks from code
        /// </summary>
        /// <param name="code">Code to analyze</param>
        /// <param name="variableName">Variable name</param>
        /// <returns>List of range-based constraints</returns>
        private List<VariableConstraint> ExtractRangeChecks(string code, string variableName)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                // Look for range checks of the form: if (variable >= min && variable <= max)
                var rangePattern = new Regex($@"if\s*\(\s*{Regex.Escape(variableName)}\s*(>=|>)\s*([^&|]+)\s*&&\s*{Regex.Escape(variableName)}\s*(<|<=)\s*([^&|]+)\s*\)");
                var matches = rangePattern.Matches(code);

                foreach (Match match in matches)
                {
                    string minOp = match.Groups[1].Value;
                    string minValue = match.Groups[2].Value.Trim();
                    string maxOp = match.Groups[3].Value;
                    string maxValue = match.Groups[4].Value.Trim();

                    // Adjust bounds based on operators
                    if (minOp == ">")
                    {
                        // Exclusive lower bound, add 1 to minimum
                        if (double.TryParse(minValue, out double minDouble))
                        {
                            minValue = (minDouble + 1).ToString();
                        }
                    }

                    if (maxOp == "<")
                    {
                        // Exclusive upper bound, subtract 1 from maximum
                        if (double.TryParse(maxValue, out double maxDouble))
                        {
                            maxValue = (maxDouble - 1).ToString();
                        }
                    }

                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Range,
                        MinValue = minValue,
                        MaxValue = maxValue,
                        Source = "Code range check"
                    });
                }

                // Look for individual bounds checks: if (variable >= min) or if (variable <= max)
                var lowerBoundPattern = new Regex($@"if\s*\(\s*{Regex.Escape(variableName)}\s*(>=|>)\s*([^&|]+)\s*\)");
                var upperBoundPattern = new Regex($@"if\s*\(\s*{Regex.Escape(variableName)}\s*(<|<=)\s*([^&|]+)\s*\)");

                var lowerMatches = lowerBoundPattern.Matches(code);
                var upperMatches = upperBoundPattern.Matches(code);

                foreach (Match match in lowerMatches)
                {
                    string op = match.Groups[1].Value;
                    string value = match.Groups[2].Value.Trim();

                    // Adjust bound based on operator
                    if (op == ">")
                    {
                        // Exclusive lower bound
                        if (double.TryParse(value, out double valueDouble))
                        {
                            value = (valueDouble + 1).ToString();
                        }
                    }

                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Range,
                        MinValue = value,
                        MaxValue = null,
                        Source = "Code lower bound check"
                    });
                }

                foreach (Match match in upperMatches)
                {
                    string op = match.Groups[1].Value;
                    string value = match.Groups[2].Value.Trim();

                    // Adjust bound based on operator
                    if (op == "<")
                    {
                        // Exclusive upper bound
                        if (double.TryParse(value, out double valueDouble))
                        {
                            value = (valueDouble - 1).ToString();
                        }
                    }

                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Range,
                        MinValue = null,
                        MaxValue = value,
                        Source = "Code upper bound check"
                    });
                }

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting range checks for variable {variableName}: {ex.Message}");
                return constraints;
            }
        }

        /// <summary>
        /// Extracts equality checks from code
        /// </summary>
        /// <param name="code">Code to analyze</param>
        /// <param name="variableName">Variable name</param>
        /// <returns>List of equality-based constraints</returns>
        private List<VariableConstraint> ExtractEqualityChecks(string code, string variableName)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                // Look for equality checks of the form: if (variable == value)
                var equalityPattern = new Regex($@"if\s*\(\s*{Regex.Escape(variableName)}\s*==\s*([^&|]+)\s*\)");
                var matches = equalityPattern.Matches(code);

                if (matches.Count > 0)
                {
                    var allowedValues = new List<string>();

                    foreach (Match match in matches)
                    {
                        string value = match.Groups[1].Value.Trim();
                        if (!allowedValues.Contains(value))
                        {
                            allowedValues.Add(value);
                        }
                    }

                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Enumeration,
                        AllowedValues = allowedValues,
                        Source = "Code equality checks"
                    });
                }

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting equality checks for variable {variableName}: {ex.Message}");
                return constraints;
            }
        }

        /// <summary>
        /// Extracts switch cases from code
        /// </summary>
        /// <param name="code">Code to analyze</param>
        /// <param name="variableName">Variable name</param>
        /// <returns>List of switch case values</returns>
        private List<string> ExtractSwitchCases(string code, string variableName)
        {
            var switchCases = new List<string>();

            try
            {
                // Look for switch statements of the form: switch (variable) { case value1: ... case value2: ... }
                var switchPattern = new Regex($@"switch\s*\(\s*{Regex.Escape(variableName)}\s*\)\s*{{([^}}]+)}}");
                var matches = switchPattern.Matches(code);

                foreach (Match match in matches)
                {
                    string switchBody = match.Groups[1].Value;
                    var casePattern = new Regex(@"case\s+([^:]+):");
                    var caseMatches = casePattern.Matches(switchBody);

                    foreach (Match caseMatch in caseMatches)
                    {
                        string caseValue = caseMatch.Groups[1].Value.Trim();
                        if (!switchCases.Contains(caseValue))
                        {
                            switchCases.Add(caseValue);
                        }
                    }
                }

                return switchCases;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting switch cases for variable {variableName}: {ex.Message}");
                return switchCases;
            }
        }

        /// <summary>
        /// Extracts array access constraints from code
        /// </summary>
        /// <param name="code">Code to analyze</param>
        /// <param name="variableName">Variable name</param>
        /// <returns>List of array access constraints</returns>
        private List<VariableConstraint> ExtractArrayAccessConstraints(string code, string variableName)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                // Look for array accesses of the form: array[variable]
                var arrayAccessPattern = new Regex($@"[a-zA-Z_]\w*\[\s*{Regex.Escape(variableName)}\s*\]");
                var matches = arrayAccessPattern.Matches(code);

                if (matches.Count > 0)
                {
                    // Look for array sizes
                    var arraySizePattern = new Regex(@"([a-zA-Z_]\w*)\[(\d+)\]");
                    var sizeMatches = arraySizePattern.Matches(code);

                    foreach (Match match in matches)
                    {
                        string arrayExpression = match.Value;
                        string arrayName = arrayExpression.Substring(0, arrayExpression.IndexOf('['));

                        // Find the size of this array
                        foreach (Match sizeMatch in sizeMatches)
                        {
                            if (sizeMatch.Groups[1].Value == arrayName)
                            {
                                string sizeStr = sizeMatch.Groups[2].Value;
                                if (int.TryParse(sizeStr, out int size))
                                {
                                    // Array indices are typically 0 to size-1
                                    constraints.Add(new VariableConstraint
                                    {
                                        VariableName = variableName,
                                        Type = ConstraintType.Range,
                                        MinValue = "0",
                                        MaxValue = (size - 1).ToString(),
                                        Source = $"Array access for {arrayName}"
                                    });
                                    break;
                                }
                            }
                        }
                    }
                }

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting array access constraints for variable {variableName}: {ex.Message}");
                return constraints;
            }
        }

        /// <summary>
        /// Extracts constraints from assignment statements
        /// </summary>
        /// <param name="code">Code to analyze</param>
        /// <param name="variableName">Variable name</param>
        /// <returns>List of assignment-based constraints</returns>
        private List<VariableConstraint> ExtractAssignmentConstraints(string code, string variableName)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                // Look for assignments of the form: variable = value;
                var assignmentPattern = new Regex($@"{Regex.Escape(variableName)}\s*=\s*([^;]+);");
                var matches = assignmentPattern.Matches(code);

                if (matches.Count > 0)
                {
                    var assignedValues = new List<string>();

                    foreach (Match match in matches)
                    {
                        string value = match.Groups[1].Value.Trim();

                        // Only add simple literal values to the list
                        if (Regex.IsMatch(value, @"^[0-9]+$") || // Integer
                            Regex.IsMatch(value, @"^[0-9]*\.[0-9]+$") || // Float
                            Regex.IsMatch(value, @"^'.'$")) // Character
                        {
                            if (!assignedValues.Contains(value))
                            {
                                assignedValues.Add(value);
                            }
                        }
                    }

                    if (assignedValues.Count > 0)
                    {
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variableName,
                            Type = ConstraintType.Enumeration,
                            AllowedValues = assignedValues,
                            Source = "Code assignments"
                        });
                    }
                }

                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting assignment constraints for variable {variableName}: {ex.Message}");
                return constraints;
            }
        }
    }
}