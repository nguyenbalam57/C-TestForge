using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;
using C_TestForge.Models.Interface;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Service for analyzing branches in code
    /// </summary>
    public interface IBranchAnalysisService
    {
        /// <summary>
        /// Analyzes branches in the given function
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <returns>The branch analysis result</returns>
        Task<BranchAnalysisResult> AnalyzeBranchesAsync(
            string functionName,
            string filePath);

        /// <summary>
        /// Finds paths through the function that cover the given branches
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="branchIds">The branch IDs to cover</param>
        /// <returns>List of paths that cover the branches</returns>
        Task<List<CFunction>> FindPathsCoveringBranchesAsync(
            string functionName,
            string filePath,
            IEnumerable<int> branchIds);

        /// <summary>
        /// Determines if a branch is feasible (can be executed)
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <param name="branchId">The branch ID</param>
        /// <returns>True if the branch is feasible, false otherwise</returns>
        Task<bool> IsBranchFeasibleAsync(
            string functionName,
            string filePath,
            int branchId);
    }
}
