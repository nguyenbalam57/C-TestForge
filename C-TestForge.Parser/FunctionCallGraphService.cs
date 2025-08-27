using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Models;
using C_TestForge.Models.CodeAnalysis.CallGraph;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Service for analyzing function call graphs
    /// </summary>
    public class FunctionCallGraphService : IFunctionCallGraphService
    {
        private readonly IClangSharpParserService _parserService;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="parserService">The parser service</param>
        public FunctionCallGraphService(IClangSharpParserService parserService)
        {
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
        }

        /// <summary>
        /// Builds a call graph for the given function
        /// </summary>
        public async Task<FunctionCallGraph> BuildCallGraphAsync(string rootFunctionName, SourceFile sourceFile, int maxDepth = -1)
        {
            if (string.IsNullOrEmpty(rootFunctionName))
                throw new ArgumentNullException(nameof(rootFunctionName));

            //// Extract all functions
            //var functions = await _parserService.ExtractFunctionsAsync(sourceFile);
            //if (!functions.Any(f => f.Name == rootFunctionName))
            //    throw new ArgumentException($"Function not found: {rootFunctionName}", nameof(rootFunctionName));

            //// Create call graph
            //var graph = new FunctionCallGraph
            //{
            //    RootFunctionName = rootFunctionName,
            //    Nodes = new List<FunctionCallNode>(),
            //    Edges = new List<FunctionCallEdge>()
            //};

            //// Build the graph
            //var processedFunctions = new HashSet<string>();
            //await BuildCallGraphRecursiveAsync(rootFunctionName, sourceFile.FilePath, 0, maxDepth, graph, processedFunctions, functions);

            return new FunctionCallGraph();
        }

        /// <summary>
        /// Recursive method to build the call graph
        /// </summary>
        private async Task BuildCallGraphRecursiveAsync(
            string functionName,
            string filePath,
            int depth,
            int maxDepth,
            FunctionCallGraph graph,
            HashSet<string> processedFunctions,
            List<CFunction> functions)
        {
            // Stop if max depth reached or if function already processed
            if ((maxDepth >= 0 && depth > maxDepth) || processedFunctions.Contains(functionName))
                return;

            // Mark function as processed
            processedFunctions.Add(functionName);

            // Find the function
            var function = functions.FirstOrDefault(f => f.Name == functionName);
            if (function == null)
                return; // Function not found, might be external

            // Add node for this function
            var nodeId = graph.Nodes.Count;
            var node = new FunctionCallNode
            {
                Id = nodeId,
                FunctionName = functionName,
                FilePath = filePath,
                Depth = depth
            };
            graph.Nodes.Add(node);

            // Process called functions
            foreach (var calledFunctionName in function.CalledFunctions)
            {
                // Add node for the called function if it doesn't exist
                var existingNode = graph.Nodes.FirstOrDefault(n => n.FunctionName == calledFunctionName);
                int calledNodeId;

                if (existingNode == null)
                {
                    calledNodeId = graph.Nodes.Count;
                    var calledNode = new FunctionCallNode
                    {
                        Id = calledNodeId,
                        FunctionName = calledFunctionName,
                        FilePath = filePath,
                        Depth = depth + 1
                    };
                    graph.Nodes.Add(calledNode);
                }
                else
                {
                    calledNodeId = existingNode.Id;
                }

                // Add edge
                var edge = new FunctionCallEdge
                {
                    SourceNodeId = nodeId,
                    TargetNodeId = calledNodeId,
                    CallSiteLineNumber = 0 // Not available from current function info
                };
                graph.Edges.Add(edge);

                // Recursively process the called function
                await BuildCallGraphRecursiveAsync(calledFunctionName, filePath, depth + 1, maxDepth, graph, processedFunctions, functions);
            }
        }

        /// <summary>
        /// Finds all paths from the root function to leaf functions
        /// </summary>
        public async Task<List<FunctionCallPath>> FindCallPathsAsync(string rootFunctionName, SourceFile sourceFile, int maxDepth = -1)
        {
            if (string.IsNullOrEmpty(rootFunctionName))
                throw new ArgumentNullException(nameof(rootFunctionName));

            // Build call graph
            var graph = await BuildCallGraphAsync(rootFunctionName, sourceFile, maxDepth);

            // Find all paths
            var paths = new List<FunctionCallPath>();
            var rootNode = graph.Nodes.FirstOrDefault(n => n.FunctionName == rootFunctionName);
            if (rootNode == null)
                return paths;

            // Find leaf nodes (nodes with no outgoing edges)
            var leafNodes = graph.Nodes.Where(n => !graph.Edges.Any(e => e.SourceNodeId == n.Id)).ToList();

            // Find paths from root to each leaf
            int pathId = 0;
            foreach (var leafNode in leafNodes)
            {
                var path = new FunctionCallPath
                {
                    Id = pathId++,
                    FunctionNames = new List<string>(),
                    CallSiteLineNumbers = new List<int>()
                };

                // Find path from root to leaf
                FindPathFromRootToLeaf(rootNode.Id, leafNode.Id, new List<int>(), path, graph);

                if (path.FunctionNames.Count > 0)
                {
                    paths.Add(path);
                }
            }

            return paths;
        }

        /// <summary>
        /// Recursive method to find a path from root to leaf
        /// </summary>
        private bool FindPathFromRootToLeaf(
            int currentNodeId,
            int targetNodeId,
            List<int> visitedNodes,
            FunctionCallPath path,
            FunctionCallGraph graph)
        {
            // If current node is the target, we've found the path
            if (currentNodeId == targetNodeId)
            {
                var node = graph.Nodes.FirstOrDefault(n => n.Id == currentNodeId);
                if (node != null)
                {
                    path.FunctionNames.Add(node.FunctionName);
                }
                return true;
            }

            // If already visited, avoid cycles
            if (visitedNodes.Contains(currentNodeId))
                return false;

            // Mark node as visited
            visitedNodes.Add(currentNodeId);

            // Add current node to path
            var currentNode = graph.Nodes.FirstOrDefault(n => n.Id == currentNodeId);
            if (currentNode != null)
            {
                path.FunctionNames.Add(currentNode.FunctionName);
            }

            // Find edges from current node
            var edges = graph.Edges.Where(e => e.SourceNodeId == currentNodeId).ToList();

            // Try each edge
            foreach (var edge in edges)
            {
                path.CallSiteLineNumbers.Add(edge.CallSiteLineNumber);

                if (FindPathFromRootToLeaf(edge.TargetNodeId, targetNodeId, new List<int>(visitedNodes), path, graph))
                {
                    return true;
                }

                // Remove last call site line number if path not found
                if (path.CallSiteLineNumbers.Count > 0)
                {
                    path.CallSiteLineNumbers.RemoveAt(path.CallSiteLineNumbers.Count - 1);
                }
            }

            // Remove current node from path if no path found
            if (path.FunctionNames.Count > 0)
            {
                path.FunctionNames.RemoveAt(path.FunctionNames.Count - 1);
            }

            return false;
        }

        /// <summary>
        /// Analyzes potential cyclic dependencies in the call graph
        /// </summary>
        public async Task<List<CyclicDependency>> AnalyzeCyclicDependenciesAsync(SourceFile sourceFile)
        {
            //string filePath = sourceFile.FilePath;

            //if (string.IsNullOrEmpty(filePath))
            //    throw new ArgumentNullException(nameof(filePath));
            //if (!File.Exists(filePath))
            //    throw new FileNotFoundException("File not found", filePath);

            //// Extract all functions
            //var functions = await _parserService.ExtractFunctionsAsync(sourceFile);

            //// Create adjacency list
            //var adjacencyList = new Dictionary<string, List<string>>();
            //foreach (var function in functions)
            //{
            //    adjacencyList[function.Name] = function.CalledFunctions;
            //}

            //// Find cycles using DFS
            //var cycles = new List<CyclicDependency>();
            //var visited = new HashSet<string>();
            //var recursionStack = new HashSet<string>();
            //var currentPath = new List<string>();

            //foreach (var function in functions)
            //{
            //    if (!visited.Contains(function.Name))
            //    {
            //        FindCyclesUsingDFS(function.Name, adjacencyList, visited, recursionStack, currentPath, cycles);
            //    }
            //}

            return new List<CyclicDependency>();
        }

        /// <summary>
        /// Recursive method to find cycles using DFS
        /// </summary>
        private void FindCyclesUsingDFS(
            string currentFunction,
            Dictionary<string, List<string>> adjacencyList,
            HashSet<string> visited,
            HashSet<string> recursionStack,
            List<string> currentPath,
            List<CyclicDependency> cycles)
        {
            // Mark current function as visited and add to recursion stack
            visited.Add(currentFunction);
            recursionStack.Add(currentFunction);
            currentPath.Add(currentFunction);

            // Visit all called functions
            if (adjacencyList.TryGetValue(currentFunction, out var calledFunctions))
            {
                foreach (var calledFunction in calledFunctions)
                {
                    // If the called function is not visited, recursively visit it
                    if (!visited.Contains(calledFunction))
                    {
                        FindCyclesUsingDFS(calledFunction, adjacencyList, visited, recursionStack, currentPath, cycles);
                    }
                    // If the called function is in the recursion stack, we've found a cycle
                    else if (recursionStack.Contains(calledFunction))
                    {
                        // Extract cycle
                        var cycleStartIndex = currentPath.IndexOf(calledFunction);
                        var cycle = currentPath.Skip(cycleStartIndex).ToList();

                        // Add cycle to the list
                        cycles.Add(new CyclicDependency
                        {
                            FunctionNames = new List<string>(cycle)
                        });
                    }
                }
            }

            // Remove current function from recursion stack and current path
            recursionStack.Remove(currentFunction);
            currentPath.RemoveAt(currentPath.Count - 1);
        }
    }
}
