using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Models.CodeAnalysis;
using C_TestForge.Models.Core;
using C_TestForge.Models.Core.Enumerations;
using C_TestForge.Models.Core.SupportingClasses;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using ClangSharp;
using ClangSharp.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
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
        private readonly ITypeManager _typeManager;

        /// <summary>
        /// Constructor for VariableAnalysisService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="sourceCodeService">Source code service for reading source files</param>
        public VariableAnalysisService(
            ILogger<VariableAnalysisService> logger,
            ISourceCodeService sourceCodeService,
            ITypeManager typeManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
            _typeManager = typeManager ?? throw new ArgumentNullException(nameof(typeManager));
        }

        /// <inheritdoc/>
        public unsafe void ExtractVariable(CXCursor cursor, ParseResult result)
        {
            try
            {
                if (cursor.Kind != CXCursorKind.CXCursor_VarDecl &&
                    cursor.Kind != CXCursorKind.CXCursor_ParmDecl &&
                    cursor.Kind != CXCursorKind.CXCursor_FieldDecl)
                {
                    return;
                }

                string variableName = cursor.Spelling.ToString();
                _logger.LogDebug($"Extracting variable: {variableName}");

                // Get variable location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceFile = file != null ? Path.GetFileName(file.Name.ToString()) : string.Empty;

                // Get variable type
                var type = cursor.Type;
                string typeName = type.Spelling.ToString();
                string originalTypeName = GetOriginalTypeName(type, cursor);

                // Determine variable scope and storage class
                VariableScope scope = DetermineScope(cursor);
                StorageClass storageClass = DetermineStorageClass(cursor);

                // Determine variable type category
                VariableType variableType = DetermineVariableType(type);

                // Check type qualifiers
                bool isConst = IsTypeConst(type);
                bool isVolatile = IsTypeVolatile(type);
                bool isRestrict = IsTypeRestrict(type);

                // Analyze pointer information
                var pointerInfo = AnalyzePointerType(type);
                bool isPointer = pointerInfo.IsPointer;
                int pointerDepth = pointerInfo.Depth;

                // Analyze array information
                var arrayInfo = AnalyzeArrayType(type);
                bool isArray = arrayInfo.IsArray;
                List<int> arrayDimensions = arrayInfo.Dimensions;

                // Extract default value if available
                string defaultValue = ExtractDefaultValue(cursor);

                // Determine if it's a custom type
                bool isCustomType = IsCustomUserDefinedType(type, result);

                // Create variable object
                var variable = new CVariable
                {
                    Name = variableName,
                    TypeName = CleanTypeName(typeName),
                    OriginalTypeName = originalTypeName,
                    IsCustomType = isCustomType,
                    VariableType = variableType,
                    Scope = scope,
                    StorageClass = storageClass,
                    DefaultValue = defaultValue,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    IsConst = isConst,
                    IsVolatile = isVolatile,
                    IsPointer = isPointer,
                    PointerDepth = pointerDepth,
                    IsArray = isArray,
                    ArrayDimensions = arrayDimensions,
                    Size = DetermineSize(type)
                };

                // Extract variable attributes
                variable.Attributes = ExtractVariableAttributes(cursor);

                // Extract constraints if any
                variable.Constraints = ExtractVariableConstraints(cursor, variable);

                // Add to result
                result.Variables.Add(variable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting variable: {ex.Message}");
                return;
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
                        if (function.UsedVariables != null && function.UsedVariables.Contains(variable.Name))
                        {
                            variable.UsedByFunctions.Add(function.Name);
                        }
                    }

                    // Look for enum constraints
                    if (variable.VariableType == VariableType.Enum || variable.TypeName.Contains("enum"))
                    {
                        var enumConstraints = ExtractEnumConstraints(variable, definitions);
                        constraints.AddRange(enumConstraints);
                    }

                    // Extract constraints from array types
                    if (variable.IsArray)
                    {
                        var arrayConstraints = ExtractArrayConstraints(variable);
                        constraints.AddRange(arrayConstraints);
                    }
                }

                // Analyze function bodies for additional constraints
                foreach (var function in functions)
                {
                    if (function.UsedVariables != null)
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
            try
            {
                var storageClass = cursor.StorageClass;
                var semanticParent = cursor.SemanticParent;

                // Check storage class first
                switch (storageClass)
                {
                    case CX_StorageClass.CX_SC_Static:
                        return VariableScope.Static;
                    case CX_StorageClass.CX_SC_Extern:
                        return VariableScope.Extern;
                    case CX_StorageClass.CX_SC_Register:
                        return VariableScope.Register;
                    case CX_StorageClass.CX_SC_Auto:
                        return VariableScope.Auto;
                }

                // Check semantic context
                if (semanticParent.Kind == CXCursorKind.CXCursor_TranslationUnit)
                {
                    return VariableScope.Global;
                }
                else if (semanticParent.Kind == CXCursorKind.CXCursor_FunctionDecl)
                {
                    return cursor.Kind == CXCursorKind.CXCursor_ParmDecl ?
                           VariableScope.Parameter : VariableScope.Local;
                }
                else if (semanticParent.Kind == CXCursorKind.CXCursor_CompoundStmt)
                {
                    return VariableScope.Local;
                }

                return VariableScope.Local; // Default fallback
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not determine scope for variable, defaulting to Local: {ex.Message}");
                return VariableScope.Local;
            }
        }

        /// <summary>
        /// Determines the variable type
        /// </summary>
        /// <param name="type">Clang type</param>
        /// <returns>Variable type</returns>
        private VariableType DetermineVariableType(CXType type)
        {
            try
            {
                var kind = type.kind;
                return kind switch
                {
                    CXTypeKind.CXType_Void => VariableType.Void,
                    CXTypeKind.CXType_Bool => VariableType.Bool,
                    CXTypeKind.CXType_Char_U or CXTypeKind.CXType_Char_S => VariableType.Char,
                    CXTypeKind.CXType_UChar => VariableType.UnsignedChar,
                    CXTypeKind.CXType_SChar => VariableType.SignedChar,
                    CXTypeKind.CXType_UShort => VariableType.UnsignedShort,
                    CXTypeKind.CXType_Short => VariableType.Short,
                    CXTypeKind.CXType_UInt => VariableType.UnsignedInt,
                    CXTypeKind.CXType_Int => VariableType.Int,
                    CXTypeKind.CXType_ULong => VariableType.UnsignedLong,
                    CXTypeKind.CXType_Long => VariableType.Long,
                    CXTypeKind.CXType_ULongLong => VariableType.UnsignedLongLong,
                    CXTypeKind.CXType_LongLong => VariableType.LongLong,
                    CXTypeKind.CXType_Float => VariableType.Float,
                    CXTypeKind.CXType_Double => VariableType.Double,
                    CXTypeKind.CXType_LongDouble => VariableType.LongDouble,
                    CXTypeKind.CXType_Pointer => VariableType.Pointer,
                    CXTypeKind.CXType_Record => VariableType.Struct,
                    CXTypeKind.CXType_Enum => VariableType.Enum,
                    CXTypeKind.CXType_Typedef => DetermineVariableType(type.CanonicalType),
                    CXTypeKind.CXType_ConstantArray or CXTypeKind.CXType_IncompleteArray => VariableType.Array,
                    CXTypeKind.CXType_FunctionProto or CXTypeKind.CXType_FunctionNoProto => VariableType.Function,
                    _ => VariableType.Unknown
                };
            }
            catch
            {
                return VariableType.Unknown;
            }
        }

        /// <summary>
        /// Gets the literal value from a Clang cursor
        /// </summary>
        /// <param name="cursor">Literal cursor</param>
        /// <returns>String representation of the literal value</returns>
        private unsafe string GetLiteralValue(CXCursor cursor)
        {
            try
            {
                // Try to get the spelling directly from the cursor
                var spelling = cursor.Spelling.ToString();
                if (!string.IsNullOrEmpty(spelling))
                {
                    return spelling;
                }

                // Alternative: Try to get the display name
                var displayName = cursor.DisplayName.ToString();
                if (!string.IsNullOrEmpty(displayName))
                {
                    return displayName;
                }

                // As a fallback, try to extract from source location
                var range = cursor.Extent;
                var startLocation = range.Start;
                var endLocation = range.End;

                // Get file and positions
                CXFile file;
                uint startLine, startColumn, startOffset;
                uint endLine, endColumn, endOffset;

                startLocation.GetFileLocation(out file, out startLine, out startColumn, out startOffset);
                endLocation.GetFileLocation(out file, out endLine, out endColumn, out endOffset);

                // If we have a valid range, we could read from source if available
                // For now, return null as we can't safely extract without the tokenization API
                return null;
            }
            catch
            {
                return null;
            }
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
                long sizeInBytes = type.SizeOf;
                return sizeInBytes > 0 && sizeInBytes <= int.MaxValue ? (int)sizeInBytes : 0;
            }
            catch
            {
                // Fallback to typical sizes if Clang can't determine
                var variableType = DetermineVariableType(type);
                return variableType.GetTypicalSize();
            }
        }

        /// <summary>
        /// Extracts constraints based on the variable's type
        /// </summary>
        /// <param name="variable">Variable to analyze</param>
        /// <returns>List of type-based constraints</returns>
        /// <summary>
        /// Extracts type-based constraints for a variable
        /// </summary>
        private List<VariableConstraint> ExtractTypeConstraints(CVariable variable)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                // Try to get constraint from TypeManager first
                var typeConstraint = _typeManager?.GetConstraintForType(variable.TypeName, variable.Name);
                if (typeConstraint != null)
                {
                    constraints.Add(typeConstraint);
                    return constraints;
                }

                // Generate constraints based on variable type
                switch (variable.VariableType)
                {
                    case VariableType.Bool:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Enumeration,
                            AllowedValues = new List<string> { "0", "1", "false", "true" },
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.SignedChar:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "-128",
                            MaxValue = "127",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.UnsignedChar:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "0",
                            MaxValue = "255",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.Short:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "-32768",
                            MaxValue = "32767",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.UnsignedShort:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "0",
                            MaxValue = "65535",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.Int:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "-2147483648",
                            MaxValue = "2147483647",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.UnsignedInt:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "0",
                            MaxValue = "4294967295",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.Long:
                        // Platform dependent, but typically same as int on 32-bit, long long on 64-bit
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = IntPtr.Size == 8 ? "-9223372036854775808" : "-2147483648",
                            MaxValue = IntPtr.Size == 8 ? "9223372036854775807" : "2147483647",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.UnsignedLong:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "0",
                            MaxValue = IntPtr.Size == 8 ? "18446744073709551615" : "4294967295",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.LongLong:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "-9223372036854775808",
                            MaxValue = "9223372036854775807",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.UnsignedLongLong:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "0",
                            MaxValue = "18446744073709551615",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.Float:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "-3.4028235e38",
                            MaxValue = "3.4028235e38",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;

                    case VariableType.Double:
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = "-1.7976931348623157e308",
                            MaxValue = "1.7976931348623157e308",
                            Source = $"Type constraint: {variable.TypeName}"
                        });
                        break;
                }

                // Add pointer constraints
                if (variable.IsPointer)
                {
                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variable.Name,
                        Type = ConstraintType.Custom,
                        Expression = "ptr != NULL",
                        Source = "Pointer constraint"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting type constraints for variable {variable.Name}: {ex.Message}");
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

                string enumTypeName = ExtractEnumTypeName(variable.TypeName);
                if (!string.IsNullOrEmpty(enumTypeName))
                {
                    foreach (var definition in definitions)
                    {
                        if (definition.DefinitionType == DefinitionType.EnumValue)
                        {
                            // Check if this definition has a Context property, otherwise assume it's related
                            var hasContext = definition.GetType().GetProperty("Context") != null;
                            if (!hasContext ||
                                (hasContext && definition.GetType().GetProperty("Context")?.GetValue(definition)?.ToString()?.Contains(enumTypeName) == true))
                            {
                                enumValues.Add(definition.Name);
                            }
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting enum constraints for variable {variable.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extracts enum type name from complex type string
        /// </summary>
        private string ExtractEnumTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;

            var match = Regex.Match(typeName, @"enum\s+(\w+)");
            return match.Success ? match.Groups[1].Value : null;
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

                // Look for switch statements
                var switchCases = ExtractSwitchCases(function.Body, variable.Name);
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting usage constraints for variable {variable.Name} in function {function.Name}: {ex.Message}");
            }

            return constraints;
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
                if (sourceFile?.Lines == null || sourceFile.Lines.Count == 0)
                {
                    return constraints;
                }

                // Look for comments around the variable declaration
                int startLine = Math.Max(0, variable.LineNumber - 5);
                int endLine = Math.Min(sourceFile.Lines.Count - 1, variable.LineNumber + 5);

                for (int i = startLine; i <= endLine; i++)
                {
                    string line = sourceFile.Lines[i];

                    // Look for range comments
                    var rangeMatch = Regex.Match(line, @"(?://|/\*)\s*(?:Range|Valid range|Value range):\s*(-?\d+(?:\.\d+)?)\s*(?:to|-)\s*(-?\d+(?:\.\d+)?)", RegexOptions.IgnoreCase);
                    if (rangeMatch.Success)
                    {
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Range,
                            MinValue = rangeMatch.Groups[1].Value,
                            MaxValue = rangeMatch.Groups[2].Value,
                            Source = $"Comment at line {i + 1}"
                        });
                    }

                    // Look for enumeration comments
                    var enumMatch = Regex.Match(line, @"(?://|/\*)\s*(?:Valid values|Allowed|Values):\s*([\w\d\s,]+)", RegexOptions.IgnoreCase);
                    if (enumMatch.Success)
                    {
                        var values = enumMatch.Groups[1].Value
                            .Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
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
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints from comments for variable {variable.Name}: {ex.Message}");
            }

            return constraints;
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
                if (sourceFile?.Content == null)
                {
                    return constraints;
                }

                string content = sourceFile.Content;

                // Extract various constraint patterns
                constraints.AddRange(ExtractRangeChecks(content, variable.Name));
                constraints.AddRange(ExtractEqualityChecks(content, variable.Name));
                constraints.AddRange(ExtractAssignmentConstraints(content, variable.Name));
                constraints.AddRange(ExtractArrayAccessConstraints(content, variable.Name));

                // Extract switch case constraints
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

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints from patterns for variable {variable.Name}: {ex.Message}");
            }
            return constraints;
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
                var escapedVarName = Regex.Escape(variableName);

                // Range checks: if (var >= min && var <= max)
                var rangePattern = new Regex($@"if\s*\(\s*{escapedVarName}\s*(>=|>)\s*([^&|]+)\s*&&\s*{escapedVarName}\s*(<|<=)\s*([^&|)]+)\s*\)");
                var matches = rangePattern.Matches(code);

                foreach (Match match in matches)
                {
                    string minOp = match.Groups[1].Value;
                    string minValue = match.Groups[2].Value.Trim();
                    string maxOp = match.Groups[3].Value;
                    string maxValue = match.Groups[4].Value.Trim();

                    // Adjust for exclusive bounds
                    if (minOp == ">" && double.TryParse(minValue, out double minDouble))
                    {
                        minValue = (minDouble + 1).ToString();
                    }
                    if (maxOp == "<" && double.TryParse(maxValue, out double maxDouble))
                    {
                        maxValue = (maxDouble - 1).ToString();
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

                // Individual bounds
                var lowerBoundPattern = new Regex($@"if\s*\(\s*{escapedVarName}\s*(>=|>)\s*([^&|)]+)\s*\)");
                var upperBoundPattern = new Regex($@"if\s*\(\s*{escapedVarName}\s*(<|<=)\s*([^&|)]+)\s*\)");

                foreach (Match match in lowerBoundPattern.Matches(code))
                {
                    string op = match.Groups[1].Value;
                    string value = match.Groups[2].Value.Trim();

                    if (op == ">" && double.TryParse(value, out double valueDouble))
                    {
                        value = (valueDouble + 1).ToString();
                    }

                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.MinValue,
                        MinValue = value,
                        Source = "Code lower bound check"
                    });
                }

                foreach (Match match in upperBoundPattern.Matches(code))
                {
                    string op = match.Groups[1].Value;
                    string value = match.Groups[2].Value.Trim();

                    if (op == "<" && double.TryParse(value, out double valueDouble))
                    {
                        value = (valueDouble - 1).ToString();
                    }

                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.MaxValue,
                        MaxValue = value,
                        Source = "Code upper bound check"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting range checks for variable {variableName}: {ex.Message}");
            }

            return constraints;
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
                var escapedVarName = Regex.Escape(variableName);
                var equalityPattern = new Regex($@"if\s*\(\s*{escapedVarName}\s*==\s*([^&|)]+)\s*\)");
                var matches = equalityPattern.Matches(code);

                if (matches.Count > 0)
                {
                    var allowedValues = matches
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value.Trim())
                        .Distinct()
                        .ToList();

                    constraints.Add(new VariableConstraint
                    {
                        VariableName = variableName,
                        Type = ConstraintType.Enumeration,
                        AllowedValues = allowedValues,
                        Source = "Code equality checks"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting equality checks for variable {variableName}: {ex.Message}");
            }

            return constraints;
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
                var escapedVarName = Regex.Escape(variableName);
                var switchPattern = new Regex($@"switch\s*\(\s*{escapedVarName}\s*\)\s*{{([^}}]+)}}", RegexOptions.Singleline);
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting switch cases for variable {variableName}: {ex.Message}");
            }

            return switchCases;
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
                var escapedVarName = Regex.Escape(variableName);
                var arrayAccessPattern = new Regex($@"[a-zA-Z_]\w*\[\s*{escapedVarName}\s*\]");
                var matches = arrayAccessPattern.Matches(code);

                if (matches.Count > 0)
                {
                    // Look for array size declarations
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
                                if (int.TryParse(sizeMatch.Groups[2].Value, out int size))
                                {
                                    constraints.Add(new VariableConstraint
                                    {
                                        VariableName = variableName,
                                        Type = ConstraintType.Range,
                                        MinValue = "0",
                                        MaxValue = (size - 1).ToString(),
                                        Source = $"Array access for {arrayName}[{size}]"
                                    });
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting array access constraints for variable {variableName}: {ex.Message}");
            }

            return constraints;
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
                var escapedVarName = Regex.Escape(variableName);
                var assignmentPattern = new Regex($@"{escapedVarName}\s*=\s*([^;]+);");
                var matches = assignmentPattern.Matches(code);

                if (matches.Count > 0)
                {
                    var assignedValues = matches
                        .Cast<Match>()
                        .Select(m => m.Groups[1].Value.Trim())
                        .Where(v => Regex.IsMatch(v, @"^[0-9]+$|^[0-9]*\.[0-9]+$|^'.'$")) // Simple literals only
                        .Distinct()
                        .ToList();

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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting assignment constraints for variable {variableName}: {ex.Message}");
            }

            return constraints;
        }

        private string GetOriginalTypeName(CXType type, CXCursor cursor)
        {
            try
            {
                // Get the canonical type to resolve typedefs
                var canonicalType = type.CanonicalType;
                if (canonicalType.Spelling.ToString() != type.Spelling.ToString())
                {
                    return type.Spelling.ToString(); // This is the typedef name
                }
                return canonicalType.Spelling.ToString();
            }
            catch
            {
                return type.Spelling.ToString();
            }
        }

        private StorageClass DetermineStorageClass(CXCursor cursor)
        {
            try
            {
                var storageClass = cursor.StorageClass;
                return storageClass switch
                {
                    CX_StorageClass.CX_SC_Auto => StorageClass.Auto,
                    CX_StorageClass.CX_SC_Register => StorageClass.Register,
                    CX_StorageClass.CX_SC_Static => StorageClass.Static,
                    CX_StorageClass.CX_SC_Extern => StorageClass.Extern,
                    _ => StorageClass.Auto
                };
            }
            catch
            {
                return StorageClass.Auto;
            }
        }

        private bool IsTypeConst(CXType type)
        {
            try
            {
                return type.IsConstQualified;
            }
            catch
            {
                return false;
            }
        }

        private bool IsTypeVolatile(CXType type)
        {
            try
            {
                return type.IsVolatileQualified;
            }
            catch
            {
                return false;
            }
        }

        private bool IsTypeRestrict(CXType type)
        {
            try
            {
                return type.IsRestrictQualified;
            }
            catch
            {
                return false;
            }
        }

        private (bool IsPointer, int Depth) AnalyzePointerType(CXType type)
        {
            int depth = 0;
            var currentType = type;

            try
            {
                while (currentType.kind == CXTypeKind.CXType_Pointer)
                {
                    depth++;
                    currentType = currentType.PointeeType;
                }

                return (depth > 0, depth);
            }
            catch
            {
                return (false, 0);
            }
        }

        private (bool IsArray, List<int> Dimensions) AnalyzeArrayType(CXType type)
        {
            var dimensions = new List<int>();
            var currentType = type;

            try
            {
                while (currentType.kind == CXTypeKind.CXType_ConstantArray ||
                       currentType.kind == CXTypeKind.CXType_IncompleteArray)
                {
                    if (currentType.kind == CXTypeKind.CXType_ConstantArray)
                    {
                        dimensions.Add((int)currentType.NumElements);
                    }
                    else
                    {
                        dimensions.Add(-1); // Incomplete array
                    }
                    currentType = currentType.ArrayElementType;
                }

                return (dimensions.Count > 0, dimensions);
            }
            catch
            {
                return (false, new List<int>());
            }
        }

        private unsafe string ExtractDefaultValue(CXCursor cursor)
        {
            string defaultValue = null;

            try
            {
                cursor.VisitChildren((child, parent, clientData) =>
                {
                    switch (child.Kind)
                    {
                        case CXCursorKind.CXCursor_IntegerLiteral:
                        case CXCursorKind.CXCursor_FloatingLiteral:
                        case CXCursorKind.CXCursor_StringLiteral:
                        case CXCursorKind.CXCursor_CharacterLiteral:
                            defaultValue = GetLiteralValue(child);
                            return CXChildVisitResult.CXChildVisit_Break;
                        case CXCursorKind.CXCursor_InitListExpr:
                            defaultValue = ExtractInitListValue(child);
                            return CXChildVisitResult.CXChildVisit_Break;
                        default:
                            return CXChildVisitResult.CXChildVisit_Continue;
                    }
                }, default(CXClientData));
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract default value: {ex.Message}");
            }

            return defaultValue;
        }

        private unsafe string ExtractInitListValue(CXCursor cursor)
        {
            var values = new List<string>();

            try
            {
                cursor.VisitChildren((child, parent, clientData) =>
                {
                    var value = GetLiteralValue(child);
                    if (!string.IsNullOrEmpty(value))
                    {
                        values.Add(value);
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));

                return values.Count > 0 ? $"{{{string.Join(", ", values)}}}" : null;
            }
            catch
            {
                return null;
            }
        }

        private bool IsCustomUserDefinedType(CXType type, ParseResult result)
        {
            try
            {
                var canonicalType = type.CanonicalType;
                string typeName = canonicalType.Spelling.ToString();

                // Check if it's a struct, union, or enum defined in this parse result
                return result.Structures.Any(s => s.Name == typeName) ||
                       result.Unions.Any(u => u.Name == typeName) ||
                       result.Enumerations.Any(e => e.Name == typeName) ||
                       result.Typedefs.Any(t => t.Name == typeName);
            }
            catch
            {
                return false;
            }
        }

        private string CleanTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return string.Empty;

            // Remove extra spaces and normalize
            return System.Text.RegularExpressions.Regex.Replace(typeName.Trim(), @"\s+", " ");
        }

        private unsafe List<CVariableAttribute> ExtractVariableAttributes(CXCursor cursor)
        {
            var attributes = new List<CVariableAttribute>();

            try
            {
                cursor.VisitChildren((child, parent, clientData) =>
                {
                    if (child.Kind == CXCursorKind.CXCursor_AnnotateAttr ||
                        child.Kind == CXCursorKind.CXCursor_PackedAttr ||
                        child.Kind == CXCursorKind.CXCursor_AlignedAttr)
                    {
                        var attr = new CVariableAttribute
                        {
                            Name = child.Spelling.ToString()
                        };

                        // Extract attribute parameters if any
                        child.VisitChildren((attrChild, attrParent, attrClientData) =>
                        {
                            attr.Parameters.Add(attrChild.Spelling.ToString());
                            return CXChildVisitResult.CXChildVisit_Continue;
                        }, default(CXClientData));

                        attributes.Add(attr);
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract attributes: {ex.Message}");
            }

            return attributes;
        }

        private List<VariableConstraint> ExtractVariableConstraints(CXCursor cursor, CVariable variable)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                // Add basic type constraints
                constraints.AddRange(ExtractTypeConstraints(variable));

                // Extract constraints from attributes
                foreach (var attr in variable.Attributes)
                {
                    var attrConstraints = ExtractConstraintsFromAttribute(attr, variable);
                    constraints.AddRange(attrConstraints);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract constraints: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extracts constraints from variable attributes
        /// </summary>
        private List<VariableConstraint> ExtractConstraintsFromAttribute(CVariableAttribute attribute, CVariable variable)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                switch (attribute.Name.ToLower())
                {
                    case "packed":
                        // Packed attribute doesn't add value constraints but affects memory layout
                        break;

                    case "aligned":
                        if (attribute.Parameters.Count > 0 && int.TryParse(attribute.Parameters[0], out int alignment))
                        {
                            constraints.Add(new VariableConstraint
                            {
                                VariableName = variable.Name,
                                Type = ConstraintType.Custom,
                                Expression = $"alignment == {alignment}",
                                Source = $"Aligned attribute: {alignment}"
                            });
                        }
                        break;

                    case "deprecated":
                        constraints.Add(new VariableConstraint
                        {
                            VariableName = variable.Name,
                            Type = ConstraintType.Custom,
                            Expression = "deprecated",
                            Source = "Deprecated attribute"
                        });
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting constraints from attribute {attribute.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extracts array-specific constraints
        /// </summary>
        private List<VariableConstraint> ExtractArrayConstraints(CVariable variable)
        {
            var constraints = new List<VariableConstraint>();

            try
            {
                if (variable.ArrayDimensions != null && variable.ArrayDimensions.Count > 0)
                {
                    for (int i = 0; i < variable.ArrayDimensions.Count; i++)
                    {
                        int dimension = variable.ArrayDimensions[i];
                        if (dimension > 0)
                        {
                            constraints.Add(new VariableConstraint
                            {
                                VariableName = $"{variable.Name}[{i}]",
                                Type = ConstraintType.Range,
                                MinValue = "0",
                                MaxValue = (dimension - 1).ToString(),
                                Source = $"Array dimension {i + 1}: size {dimension}"
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting array constraints for variable {variable.Name}: {ex.Message}");
            }

            return constraints;
        }

    }
}