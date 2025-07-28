using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models;
using ClangSharp.Interop;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace C_TestForge.Parser
{
    #region PreprocessorService Implementation

    /// <summary>
    /// Implementation of the preprocessor service
    /// </summary>
    public class PreprocessorService : IPreprocessorService
    {
        private readonly ILogger<PreprocessorService> _logger;
        private readonly IFileService _fileService;

        public PreprocessorService(
            ILogger<PreprocessorService> logger,
            IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        /// <inheritdoc/>
        public async Task<PreprocessorResult> ExtractPreprocessorDefinitionsAsync(CXTranslationUnit translationUnit, string sourceFileName)
        {
            try
            {
                _logger.LogInformation("Extracting preprocessor definitions");

                var result = new PreprocessorResult
                {
                    Definitions = new List<CDefinition>(),
                    ConditionalDirectives = new List<ConditionalDirective>(),
                    Includes = new List<IncludeDirective>()
                };

                // Get all tokens from the translation unit
                CXSourceRange extent = translationUnit.Cursor.Extent;
                CXToken[] tokens = translationUnit.Tokenize(extent).ToArray();

                // Process the tokens to find preprocessor directives
                await ProcessTokensAsync(tokens, translationUnit, sourceFileName, result);

                _logger.LogInformation($"Extracted {result.Definitions.Count} preprocessor definitions, " +
                                       $"{result.ConditionalDirectives.Count} conditional directives, " +
                                       $"{result.Includes.Count} includes");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting preprocessor definitions");
                throw;
            }
        }

        /// <inheritdoc/>
        public bool IsDefinitionEnabled(CDefinition definition, Dictionary<string, string> activeDefinitions)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            // Check if the definition is in the active definitions
            if (activeDefinitions.ContainsKey(definition.Name))
            {
                // Compare values if both are non-null
                if (!string.IsNullOrEmpty(definition.Value) && !string.IsNullOrEmpty(activeDefinitions[definition.Name]))
                {
                    return definition.Value == activeDefinitions[definition.Name];
                }

                // If only one has a value, consider it enabled if it's in the active definitions
                return true;
            }

            // Consider definitions not in the active definitions as enabled by default
            return true;
        }

        /// <inheritdoc/>
        public bool EvaluateConditionalDirective(ConditionalDirective directive, Dictionary<string, string> activeDefinitions)
        {
            if (directive == null)
            {
                throw new ArgumentNullException(nameof(directive));
            }

            switch (directive.Type)
            {
                case ConditionalType.IfDef:
                    return activeDefinitions.ContainsKey(directive.Condition);

                case ConditionalType.IfNDef:
                    return !activeDefinitions.ContainsKey(directive.Condition);

                case ConditionalType.If:
                case ConditionalType.ElseIf:
                    return EvaluateConditionExpression(directive.Condition, activeDefinitions);

                case ConditionalType.Else:
                    // For Else, we need to check if any of the previous conditions are true
                    if (directive.ParentDirective != null)
                    {
                        // If the parent directive is satisfied, then this else branch is not taken
                        bool parentSatisfied = EvaluateConditionalDirective(directive.ParentDirective, activeDefinitions);

                        // If any sibling else-if branches are satisfied, then this else branch is not taken
                        bool anySiblingSatisfied = false;
                        foreach (var sibling in directive.ParentDirective.Branches)
                        {
                            if (sibling != directive && sibling.Type == ConditionalType.ElseIf)
                            {
                                if (EvaluateConditionalDirective(sibling, activeDefinitions))
                                {
                                    anySiblingSatisfied = true;
                                    break;
                                }
                            }
                        }

                        // This else branch is taken if neither the parent nor any siblings are satisfied
                        return !parentSatisfied && !anySiblingSatisfied;
                    }

                    // If there's no parent, something is wrong
                    return false;

                default:
                    throw new NotSupportedException($"Conditional directive type not supported: {directive.Type}");
            }
        }

        /// <inheritdoc/>
        public async Task<List<ConditionalDirective>> ExtractConditionalDirectivesAsync(string sourceCode, string fileName)
        {
            try
            {
                _logger.LogInformation($"Extracting conditional directives from {fileName}");

                var conditionalDirectives = new List<ConditionalDirective>();
                var conditionalStack = new Stack<ConditionalDirective>();

                // Split the source code into lines
                string[] lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Regular expressions for matching conditional directives
                var ifdefRegex = new Regex(@"^\s*#\s*ifdef\s+(\w+)");
                var ifndefRegex = new Regex(@"^\s*#\s*ifndef\s+(\w+)");
                var ifRegex = new Regex(@"^\s*#\s*if\s+(.+)");
                var elifRegex = new Regex(@"^\s*#\s*elif\s+(.+)");
                var elseRegex = new Regex(@"^\s*#\s*else");
                var endifRegex = new Regex(@"^\s*#\s*endif");

                for (int lineNum = 0; lineNum < lines.Length; lineNum++)
                {
                    string line = lines[lineNum];

                    // Check for #ifdef
                    var ifdefMatch = ifdefRegex.Match(line);
                    if (ifdefMatch.Success)
                    {
                        string macroName = ifdefMatch.Groups[1].Value;
                        var directive = new ConditionalDirective
                        {
                            Type = ConditionalType.IfDef,
                            Condition = macroName,
                            LineNumber = lineNum + 1,
                            SourceFile = fileName,
                            Dependencies = new List<string> { macroName }
                        };

                        conditionalDirectives.Add(directive);
                        conditionalStack.Push(directive);
                        continue;
                    }

                    // Check for #ifndef
                    var ifndefMatch = ifndefRegex.Match(line);
                    if (ifndefMatch.Success)
                    {
                        string macroName = ifndefMatch.Groups[1].Value;
                        var directive = new ConditionalDirective
                        {
                            Type = ConditionalType.IfNDef,
                            Condition = macroName,
                            LineNumber = lineNum + 1,
                            SourceFile = fileName,
                            Dependencies = new List<string> { macroName }
                        };

                        conditionalDirectives.Add(directive);
                        conditionalStack.Push(directive);
                        continue;
                    }

                    // Check for #if
                    var ifMatch = ifRegex.Match(line);
                    if (ifMatch.Success)
                    {
                        string condition = ifMatch.Groups[1].Value;
                        var directive = new ConditionalDirective
                        {
                            Type = ConditionalType.If,
                            Condition = condition,
                            LineNumber = lineNum + 1,
                            SourceFile = fileName,
                            Dependencies = ExtractDependenciesFromCondition(condition)
                        };

                        conditionalDirectives.Add(directive);
                        conditionalStack.Push(directive);
                        continue;
                    }

                    // Check for #elif
                    var elifMatch = elifRegex.Match(line);
                    if (elifMatch.Success)
                    {
                        if (conditionalStack.Count > 0)
                        {
                            string condition = elifMatch.Groups[1].Value;
                            var parentDirective = conditionalStack.Peek();

                            var directive = new ConditionalDirective
                            {
                                Type = ConditionalType.ElseIf,
                                Condition = condition,
                                LineNumber = lineNum + 1,
                                SourceFile = fileName,
                                ParentDirective = parentDirective,
                                Dependencies = ExtractDependenciesFromCondition(condition)
                            };

                            conditionalDirectives.Add(directive);
                            parentDirective.Branches.Add(directive);
                        }
                        continue;
                    }

                    // Check for #else
                    var elseMatch = elseRegex.Match(line);
                    if (elseMatch.Success)
                    {
                        if (conditionalStack.Count > 0)
                        {
                            var parentDirective = conditionalStack.Peek();

                            var directive = new ConditionalDirective
                            {
                                Type = ConditionalType.Else,
                                LineNumber = lineNum + 1,
                                SourceFile = fileName,
                                ParentDirective = parentDirective
                            };

                            conditionalDirectives.Add(directive);
                            parentDirective.Branches.Add(directive);
                        }
                        continue;
                    }

                    // Check for #endif
                    var endifMatch = endifRegex.Match(line);
                    if (endifMatch.Success)
                    {
                        if (conditionalStack.Count > 0)
                        {
                            var directive = conditionalStack.Pop();
                            directive.EndLineNumber = lineNum + 1;
                        }
                        continue;
                    }
                }

                _logger.LogInformation($"Extracted {conditionalDirectives.Count} conditional directives from {fileName}");

                return conditionalDirectives;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting conditional directives from {fileName}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<IncludeDirective>> ExtractIncludeDirectivesAsync(string sourceCode, string fileName)
        {
            try
            {
                _logger.LogInformation($"Extracting include directives from {fileName}");

                var includeDirectives = new List<IncludeDirective>();

                // Split the source code into lines
                string[] lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Regular expressions for matching include directives
                var includeRegex = new Regex(@"^\s*#\s*include\s+[<""]([^>""]+)[>""]");

                for (int lineNum = 0; lineNum < lines.Length; lineNum++)
                {
                    string line = lines[lineNum];

                    var includeMatch = includeRegex.Match(line);
                    if (includeMatch.Success)
                    {
                        string includePath = includeMatch.Groups[1].Value;
                        bool isSystemInclude = line.Contains('<') && line.Contains('>');

                        var directive = new IncludeDirective
                        {
                            FilePath = includePath,
                            LineNumber = lineNum + 1,
                            IsSystemInclude = isSystemInclude
                        };

                        includeDirectives.Add(directive);
                    }
                }

                _logger.LogInformation($"Extracted {includeDirectives.Count} include directives from {fileName}");

                return includeDirectives;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting include directives from {fileName}");
                throw;
            }
        }

        private async Task ProcessTokensAsync(CXToken[] tokens, CXTranslationUnit translationUnit, string sourceFileName, PreprocessorResult result)
        {
            // Create a map to track the current conditional directive stack
            var conditionalStack = new Stack<ConditionalDirective>();
            Dictionary<string, int> definitionLineMap = new Dictionary<string, int>();

            // Extract includes
            var includes = await ExtractIncludesAsync(translationUnit, sourceFileName);
            result.Includes.AddRange(includes);

            // Extract definitions
            var definitions = await ExtractDefinitionsAsync(translationUnit, sourceFileName, definitionLineMap);
            result.Definitions.AddRange(definitions);

            // Extract conditional directives
            var conditionals = await ExtractConditionalsAsync(translationUnit, sourceFileName, conditionalStack, definitionLineMap);
            result.ConditionalDirectives.AddRange(conditionals);
        }

        private async Task<List<IncludeDirective>> ExtractIncludesAsync(CXTranslationUnit translationUnit, string sourceFileName)
        {
            var includes = new List<IncludeDirective>();

            unsafe
            {
                // Use the ClangSharp cursor visitor to find includes
                translationUnit.Cursor.VisitChildren((cursor, parent, clientData) =>
                {
                    if (cursor.Kind == CXCursorKind.CXCursor_InclusionDirective)
                    {
                        string includedFile = cursor.DisplayName.ToString();
                        cursor.Location.GetFileLocation(out var file, out uint line, out uint column, out _);
                        if (file != null && Path.GetFileName(file.Name.ToString()) == sourceFileName)
                        {
                            includes.Add(new IncludeDirective
                            {
                                FilePath = includedFile,
                                LineNumber = (int)line,
                                IsSystemInclude = includedFile.StartsWith('<') && includedFile.EndsWith('>')
                            });
                        }
                    }

                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));
            }
            

            await Task.CompletedTask;

            return includes;
        }

        private async Task<List<CDefinition>> ExtractDefinitionsAsync(CXTranslationUnit translationUnit, string sourceFileName, Dictionary<string, int> definitionLineMap)
        {
            var definitions = new List<CDefinition>();

            // Visit preprocessor nodes to find macro definitions
            unsafe
            {
                translationUnit.Cursor.VisitChildren((cursor, parent, clientData) =>
                {
                    if (cursor.Kind == CXCursorKind.CXCursor_MacroDefinition)
                    {
                        string macroName = cursor.Spelling.ToString();
                        cursor.Location.GetFileLocation(out var file, out uint line, out uint column, out _);

                        if (file != null && Path.GetFileName(file.Name.ToString()) == sourceFileName)
                        {
                            // Get the tokens for this macro to extract its value
                            // Lấy phạm vi (extent) của cursor
                            CXSourceRange extent = cursor.Extent;
                            // Lấy tokens từ extent
                            CXToken[] macroTokens = translationUnit.Tokenize(extent).ToArray();

                            string macroValue = null;
                            bool isFunctionLike = false;
                            List<string> parameters = null;

                            if (macroTokens.Length > 1)
                            {
                                // Check if this is a function-like macro
                                isFunctionLike = macroTokens.Any(t => t.GetSpelling(translationUnit).ToString() == "(");

                                if (isFunctionLike)
                                {
                                    parameters = ExtractMacroParameters(macroTokens, translationUnit);
                                }

                                // Extract the macro value
                                macroValue = ExtractMacroValue(macroTokens, translationUnit);
                            }

                            var definition = new CDefinition
                            {
                                Name = macroName,
                                Value = macroValue,
                                LineNumber = (int)line,
                                ColumnNumber = (int)column,
                                SourceFile = sourceFileName,
                                IsFunctionLike = isFunctionLike,
                                Parameters = parameters,
                                DefinitionType = isFunctionLike ? DefinitionType.MacroFunction : DefinitionType.MacroConstant
                            };

                            definitions.Add(definition);

                            // Track the line number of this definition
                            if (!definitionLineMap.ContainsKey(definition.Name))
                            {
                                definitionLineMap.Add(definition.Name, (int)line);
                            }
                        }
                    }

                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));
            }


            await Task.CompletedTask;

            return definitions;
        }

        private List<string> ExtractMacroParameters(CXToken[] tokens, CXTranslationUnit translationUnit)
        {
            var parameters = new List<string>();
            bool inParameters = false;

            foreach (var token in tokens)
            {
                string spelling = token.GetSpelling(translationUnit).ToString();

                if (spelling == "(")
                {
                    inParameters = true;
                    continue;
                }

                if (inParameters)
                {
                    if (spelling == ")")
                    {
                        break;
                    }

                    if (spelling != ",")
                    {
                        parameters.Add(spelling);
                    }
                }
            }

            return parameters;
        }

        private string ExtractMacroValue(CXToken[] tokens, CXTranslationUnit translationUnit)
        {
            // Skip the macro name and any parameters
            bool foundRightParen = false;
            int startIndex = 1; // Skip the macro name

            for (int i = startIndex; i < tokens.Length; i++)
            {
                string spelling = tokens[i].GetSpelling(translationUnit).ToString();

                if (spelling == "(")
                {
                    // Skip to the closing parenthesis
                    while (i < tokens.Length && tokens[i].GetSpelling(translationUnit).ToString() != ")")
                    {
                        i++;
                    }

                    if (i < tokens.Length)
                    {
                        foundRightParen = true;
                        startIndex = i + 1;
                        break;
                    }
                }
                else
                {
                    // This is a simple macro, not function-like
                    startIndex = i;
                    break;
                }
            }

            if (foundRightParen || startIndex > 1)
            {
                // For function-like macros, start after the closing parenthesis
                // For simple macros, start after the macro name

                if (startIndex < tokens.Length)
                {
                    // Combine all remaining tokens as the macro value
                    return string.Join(" ", tokens.Skip(startIndex).Select(t => t.GetSpelling(translationUnit).ToString()));
                }
            }

            return null;
        }

        private async Task<List<ConditionalDirective>> ExtractConditionalsAsync(
            CXTranslationUnit translationUnit,
            string sourceFileName,
            Stack<ConditionalDirective> conditionalStack,
            Dictionary<string, int> definitionLineMap)
        {
            var conditionals = new List<ConditionalDirective>();

            // We need to manually parse the file to extract conditional directives
            string filePath = translationUnit.Spelling.ToString();
            if (!_fileService.FileExists(filePath))
            {
                _logger.LogWarning($"Source file not found for conditional directive extraction: {filePath}");
                return conditionals;
            }

            string content = await _fileService.ReadFileAsync(filePath);
            string[] lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Regular expressions for matching conditional directives
            var ifdefRegex = new Regex(@"^\s*#\s*ifdef\s+(\w+)");
            var ifndefRegex = new Regex(@"^\s*#\s*ifndef\s+(\w+)");
            var ifRegex = new Regex(@"^\s*#\s*if\s+(.+)");
            var elifRegex = new Regex(@"^\s*#\s*elif\s+(.+)");
            var elseRegex = new Regex(@"^\s*#\s*else");
            var endifRegex = new Regex(@"^\s*#\s*endif");

            for (int lineNum = 0; lineNum < lines.Length; lineNum++)
            {
                string line = lines[lineNum];

                // Check for #ifdef
                var ifdefMatch = ifdefRegex.Match(line);
                if (ifdefMatch.Success)
                {
                    string macroName = ifdefMatch.Groups[1].Value;
                    var directive = new ConditionalDirective
                    {
                        Type = ConditionalType.IfDef,
                        Condition = macroName,
                        LineNumber = lineNum + 1,
                        SourceFile = sourceFileName,
                        Dependencies = new List<string> { macroName }
                    };

                    conditionals.Add(directive);
                    conditionalStack.Push(directive);
                    continue;
                }

                // Check for #ifndef
                var ifndefMatch = ifndefRegex.Match(line);
                if (ifndefMatch.Success)
                {
                    string macroName = ifndefMatch.Groups[1].Value;
                    var directive = new ConditionalDirective
                    {
                        Type = ConditionalType.IfNDef,
                        Condition = macroName,
                        LineNumber = lineNum + 1,
                        SourceFile = sourceFileName,
                        Dependencies = new List<string> { macroName }
                    };

                    conditionals.Add(directive);
                    conditionalStack.Push(directive);
                    continue;
                }

                // Check for #if
                var ifMatch = ifRegex.Match(line);
                if (ifMatch.Success)
                {
                    string condition = ifMatch.Groups[1].Value;
                    var directive = new ConditionalDirective
                    {
                        Type = ConditionalType.If,
                        Condition = condition,
                        LineNumber = lineNum + 1,
                        SourceFile = sourceFileName,
                        Dependencies = ExtractDependenciesFromCondition(condition)
                    };

                    conditionals.Add(directive);
                    conditionalStack.Push(directive);
                    continue;
                }

                // Check for #elif
                var elifMatch = elifRegex.Match(line);
                if (elifMatch.Success)
                {
                    if (conditionalStack.Count > 0)
                    {
                        string condition = elifMatch.Groups[1].Value;
                        var parentDirective = conditionalStack.Peek();

                        var directive = new ConditionalDirective
                        {
                            Type = ConditionalType.ElseIf,
                            Condition = condition,
                            LineNumber = lineNum + 1,
                            SourceFile = sourceFileName,
                            ParentDirective = parentDirective,
                            Dependencies = ExtractDependenciesFromCondition(condition)
                        };

                        conditionals.Add(directive);
                        parentDirective.Branches.Add(directive);
                    }
                    continue;
                }

                // Check for #else
                var elseMatch = elseRegex.Match(line);
                if (elseMatch.Success)
                {
                    if (conditionalStack.Count > 0)
                    {
                        var parentDirective = conditionalStack.Peek();

                        var directive = new ConditionalDirective
                        {
                            Type = ConditionalType.Else,
                            LineNumber = lineNum + 1,
                            SourceFile = sourceFileName,
                            ParentDirective = parentDirective
                        };

                        conditionals.Add(directive);
                        parentDirective.Branches.Add(directive);
                    }
                    continue;
                }

                // Check for #endif
                var endifMatch = endifRegex.Match(line);
                if (endifMatch.Success)
                {
                    if (conditionalStack.Count > 0)
                    {
                        var directive = conditionalStack.Pop();
                        directive.EndLineNumber = lineNum + 1;
                    }
                    continue;
                }
            }

            return conditionals;
        }

        private List<string> ExtractDependenciesFromCondition(string condition)
        {
            var dependencies = new List<string>();

            // Simple parsing to extract macro names from condition
            // This is a simplified approach - a real parser would be more sophisticated

            // Replace operators with spaces to isolate identifiers
            string sanitized = Regex.Replace(condition, @"[!&|^~<>=\+\-\*/\(\)%]", " ");

            // Split into tokens
            string[] tokens = sanitized.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                // Skip numeric literals
                if (Regex.IsMatch(token, @"^\d+$") ||
                    Regex.IsMatch(token, @"^0x[0-9a-fA-F]+$") ||
                    Regex.IsMatch(token, @"^0[0-7]+$"))
                {
                    continue;
                }

                // Skip the "defined" keyword
                if (token == "defined")
                {
                    continue;
                }

                // Add potential macro names
                if (Regex.IsMatch(token, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                {
                    dependencies.Add(token);
                }
            }

            return dependencies;
        }

        private bool EvaluateConditionExpression(string condition, Dictionary<string, string> activeDefinitions)
        {
            // This is a simplified evaluator - a real implementation would use a proper expression parser

            // Replace defined(X) with 1 or 0
            condition = Regex.Replace(condition, @"defined\s*\(\s*(\w+)\s*\)", match =>
            {
                string macroName = match.Groups[1].Value;
                return activeDefinitions.ContainsKey(macroName) ? "1" : "0";
            });

            // Replace defined X with 1 or 0
            condition = Regex.Replace(condition, @"defined\s+(\w+)", match =>
            {
                string macroName = match.Groups[1].Value;
                return activeDefinitions.ContainsKey(macroName) ? "1" : "0";
            });

            // Replace macro names with their values
            foreach (var macro in activeDefinitions)
            {
                string pattern = $@"\b{Regex.Escape(macro.Key)}\b";
                string value = string.IsNullOrEmpty(macro.Value) ? "1" : macro.Value;
                condition = Regex.Replace(condition, pattern, value);
            }

            // Replace remaining macro names (not in activeDefinitions) with 0
            condition = Regex.Replace(condition, @"\b[a-zA-Z_][a-zA-Z0-9_]*\b", "0");

            try
            {
                // This is a security risk in a real application - would need a proper expression evaluator
                // For a real implementation, use a safe expression evaluator or parser
                return Convert.ToBoolean(Evaluate(condition));
            }
            catch
            {
                // If evaluation fails, assume the condition is false
                return false;
            }
        }

        private int Evaluate(string expression)
        {
            // This is a very simplified evaluator that only handles basic expressions
            // A real implementation would use a proper expression evaluator

            // Replace logical operators with bitwise operators
            expression = expression.Replace("&&", "&").Replace("||", "|");

            // Evaluate the expression
            // This is a security risk in a real application
            // For a real implementation, use a safe expression evaluator
            return 0; // Placeholder - would be a real evaluation in a full implementation
        }
    }

    #endregion
}
