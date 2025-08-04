using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Core;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Implementation of the analysis service for analyzing C source code
    /// </summary>
    public class AnalysisService : IAnalysisService
    {
        private readonly ILogger<AnalysisService> _logger;
        private readonly IParserService _parserService;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly IFunctionAnalysisService _functionAnalysisService;
        private readonly IVariableAnalysisService _variableAnalysisService;
        private readonly IMacroAnalysisService _macroAnalysisService;
        private readonly IFileService _fileService;

        /// <summary>
        /// Constructor for AnalysisService
        /// </summary>
        public AnalysisService(
            ILogger<AnalysisService> logger,
            IParserService parserService,
            ISourceCodeService sourceCodeService,
            IFunctionAnalysisService functionAnalysisService,
            IVariableAnalysisService variableAnalysisService,
            IMacroAnalysisService macroAnalysisService,
            IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
            _functionAnalysisService = functionAnalysisService ?? throw new ArgumentNullException(nameof(functionAnalysisService));
            _variableAnalysisService = variableAnalysisService ?? throw new ArgumentNullException(nameof(variableAnalysisService));
            _macroAnalysisService = macroAnalysisService ?? throw new ArgumentNullException(nameof(macroAnalysisService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        /// <inheritdoc/>
        public async Task<AnalysisResult> AnalyzeSourceFileAsync(SourceFile sourceFile, AnalysisOptions options)
        {
            try
            {
                _logger.LogInformation($"Analyzing source file: {sourceFile.FilePath}");

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                // Create a new analysis result
                var result = new AnalysisResult();

                // Create parsing options from analysis options
                var parseOptions = new ParseOptions
                {
                    ParsePreprocessorDefinitions = options.AnalyzePreprocessorDefinitions,
                    AnalyzeVariables = options.AnalyzeVariables,
                    AnalyzeFunctions = options.AnalyzeFunctions
                };

                // Parse the source file
                var parseResult = await _parserService.ParseSourceFileParserAsync(sourceFile, parseOptions);

                // Copy results from parse result
                result.Definitions.AddRange(parseResult.Definitions);
                result.Variables.AddRange(parseResult.Variables);
                result.Functions.AddRange(parseResult.Functions);
                result.ConditionalDirectives.AddRange(parseResult.ConditionalDirectives);

                // Analyze function relationships if requested
                if (options.AnalyzeFunctionRelationships && result.Functions.Count > 0)
                {
                    var relationships = await _functionAnalysisService.AnalyzeFunctionRelationshipsAsync(result.Functions);
                    result.FunctionRelationships.AddRange(relationships);
                }

                // Analyze variable constraints if requested
                if (options.AnalyzeVariableConstraints && result.Variables.Count > 0)
                {
                    var constraints = await _variableAnalysisService.AnalyzeVariablesAsync(
                        result.Variables, result.Functions, result.Definitions);
                    result.VariableConstraints.AddRange(constraints);
                }

                // Perform additional analysis based on detail level
                if (options.DetailLevel >= AnalysisLevel.Detailed)
                {
                    await PerformDetailedAnalysisAsync(result, sourceFile);
                }

                if (options.DetailLevel >= AnalysisLevel.Comprehensive)
                {
                    await PerformComprehensiveAnalysisAsync(result, sourceFile);
                }

                _logger.LogInformation($"Analysis complete for source file: {sourceFile.FilePath}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing source file: {sourceFile.FilePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<AnalysisResult> AnalyzeProjectAsync(Project project, AnalysisOptions options)
        {
            try
            {
                _logger.LogInformation($"Analyzing project with {project.SourceFiles.Count} source files");

                if (project == null)
                {
                    throw new ArgumentNullException(nameof(project));
                }

                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                // Create a new analysis result
                var result = new AnalysisResult();

                // Analyze each source file
                foreach (var sourceFilePath in project.SourceFiles)
                {
                    try
                    {
                        // Load the source file
                        var sourceFile = await _sourceCodeService.LoadSourceFileAsync(sourceFilePath);

                        // Analyze the source file
                        var fileResult = await AnalyzeSourceFileAsync(sourceFile, options);

                        // Merge results
                        result.Definitions.AddRange(fileResult.Definitions);
                        result.Variables.AddRange(fileResult.Variables);
                        result.Functions.AddRange(fileResult.Functions);
                        result.ConditionalDirectives.AddRange(fileResult.ConditionalDirectives);
                        result.FunctionRelationships.AddRange(fileResult.FunctionRelationships);
                        result.VariableConstraints.AddRange(fileResult.VariableConstraints);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error analyzing source file: {sourceFilePath}");
                        // Continue with the next file
                    }
                }

                // Analyze cross-file relationships
                if (options.AnalyzeCrossFileRelationships)
                {
                    await AnalyzeCrossFileRelationshipsAsync(result, project);
                }

                _logger.LogInformation($"Project analysis complete. Found {result.Functions.Count} functions, {result.Variables.Count} variables, and {result.Definitions.Count} macros");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing project: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> IsSourceFileModifiedAsync(SourceFile sourceFile)
        {
            try
            {
                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                if (string.IsNullOrEmpty(sourceFile.FilePath))
                {
                    throw new ArgumentException("Source file path cannot be null or empty", nameof(sourceFile));
                }

                if (!_fileService.FileExists(sourceFile.FilePath))
                {
                    _logger.LogWarning($"Source file not found: {sourceFile.FilePath}");
                    return true; // Consider as modified if the file doesn't exist
                }

                // Get the last modified time from the file system
                DateTime lastModified = File.GetLastWriteTime(sourceFile.FilePath);

                // Compare with the last modified time stored in the source file object
                return lastModified > sourceFile.LastModified;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if source file is modified: {sourceFile.FilePath}");
                return true; // Consider as modified if there's an error
            }
        }

        /// <summary>
        /// Performs detailed analysis on a source file
        /// </summary>
        /// <param name="result">Analysis result to update</param>
        /// <param name="sourceFile">Source file to analyze</param>
        /// <returns>Task</returns>
        private async Task PerformDetailedAnalysisAsync(AnalysisResult result, SourceFile sourceFile)
        {
            // Perform more detailed analysis of the source file

            // Analyze function complexity for each function
            foreach (var function in result.Functions)
            {
                await _functionAnalysisService.AnalyzeFunctionComplexityAsync(function, sourceFile);
            }

            // Extract more detailed constraints for each variable
            foreach (var variable in result.Variables)
            {
                var constraints = await _variableAnalysisService.ExtractConstraintsAsync(variable, sourceFile);
                result.VariableConstraints.AddRange(constraints);
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Performs comprehensive analysis on a source file
        /// </summary>
        /// <param name="result">Analysis result to update</param>
        /// <param name="sourceFile">Source file to analyze</param>
        /// <returns>Task</returns>
        private async Task PerformComprehensiveAnalysisAsync(AnalysisResult result, SourceFile sourceFile)
        {
            // Perform comprehensive analysis of the source file

            // Analyze control flow for each function
            foreach (var function in result.Functions)
            {
                await _functionAnalysisService.ExtractControlFlowGraphAsync(function);
            }

            // Analyze data flow for each variable in each function
            foreach (var function in result.Functions)
            {
                foreach (var variable in result.Variables)
                {
                    if (function.UsedVariables.Contains(variable.Name) ||
                        function.Parameters.Any(p => p.Name == variable.Name))
                    {
                        await _variableAnalysisService.AnalyzeVariablesAsync(
                            new List<CVariable> { variable },
                            new List<CFunction> { function },
                            result.Definitions);
                    }
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Analyzes relationships between source files in a project
        /// </summary>
        /// <param name="result">Analysis result to update</param>
        /// <param name="project">Project to analyze</param>
        /// <returns>Task</returns>
        private async Task AnalyzeCrossFileRelationshipsAsync(AnalysisResult result, Project project)
        {
            // Analyze relationships between functions across files
            var functionRelationships = await _functionAnalysisService.AnalyzeFunctionRelationshipsAsync(result.Functions);

            // Add any new relationships
            foreach (var relationship in functionRelationships)
            {
                if (!result.FunctionRelationships.Any(r =>
                    r.CallerName == relationship.CallerName &&
                    r.CalleeName == relationship.CalleeName))
                {
                    result.FunctionRelationships.Add(relationship);
                }
            }

            // Analyze macro relationships across files
            await _macroAnalysisService.AnalyzeMacroRelationshipsAsync(result.Definitions, result.ConditionalDirectives);

            await Task.CompletedTask;
        }
    }

}