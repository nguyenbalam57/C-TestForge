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
    #region AnalysisService Implementation

    /// <summary>
    /// Implementation of the analysis service
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
                var parseResult = await _parserService.ParseSourceFileAsync(sourceFile.FilePath, parseOptions);

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

                _logger.LogInformation($"Completed analysis of source file: {sourceFile.FilePath}");

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
                _logger.LogInformation($"Analyzing project: {project.Name}");

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

                // Load all source files
                var sourceFiles = new List<SourceFile>();
                foreach (var sourceFilePath in project.SourceFiles)
                {
                    if (_fileService.FileExists(sourceFilePath))
                    {
                        var sourceFile = await _sourceCodeService.LoadSourceFileAsync(sourceFilePath);
                        sourceFiles.Add(sourceFile);
                    }
                    else
                    {
                        _logger.LogWarning($"Source file not found: {sourceFilePath}");
                    }
                }

                // Analyze each source file
                foreach (var sourceFile in sourceFiles)
                {
                    var fileResult = await AnalyzeSourceFileAsync(sourceFile, options);

                    // Merge results
                    result.Definitions.AddRange(fileResult.Definitions);
                    result.Variables.AddRange(fileResult.Variables);
                    result.Functions.AddRange(fileResult.Functions);
                    result.ConditionalDirectives.AddRange(fileResult.ConditionalDirectives);
                    result.FunctionRelationships.AddRange(fileResult.FunctionRelationships);
                    result.VariableConstraints.AddRange(fileResult.VariableConstraints);
                }

                // Consolidate and deduplicate results
                result.Definitions = result.Definitions.GroupBy(d => d.Name)
                    .Select(g => g.First())
                    .ToList();

                result.Variables = result.Variables.GroupBy(v => v.Name)
                    .Select(g => g.First())
                    .ToList();

                result.Functions = result.Functions.GroupBy(f => f.Name)
                    .Select(g => g.First())
                    .ToList();

                // Analyze cross-file relationships
                await AnalyzeCrossFileRelationshipsAsync(result, project);

                _logger.LogInformation($"Completed analysis of project: {project.Name}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing project: {project.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<AnalysisResult> AnalyzeFunctionAsync(CFunction function, Project projectContext, AnalysisOptions options)
        {
            try
            {
                _logger.LogInformation($"Analyzing function: {function.Name}");

                if (function == null)
                {
                    throw new ArgumentNullException(nameof(function));
                }

                if (projectContext == null)
                {
                    throw new ArgumentNullException(nameof(projectContext));
                }

                if (options == null)
                {
                    throw new ArgumentNullException(nameof(options));
                }

                // Create a new analysis result
                var result = new AnalysisResult();

                // Add the function to the result
                result.Functions.Add(function);

                // Find the source file that contains this function
                string sourceFilePath = projectContext.SourceFiles.FirstOrDefault(
                    path => _fileService.GetFileName(path) == function.SourceFile);

                if (string.IsNullOrEmpty(sourceFilePath))
                {
                    _logger.LogWarning($"Source file not found for function: {function.Name}, SourceFile: {function.SourceFile}");
                    return result;
                }

                // Load the source file
                var sourceFile = await _sourceCodeService.LoadSourceFileAsync(sourceFilePath);

                // Analyze the function's variables
                var usedVariables = await _functionAnalysisService.AnalyzeFunctionVariableUsageAsync(
                    function, result.Variables);

                result.Variables.AddRange(usedVariables);

                // Analyze the function's complexity
                var complexity = await _functionAnalysisService.AnalyzeFunctionComplexityAsync(function, sourceFile);

                // Analyze control flow
                var controlFlow = await _functionAnalysisService.ExtractControlFlowGraphAsync(function, sourceFile);

                // If requested, analyze called functions
                if (options.AnalyzeFunctionRelationships)
                {
                    await AnalyzeCalledFunctionsAsync(function, projectContext, result, options);
                }

                _logger.LogInformation($"Completed analysis of function: {function.Name}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing function: {function.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<CallGraph> AnalyzeCallGraphAsync(CFunction function, Project projectContext, int maxDepth = 0)
        {
            try
            {
                _logger.LogInformation($"Analyzing call graph for function: {function.Name}");

                if (function == null)
                {
                    throw new ArgumentNullException(nameof(function));
                }

                if (projectContext == null)
                {
                    throw new ArgumentNullException(nameof(projectContext));
                }

                // Create a new call graph
                var graph = new CallGraph
                {
                    RootFunction = function.Name,
                    Nodes = new List<CallGraphNode>(),
                    Edges = new List<CallGraphEdge>()
                };

                // Add the root node
                var rootNode = new CallGraphNode
                {
                    Id = Guid.NewGuid().ToString(),
                    FunctionName = function.Name,
                    SourceFile = function.SourceFile,
                    LineNumber = function.LineNumber,
                    Depth = 0
                };

                graph.Nodes.Add(rootNode);

                // Track visited functions to avoid cycles
                var visited = new HashSet<string>();
                visited.Add(function.Name);

                // Analyze the call graph recursively
                await BuildCallGraphAsync(function, projectContext, graph, rootNode.Id, visited, 1, maxDepth);

                _logger.LogInformation($"Completed call graph analysis for function: {function.Name}, Nodes: {graph.Nodes.Count}, Edges: {graph.Edges.Count}");

                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing call graph for function: {function.Name}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<DataFlowGraph> AnalyzeDataFlowAsync(CFunction function, Project projectContext)
        {
            try
            {
                _logger.LogInformation($"Analyzing data flow for function: {function.Name}");

                if (function == null)
                {
                    throw new ArgumentNullException(nameof(function));
                }

                if (projectContext == null)
                {
                    throw new ArgumentNullException(nameof(projectContext));
                }

                // Create a new data flow graph
                var graph = new DataFlowGraph
                {
                    FunctionName = function.Name,
                    Nodes = new List<DataFlowNode>(),
                    Edges = new List<DataFlowEdge>()
                };

                // Find the source file that contains this function
                string sourceFilePath = projectContext.SourceFiles.FirstOrDefault(
                    path => _fileService.GetFileName(path) == function.SourceFile);

                if (string.IsNullOrEmpty(sourceFilePath))
                {
                    _logger.LogWarning($"Source file not found for function: {function.Name}, SourceFile: {function.SourceFile}");
                    return graph;
                }

                // Load the source file
                var sourceFile = await _sourceCodeService.LoadSourceFileAsync(sourceFilePath);

                // Get all variables used in the function
                var allVariables = new List<CVariable>(function.Parameters);

                // Add all variables used in the function
                foreach (var variableName in function.UsedVariables)
                {
                    // Add as a placeholder if not already in the list
                    if (!allVariables.Any(v => v.Name == variableName))
                    {
                        allVariables.Add(new CVariable
                        {
                            Name = variableName,
                            TypeName = "unknown", // Placeholder
                            VariableType = VariableType.Primitive, // Placeholder
                            Scope = VariableScope.Local // Placeholder
                        });
                    }
                }

                // Analyze data flow for each variable
                foreach (var variable in allVariables)
                {
                    var dataFlow = await _variableAnalysisService.AnalyzeVariableDataFlowAsync(
                        variable, function, sourceFile);

                    // Create nodes for each assignment
                    foreach (var assignment in dataFlow.Assignments)
                    {
                        var node = new DataFlowNode
                        {
                            Id = Guid.NewGuid().ToString(),
                            VariableName = variable.Name,
                            LineNumber = assignment.LineNumber,
                            NodeType = "Assignment"
                        };

                        graph.Nodes.Add(node);
                    }

                    // Create nodes for each usage
                    foreach (var usage in dataFlow.Usages)
                    {
                        if (usage.UsageType == "Read")
                        {
                            var node = new DataFlowNode
                            {
                                Id = Guid.NewGuid().ToString(),
                                VariableName = variable.Name,
                                LineNumber = usage.LineNumber,
                                NodeType = "Read"
                            };

                            graph.Nodes.Add(node);
                        }
                    }
                }

                // Create edges between nodes
                CreateDataFlowEdges(graph);

                _logger.LogInformation($"Completed data flow analysis for function: {function.Name}, Nodes: {graph.Nodes.Count}, Edges: {graph.Edges.Count}");

                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing data flow for function: {function.Name}");
                throw;
            }
        }

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

        private async Task PerformComprehensiveAnalysisAsync(AnalysisResult result, SourceFile sourceFile)
        {
            // Perform comprehensive analysis of the source file

            // Analyze control flow for each function
            foreach (var function in result.Functions)
            {
                await _functionAnalysisService.ExtractControlFlowGraphAsync(function, sourceFile);
            }

            // Analyze data flow for each variable in each function
            foreach (var function in result.Functions)
            {
                foreach (var variable in result.Variables)
                {
                    if (function.UsedVariables.Contains(variable.Name) ||
                        function.Parameters.Any(p => p.Name == variable.Name))
                    {
                        await _variableAnalysisService.AnalyzeVariableDataFlowAsync(variable, function, sourceFile);
                    }
                }
            }

            await Task.CompletedTask;
        }

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

            // Analyze macro dependencies across files
            foreach (var definition in result.Definitions)
            {
                await _macroAnalysisService.ExtractMacroDependenciesAsync(definition, result.Definitions);
            }

            await Task.CompletedTask;
        }

        private async Task AnalyzeCalledFunctionsAsync(CFunction function, Project projectContext, AnalysisResult result, AnalysisOptions options)
        {
            // Get all project files
            var allSourceFiles = new List<string>(projectContext.SourceFiles);

            // Track visited functions to avoid cycles
            var visited = new HashSet<string>();
            visited.Add(function.Name);

            // Analyze called functions recursively
            await AnalyzeCalledFunctionsRecursiveAsync(function, projectContext, allSourceFiles, result, visited, options);
        }

        private async Task AnalyzeCalledFunctionsRecursiveAsync(CFunction function, Project projectContext,
            List<string> allSourceFiles, AnalysisResult result, HashSet<string> visited, AnalysisOptions options)
        {
            foreach (var calledFunctionName in function.CalledFunctions)
            {
                // Skip if already visited
                if (visited.Contains(calledFunctionName))
                {
                    continue;
                }

                visited.Add(calledFunctionName);

                // Find the called function
                var calledFunction = result.Functions.FirstOrDefault(f => f.Name == calledFunctionName);

                if (calledFunction == null)
                {
                    // Function not found, search for it in all source files
                    calledFunction = await FindFunctionInSourceFilesAsync(calledFunctionName, allSourceFiles);
                }

                if (calledFunction != null)
                {
                    // Add the function to the result if not already there
                    if (!result.Functions.Any(f => f.Name == calledFunction.Name))
                    {
                        result.Functions.Add(calledFunction);
                    }

                    // Add the relationship
                    var relationship = new FunctionRelationship
                    {
                        CallerName = function.Name,
                        CalleeName = calledFunction.Name,
                        LineNumber = function.LineNumber, // Ideally, we would find the exact line number of the call
                        SourceFile = function.SourceFile
                    };

                    if (!result.FunctionRelationships.Any(r =>
                        r.CallerName == relationship.CallerName &&
                        r.CalleeName == relationship.CalleeName))
                    {
                        result.FunctionRelationships.Add(relationship);
                    }

                    // Recursively analyze this function's called functions
                    await AnalyzeCalledFunctionsRecursiveAsync(calledFunction, projectContext, allSourceFiles, result, visited, options);
                }
            }
        }

        private async Task<CFunction> FindFunctionInSourceFilesAsync(string functionName, List<string> sourceFiles)
        {
            foreach (var sourceFilePath in sourceFiles)
            {
                if (_fileService.FileExists(sourceFilePath))
                {
                    // Parse the source file
                    var parseOptions = new ParseOptions
                    {
                        AnalyzeFunctions = true,
                        AnalyzeVariables = false,
                        ParsePreprocessorDefinitions = false
                    };

                    var parseResult = await _parserService.ParseSourceFileAsync(sourceFilePath, parseOptions);

                    // Look for the function
                    var function = parseResult.Functions.FirstOrDefault(f => f.Name == functionName);

                    if (function != null)
                    {
                        return function;
                    }
                }
            }

            return null;
        }

        private async Task BuildCallGraphAsync(CFunction function, Project projectContext, CallGraph graph,
            string parentNodeId, HashSet<string> visited, int depth, int maxDepth)
        {
            // Stop if we've reached the maximum depth
            if (maxDepth > 0 && depth > maxDepth)
            {
                return;
            }

            foreach (var calledFunctionName in function.CalledFunctions)
            {
                // Find the called function
                var calledFunction = await FindFunctionAsync(calledFunctionName, projectContext);

                if (calledFunction != null)
                {
                    // Create a node for the called function
                    var nodeId = visited.Contains(calledFunctionName) ?
                        graph.Nodes.First(n => n.FunctionName == calledFunctionName).Id :
                        Guid.NewGuid().ToString();

                    if (!visited.Contains(calledFunctionName))
                    {
                        var node = new CallGraphNode
                        {
                            Id = nodeId,
                            FunctionName = calledFunctionName,
                            SourceFile = calledFunction.SourceFile,
                            LineNumber = calledFunction.LineNumber,
                            Depth = depth
                        };

                        graph.Nodes.Add(node);
                        visited.Add(calledFunctionName);
                    }

                    // Create an edge from the parent to this function
                    var edge = new CallGraphEdge
                    {
                        Id = Guid.NewGuid().ToString(),
                        SourceId = parentNodeId,
                        TargetId = nodeId,
                        LineNumber = function.LineNumber, // Ideally, we would find the exact line number of the call
                        SourceFile = function.SourceFile
                    };

                    graph.Edges.Add(edge);

                    // Recursively build the call graph for this function
                    if (!visited.Contains(calledFunctionName))
                    {
                        await BuildCallGraphAsync(calledFunction, projectContext, graph, nodeId, visited, depth + 1, maxDepth);
                    }
                }
            }
        }

        private async Task<CFunction> FindFunctionAsync(string functionName, Project projectContext)
        {
            // Parse all source files to find the function
            foreach (var sourceFilePath in projectContext.SourceFiles)
            {
                if (_fileService.FileExists(sourceFilePath))
                {
                    // Parse the source file
                    var parseOptions = new ParseOptions
                    {
                        AnalyzeFunctions = true,
                        AnalyzeVariables = false,
                        ParsePreprocessorDefinitions = false
                    };

                    var parseResult = await _parserService.ParseSourceFileAsync(sourceFilePath, parseOptions);

                    // Look for the function
                    var function = parseResult.Functions.FirstOrDefault(f => f.Name == functionName);

                    if (function != null)
                    {
                        return function;
                    }
                }
            }

            return null;
        }

        private void CreateDataFlowEdges(DataFlowGraph graph)
        {
            // Group nodes by variable name
            var nodesByVariable = graph.Nodes.GroupBy(n => n.VariableName)
                .ToDictionary(g => g.Key, g => g.OrderBy(n => n.LineNumber).ToList());

            foreach (var variable in nodesByVariable.Keys)
            {
                var nodes = nodesByVariable[variable];

                // Create edges between assignments and reads
                for (int i = 0; i < nodes.Count; i++)
                {
                    var node = nodes[i];

                    if (node.NodeType == "Assignment")
                    {
                        // Find all subsequent reads until the next assignment
                        for (int j = i + 1; j < nodes.Count; j++)
                        {
                            var nextNode = nodes[j];

                            if (nextNode.NodeType == "Read")
                            {
                                // Create an edge from the assignment to the read
                                var edge = new DataFlowEdge
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    SourceId = node.Id,
                                    TargetId = nextNode.Id,
                                    EdgeType = "DataFlow"
                                };

                                graph.Edges.Add(edge);
                            }
                            else if (nextNode.NodeType == "Assignment")
                            {
                                // Stop at the next assignment
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    #endregion
}
