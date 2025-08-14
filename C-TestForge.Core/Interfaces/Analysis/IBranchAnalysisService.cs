using C_TestForge.Models.CodeAnalysis.BranchAnalysis;
using C_TestForge.Models.Projects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Interface for analyzing branches in code
    /// </summary>
    public interface IBranchAnalysisService
    {
        /// <summary>
        /// Analyzes branches in the given function
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <returns>The branch analysis result</returns>
        Task<BranchAnalysisResult> AnalyzeBranchesAsync(string functionName, SourceFile sourceFile);

        /// <summary>
        /// Finds paths through the function that cover the given branches
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="branchIds">The branch IDs to cover</param>
        /// <returns>The paths covering the branches</returns>
        Task<List<CFunctionPath>> FindPathsCoveringBranchesAsync(string functionName, SourceFile sourceFile, IEnumerable<int> branchIds);

        /// <summary>
        /// Determines if a branch is feasible (can be executed)
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="branchId">The branch ID</param>
        /// <returns>True if the branch is feasible, otherwise false</returns>
        Task<bool> IsBranchFeasibleAsync(string functionName, SourceFile sourceFile, int branchId);
    }
}