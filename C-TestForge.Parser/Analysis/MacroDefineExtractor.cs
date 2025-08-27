using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Models.Core;
using C_TestForge.Models.Core.Enumerations;
using C_TestForge.Models.Parse;
using ClangSharp.Interop;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.Parser.Analysis
{
    /// <summary>
    /// Implementation of macro definition extraction and analysis service
    /// </summary>
    public class MacroDefineExtractor : IMacroDefineExtractor
    {
        private readonly ILogger<MacroDefineExtractor> _logger;
        private readonly ITypeManager _typeManager;

        // Regex để kiểm tra function-like macro từ định nghĩa đầy đủ
        private static readonly Regex FunctionMacroRegex = new Regex(
            @"^([A-Za-z_][A-Za-z0-9_]*)\s*\(\s*([^)]*)\s*\)\s*(.*)",
            RegexOptions.Compiled);

        // Regex để kiểm tra function-like macro từ phần value (đã tách tên)
        private static readonly Regex FunctionMacroFromValueRegex = new Regex(
            @"^\s*\(\s*([^)]*)\s*\)\s+(.+)",
            RegexOptions.Compiled);

        private static readonly Regex ParameterRegex = new Regex(@"[A-Za-z_][A-Za-z0-9_]*",
            RegexOptions.Compiled);

        private static readonly HashSet<string> SystemMacros = new HashSet<string>
        {
            "__FILE__", "__LINE__", "__DATE__", "__TIME__", "__STDC__", "__STDC_VERSION__",
            "__GNUC__", "__GNUC_MINOR__", "__GNUC_PATCHLEVEL__", "__VERSION__",
            "_WIN32", "_WIN64", "__linux__", "__unix__", "__APPLE__", "__MACH__",
            "NULL", "TRUE", "FALSE", "EOF", "BUFSIZ", "FILENAME_MAX", "PATH_MAX",
            "INT_MAX", "INT_MIN", "UINT_MAX", "LONG_MAX", "LONG_MIN", "SIZE_MAX"
        };

        private static readonly HashSet<string> ConditionalMacros = new HashSet<string>
        {
            "DEBUG", "NDEBUG", "_DEBUG", "RELEASE", "_RELEASE"
        };

        /// <summary>
        /// Constructor for MacroDefineExtractor
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="typeManager">Type manager for type resolution</param>
        public MacroDefineExtractor(
            ILogger<MacroDefineExtractor> logger,
            ITypeManager typeManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _typeManager = typeManager ?? throw new ArgumentNullException(nameof(typeManager));
        }

        /// <inheritdoc/>
        public unsafe void ExtractMacroDefine(CXCursor cursor, ParseResult result)
        {
            try
            {
                if (cursor.Kind != CXCursorKind.CXCursor_MacroDefinition)
                {
                    return;
                }

                string macroName = cursor.Spelling.ToString();
                _logger.LogDebug($"Extracting macro definition: {macroName}");

                // Get macro location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                string sourceFile = file != null ? System.IO.Path.GetFileName(file.Name.ToString()) : string.Empty;

                if (string.IsNullOrWhiteSpace(sourceFile))
                {
                    return;
                }

                // Extract macro definition text
                string definitionText = ExtractMacroDefinitionText(cursor);
                (string macroValue, List<string> parameters) = ExtractMacroValue(definitionText);

                // Analyze macro properties
                bool isFunctionLike = parameters != null && parameters.Count > 0;
                DefinitionType definitionType = DetermineDefinitionType(macroName, macroValue, isFunctionLike);
                bool isSystemMacro = IsSystemMacro(macroName);

                // Extract documentation
                string documentation = ExtractMacroDocumentation(cursor);

                // Create macro definition object
                var definition = new CDefinition
                {
                    Name = macroName,
                    Value = macroValue,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFile,
                    IsFunctionLike = isFunctionLike,
                    Parameters = parameters,
                    DefinitionType = definitionType,
                    IsSystemMacro = isSystemMacro,
                    IsEnabled = true,
                    Documentation = documentation,
                    UsageCount = 0
                };

                // Analyze dependencies
                definition.Dependencies = AnalyzeMacroDependencies(definition, result.Definitions);

                // Validate definition
                var validationErrors = ValidateMacroDefinition(definition);
                result.ParseErrors.AddRange(validationErrors);

                

                // Add to result
                result.Definitions.Add(definition);

                // Update statistics
                result.Statistics.SymbolsResolved++;

                _logger.LogDebug($"Successfully extracted macro: {macroName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting macro definition: {ex.Message}");
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);
                result.ParseErrors.Add(new ParseError
                {
                    Message = $"Failed to extract macro definition: {ex.Message}",
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    Severity = ErrorSeverity.Warning,
                });
                return;
            }
        }

        /// <inheritdoc/>
        public async Task<List<MacroDependency>> AnalyzeMacroDependenciesAsync(
            List<CDefinition> definitions,
            ParseResult result)
        {
            _logger.LogInformation($"Analyzing dependencies for {definitions.Count} macro definitions");

            var dependencies = new List<MacroDependency>();

            try
            {
                foreach (var definition in definitions)
                {
                    _logger.LogDebug($"Analyzing dependencies for macro: {definition.Name}");

                    // Find direct dependencies
                    var directDeps = FindDirectDependencies(definition, definitions);

                    foreach (var dep in directDeps)
                    {
                        dependencies.Add(new MacroDependency
                        {
                            MacroName = definition.Name,
                            DependsOn = dep,
                            DependencyType = MacroDependencyType.Direct,
                            LineNumber = definition.LineNumber
                        });
                    }

                    // Analyze circular dependencies
                    var circularDeps = DetectCircularDependencies(definition, definitions);
                    foreach (var circularDep in circularDeps)
                    {
                        result.ParseWarnings.Add(new ParseWarning
                        {
                            Message = $"Circular dependency detected: {definition.Name} <-> {circularDep}",
                            LineNumber = definition.LineNumber,
                            Category = "Macro Dependencies",
                            Code = "MACRO_CIRCULAR"
                        });
                    }

                    // Check for undefined dependencies
                    var undefinedDeps = FindUndefinedDependencies(definition, definitions);
                    foreach (var undefinedDep in undefinedDeps)
                    {
                        result.ParseWarnings.Add(new ParseWarning
                        {
                            Message = $"Macro '{definition.Name}' references undefined macro '{undefinedDep}'",
                            LineNumber = definition.LineNumber,
                            Category = "Undefined References",
                            Code = "MACRO_UNDEFINED"
                        });
                    }
                }

                _logger.LogInformation($"Found {dependencies.Count} macro dependencies");
                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing macro dependencies: {ex.Message}");
                return dependencies;
            }
        }

        /// <inheritdoc/>
        public List<ParseError> ValidateMacroDefinition(CDefinition definition)
        {
            var errors = new List<ParseError>();

            try
            {
                if (definition == null)
                {
                    errors.Add(CreateValidationError("Null macro definition", 0, 0));
                    return errors;
                }

                _logger.LogDebug($"Validating macro definition: {definition.Name}");

                // Validate name
                ValidateMacroName(definition, errors);

                // Validate function-like macro parameters
                if (definition.IsFunctionLike)
                {
                    ValidateFunctionMacroParameters(definition, errors);
                }

                // Validate macro value
                ValidateMacroValue(definition, errors);

                // Check for naming conventions
                ValidateNamingConventions(definition, errors);

                // Check for potential side effects
                DetectPotentialSideEffects(definition, errors);

                _logger.LogDebug($"Validation completed for macro: {definition.Name}, found {errors.Count} issues");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating macro definition {definition?.Name}: {ex.Message}");
                errors.Add(CreateValidationError($"Validation error: {ex.Message}",
                    definition?.LineNumber ?? 0, definition?.ColumnNumber ?? 0));
            }

            return errors;
        }

        /// <inheritdoc/>
        public async Task<List<MacroConstraint>> ExtractMacroConstraintsAsync(
            CDefinition definition,
            string sourceCode)
        {
            _logger.LogInformation($"Extracting constraints for macro {definition.Name}");

            var constraints = new List<MacroConstraint>();

            try
            {
                // Extract usage patterns
                var usagePatterns = await ExtractUsagePatternsAsync(definition, sourceCode);
                constraints.AddRange(usagePatterns);

                // Extract conditional usage
                var conditionalUsage = ExtractConditionalUsage(definition, sourceCode);
                constraints.AddRange(conditionalUsage);

                // Extract value constraints
                var valueConstraints = ExtractValueConstraints(definition);
                constraints.AddRange(valueConstraints);

                _logger.LogInformation($"Extracted {constraints.Count} constraints for macro {definition.Name}");
                return constraints;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting constraints for macro {definition.Name}: {ex.Message}");
                return constraints;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Extract macro definition text from cursor
        /// </summary>
        private unsafe string ExtractMacroDefinitionText(CXCursor cursor)
        {
            try
            {
                var range = cursor.Extent;
                var translationUnit = cursor.TranslationUnit;
                var tokens = translationUnit.Tokenize(range);
                
                if (tokens.Length < 2)
                    return string.Empty;

                var definitionTokens = new List<string>();
                for (int i = 1; i < tokens.Length; i++)
                {
                    var tokenSpelling = clang.getTokenSpelling(translationUnit, tokens[i]);
                    definitionTokens.Add(tokenSpelling.ToString());
                }
                return string.Join(" ", definitionTokens);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract macro definition text: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Extract macro value from definition text
        /// Trả về giá trị và danh sách tham số nếu là function-like macro
        /// </summary>
        private (string, List<string>) ExtractMacroValue(string definitionText)
        {
            if (string.IsNullOrWhiteSpace(definitionText))
                return (string.Empty, new List<string>());

            try
            {
                // Handle function-like macros
                if (IsFunctionLikeMacroFromValue(definitionText))
                {
                    // Lâý phần sau dấu đóng ngoặc, phần body
                    var match = FunctionMacroFromValueRegex.Match(definitionText);
                    if (match.Success && match.Groups.Count > 2)
                    {
                        var pra = ExtractMacroParameters(match.Groups[1].Value.Trim());
                        // Return the part after the closing parenthesis
                        var body = match.Groups[2].Value.Trim();
                        return (body, pra);
                    }
                }

                return (definitionText.Trim(), new List<string>());
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Could not extract macro value: {ex.Message}");
                return (definitionText.Trim(), new List<string>());
            }
        }

        /// <summary>
        /// Extract documentation from macro cursor
        /// </summary>
        private unsafe string ExtractMacroDocumentation(CXCursor cursor)
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
                _logger.LogWarning($"Could not extract macro documentation: {ex.Message}");
                return string.Empty;
            }
        }

        /// <summary>
        /// Analyze macro dependencies
        /// </summary>
        private List<string> AnalyzeMacroDependencies(CDefinition definition, List<CDefinition> allDefinitions)
        {
            var dependencies = new HashSet<string>();

            try
            {
                if (string.IsNullOrWhiteSpace(definition.Value))
                    return new List<string>();

                // Find macro references in the value
                var macroRefs = Regex.Matches(definition.Value, @"\b[A-Z_][A-Z0-9_]*\b");

                foreach (Match match in macroRefs)
                {
                    string macroName = match.Value;

                    // Check if this macro exists in our definitions
                    if (allDefinitions.Any(d => d.Name == macroName) && macroName != definition.Name)
                    {
                        dependencies.Add(macroName);
                    }
                }

                // For function-like macros, check parameter usage
                if (definition.IsFunctionLike && definition.Parameters.Any())
                {
                    foreach (var param in definition.Parameters)
                    {
                        var paramRefs = Regex.Matches(definition.Value, $@"\b{Regex.Escape(param)}\b");
                        if (paramRefs.Count == 0)
                        {
                            _logger.LogWarning($"Parameter '{param}' in macro '{definition.Name}' is unused");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing dependencies for macro {definition.Name}: {ex.Message}");
            }

            return dependencies.ToList();
        }

        /// <summary>
        /// Find direct dependencies of a macro
        /// </summary>
        private List<string> FindDirectDependencies(CDefinition definition, List<CDefinition> allDefinitions)
        {
            var dependencies = new List<string>();

            try
            {
                if (string.IsNullOrWhiteSpace(definition.Value))
                    return dependencies;

                // Look for macro identifiers in the value
                var identifiers = Regex.Matches(definition.Value, @"\b[A-Za-z_][A-Za-z0-9_]*\b");

                foreach (Match match in identifiers)
                {
                    string identifier = match.Value;

                    // Skip parameters for function-like macros
                    if (definition.IsFunctionLike && definition.Parameters.Contains(identifier))
                        continue;

                    // Check if this identifier is a defined macro
                    if (allDefinitions.Any(d => d.Name == identifier) && identifier != definition.Name)
                    {
                        if (!dependencies.Contains(identifier))
                        {
                            dependencies.Add(identifier);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding dependencies for macro {definition.Name}: {ex.Message}");
            }

            return dependencies;
        }

        /// <summary>
        /// Detect circular dependencies
        /// </summary>
        private List<string> DetectCircularDependencies(CDefinition definition, List<CDefinition> allDefinitions)
        {
            var circularDeps = new List<string>();

            try
            {
                var visited = new HashSet<string>();
                var currentPath = new HashSet<string>();

                DetectCircularDependenciesRecursive(definition.Name, allDefinitions, visited, currentPath, circularDeps);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error detecting circular dependencies for macro {definition.Name}: {ex.Message}");
            }

            return circularDeps;
        }

        /// <summary>
        /// Recursive helper for circular dependency detection
        /// </summary>
        private void DetectCircularDependenciesRecursive(
            string macroName,
            List<CDefinition> allDefinitions,
            HashSet<string> visited,
            HashSet<string> currentPath,
            List<string> circularDeps)
        {
            if (currentPath.Contains(macroName))
            {
                if (!circularDeps.Contains(macroName))
                {
                    circularDeps.Add(macroName);
                }
                return;
            }

            if (visited.Contains(macroName))
                return;

            visited.Add(macroName);
            currentPath.Add(macroName);

            var definition = allDefinitions.FirstOrDefault(d => d.Name == macroName);
            if (definition?.Dependencies != null)
            {
                foreach (var dependency in definition.Dependencies)
                {
                    DetectCircularDependenciesRecursive(dependency, allDefinitions, visited, currentPath, circularDeps);
                }
            }

            currentPath.Remove(macroName);
        }

        /// <summary>
        /// Find undefined dependencies
        /// </summary>
        private List<string> FindUndefinedDependencies(CDefinition definition, List<CDefinition> allDefinitions)
        {
            var undefinedDeps = new List<string>();

            try
            {
                if (definition.Dependencies != null)
                {
                    foreach (var dependency in definition.Dependencies)
                    {
                        if (!allDefinitions.Any(d => d.Name == dependency) && !SystemMacros.Contains(dependency))
                        {
                            undefinedDeps.Add(dependency);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding undefined dependencies for macro {definition.Name}: {ex.Message}");
            }

            return undefinedDeps;
        }

        /// <summary>
        /// Validate macro name
        /// </summary>
        private void ValidateMacroName(CDefinition definition, List<ParseError> errors)
        {
            if (string.IsNullOrWhiteSpace(definition.Name))
            {
                errors.Add(CreateValidationError("Macro name cannot be empty",
                    definition.LineNumber, definition.ColumnNumber));
                return;
            }

            if (!Regex.IsMatch(definition.Name, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            {
                errors.Add(CreateValidationError($"Invalid macro name: {definition.Name}",
                    definition.LineNumber, definition.ColumnNumber));
            }
        }

        /// <summary>
        /// Validate function-like macro parameters
        /// </summary>
        private void ValidateFunctionMacroParameters(CDefinition definition, List<ParseError> errors)
        {
            if (definition.Parameters == null)
                return;

            var paramNames = new HashSet<string>();

            foreach (var param in definition.Parameters)
            {
                if (string.IsNullOrWhiteSpace(param))
                {
                    errors.Add(CreateValidationError("Empty parameter in function-like macro",
                        definition.LineNumber, definition.ColumnNumber));
                }
                else if (!Regex.IsMatch(param, @"^[A-Za-z_][A-Za-z0-9_]*$"))
                {
                    errors.Add(CreateValidationError($"Invalid parameter name: {param}",
                        definition.LineNumber, definition.ColumnNumber));
                }
                else if (!paramNames.Add(param))
                {
                    errors.Add(CreateValidationError($"Duplicate parameter name: {param}",
                        definition.LineNumber, definition.ColumnNumber));
                }
            }
        }

        /// <summary>
        /// Validate macro value
        /// </summary>
        private void ValidateMacroValue(CDefinition definition, List<ParseError> errors)
        {
            if (string.IsNullOrEmpty(definition.Value))
                return;

            try
            {
                // Check for unbalanced parentheses
                ValidateParentheses(definition, errors);

                // Check for string literal issues
                ValidateStringLiterals(definition, errors);

                // Check for numeric literal issues
                ValidateNumericLiterals(definition, errors);
            }
            catch (Exception ex)
            {
                errors.Add(CreateValidationError($"Error validating macro value: {ex.Message}",
                    definition.LineNumber, definition.ColumnNumber));
            }
        }

        /// <summary>
        /// Validate parentheses balance
        /// </summary>
        private void ValidateParentheses(CDefinition definition, List<ParseError> errors)
        {
            int parenCount = 0;
            foreach (char c in definition.Value)
            {
                if (c == '(') parenCount++;
                else if (c == ')') parenCount--;

                if (parenCount < 0)
                {
                    errors.Add(CreateValidationError("Unbalanced parentheses in macro value",
                        definition.LineNumber, definition.ColumnNumber));
                    return;
                }
            }

            if (parenCount > 0)
            {
                errors.Add(CreateValidationError("Unbalanced parentheses in macro value",
                    definition.LineNumber, definition.ColumnNumber));
            }
        }

        /// <summary>
        /// Validate string literals
        /// </summary>
        private void ValidateStringLiterals(CDefinition definition, List<ParseError> errors)
        {
            var stringPattern = new Regex(@"""([^""\\]|\\.)*""");
            var matches = stringPattern.Matches(definition.Value);

            foreach (Match match in matches)
            {
                string literal = match.Value;
                if (!literal.EndsWith("\"") || literal.Length < 2)
                {
                    errors.Add(CreateValidationError("Malformed string literal in macro value",
                        definition.LineNumber, definition.ColumnNumber, ErrorSeverity.Warning));
                }
            }
        }

        /// <summary>
        /// Validate numeric literals
        /// </summary>
        private void ValidateNumericLiterals(CDefinition definition, List<ParseError> errors)
        {
            // Check for malformed numeric literals
            var numericPattern = new Regex(@"\b\d+[a-zA-Z_]\w*\b");
            var matches = numericPattern.Matches(definition.Value);

            foreach (Match match in matches)
            {
                string literal = match.Value;
                if (!Regex.IsMatch(literal, @"^\d+[fFlLuU]*$") && !Regex.IsMatch(literal, @"^0[xX][0-9a-fA-F]+[uUlL]*$"))
                {
                    errors.Add(CreateValidationError($"Potentially malformed numeric literal: {literal}",
                        definition.LineNumber, definition.ColumnNumber, ErrorSeverity.Warning));
                }
            }
        }

        /// <summary>
        /// Validate naming conventions
        /// </summary>
        private void ValidateNamingConventions(CDefinition definition, List<ParseError> errors)
        {
            // Check for lowercase macro names (typically should be uppercase)
            if (definition.Name.Any(char.IsLower) && !definition.IsSystemMacro)
            {
                errors.Add(CreateValidationError($"Macro name '{definition.Name}' should typically be uppercase",
                    definition.LineNumber, definition.ColumnNumber, ErrorSeverity.Info));
            }

            // Check for reserved name patterns
            if (definition.Name.StartsWith("__") && !definition.IsSystemMacro)
            {
                errors.Add(CreateValidationError($"Macro name '{definition.Name}' uses reserved pattern '__'",
                    definition.LineNumber, definition.ColumnNumber, ErrorSeverity.Warning));
            }
        }

        /// <summary>
        /// Detect potential side effects
        /// </summary>
        private void DetectPotentialSideEffects(CDefinition definition, List<ParseError> errors)
        {
            if (string.IsNullOrEmpty(definition.Value))
                return;

            // Check for increment/decrement operators
            if (definition.Value.Contains("++") || definition.Value.Contains("--"))
            {
                errors.Add(CreateValidationError("Macro contains increment/decrement operators - may cause side effects",
                    definition.LineNumber, definition.ColumnNumber, ErrorSeverity.Warning));
            }

            // Check for function calls
            if (Regex.IsMatch(definition.Value, @"\w+\s*\("))
            {
                errors.Add(CreateValidationError("Macro contains function calls - consider side effects",
                    definition.LineNumber, definition.ColumnNumber, ErrorSeverity.Info));
            }

            // Check for assignment operators
            if (Regex.IsMatch(definition.Value, @"[^=!<>]=(?!=)"))
            {
                errors.Add(CreateValidationError("Macro contains assignment operators - may cause side effects",
                    definition.LineNumber, definition.ColumnNumber, ErrorSeverity.Warning));
            }
        }

        /// <summary>
        /// Extract usage patterns from source code
        /// </summary>
        private async Task<List<MacroConstraint>> ExtractUsagePatternsAsync(CDefinition definition, string sourceCode)
        {
            var constraints = new List<MacroConstraint>();

            try
            {
                if (string.IsNullOrEmpty(sourceCode))
                    return constraints;

                var usagePattern = new Regex($@"\b{Regex.Escape(definition.Name)}\b");
                var matches = usagePattern.Matches(sourceCode);

                if (matches.Count > 0)
                {
                    constraints.Add(new MacroConstraint
                    {
                        MacroName = definition.Name,
                        ConstraintType = MacroConstraintType.UsageCount,
                        Value = matches.Count.ToString(),
                        Source = "Usage pattern analysis"
                    });
                }

                // Analyze context of usage
                foreach (Match match in matches)
                {
                    var context = ExtractUsageContext(sourceCode, match.Index, 50);
                    var contextConstraint = AnalyzeUsageContext(definition, context);
                    if (contextConstraint != null)
                    {
                        constraints.Add(contextConstraint);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting usage patterns for macro {definition.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract usage context around a match
        /// </summary>
        private string ExtractUsageContext(string sourceCode, int position, int contextLength)
        {
            int start = Math.Max(0, position - contextLength);
            int end = Math.Min(sourceCode.Length, position + contextLength);
            return sourceCode.Substring(start, end - start);
        }

        /// <summary>
        /// Analyze usage context for constraints
        /// </summary>
        private MacroConstraint AnalyzeUsageContext(CDefinition definition, string context)
        {
            // Analyze if macro is used in specific contexts that imply constraints
            if (context.Contains("if") && context.Contains("=="))
            {
                return new MacroConstraint
                {
                    MacroName = definition.Name,
                    ConstraintType = MacroConstraintType.ConditionalUsage,
                    Value = "equality_check",
                    Source = "Conditional usage analysis"
                };
            }

            return null;
        }

        /// <summary>
        /// Extract conditional usage patterns
        /// </summary>
        private List<MacroConstraint> ExtractConditionalUsage(CDefinition definition, string sourceCode)
        {
            var constraints = new List<MacroConstraint>();

            try
            {
                // Look for #ifdef, #ifndef patterns
                var conditionalPattern = new Regex($@"#(?:ifdef|ifndef|if\s+defined)\s+{Regex.Escape(definition.Name)}\b");
                var matches = conditionalPattern.Matches(sourceCode);

                if (matches.Count > 0)
                {
                    constraints.Add(new MacroConstraint
                    {
                        MacroName = definition.Name,
                        ConstraintType = MacroConstraintType.ConditionalCompilation,
                        Value = matches.Count.ToString(),
                        Source = "Conditional compilation analysis"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting conditional usage for macro {definition.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Extract value constraints based on macro type and value
        /// </summary>
        private List<MacroConstraint> ExtractValueConstraints(CDefinition definition)
        {
            var constraints = new List<MacroConstraint>();

            try
            {
                if (definition.DefinitionType == DefinitionType.Constant && !string.IsNullOrEmpty(definition.Value))
                {
                    // Check if it's a numeric constant
                    if (IsNumericConstant(definition.Value))
                    {
                        constraints.Add(new MacroConstraint
                        {
                            MacroName = definition.Name,
                            ConstraintType = MacroConstraintType.NumericValue,
                            Value = definition.Value,
                            Source = "Numeric constant analysis"
                        });
                    }

                    // Check if it's a string constant
                    else if (definition.Value.StartsWith("\"") && definition.Value.EndsWith("\""))
                    {
                        constraints.Add(new MacroConstraint
                        {
                            MacroName = definition.Name,
                            ConstraintType = MacroConstraintType.StringValue,
                            Value = definition.Value,
                            Source = "String constant analysis"
                        });
                    }
                }

                // For enum-like macros
                if (definition.DefinitionType == DefinitionType.EnumValue)
                {
                    constraints.Add(new MacroConstraint
                    {
                        MacroName = definition.Name,
                        ConstraintType = MacroConstraintType.EnumValue,
                        Value = definition.Value,
                        Source = "Enum value analysis"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting value constraints for macro {definition.Name}: {ex.Message}");
            }

            return constraints;
        }

        /// <summary>
        /// Kiểm tra từ định nghĩa đầy đủ: "MY_MACRO_ADD(a, b) ((a) + (b))"
        /// </summary>
        private bool IsFunctionLikeMacro(string macroText)
        {
            if (string.IsNullOrWhiteSpace(macroText))
                return false;

            return FunctionMacroRegex.IsMatch(macroText.Trim());
        }

        /// <summary>
        /// Kiểm tra từ phần value: "(a, b) ((a) + (b))"
        /// </summary>
        public static bool IsFunctionLikeMacroFromValue(string macroValue)
        {
            if (string.IsNullOrWhiteSpace(macroValue))
                return false;

            var trimmed = macroValue.Trim();

            // Phải bắt đầu bằng dấu (
            if (!trimmed.StartsWith("("))
                return false;

            // Tìm dấu ) đầu tiên
            int firstCloseParen = FindFirstClosingParen(trimmed);
            if (firstCloseParen == -1)
                return false;

            // Kiểm tra xem sau dấu ) có khoảng trắng và nội dung không
            if (firstCloseParen + 1 >= trimmed.Length)
                return false;

            string afterParen = trimmed.Substring(firstCloseParen + 1);

            // Phải có ít nhất một khoảng trắng và sau đó là nội dung
            return afterParen.StartsWith(" ") || afterParen.StartsWith("\t");
        }

        /// <summary>
        /// Tìm dấu ) đầu tiên, tôn trọng nesting
        /// </summary>
        private static int FindFirstClosingParen(string text)
        {
            int depth = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '(')
                {
                    depth++;
                }
                else if (text[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return i;
                    }
                }
            }
            return -1; // Không tìm thấy dấu ) khớp
        }

        /// <summary>
        /// Extract macro parameters from function-like macro
        /// </summary>
        private List<string> ExtractMacroParameters(string macroText)
        {
            var parameters = new List<string>();

            if(string.IsNullOrWhiteSpace(macroText))
                return parameters;

            try
            {
                // Split by comma and clean up each parameter
                var paramParts = macroText.Split(',');
                foreach (var part in paramParts)
                {
                    var cleanParam = part.Trim();
                    if (!string.IsNullOrWhiteSpace(cleanParam))
                    {
                        parameters.Add(cleanParam);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error extracting macro parameters: {ex.Message}");
            }

            return parameters;
        }

        /// <summary>
        /// Determine the type of definition
        /// </summary>
        private DefinitionType DetermineDefinitionType(string name, string value, bool isFunctionLike)
        {
            try
            {
                if (isFunctionLike)
                    return DefinitionType.FunctionMacro;

                if (ConditionalMacros.Contains(name))
                    return DefinitionType.Conditional;

                // Check if it's a numeric constant
                if (IsNumericConstant(value))
                    return DefinitionType.Constant;

                // Check if it looks like an enum value
                if (IsEnumLikeValue(name, value))
                    return DefinitionType.EnumValue;

                return DefinitionType.Macro;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error determining definition type for {name}: {ex.Message}");
                return DefinitionType.Macro;
            }
        }

        /// <summary>
        /// Check if value is a numeric constant
        /// </summary>
        private bool IsNumericConstant(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            // Simple numeric patterns
            return Regex.IsMatch(value.Trim(), @"^-?\d+(\.\d+)?([eE][+-]?\d+)?[fFlLuU]*$") ||
                   Regex.IsMatch(value.Trim(), @"^0[xX][0-9a-fA-F]+[uUlL]*$");
        }

        /// <summary>
        /// Check if this looks like an enum value
        /// </summary>
        private bool IsEnumLikeValue(string name, string value)
        {
            try
            {
                // Enum-like naming convention and simple numeric value
                return name.All(char.IsUpper) &&
                       (IsNumericConstant(value) || string.IsNullOrWhiteSpace(value));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if macro is a system/standard macro
        /// </summary>
        private bool IsSystemMacro(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            return SystemMacros.Contains(name) ||
                   name.StartsWith("__") ||
                   name.StartsWith("_") && name.Length > 1 && char.IsUpper(name[1]);
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
