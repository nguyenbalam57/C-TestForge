using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Implementation of the macro analysis service for analyzing macro relationships and dependencies
    /// </summary>
    public class MacroAnalysisService : IMacroAnalysisService
    {
        private readonly ILogger<MacroAnalysisService> _logger;
        private readonly IFileService _fileService;

        /// <summary>
        /// Constructor for MacroAnalysisService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="fileService">File service for accessing project files</param>
        public MacroAnalysisService(
            ILogger<MacroAnalysisService> logger,
            IFileService fileService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService;
        }

        /// <inheritdoc/>
        public async Task AnalyzeMacroRelationshipsAsync(List<CDefinition> definitions, List<ConditionalDirective> conditionalDirectives)
        {
            try
            {
                _logger.LogInformation($"Analyzing relationships between {definitions.Count} macros and {conditionalDirectives.Count} conditional directives");

                // Build a dictionary for quick lookup
                var definitionDict = definitions.ToDictionary(d => d.Name, d => d);

                // Analyze each definition for dependencies
                foreach (var definition in definitions)
                {
                    _logger.LogDebug($"Analyzing dependencies for definition: {definition.Name}");

                    // Clear existing dependencies
                    definition.Dependencies.Clear();

                    // Look for dependencies in the value
                    if (!string.IsNullOrEmpty(definition.Value))
                    {
                        // Tokenize the value and check for references to other macros
                        string[] tokens = SplitIntoTokens(definition.Value);

                        foreach (var token in tokens)
                        {
                            if (definitionDict.TryGetValue(token, out var referencedDefinition))
                            {
                                _logger.LogDebug($"Definition {definition.Name} depends on {token}");
                                definition.Dependencies.Add(token);
                            }
                        }
                    }
                }

                // Analyze each conditional directive for definition dependencies
                foreach (var directive in conditionalDirectives)
                {
                    _logger.LogDebug($"Analyzing dependencies for conditional directive at line {directive.LineNumber}");

                    // Check all dependencies from the condition
                    foreach (var dependency in directive.Dependencies)
                    {
                        if (definitionDict.TryGetValue(dependency, out var definition))
                        {
                            _logger.LogDebug($"Conditional directive at line {directive.LineNumber} depends on definition {dependency}");

                            // We could update the conditional directive here if needed
                        }
                    }
                }

                // Analyze each definition for circular dependencies
                var circularDependencies = FindCircularDependencies(definitions);
                if (circularDependencies.Count > 0)
                {
                    _logger.LogWarning($"Found {circularDependencies.Count} circular dependencies among macros");
                    foreach (var cycle in circularDependencies)
                    {
                        _logger.LogWarning($"Circular dependency: {string.Join(" -> ", cycle)} -> {cycle[0]}");
                    }
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing macro relationships: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public async Task<List<CDefinition>> ExtractMacroDependenciesAsync(CDefinition definition, List<CDefinition> allDefinitions)
        {
            try
            {
                _logger.LogInformation($"Extracting dependencies for macro: {definition.Name}");

                var dependencies = new List<CDefinition>();
                var dependencyNames = new HashSet<string>();

                // Build a dictionary for quick lookup
                var definitionDict = allDefinitions.ToDictionary(d => d.Name, d => d);

                // Add direct dependencies
                foreach (var depName in definition.Dependencies)
                {
                    if (definitionDict.TryGetValue(depName, out var depDefinition) && !dependencyNames.Contains(depName))
                    {
                        dependencies.Add(depDefinition);
                        dependencyNames.Add(depName);
                    }
                }

                // Add transitive dependencies (recursive)
                var processedDefs = new HashSet<string> { definition.Name };
                await AddTransitiveDependenciesAsync(dependencies, dependencyNames, definitionDict, processedDefs);

                _logger.LogInformation($"Found {dependencies.Count} dependencies for macro {definition.Name}");

                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting macro dependencies for {definition.Name}: {ex.Message}");
                return new List<CDefinition>();
            }
        }

        /// <inheritdoc/>
        public async Task<string> EvaluateMacroExpressionAsync(string expression, Dictionary<string, string> activeDefinitions)
        {
            try
            {
                _logger.LogInformation($"Evaluating macro expression: {expression}");

                // Replace defined() expressions
                var definedRegex = new Regex(@"defined\s*\(\s*(\w+)\s*\)");
                expression = definedRegex.Replace(expression, match =>
                {
                    string macroName = match.Groups[1].Value;
                    return activeDefinitions.ContainsKey(macroName) ? "1" : "0";
                });

                // Replace macro names with their values
                foreach (var kvp in activeDefinitions)
                {
                    // Only replace whole words, not parts of other words
                    expression = Regex.Replace(expression, $@"\b{Regex.Escape(kvp.Key)}\b", kvp.Value);
                }

                // Replace any remaining macro names with 0 (assuming they're undefined)
                expression = Regex.Replace(expression, @"\b[A-Za-z_]\w*\b", "0");

                // Handle common operators
                expression = expression.Replace("&&", " && ").Replace("||", " || ");
                expression = expression.Replace("==", " == ").Replace("!=", " != ");
                expression = expression.Replace(">", " > ").Replace("<", " < ");
                expression = expression.Replace(">=", " >= ").Replace("<=", " <= ");

                // Evaluate the simplified expression
                // This is a very basic evaluator, a real implementation would use a proper expression evaluator
                string result = await EvaluateSimplifiedExpressionAsync(expression);

                _logger.LogInformation($"Evaluated expression '{expression}' to '{result}'");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error evaluating macro expression '{expression}': {ex.Message}");
                return "0"; // Default to false for errors
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> FindMacroUsagesAsync(CDefinition definition, Project projectContext)
        {
            try
            {
                _logger.LogInformation($"Finding usages of macro: {definition.Name}");

                if (projectContext == null || _fileService == null)
                {
                    _logger.LogWarning("Cannot find macro usages: project context or file service is null");
                    return new List<string>();
                }

                var usageFiles = new List<string>();

                // Get all source files in the project
                var sourceFiles = projectContext.SourceFiles;

                foreach (var file in sourceFiles)
                {
                    string content = await _fileService.ReadFileAsync(file);

                    // Check for macro usage
                    if (IsMacroUsedInFile(definition, content))
                    {
                        usageFiles.Add(file);
                    }
                }

                _logger.LogInformation($"Found {usageFiles.Count} files that use macro {definition.Name}");

                return usageFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding macro usages for {definition.Name}: {ex.Message}");
                return new List<string>();
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

            // Split by whitespace and filter out empty strings and C keywords
            return normalized.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Where(t => !IsKeywordOrLiteral(t))
                .Where(t => !t.All(c => char.IsDigit(c))) // Filter out numeric literals
                .ToArray();
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
        /// Finds circular dependencies among macro definitions
        /// </summary>
        /// <param name="definitions">List of macro definitions</param>
        /// <returns>List of circular dependency chains</returns>
        private List<List<string>> FindCircularDependencies(List<CDefinition> definitions)
        {
            var result = new List<List<string>>();
            var definitionDict = definitions.ToDictionary(d => d.Name, d => d);

            foreach (var definition in definitions)
            {
                var visited = new HashSet<string>();
                var path = new List<string>();

                FindCycles(definition.Name, definitionDict, visited, path, result);
            }

            return result;
        }

        /// <summary>
        /// Recursively finds cycles in the dependency graph using DFS
        /// </summary>
        /// <param name="current">Current macro name</param>
        /// <param name="definitions">Dictionary of definitions</param>
        /// <param name="visited">Set of visited macros in the current search</param>
        /// <param name="path">Current path being explored</param>
        /// <param name="cycles">List of found cycles</param>
        private void FindCycles(
            string current,
            Dictionary<string, CDefinition> definitions,
            HashSet<string> visited,
            List<string> path,
            List<List<string>> cycles)
        {
            if (!definitions.ContainsKey(current))
            {
                return;
            }

            if (visited.Contains(current))
            {
                // Found a cycle
                int cycleStart = path.IndexOf(current);
                if (cycleStart >= 0)
                {
                    var cycle = new List<string>();
                    for (int i = cycleStart; i < path.Count; i++)
                    {
                        cycle.Add(path[i]);
                    }

                    // Check if this cycle is already in the result
                    if (!cycles.Any(c =>
                        c.Count == cycle.Count &&
                        !c.Except(cycle).Any()))
                    {
                        cycles.Add(cycle);
                    }
                }
                return;
            }

            visited.Add(current);
            path.Add(current);

            foreach (var dep in definitions[current].Dependencies)
            {
                FindCycles(dep, definitions, visited, path, cycles);
            }

            path.RemoveAt(path.Count - 1);
            visited.Remove(current);
        }

        /// <summary>
        /// Adds transitive dependencies to the dependency list
        /// </summary>
        /// <param name="dependencies">List of dependencies to update</param>
        /// <param name="dependencyNames">Set of dependency names for quick lookup</param>
        /// <param name="definitionDict">Dictionary of all definitions</param>
        /// <param name="processedDefs">Set of already processed definitions to avoid cycles</param>
        /// <returns>Task</returns>
        private async Task AddTransitiveDependenciesAsync(
            List<CDefinition> dependencies,
            HashSet<string> dependencyNames,
            Dictionary<string, CDefinition> definitionDict,
            HashSet<string> processedDefs)
        {
            // Process all current dependencies
            for (int i = 0; i < dependencies.Count; i++)
            {
                var dependency = dependencies[i];

                // Skip if already processed to avoid cycles
                if (processedDefs.Contains(dependency.Name))
                {
                    continue;
                }

                processedDefs.Add(dependency.Name);

                // Add dependencies of this dependency
                foreach (var subDepName in dependency.Dependencies)
                {
                    if (definitionDict.TryGetValue(subDepName, out var subDep) &&
                        !dependencyNames.Contains(subDepName) &&
                        !processedDefs.Contains(subDepName))
                    {
                        dependencies.Add(subDep);
                        dependencyNames.Add(subDepName);
                    }
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Evaluates a simplified preprocessor expression
        /// </summary>
        /// <param name="expression">Expression to evaluate</param>
        /// <returns>Result of the evaluation</returns>
        private async Task<string> EvaluateSimplifiedExpressionAsync(string expression)
        {
            try
            {
                // This is a very basic expression evaluator
                // A real implementation would use a proper expression evaluator or parser

                // Handle simple literals
                if (int.TryParse(expression.Trim(), out int value))
                {
                    return value != 0 ? "1" : "0";
                }

                // Handle logical operations
                if (expression.Contains("&&"))
                {
                    string[] parts = expression.Split(new[] { "&&" }, StringSplitOptions.None);
                    bool result = true;

                    foreach (var part in parts)
                    {
                        string partResult = await EvaluateSimplifiedExpressionAsync(part.Trim());
                        if (partResult == "0")
                        {
                            result = false;
                            break;
                        }
                    }

                    return result ? "1" : "0";
                }

                if (expression.Contains("||"))
                {
                    string[] parts = expression.Split(new[] { "||" }, StringSplitOptions.None);
                    bool result = false;

                    foreach (var part in parts)
                    {
                        string partResult = await EvaluateSimplifiedExpressionAsync(part.Trim());
                        if (partResult != "0")
                        {
                            result = true;
                            break;
                        }
                    }

                    return result ? "1" : "0";
                }

                // Handle equality operations
                if (expression.Contains("=="))
                {
                    string[] parts = expression.Split(new[] { "==" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        string left = await EvaluateSimplifiedExpressionAsync(parts[0].Trim());
                        string right = await EvaluateSimplifiedExpressionAsync(parts[1].Trim());
                        return left == right ? "1" : "0";
                    }
                }

                if (expression.Contains("!="))
                {
                    string[] parts = expression.Split(new[] { "!=" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        string left = await EvaluateSimplifiedExpressionAsync(parts[0].Trim());
                        string right = await EvaluateSimplifiedExpressionAsync(parts[1].Trim());
                        return left != right ? "1" : "0";
                    }
                }

                // Handle comparison operations
                if (expression.Contains(">"))
                {
                    string[] parts = expression.Split(new[] { ">" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0].Trim(), out int left) &&
                            int.TryParse(parts[1].Trim(), out int right))
                        {
                            return left > right ? "1" : "0";
                        }
                    }
                }

                if (expression.Contains("<"))
                {
                    string[] parts = expression.Split(new[] { "<" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0].Trim(), out int left) &&
                            int.TryParse(parts[1].Trim(), out int right))
                        {
                            return left < right ? "1" : "0";
                        }
                    }
                }

                if (expression.Contains(">="))
                {
                    string[] parts = expression.Split(new[] { ">=" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0].Trim(), out int left) &&
                            int.TryParse(parts[1].Trim(), out int right))
                        {
                            return left >= right ? "1" : "0";
                        }
                    }
                }

                if (expression.Contains("<="))
                {
                    string[] parts = expression.Split(new[] { "<=" }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        if (int.TryParse(parts[0].Trim(), out int left) &&
                            int.TryParse(parts[1].Trim(), out int right))
                        {
                            return left <= right ? "1" : "0";
                        }
                    }
                }

                // Handle logical not
                if (expression.Trim().StartsWith("!"))
                {
                    string operand = expression.Trim().Substring(1);
                    string result = await EvaluateSimplifiedExpressionAsync(operand);
                    return result == "0" ? "1" : "0";
                }

                // Default to 0 for expressions we can't evaluate
                return "0";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error evaluating simplified expression '{expression}': {ex.Message}");
                return "0";
            }
        }

        /// <summary>
        /// Checks if a macro is used in a file
        /// </summary>
        /// <param name="definition">Macro definition</param>
        /// <param name="fileContent">Content of the file</param>
        /// <returns>True if the macro is used in the file, false otherwise</returns>
        private bool IsMacroUsedInFile(CDefinition definition, string fileContent)
        {
            try
            {
                string name = definition.Name;

                // Look for direct usage of the macro name
                bool isDefined = Regex.IsMatch(fileContent, $@"#define\s+{Regex.Escape(name)}\b");
                if (isDefined)
                {
                    // Skip if it's the definition itself
                    return false;
                }

                bool isUsedInIfdef = Regex.IsMatch(fileContent, $@"#ifdef\s+{Regex.Escape(name)}\b");
                bool isUsedInIfndef = Regex.IsMatch(fileContent, $@"#ifndef\s+{Regex.Escape(name)}\b");
                bool isUsedInIf = Regex.IsMatch(fileContent, $@"defined\s*\(\s*{Regex.Escape(name)}\s*\)");

                // Check for usage in code (word boundaries to avoid partial matches)
                bool isUsedInCode = Regex.IsMatch(fileContent, $@"\b{Regex.Escape(name)}\b");

                return isUsedInIfdef || isUsedInIfndef || isUsedInIf || isUsedInCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if macro {definition.Name} is used in file: {ex.Message}");
                return false;
            }
        }
    }
}