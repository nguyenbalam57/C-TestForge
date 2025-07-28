using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    #region MacroAnalysisService Implementation

    /// <summary>
    /// Implementation of the macro analysis service
    /// </summary>
    public class MacroAnalysisService : IMacroAnalysisService
    {
        private readonly ILogger<MacroAnalysisService> _logger;

        public MacroAnalysisService(ILogger<MacroAnalysisService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
                        // This is a simplified approach - a real implementation would use a proper parser
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

                    // Clear existing dependencies
                    directive.Dependencies.Clear();

                    // Extract dependencies from the condition
                    if (!string.IsNullOrEmpty(directive.Condition))
                    {
                        string[] tokens = SplitIntoTokens(directive.Condition);

                        foreach (var token in tokens)
                        {
                            // Skip keywords
                            if (token == "defined" || IsNumeric(token))
                            {
                                continue;
                            }

                            if (definitionDict.ContainsKey(token))
                            {
                                _logger.LogDebug($"Conditional directive at line {directive.LineNumber} depends on {token}");
                                directive.Dependencies.Add(token);
                            }
                        }
                    }
                }

                // Check for circular dependencies
                CheckForCircularDependencies(definitions);

                _logger.LogInformation($"Completed analysis of macro relationships");

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing macro relationships");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<CDefinition>> ExtractMacroDependenciesAsync(CDefinition definition, List<CDefinition> allDefinitions)
        {
            try
            {
                _logger.LogInformation($"Extracting dependencies for macro: {definition.Name}");

                if (definition == null)
                {
                    throw new ArgumentNullException(nameof(definition));
                }

                if (allDefinitions == null)
                {
                    throw new ArgumentNullException(nameof(allDefinitions));
                }

                var dependencies = new List<CDefinition>();
                var visited = new HashSet<string>();

                // Build a dictionary for quick lookup
                var definitionDict = allDefinitions.ToDictionary(d => d.Name, d => d);

                // Recursively extract dependencies
                ExtractDependenciesRecursive(definition, definitionDict, dependencies, visited, 0);

                _logger.LogInformation($"Extracted {dependencies.Count} dependencies for macro: {definition.Name}");

                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting dependencies for macro: {definition.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> EvaluateMacroExpressionAsync(string expression, Dictionary<string, string> activeDefinitions)
        {
            try
            {
                _logger.LogInformation($"Evaluating macro expression: {expression}");

                if (string.IsNullOrEmpty(expression))
                {
                    return "";
                }

                if (activeDefinitions == null)
                {
                    throw new ArgumentNullException(nameof(activeDefinitions));
                }

                // Replace defined(X) with 1 or 0
                string result = Regex.Replace(expression, @"defined\s*\(\s*(\w+)\s*\)", match =>
                {
                    string macroName = match.Groups[1].Value;
                    return activeDefinitions.ContainsKey(macroName) ? "1" : "0";
                });

                // Replace defined X with 1 or 0
                result = Regex.Replace(result, @"defined\s+(\w+)", match =>
                {
                    string macroName = match.Groups[1].Value;
                    return activeDefinitions.ContainsKey(macroName) ? "1" : "0";
                });

                // Replace macro names with their values
                foreach (var macro in activeDefinitions)
                {
                    string pattern = $@"\b{Regex.Escape(macro.Key)}\b";
                    string value = string.IsNullOrEmpty(macro.Value) ? "1" : macro.Value;
                    result = Regex.Replace(result, pattern, value);
                }

                // Replace remaining macro names (not in activeDefinitions) with 0
                result = Regex.Replace(result, @"\b[a-zA-Z_][a-zA-Z0-9_]*\b", "0");

                _logger.LogInformation($"Evaluated macro expression: {expression} to {result}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error evaluating macro expression: {expression}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> FindMacroUsagesAsync(CDefinition definition, Project projectContext)
        {
            try
            {
                _logger.LogInformation($"Finding usages of macro: {definition.Name}");

                if (definition == null)
                {
                    throw new ArgumentNullException(nameof(definition));
                }

                if (projectContext == null)
                {
                    throw new ArgumentNullException(nameof(projectContext));
                }

                var usages = new List<string>();

                // Check all source files in the project
                foreach (var sourceFilePath in projectContext.SourceFiles)
                {
                    if (File.Exists(sourceFilePath))
                    {
                        string content = await File.ReadAllTextAsync(sourceFilePath);

                        // Check if the file contains the macro name
                        var regex = new Regex($@"\b{Regex.Escape(definition.Name)}\b");
                        if (regex.IsMatch(content))
                        {
                            usages.Add(sourceFilePath);
                        }
                    }
                }

                _logger.LogInformation($"Found {usages.Count} usages of macro: {definition.Name}");

                return usages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding usages of macro: {definition.Name}");
                throw;
            }
        }

        private string[] SplitIntoTokens(string text)
        {
            // Split the text into tokens
            // This is a simplified approach - a real implementation would use a proper tokenizer
            var tokens = new List<string>();

            // Replace operators with spaces to isolate identifiers
            string sanitized = Regex.Replace(text, @"[!&|^~<>=\+\-\*/\(\)%]", " ");

            // Split into tokens
            string[] parts = sanitized.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                // Skip numeric literals
                if (!IsNumeric(part))
                {
                    // Only add valid C identifiers
                    if (Regex.IsMatch(part, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
                    {
                        tokens.Add(part);
                    }
                }
            }

            return tokens.ToArray();
        }

        private bool IsNumeric(string text)
        {
            // Check if the text is a numeric literal
            return Regex.IsMatch(text, @"^[0-9]+$") ||
                   Regex.IsMatch(text, @"^0x[0-9a-fA-F]+$") ||
                   Regex.IsMatch(text, @"^0[0-7]+$") ||
                   Regex.IsMatch(text, @"^[0-9]*\.[0-9]+$");
        }

        private void CheckForCircularDependencies(List<CDefinition> definitions)
        {
            foreach (var definition in definitions)
            {
                var visited = new HashSet<string>();
                var path = new Stack<string>();

                if (HasCircularDependency(definition.Name, definitions, visited, path))
                {
                    _logger.LogWarning($"Circular dependency detected for macro: {definition.Name}. Path: {string.Join(" -> ", path)}");
                }
            }
        }

        private bool HasCircularDependency(string definitionName, List<CDefinition> definitions, HashSet<string> visited, Stack<string> path)
        {
            if (path.Contains(definitionName))
            {
                // Found a circular dependency
                path.Push(definitionName);
                return true;
            }

            if (visited.Contains(definitionName))
            {
                // Already visited this definition and it's not part of a cycle
                return false;
            }

            visited.Add(definitionName);
            path.Push(definitionName);

            var definition = definitions.FirstOrDefault(d => d.Name == definitionName);
            if (definition != null)
            {
                foreach (var dependency in definition.Dependencies)
                {
                    if (HasCircularDependency(dependency, definitions, visited, path))
                    {
                        return true;
                    }
                }
            }

            path.Pop();
            return false;
        }

        private void ExtractDependenciesRecursive(CDefinition definition, Dictionary<string, CDefinition> definitionDict,
            List<CDefinition> dependencies, HashSet<string> visited, int depth)
        {
            // Avoid infinite recursion
            if (depth > 100)
            {
                _logger.LogWarning($"Reached maximum recursion depth for macro dependency extraction: {definition.Name}");
                return;
            }

            // Skip if already visited
            if (visited.Contains(definition.Name))
            {
                return;
            }

            visited.Add(definition.Name);

            // Process each dependency
            foreach (var dependencyName in definition.Dependencies)
            {
                if (definitionDict.TryGetValue(dependencyName, out var dependencyDefinition))
                {
                    if (!dependencies.Any(d => d.Name == dependencyDefinition.Name))
                    {
                        dependencies.Add(dependencyDefinition);
                    }

                    // Recursively process this dependency's dependencies
                    ExtractDependenciesRecursive(dependencyDefinition, definitionDict, dependencies, visited, depth + 1);
                }
            }
        }
    }

    #endregion
}
