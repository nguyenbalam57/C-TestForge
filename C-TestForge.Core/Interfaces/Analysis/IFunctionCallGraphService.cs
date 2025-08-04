using C_TestForge.Models.CodeAnalysis.CallGraph;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Service for analyzing function call graphs
    /// </summary>
    public interface IFunctionCallGraphService
    {
        /// <summary>
        /// Builds a call graph for the given function
        /// </summary>
        /// <param name="rootFunctionName">The root function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="maxDepth">The maximum depth to include</param>
        /// <returns>The function call graph</returns>
        Task<FunctionCallGraph> BuildCallGraphAsync(
            string rootFunctionName,
            SourceFile sourceFile,
            int maxDepth = -1);

        /// <summary>
        /// Finds all paths from the root function to leaf functions
        /// </summary>
        /// <param name="rootFunctionName">The root function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="maxDepth">The maximum depth to include</param>
        /// <returns>List of function call paths</returns>
        Task<List<FunctionCallPath>> FindCallPathsAsync(
            string rootFunctionName,
            SourceFile sourceFile,
            int maxDepth = -1);

        /// <summary>
        /// Analyzes potential cyclic dependencies in the call graph
        /// </summary>
        /// <param name="filePath">The file path</param>
        /// <returns>List of cyclic dependencies</returns>
        Task<List<CyclicDependency>> AnalyzeCyclicDependenciesAsync(
            SourceFile sourceFile);
    }
}
