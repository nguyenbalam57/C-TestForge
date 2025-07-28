using System.Collections.Generic;

namespace C_TestForge.Models.CodeAnalysis.Coverage
{
    /// <summary>
    /// Represents the result of a code coverage analysis
    /// </summary>
    public class CodeCoverageResult
    {
        /// <summary>
        /// Gets or sets the function name
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Gets or sets the line coverage percentage
        /// </summary>
        public double LineCoverage { get; set; }

        /// <summary>
        /// Gets or sets the branch coverage percentage
        /// </summary>
        public double BranchCoverage { get; set; }

        /// <summary>
        /// Gets or sets the path coverage percentage
        /// </summary>
        public double PathCoverage { get; set; }

        /// <summary>
        /// Gets or sets the covered lines
        /// </summary>
        public List<int> CoveredLines { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the uncovered lines
        /// </summary>
        public List<int> UncoveredLines { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the covered branches
        /// </summary>
        public List<int> CoveredBranches { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the uncovered branches
        /// </summary>
        public List<int> UncoveredBranches { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the covered paths
        /// </summary>
        public List<int> CoveredPaths { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the uncovered paths
        /// </summary>
        public List<int> UncoveredPaths { get; set; } = new List<int>();
    }
}