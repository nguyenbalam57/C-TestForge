using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Models.Core;
using C_TestForge.Models.Core.Enumerations;
using C_TestForge.Models.Core.SupportingClasses;
using C_TestForge.Models.Core.SupportingClasses.Enums;
using C_TestForge.Models.Parse;
using ClangSharp.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.Parser.Analysis
{
    /// <summary>
    /// Implementation of enum analysis and extraction service
    /// </summary>
    public class EnumAnalysisExtractor : IEnumAnalysisExtractor
    {
        private readonly ILogger<EnumAnalysisExtractor> _logger;
        private readonly ITypeManager _typeManager;

        private static readonly Regex IntegerValueRegex = new Regex(@"^[+-]?\d+$", RegexOptions.Compiled);
        private static readonly Regex HexValueRegex = new Regex(@"^0[xX][0-9a-fA-F]+$", RegexOptions.Compiled);
        private static readonly Regex OctalValueRegex = new Regex(@"^0[0-7]+$", RegexOptions.Compiled);
        private static readonly Regex ExpressionRegex = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*(\s*[+\-*/]\s*\d+)*$", RegexOptions.Compiled);

        private static readonly HashSet<string> ReservedEnumNames = new HashSet<string>
        {
            "auto", "break", "case", "char", "const", "continue", "default", "do",
            "double", "else", "enum", "extern", "float", "for", "goto", "if",
            "int", "long", "register", "return", "short", "signed", "sizeof", "static",
            "struct", "switch", "typedef", "union", "unsigned", "void", "volatile", "while"
        };

        /// <summary>
        /// Constructor for EnumAnalysisExtractor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="typeManager">Type manager for type resolution</param>
        public EnumAnalysisExtractor(
            ILogger<EnumAnalysisExtractor> logger,
            ITypeManager typeManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _typeManager = typeManager ?? throw new ArgumentNullException(nameof(typeManager));
        }

        /// <inheritdoc/>
        public unsafe void ExtractEnum(CXCursor cursor, ParseResult result)
        {
            try
            {
                if (cursor.Kind != CXCursorKind.CXCursor_EnumDecl)
                {
                    return;
                }

                string enumName = cursor.Spelling.ToString();
                _logger.LogDebug($"Extracting enum: {enumName}");

                // Get enum location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceFile = file != null ? System.IO.Path.GetFileName(file.Name.ToString()) : string.Empty;

                // Check if this is an anonymous enum
                bool isAnonymous = string.IsNullOrEmpty(enumName);
                if (isAnonymous)
                {
                    enumName = $"<anonymous_enum_{line}_{column}>";
                }

                // Extract underlying type
                string underlyingType = ExtractUnderlyingType(cursor);

                // Extract documentation
                string documentation = ExtractEnumDocumentation(cursor);

                // Create enum object
                var enumEntity = new CEnum
                {
                    Name = enumName,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    UnderlyingType = underlyingType,
                    IsAnonymous = isAnonymous,
                    Documentation = documentation,
                    Size = GetEnumSize(underlyingType)
                };

                // Extract enum values
                ExtractEnumValues(cursor, enumEntity);

                // Validate enum
                var validationErrors = ValidateEnum(enumEntity);
                result.ParseErrors.AddRange(validationErrors);

                // Analyze enum patterns
                AnalyzeEnumPatterns(enumEntity, result);

                // Add to result
                result.Enumerations.Add(enumEntity);

                // Update statistics
                result.Statistics.SymbolsResolved++;

                _logger.LogDebug($"Successfully extracted enum: {enumName} with {enumEntity.Values.Count} values");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting enum: {ex.Message}");
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                result.ParseErrors.Add(new ParseError
                {
                    Message = $"Failed to extract enum: {ex.Message}",
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    Severity = ErrorSeverity.Warning,
                });
            }
        }

        /// <inheritdoc/>
        public async Task<List<EnumDependency>> AnalyzeEnumDependenciesAsync(
            List<CEnum> enums,
            ParseResult result)
        {
            _logger.LogInformation($"Analyzing dependencies for {enums.Count} enums");

            var dependencies = new List<EnumDependency>();

            try
            {
                foreach (var enumEntity in enums)
                {
                    _logger.LogDebug($"Analyzing dependencies for enum: {enumEntity.Name}");

                    // Check for enum value dependencies
                    foreach (var value in enumEntity.Values)
                    {
                        if (!string.IsNullOrEmpty(value.ValueExpression))
                        {
                            var deps = FindEnumValueDependencies(value.ValueExpression, enums);
                            foreach (var dep in deps)
                            {
                                dependencies.Add(new EnumDependency
                                {
                                    EnumName = enumEntity.Name,
                                    DependsOn = dep,
                                    DependencyType = EnumDependencyType.ValueDependency,
                                    LineNumber = value.LineNumber
                                });
                            }
                        }
                    }

                    // Check for circular dependencies
                    var circularDeps = await DetectCircularDependenciesAsync(enumEntity, enums);
                    foreach (var circularDep in circularDeps)
                    {
                        result.ParseWarnings.Add(new ParseWarning
                        {
                            Message = $"Potential circular dependency in enum values: {enumEntity.Name} <-> {circularDep}",
                            LineNumber = enumEntity.LineNumber,
                            Category = "Enum Dependencies",
                            Code = "ENUM_CIRCULAR"
                        });
                    }
                }

                _logger.LogInformation($"Found {dependencies.Count} enum dependencies");
                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing enum dependencies: {ex.Message}");
                return dependencies;
            }
        }

        /// <inheritdoc/>
        public List<ParseError> ValidateEnum(CEnum enumEntity)
        {
            var errors = new List<ParseError>();

            try
            {
                if (enumEntity == null)
                {
                    errors.Add(CreateValidationError("Null enum entity", 0, 0));
                    return errors;
                }

                _logger.LogDebug($"Validating enum: {enumEntity.Name}");

                // Validate enum name
                ValidateEnumName(enumEntity, errors);

                // Validate enum values
                ValidateEnumValues(enumEntity, errors);

                // Check for duplicate values
                CheckDuplicateValues(enumEntity, errors);

                // Check for value gaps
                CheckValueGaps(enumEntity, errors);

                // Validate naming conventions
                ValidateNamingConventions(enumEntity, errors);

                // Check for potential overflow
                CheckValueOverflow(enumEntity, errors);

                _logger.LogDebug($"Validation completed for enum: {enumEntity.Name}, found {errors.Count} issues");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating enum {enumEntity?.Name}: {ex.Message}");
                errors.Add(CreateValidationError($"Validation error: {ex.Message}",
                    enumEntity?.LineNumber ?? 0, enumEntity?.ColumnNumber ?? 0));
            }

            return errors;
        }

        /// <inheritdoc/>
        public async Task<List<EnumConstraint>> ExtractEnumConstraintsAsync(
            CEnum enumEntity,
            string sourceCode)
        {
            _logger.LogInformation($"Extracting constraints for enum {enumEntity.Name}");

            var constraints = new List<EnumConstraint>();

            try
            {
                // Extract value range constraints
                var rangeConstraints = ExtractValueRangeConstraints(enumEntity);
                constraints.AddRange(rangeConstraints);

                // Extract usage patterns
                var usageConstraints = await ExtractUsagePatternsAsync(enumEntity, sourceCode);
                constraints.AddRange(usageConstraints);

                // Extract sequential patterns
                var sequentialConstraints = ExtractSequentialPatterns(enumEntity);
                constraints.AddRange(sequentialConstraints);

                // Extract bit flag patterns
                var bitFlagConstraints = ExtractBitFlagPatterns(enumEntity);
                constraints.AddRange(bitFlagConstraints);

                _logger.LogInformation($"Extracted {constraints.Count} constraints for enum {enumEntity.Name}");
                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints for enum {enumEntity.Name}: {ex.Message}");
                return constraints;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Extract underlying type of enum
        /// </summary>
        private unsafe string ExtractUnderlyingType(CXCursor cursor)
        {
            try
            {
                var type = cursor.EnumDecl_IntegerType;
                if (type.kind != CXTypeKind.CXType_Invalid)
                {
                    return type.Spelling.ToString();
                }
                return "int"; // Default
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract enum underlying type: {ex.Message}");
                return "int";
            }
        }

        /// <summary>
        /// Extract documentation from enum cursor
        /// </summary>
        private unsafe string ExtractEnumDocumentation(CXCursor cursor)
        {
            try
            {
                var comment = cursor.ParsedComment;
                if (comment.Kind == CXCommentKind.CXComment_Null)
                    return string.Empty;

                return comment.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract enum documentation: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Get enum size based on underlying type
        /// </summary>
        private int GetEnumSize(string underlyingType)
        {
            return underlyingType.ToLower() switch
            {
                "char" => 1,
                "short" => 2,
                "int" => 4,
                "long" => 8,
                "long long" => 8,
                _ => 4 // Default int size
            };
        }

        /// <summary>
        /// Extract enum values from cursor
        /// </summary>
        private unsafe void ExtractEnumValues(CXCursor cursor, CEnum enumEntity)
        {
            try
            {
                cursor.VisitChildren((child, parent, clientData) =>
                {
                    if (child.Kind == CXCursorKind.CXCursor_EnumConstantDecl)
                    {
                        var enumValue = ExtractEnumValue(child, enumEntity);
                        if (enumValue != null)
                        {
                            enumEntity.Values.Add(enumValue);
                        }
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));

                // Assign automatic values for non-explicit values
                AssignAutomaticValues(enumEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting enum values for {enumEntity.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract single enum value
        /// </summary>
        private unsafe CEnumValue ExtractEnumValue(CXCursor cursor, CEnum parentEnum)
        {
            try
            {
                string valueName = cursor.Spelling.ToString();

                // Get value location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceFile = file != null ? System.IO.Path.GetFileName(file.Name.ToString()) : string.Empty;

                // Get the enum constant value
                var constantValue = cursor.EnumConstantDeclValue;
                bool hasExplicitValue = HasExplicitValue(cursor);
                string valueExpression = hasExplicitValue ? ExtractValueExpression(cursor) : string.Empty;

                // Extract documentation
                string documentation = ExtractValueDocumentation(cursor);

                return new CEnumValue
                {
                    Name = valueName,
                    Value = constantValue,
                    IsExplicitValue = hasExplicitValue,
                    ValueExpression = valueExpression,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    Documentation = documentation
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting enum value: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Check if enum value has explicit assignment
        /// </summary>
        private unsafe bool HasExplicitValue(CXCursor cursor)
        {
            try
            {
                bool hasExplicit = false;
                cursor.VisitChildren((child, parent, clientData) =>
                {
                    if (child.Kind == CXCursorKind.CXCursor_IntegerLiteral ||
                        child.Kind == CXCursorKind.CXCursor_DeclRefExpr ||
                        child.Kind == CXCursorKind.CXCursor_BinaryOperator)
                    {
                        hasExplicit = true;
                        return CXChildVisitResult.CXChildVisit_Break;
                    }
                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));
                return hasExplicit;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extract value expression text
        /// </summary>
        private unsafe string ExtractValueExpression(CXCursor cursor)
        {
            try
            {
                var range = cursor.Extent;
                var translationUnit = cursor.TranslationUnit;
                var tokens = translationUnit.Tokenize(range);

                if (tokens.Length < 2)
                    return string.Empty;

                var expressionTokens = new List<string>();
                bool foundEquals = false;

                for (int i = 0; i < tokens.Length; i++)
                {
                    var tokenSpelling = clang.getTokenSpelling(translationUnit, tokens[i]).ToString();
                    if (tokenSpelling == "=")
                    {
                        foundEquals = true;
                        continue;
                    }
                    if (foundEquals)
                    {
                        expressionTokens.Add(tokenSpelling);
                    }
                }

                return string.Join(" ", expressionTokens).Trim();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract value expression: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extract documentation for enum value
        /// </summary>
        private unsafe string ExtractValueDocumentation(CXCursor cursor)
        {
            try
            {
                var comment = cursor.ParsedComment;
                if (comment.Kind == CXCommentKind.CXComment_Null)
                    return string.Empty;

                return comment.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract value documentation: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Assign automatic values to enum values that don't have explicit values
        /// </summary>
        private void AssignAutomaticValues(CEnum enumEntity)
        {
            long currentValue = 0;

            foreach (var value in enumEntity.Values)
            {
                if (value.IsExplicitValue)
                {
                    currentValue = value.Value + 1;
                }
                else
                {
                    value.Value = currentValue;
                    currentValue++;
                }
            }
        }

        /// <summary>
        /// Find dependencies in enum value expressions
        /// </summary>
        private List<string> FindEnumValueDependencies(string expression, List<CEnum> allEnums)
        {
            var dependencies = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(expression))
                    return dependencies;

                // Find identifiers in the expression
                var identifiers = Regex.Matches(expression, @"\b[A-Za-z_][A-Za-z0-9_]*\b");

                foreach (Match match in identifiers)
                {
                    string identifier = match.Value;

                    // Check if this identifier is an enum value from any enum
                    foreach (var enumEntity in allEnums)
                    {
                        if (enumEntity.Values.Any(v => v.Name == identifier))
                        {
                            if (!dependencies.Contains(enumEntity.Name))
                            {
                                dependencies.Add(enumEntity.Name);
                            }
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding enum value dependencies: {ex.Message}");
            }

            return dependencies;
        }

        /// <summary>
        /// Detect circular dependencies in enum values
        /// </summary>
        private async Task<List<string>> DetectCircularDependenciesAsync(CEnum enumEntity, List<CEnum> allEnums)
        {
            var circularDeps = new List<string>();

            try
            {
                var visited = new HashSet<string>();
                var currentPath = new HashSet<string>();

                await Task.Run(() =>
                {
                    DetectCircularDependenciesRecursive(enumEntity.Name, allEnums, visited, currentPath, circularDeps);
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error detecting circular dependencies for enum {enumEntity.Name}: {ex.Message}");
            }

            return circularDeps;
        }

        /// <summary>
        /// Recursive helper for circular dependency detection
        /// </summary>
        private void DetectCircularDependenciesRecursive(
            string enumName,
            List<CEnum> allEnums,
            HashSet<string> visited,
            HashSet<string> currentPath,
            List<string> circularDeps)
        {
            if (currentPath.Contains(enumName))
            {
                if (!circularDeps.Contains(enumName))
                {
                    circularDeps.Add(enumName);
                }
                return;
            }

            if (visited.Contains(enumName))
                return;

            visited.Add(enumName);
            currentPath.Add(enumName);

            var enumEntity = allEnums.FirstOrDefault(e => e.Name == enumName);
            if (enumEntity != null)
            {
                foreach (var value in enumEntity.Values.Where(v => v.IsExplicitValue))
                {
                    var deps = FindEnumValueDependencies(value.ValueExpression, allEnums);
                    foreach (var dep in deps)
                    {
                        DetectCircularDependenciesRecursive(dep, allEnums, visited, currentPath, circularDeps);
                    }
                }
            }

            currentPath.Remove(enumName);
        }

        /// <summary>
        /// Analyze enum patterns and add warnings/information
        /// </summary>
        private void AnalyzeEnumPatterns(CEnum enumEntity, ParseResult result)
        {
            try
            {
                // Check for bit flag patterns
                if (IsBitFlagEnum(enumEntity))
                {
                    result.ParseWarnings.Add(new ParseWarning
                    {
                        Message = $"Enum '{enumEntity.Name}' appears to be designed for bit flags",
                        LineNumber = enumEntity.LineNumber,
                        Category = "Enum Pattern",
                        Code = "ENUM_BITFLAG"
                    });
                }

                // Check for sequential patterns
                if (IsSequentialEnum(enumEntity))
                {
                    result.ParseWarnings.Add(new ParseWarning
                    {
                        Message = $"Enum '{enumEntity.Name}' has sequential values starting from {enumEntity.Values.First().Value}",
                        LineNumber = enumEntity.LineNumber,
                        Category = "Enum Pattern",
                        Code = "ENUM_SEQUENTIAL"
                    });
                }

                // Check for sparse patterns
                if (IsSparseEnum(enumEntity))
                {
                    result.ParseWarnings.Add(new ParseWarning
                    {
                        Message = $"Enum '{enumEntity.Name}' has sparse/non-sequential values",
                        LineNumber = enumEntity.LineNumber,
                        Category = "Enum Pattern",
                        Code = "ENUM_SPARSE"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing enum patterns for {enumEntity.Name}: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if enum follows bit flag pattern
        /// </summary>
        private bool IsBitFlagEnum(CEnum enumEntity)
        {
            if (enumEntity.Values.Count < 2)
                return false;

            return enumEntity.Values.Any(v => IsPowerOfTwo(v.Value)) &&
                   enumEntity.Values.Count(v => IsPowerOfTwo(v.Value)) >= enumEntity.Values.Count / 2;
        }

        /// <summary>
        /// Check if value is power of two
        /// </summary>
        private bool IsPowerOfTwo(long value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }

        /// <summary>
        /// Check if enum has sequential values
        /// </summary>
        private bool IsSequentialEnum(CEnum enumEntity)
        {
            if (enumEntity.Values.Count < 2)
                return true;

            var sortedValues = enumEntity.Values.OrderBy(v => v.Value).ToList();
            for (int i = 1; i < sortedValues.Count; i++)
            {
                if (sortedValues[i].Value != sortedValues[i - 1].Value + 1)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Check if enum has sparse values
        /// </summary>
        private bool IsSparseEnum(CEnum enumEntity)
        {
            if (enumEntity.Values.Count < 3)
                return false;

            var sortedValues = enumEntity.Values.OrderBy(v => v.Value).ToList();
            long totalRange = sortedValues.Last().Value - sortedValues.First().Value + 1;
            return totalRange > enumEntity.Values.Count * 2;
        }

        /// <summary>
        /// Validate enum name
        /// </summary>
        private void ValidateEnumName(CEnum enumEntity, List<ParseError> errors)
        {
            if (string.IsNullOrWhiteSpace(enumEntity.Name) && !enumEntity.IsAnonymous)
            {
                errors.Add(CreateValidationError("Enum name cannot be empty",
                    enumEntity.LineNumber, enumEntity.ColumnNumber));
                return;
            }

            if (!enumEntity.IsAnonymous)
            {
                if (!Regex.IsMatch(enumEntity.Name, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                {
                    errors.Add(CreateValidationError($"Invalid enum name: {enumEntity.Name}",
                        enumEntity.LineNumber, enumEntity.ColumnNumber));
                }

                if (ReservedEnumNames.Contains(enumEntity.Name.ToLower()))
                {
                    errors.Add(CreateValidationError($"Enum name '{enumEntity.Name}' conflicts with C keyword",
                        enumEntity.LineNumber, enumEntity.ColumnNumber));
                }
            }
        }

        /// <summary>
        /// Validate enum values
        /// </summary>
        private void ValidateEnumValues(CEnum enumEntity, List<ParseError> errors)
        {
            var valueNames = new HashSet<string>();

            foreach (var value in enumEntity.Values)
            {
                // Check for empty value name
                if (string.IsNullOrWhiteSpace(value.Name))
                {
                    errors.Add(CreateValidationError("Enum value name cannot be empty",
                        value.LineNumber, value.ColumnNumber));
                    continue;
                }

                // Check for invalid value name
                if (!Regex.IsMatch(value.Name, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                {
                    errors.Add(CreateValidationError($"Invalid enum value name: {value.Name}",
                        value.LineNumber, value.ColumnNumber));
                }

                // Check for duplicate names
                if (!valueNames.Add(value.Name))
                {
                    errors.Add(CreateValidationError($"Duplicate enum value name: {value.Name}",
                        value.LineNumber, value.ColumnNumber));
                }

                // Validate explicit value expressions
                if (value.IsExplicitValue && !string.IsNullOrEmpty(value.ValueExpression))
                {
                    ValidateValueExpression(value, errors);
                }

                // Check for reserved names
                if (ReservedEnumNames.Contains(value.Name.ToLower()))
                {
                    errors.Add(CreateValidationError($"Enum value '{value.Name}' conflicts with C keyword",
                        value.LineNumber, value.ColumnNumber, ErrorSeverity.Warning));
                }
            }
        }

        /// <summary>
        /// Validate value expression
        /// </summary>
        private void ValidateValueExpression(CEnumValue enumValue, List<ParseError> errors)
        {
            try
            {
                if (!IntegerValueRegex.IsMatch(enumValue.ValueExpression) &&
                    !HexValueRegex.IsMatch(enumValue.ValueExpression) &&
                    !OctalValueRegex.IsMatch(enumValue.ValueExpression) &&
                    !ExpressionRegex.IsMatch(enumValue.ValueExpression))
                {
                    errors.Add(CreateValidationError($"Invalid value expression: {enumValue.ValueExpression}",
                        enumValue.LineNumber, enumValue.ColumnNumber, ErrorSeverity.Warning));
                }
            }
            catch (Exception ex)
            {
                errors.Add(CreateValidationError($"Error validating expression '{enumValue.ValueExpression}': {ex.Message}",
                    enumValue.LineNumber, enumValue.ColumnNumber, ErrorSeverity.Warning));
            }
        }

        /// <summary>
        /// Check for duplicate enum values
        /// </summary>
        private void CheckDuplicateValues(CEnum enumEntity, List<ParseError> errors)
        {
            var valueGroups = enumEntity.Values.GroupBy(v => v.Value).Where(g => g.Count() > 1);

            foreach (var group in valueGroups)
            {
                var duplicateNames = string.Join(", ", group.Select(v => v.Name));
                var firstValue = group.First();
                errors.Add(CreateValidationError($"Duplicate enum values: {duplicateNames} = {group.Key}",
                    firstValue.LineNumber, firstValue.ColumnNumber, ErrorSeverity.Warning));
            }
        }

        /// <summary>
        /// Check for value gaps in sequential enums
        /// </summary>
        private void CheckValueGaps(CEnum enumEntity, List<ParseError> errors)
        {
            if (enumEntity.Values.Count < 3)
                return;

            var sortedValues = enumEntity.Values.OrderBy(v => v.Value).ToList();
            var gaps = new List<long>();

            for (int i = 1; i < sortedValues.Count; i++)
            {
                long gap = sortedValues[i].Value - sortedValues[i - 1].Value;
                if (gap > 1)
                {
                    gaps.Add(gap - 1);
                }
            }

            if (gaps.Count > 0 && gaps.Sum() > enumEntity.Values.Count)
            {
                errors.Add(CreateValidationError($"Enum has large gaps in values (total gap: {gaps.Sum()})",
                    enumEntity.LineNumber, enumEntity.ColumnNumber, ErrorSeverity.Info));
            }
        }

        /// <summary>
        /// Validate naming conventions
        /// </summary>
        private void ValidateNamingConventions(CEnum enumEntity, List<ParseError> errors)
        {
            if (enumEntity.IsAnonymous)
                return;

            // Check enum name convention (should typically start with uppercase)
            if (!char.IsUpper(enumEntity.Name[0]))
            {
                errors.Add(CreateValidationError($"Enum name '{enumEntity.Name}' should typically start with uppercase",
                    enumEntity.LineNumber, enumEntity.ColumnNumber, ErrorSeverity.Info));
            }

            // Check value naming consistency
            var upperCaseValues = enumEntity.Values.Count(v => v.Name.All(char.IsUpper));
            var mixedCaseValues = enumEntity.Values.Count(v => !v.Name.All(char.IsUpper) && !v.Name.All(char.IsLower));

            if (upperCaseValues > 0 && mixedCaseValues > 0)
            {
                errors.Add(CreateValidationError("Inconsistent naming convention in enum values",
                    enumEntity.LineNumber, enumEntity.ColumnNumber, ErrorSeverity.Info));
            }
        }

        /// <summary>
        /// Check for potential value overflow
        /// </summary>
        private void CheckValueOverflow(CEnum enumEntity, List<ParseError> errors)
        {
            long minValue = GetMinValueForType(enumEntity.UnderlyingType);
            long maxValue = GetMaxValueForType(enumEntity.UnderlyingType);

            foreach (var value in enumEntity.Values)
            {
                if (value.Value < minValue || value.Value > maxValue)
                {
                    errors.Add(CreateValidationError(
                        $"Enum value '{value.Name}' ({value.Value}) exceeds range for type {enumEntity.UnderlyingType} [{minValue}, {maxValue}]",
                        value.LineNumber, value.ColumnNumber, ErrorSeverity.Warning));
                }
            }
        }

        /// <summary>
        /// Get minimum value for underlying type
        /// </summary>
        private long GetMinValueForType(string underlyingType)
        {
            return underlyingType.ToLower() switch
            {
                "char" => sbyte.MinValue,
                "unsigned char" => 0,
                "short" => short.MinValue,
                "unsigned short" => 0,
                "int" => int.MinValue,
                "unsigned int" => 0,
                "long" => long.MinValue,
                "unsigned long" => 0,
                "long long" => long.MinValue,
                "unsigned long long" => 0,
                _ => int.MinValue
            };
        }

        /// <summary>
        /// Get maximum value for underlying type
        /// </summary>
        private long GetMaxValueForType(string underlyingType)
        {
            return underlyingType.ToLower() switch
            {
                "char" => sbyte.MaxValue,
                "unsigned char" => byte.MaxValue,
                "short" => short.MaxValue,
                "unsigned short" => ushort.MaxValue,
                "int" => int.MaxValue,
                "unsigned int" => uint.MaxValue,
                "long" => long.MaxValue,
                "unsigned long" => long.MaxValue, // Simplified for cross-platform
                "long long" => long.MaxValue,
                "unsigned long long" => long.MaxValue, // Simplified
                _ => int.MaxValue
            };
        }

        /// <summary>
        /// Extract value range constraints
        /// </summary>
        private List<EnumConstraint> ExtractValueRangeConstraints(CEnum enumEntity)
        {
            var constraints = new List<EnumConstraint>();

            try
            {
                if (enumEntity.Values.Any())
                {
                    var minValue = enumEntity.Values.Min(v => v.Value);
                    var maxValue = enumEntity.Values.Max(v => v.Value);

                    constraints.Add(new EnumConstraint
                    {
                        EnumName = enumEntity.Name,
                        ConstraintType = EnumConstraintType.ValueRange,
                        MinValue = minValue,
                        MaxValue = maxValue,
                        Source = "Value range analysis"
                    });

                    // Check if all values are positive
                    if (minValue >= 0)
                    {
                        constraints.Add(new EnumConstraint
                        {
                            EnumName = enumEntity.Name,
                            ConstraintType = EnumConstraintType.PositiveValues,
                            Source = "Positive value analysis"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting value range constraints for enum {enumEntity.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract usage patterns from source code
        /// </summary>
        private async Task<List<EnumConstraint>> ExtractUsagePatternsAsync(CEnum enumEntity, string sourceCode)
        {
            var constraints = new List<EnumConstraint>();

            try
            {
                if (string.IsNullOrEmpty(sourceCode))
                    return constraints;

                await Task.Run(() =>
                {
                    // Find enum usage patterns
                    var enumUsagePattern = new Regex($@"\b{Regex.Escape(enumEntity.Name)}\b");
                    var enumMatches = enumUsagePattern.Matches(sourceCode);

                    if (enumMatches.Count > 0)
                    {
                        constraints.Add(new EnumConstraint
                        {
                            EnumName = enumEntity.Name,
                            ConstraintType = EnumConstraintType.UsageCount,
                            Value = enumMatches.Count,
                            Source = "Usage pattern analysis"
                        });
                    }

                    // Find individual value usage
                    foreach (var value in enumEntity.Values)
                    {
                        var valueUsagePattern = new Regex($@"\b{Regex.Escape(value.Name)}\b");
                        var valueMatches = valueUsagePattern.Matches(sourceCode);

                        if (valueMatches.Count > 0)
                        {
                            constraints.Add(new EnumConstraint
                            {
                                EnumName = enumEntity.Name,
                                ValueName = value.Name,
                                ConstraintType = EnumConstraintType.ValueUsageCount,
                                Value = valueMatches.Count,
                                Source = "Value usage analysis"
                            });
                        }
                    }

                    // Check for switch statement usage
                    var switchPattern = new Regex($@"switch\s*\([^)]*\b{Regex.Escape(enumEntity.Name)}\b[^)]*\)");
                    var switchMatches = switchPattern.Matches(sourceCode);

                    if (switchMatches.Count > 0)
                    {
                        constraints.Add(new EnumConstraint
                        {
                            EnumName = enumEntity.Name,
                            ConstraintType = EnumConstraintType.SwitchUsage,
                            Value = switchMatches.Count,
                            Source = "Switch statement analysis"
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting usage patterns for enum {enumEntity.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract sequential patterns
        /// </summary>
        private List<EnumConstraint> ExtractSequentialPatterns(CEnum enumEntity)
        {
            var constraints = new List<EnumConstraint>();

            try
            {
                if (IsSequentialEnum(enumEntity))
                {
                    constraints.Add(new EnumConstraint
                    {
                        EnumName = enumEntity.Name,
                        ConstraintType = EnumConstraintType.SequentialPattern,
                        MinValue = enumEntity.Values.Min(v => v.Value),
                        MaxValue = enumEntity.Values.Max(v => v.Value),
                        Source = "Sequential pattern analysis"
                    });

                    // Check for zero-based sequential
                    if (enumEntity.Values.Min(v => v.Value) == 0)
                    {
                        constraints.Add(new EnumConstraint
                        {
                            EnumName = enumEntity.Name,
                            ConstraintType = EnumConstraintType.ZeroBased,
                            Source = "Zero-based analysis"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting sequential patterns for enum {enumEntity.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract bit flag patterns
        /// </summary>
        private List<EnumConstraint> ExtractBitFlagPatterns(CEnum enumEntity)
        {
            var constraints = new List<EnumConstraint>();

            try
            {
                if (IsBitFlagEnum(enumEntity))
                {
                    constraints.Add(new EnumConstraint
                    {
                        EnumName = enumEntity.Name,
                        ConstraintType = EnumConstraintType.BitFlagPattern,
                        Source = "Bit flag pattern analysis"
                    });

                    // Find the maximum bit position used
                    var maxBitPosition = 0;
                    foreach (var value in enumEntity.Values.Where(v => IsPowerOfTwo(v.Value)))
                    {
                        var bitPosition = (int)Math.Log2(value.Value);
                        if (bitPosition > maxBitPosition)
                        {
                            maxBitPosition = bitPosition;
                        }
                    }

                    if (maxBitPosition > 0)
                    {
                        constraints.Add(new EnumConstraint
                        {
                            EnumName = enumEntity.Name,
                            ConstraintType = EnumConstraintType.MaxBitPosition,
                            Value = maxBitPosition,
                            Source = "Bit position analysis"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting bit flag patterns for enum {enumEntity.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Create validation error
        /// </summary>
        private ParseError CreateValidationError(string message, int line, int column,
            ErrorSeverity severity = ErrorSeverity.Error)
        {
            return new ParseError
            {
                Message = message,
                LineNumber = line,
                ColumnNumber = column,
                Severity = severity
            };
        }

        #endregion
    }
}