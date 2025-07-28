using System.Collections.Generic;

namespace C_TestForge.Models.CodeAnalysis.BranchAnalysis
{
    /// <summary>
    /// Represents the result of a branch analysis
    /// </summary>
    public class BranchAnalysisResult
    {
        /// <summary>
        /// Gets or sets the function name
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Gets or sets the branches in the function
        /// </summary>
        public List<CFunctionBranch> Branches { get; set; } = new List<CFunctionBranch>();

        /// <summary>
        /// Gets or sets the paths through the function
        /// </summary>
        public List<CFunctionPath> Paths { get; set; } = new List<CFunctionPath>();

        /// <summary>
        /// Gets or sets the total number of branches
        /// </summary>
        public int TotalBranches { get; set; }

        /// <summary>
        /// Gets or sets the number of feasible branches
        /// </summary>
        public int FeasibleBranches { get; set; }

        /// <summary>
        /// Gets or sets the number of infeasible branches
        /// </summary>
        public int InfeasibleBranches { get; set; }
    }
}