using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Core;
using C_TestForge.Models.Core.Enumerations;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
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
    /// Implementation of the preprocessor service for analyzing C preprocessor directives
    /// </summary>
    public class PreprocessorService : IPreprocessorService
    {
        private readonly ILogger<PreprocessorService> _logger;
        private readonly IFileService _fileService;

        /// <summary>
        /// Constructor for PreprocessorService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="fileService">File service for reading source files</param>
        public PreprocessorService(
            ILogger<PreprocessorService> logger,
            IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        /// <inheritdoc/>
        public async Task<string> ExtractPreprocessorDefinitionsAsync(CXTranslationUnit translationUnit, List<SourceFile> sourceFiles, SourceFile sourceFile)
        {

            try
            {
                // Process the file contents to extract all preprocessor directives
                if (translationUnit.Handle == IntPtr.Zero)
                {
                    _logger.LogError("Invalid translation unit");
                    return "0";
                }

                // Extract preprocessor definitions from the translation unit
                unsafe
                {
                    CXCursor cursor = clang.getTranslationUnitCursor(translationUnit);

                    // Create a visitor to find preprocessor definitions
                    cursor.VisitChildren((child, parent, clientData) =>
                    {
                        if (child.Kind == CXCursorKind.CXCursor_MacroDefinition)
                        {
                            var definition = ExtractDefinition(child, sourceFile.FileName);
                            if (definition != null)
                            {
                                sourceFile.ParseResult.Definitions.Add(definition);
                            }
                        }

                        return CXChildVisitResult.CXChildVisit_Continue;
                    }, default(CXClientData));
                }

                // Extract conditional directives from source code
                sourceFile.ParseResult.ConditionalDirectives = await ExtractConditionalDirectivesAsync(sourceFile.Content, sourceFile.FileName);

                // Process relationships between definitions and conditionals
                // Sau khi thực hiện xong các file thì sẽ tiến hành phân tích phụ thuộc giữa các macro và chỉ thị điều kiện
                await AnalyzeMacroRelationshipsAsync(sourceFiles);

                _logger.LogInformation($"Extracted {sourceFile.ParseResult.Definitions.Count} definitions, {sourceFile.ParseResult.ConditionalDirectives.Count} conditionals");

                return "1";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting preprocessor definitions: {ex.Message}");
                return "0";
            }
        }

        /// <summary>
        /// Extracts a definition from a Clang cursor
        /// </summary>
        /// <param name="cursor">Macro definition cursor</param>
        /// <param name="sourceFileName">Source file name</param>
        /// <returns>Extracted definition or null if extraction failed</returns>
        private unsafe CDefinition ExtractDefinition(CXCursor cursor, string sourceFileName)
        {
            try
            {
                string macroName = cursor.Spelling.ToString();

                // Get definition location
                CXFile file;
                uint line, column, offset;
                cursor.Location.GetFileLocation(out file, out line, out column, out offset);

                // Skip if the definition is not from the source file we're analyzing
                string definitionFile = file != null ? file.Name.ToString() : string.Empty;
                if (!definitionFile.EndsWith(sourceFileName))
                {
                    return null;
                }

                // Get definition value and check if it's a function-like macro
                string macroValue = GetMacroValue(cursor);
                bool isFunctionLike = IsFunctionLikeMacro(cursor);

                // Extract parameters for function-like macros
                List<string> parameters = new List<string>();
                if (isFunctionLike)
                {
                    parameters = ExtractMacroParameters(cursor);
                }

                // Create definition object
                var definition = new CDefinition
                {
                    Name = macroName,
                    Value = macroValue,
                    IsFunctionLike = isFunctionLike,
                    Parameters = parameters,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    SourceFile = sourceFileName,
                    DefinitionType = isFunctionLike ? DefinitionType.FunctionMacro : DefinitionType.Constant,
                    IsEnabled = true // Assume enabled by default
                };

                return definition;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting definition: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the value of a macro definition
        /// </summary>
        /// <param name="cursor">Macro definition cursor</param>
        /// <returns>The macro value as a string</returns>
        private string GetMacroValue(CXCursor cursor)
        {
            // Getting macro value is a bit tricky with ClangSharp
            // We need to use the extent to get the full text and then extract the value
            var extent = cursor.Extent;
            string fullText = extent.ToString();

            // Extract value from full text (e.g., "#define MAX(a, b) ((a) > (b) ? (a) : (b))")
            int defineIndex = fullText.IndexOf("#define");
            if (defineIndex < 0)
            {
                return string.Empty;
            }

            string afterDefine = fullText.Substring(defineIndex + "#define".Length).TrimStart();
            string macroName = cursor.Spelling.ToString();

            // Check if it's a function-like macro
            if (afterDefine.StartsWith(macroName + "("))
            {
                int closingParen = afterDefine.IndexOf(')', macroName.Length);
                if (closingParen >= 0)
                {
                    return afterDefine.Substring(closingParen + 1).TrimStart();
                }
            }
            else if (afterDefine.StartsWith(macroName))
            {
                // Simple macro
                return afterDefine.Substring(macroName.Length).TrimStart();
            }

            return string.Empty;
        }

        /// <summary>
        /// Checks if a macro is a function-like macro
        /// </summary>
        /// <param name="cursor">Macro definition cursor</param>
        /// <returns>True if the macro is function-like, false otherwise</returns>
        private bool IsFunctionLikeMacro(CXCursor cursor)
        {
            var extent = cursor.Extent;
            string fullText = extent.ToString();

            int defineIndex = fullText.IndexOf("#define");
            if (defineIndex < 0)
            {
                return false;
            }

            string afterDefine = fullText.Substring(defineIndex + "#define".Length).TrimStart();
            string macroName = cursor.Spelling.ToString();

            return afterDefine.StartsWith(macroName + "(");
        }

        /// <summary>
        /// Extracts parameters from a function-like macro
        /// </summary>
        /// <param name="cursor">Macro definition cursor</param>
        /// <returns>List of parameter names</returns>
        private List<string> ExtractMacroParameters(CXCursor cursor)
        {
            var parameters = new List<string>();

            var extent = cursor.Extent;
            string fullText = extent.ToString();

            int defineIndex = fullText.IndexOf("#define");
            if (defineIndex < 0)
            {
                return parameters;
            }

            string afterDefine = fullText.Substring(defineIndex + "#define".Length).TrimStart();
            string macroName = cursor.Spelling.ToString();

            if (afterDefine.StartsWith(macroName + "("))
            {
                int openParen = afterDefine.IndexOf('(', macroName.Length);
                int closeParen = afterDefine.IndexOf(')', openParen);

                if (openParen >= 0 && closeParen > openParen)
                {
                    string paramText = afterDefine.Substring(openParen + 1, closeParen - openParen - 1);
                    string[] paramArray = paramText.Split(',').Select(p => p.Trim()).ToArray();
                    parameters.AddRange(paramArray);
                }
            }

            return parameters;
        }

        /// <inheritdoc/>
        public async Task<List<ConditionalDirective>> ExtractConditionalDirectivesAsync(string sourceCode, string fileName)
        {
            _logger.LogInformation($"Extracting conditional directives from {fileName}");

            var conditionalDirectives = new List<ConditionalDirective>();
            var conditionalStack = new Stack<ConditionalDirective>();

            try
            {
                // Split source code into lines
                string[] lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Regular expressions for conditional directives
                var ifdefRegex = new Regex(@"^\s*#\s*ifdef\s+(\w+)");
                var ifndefRegex = new Regex(@"^\s*#\s*ifndef\s+(\w+)");
                var ifRegex = new Regex(@"^\s*#\s*if\s+(.+)$");
                var elifRegex = new Regex(@"^\s*#\s*elif\s+(.+)$");
                var elseRegex = new Regex(@"^\s*#\s*else");
                var endifRegex = new Regex(@"^\s*#\s*endif");

                // Process each line
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
                    if (elifMatch.Success && conditionalStack.Count > 0)
                    {
                        string condition = elifMatch.Groups[1].Value;
                        var parentDirective = conditionalStack.Peek();

                        var directive = new ConditionalDirective
                        {
                            Type = ConditionalType.ElseIf,
                            Condition = condition,
                            LineNumber = lineNum + 1,
                            SourceFile = fileName,
                            Dependencies = ExtractDependenciesFromCondition(condition),
                            ParentDirective = parentDirective
                        };

                        parentDirective.Branches.Add(directive);
                        conditionalDirectives.Add(directive);
                        continue;
                    }

                    // Check for #else
                    var elseMatch = elseRegex.Match(line);
                    if (elseMatch.Success && conditionalStack.Count > 0)
                    {
                        var parentDirective = conditionalStack.Peek();

                        var directive = new ConditionalDirective
                        {
                            Type = ConditionalType.Else,
                            LineNumber = lineNum + 1,
                            SourceFile = fileName,
                            ParentDirective = parentDirective
                        };

                        parentDirective.Branches.Add(directive);
                        conditionalDirectives.Add(directive);
                        continue;
                    }

                    // Check for #endif
                    var endifMatch = endifRegex.Match(line);
                    if (endifMatch.Success && conditionalStack.Count > 0)
                    {
                        var directive = conditionalStack.Pop();
                        directive.EndLineNumber = lineNum + 1;
                        continue;
                    }
                }

                // Handle any remaining unclosed conditionals
                while (conditionalStack.Count > 0)
                {
                    var directive = conditionalStack.Pop();
                    directive.EndLineNumber = lines.Length;
                }

                _logger.LogInformation($"Extracted {conditionalDirectives.Count} conditional directives");

                return conditionalDirectives;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting conditional directives: {ex.Message}");
                return conditionalDirectives;
            }
        }

        /// <inheritdoc/>
        public async Task<List<IncludeDirective>> ExtractIncludeDirectivesAsync(string sourceCode, string fileName)
        {
            _logger.LogInformation($"Extracting include directives from {fileName}");

            var includeDirectives = new List<IncludeDirective>();

            try
            {
                // Split source code into lines
                string[] lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                // Regular expressions for include directives
                var includeRegex = new Regex(@"^\s*#\s*include\s+[<""]([^>""]*)[\>""]");

                // Process each line
                for (int lineNum = 0; lineNum < lines.Length; lineNum++)
                {
                    string line = lines[lineNum];

                    // Check for #include
                    var includeMatch = includeRegex.Match(line);
                    if (includeMatch.Success)
                    {
                        string includePath = includeMatch.Groups[1].Value;
                        bool isSystemInclude = line.Contains("<" + includePath + ">");

                        var directive = new IncludeDirective
                        {
                            FilePath = includePath,
                            IsSystemInclude = isSystemInclude,
                            LineNumber = lineNum + 1,
                            //SourceFile = fileName
                        };

                        includeDirectives.Add(directive);
                    }
                }

                _logger.LogInformation($"Extracted {includeDirectives.Count} include directives");

                return includeDirectives;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting include directives: {ex.Message}");
                return includeDirectives;
            }
        }

        /// <inheritdoc/>
        public bool IsDefinitionEnabled(CDefinition definition, Dictionary<string, string> activeDefinitions)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            // Check if the definition depends on any other definitions
            foreach (var dependency in definition.Dependencies)
            {
                // If the dependency is not defined or has a different value, the definition is disabled
                if (!activeDefinitions.ContainsKey(dependency))
                {
                    return false;
                }
            }

            // The definition is enabled if it's in the active definitions
            if (activeDefinitions.ContainsKey(definition.Name))
            {
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

                    return false;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Evaluates a conditional expression
        /// </summary>
        /// <param name="expression">Expression to evaluate</param>
        /// <param name="activeDefinitions">Dictionary of active definitions</param>
        /// <returns>True if the condition is satisfied, false otherwise</returns>
        private bool EvaluateConditionExpression(string expression, Dictionary<string, string> activeDefinitions)
        {
            try
            {
                // This is a simplistic approach for demonstration
                // A real implementation would need to parse and evaluate C preprocessor expressions

                // Replace defined() expressions
                expression = Regex.Replace(expression, @"defined\s*\(\s*(\w+)\s*\)", match =>
                {
                    string macroName = match.Groups[1].Value;
                    return activeDefinitions.ContainsKey(macroName) ? "1" : "0";
                });

                // Replace macro names with their values
                foreach (var kvp in activeDefinitions)
                {
                    // Only replace whole words, not parts of other words
                    expression = Regex.Replace(expression, $@"\b{kvp.Key}\b", kvp.Value);
                }

                // Replace any remaining macro names with 0 (assuming they're undefined)
                expression = Regex.Replace(expression, @"\b[A-Za-z_]\w*\b", "0");

                // Simple expression evaluation (very basic, for demonstration only)
                // In a real implementation, you would use a proper expression evaluator

                // Replace common operators
                expression = expression.Replace("&&", " and ").Replace("||", " or ");
                expression = expression.Replace("==", " == ").Replace("!=", " != ");
                expression = expression.Replace(">", " > ").Replace("<", " < ");
                expression = expression.Replace(">=", " >= ").Replace("<=", " <= ");

                // TODO: Implement a more robust expression evaluator
                // For now, just handle some very simple cases

                // Handle simple equality
                if (expression.Contains("=="))
                {
                    string[] parts = expression.Split(new[] { "==" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        int left = int.Parse(parts[0].Trim());
                        int right = int.Parse(parts[1].Trim());
                        return left == right;
                    }
                }

                // Handle simple inequality
                if (expression.Contains("!="))
                {
                    string[] parts = expression.Split(new[] { "!=" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        int left = int.Parse(parts[0].Trim());
                        int right = int.Parse(parts[1].Trim());
                        return left != right;
                    }
                }

                // Handle basic numeric comparison
                if (int.TryParse(expression.Trim(), out int value))
                {
                    return value != 0;
                }

                // Default to false for complex expressions we can't evaluate
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error evaluating condition expression: {expression}");
                return false;
            }
        }

        /// <summary>
        /// Extracts dependencies from a condition expression
        /// </summary>
        /// <param name="condition">Condition expression</param>
        /// <returns>List of dependencies</returns>
        private List<string> ExtractDependenciesFromCondition(string condition)
        {
            var dependencies = new List<string>();

            try
            {
                // Extract defined() expressions
                var definedRegex = new Regex(@"defined\s*\(\s*(\w+)\s*\)");
                var definedMatches = definedRegex.Matches(condition);

                foreach (Match match in definedMatches)
                {
                    string macroName = match.Groups[1].Value;
                    if (!dependencies.Contains(macroName))
                    {
                        dependencies.Add(macroName);
                    }
                }

                // Extract other macro names (very basic approach)
                var macroRegex = new Regex(@"\b([A-Za-z_]\w*)\b");
                var macroMatches = macroRegex.Matches(condition);

                foreach (Match match in macroMatches)
                {
                    string macroName = match.Groups[1].Value;

                    // Skip C keywords and literals
                    if (!IsKeywordOrLiteral(macroName) && !dependencies.Contains(macroName))
                    {
                        dependencies.Add(macroName);
                    }
                }

                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting dependencies from condition: {condition}");
                return dependencies;
            }
        }

        /// <summary>
        /// Checks if a string is a C keyword or literal
        /// </summary>
        /// <param name="word">Word to check</param>
        /// <returns>True if the word is a C keyword or literal, false otherwise</returns>
        private bool IsKeywordOrLiteral(string word)
        {
            // List of common C keywords and literals
            string[] keywords = new[]
            {
                "if", "else", "for", "while", "do", "switch", "case", "default",
                "break", "continue", "return", "goto", "sizeof", "typedef", "struct",
                "union", "enum", "void", "char", "short", "int", "long", "float",
                "double", "signed", "unsigned", "const", "volatile", "auto", "register",
                "static", "extern", "true", "false", "NULL"
            };

            return keywords.Contains(word);
        }

        /// <summary>
        /// Analyzes relationships between macros and conditional directives
        /// Hàm này dùng để phân tích mối quan hệ phụ thuộc giữa các macro (định nghĩa tiền xử lý) 
        /// và các chỉ thị điều kiện (#if, #ifdef, ...) trong mã nguồn C. 
        /// Kết quả giúp xác định macro nào phụ thuộc vào macro nào
        /// và các chỉ thị điều kiện phụ thuộc vào những macro nào.
        /// </summary>
        /// <param name="definitions">List of definitions</param>
        /// <param name="conditionalDirectives">List of conditional directives</param>
        /// <returns>Task</returns>
        private async Task AnalyzeMacroRelationshipsAsync(List<SourceFile> sourceFiles)
        {
            try
            {
                var allDefinitions = sourceFiles
    .Where(f => f.ParseResult != null)
    .SelectMany(f => f.ParseResult.Definitions)
    .ToList();

                var allConditionals = sourceFiles
    .Where(f => f.ParseResult != null)
    .SelectMany(f => f.ParseResult.ConditionalDirectives)
    .ToList();

                _logger.LogInformation($"Analyzing relationships between {allDefinitions.Count} macros and {allConditionals.Count} conditional directives");

                // Build a dictionary for quick lookup
                var definitionDict = allDefinitions.ToDictionary(d => d.Name, d => d);

                foreach (var sourceFile in sourceFiles)
                {
                    // Đảm bảo ParseResult đã được khởi tạo và có dữ liệu
                    if (sourceFile.ParseResult != null)
                    {
                        // Analyze each definition for dependencies
                        foreach (var definition in sourceFile.ParseResult.Definitions)
                        {
                            _logger.LogDebug($"Analyzing dependencies for definition: {definition.Name}");

                            // Xóa danh sách phụ thuộc cũ
                            definition.Dependencies.Clear();

                            // Nếu macro có giá trị (Value), tách thành các token
                            if (!string.IsNullOrEmpty(definition.Value))
                            {
                                // This is a simplified approach - a real implementation would use a proper parser
                                string[] tokens = SplitIntoTokens(definition.Value);
                                // Kiểm tra từng token xem có phải là tên macro khác không
                                foreach (var token in tokens)
                                {
                                    if (definitionDict.TryGetValue(token, out var referencedDefinition))
                                    {
                                        // Nếu đúng là macro khác, thêm vào danh sách phụ thuộc
                                        _logger.LogDebug($"Definition {definition.Name} depends on {token}");
                                        definition.Dependencies.Add(token);
                                    }
                                }
                            }
                        }
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing macro relationships: {ex.Message}");
            }
        }

        /// <summary>
        /// Splits a string into tokens for analysis
        /// </summary>
        /// <param name="value">String to split</param>
        /// <returns>Array of tokens</returns>
        private string[] SplitIntoTokens(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return Array.Empty<string>();
            }

            // This is a very simplistic tokenizer for demonstration
            // A real implementation would use a proper C tokenizer/parser

            // Replace operators and punctuation with spaces
            string normalized = value
                .Replace("(", " ")
                .Replace(")", " ")
                .Replace("[", " ")
                .Replace("]", " ")
                .Replace("{", " ")
                .Replace("}", " ")
                .Replace("+", " ")
                .Replace("-", " ")
                .Replace("*", " ")
                .Replace("/", " ")
                .Replace("%", " ")
                .Replace("=", " ")
                .Replace("<", " ")
                .Replace(">", " ")
                .Replace("&", " ")
                .Replace("|", " ")
                .Replace("^", " ")
                .Replace("!", " ")
                .Replace("~", " ")
                .Replace(",", " ")
                .Replace(";", " ")
                .Replace(":", " ");

            // Split by whitespace and filter out empty strings
            return normalized.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Where(t => !IsKeywordOrLiteral(t))
                .Where(t => !t.All(c => char.IsDigit(c))) // Filter out numeric literals
                .ToArray();
        }
    }
}