using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestGeneration
{
    /// <summary>
    /// Strategy for generating test cases
    /// </summary>
    public enum TestGenerationStrategy
    {
        /// <summary>
        /// Generate tests that cover all branches
        /// </summary>
        BranchCoverage,

        /// <summary>
        /// Generate tests that cover all statements
        /// </summary>
        StatementCoverage,

        /// <summary>
        /// Generate tests that cover all paths
        /// </summary>
        PathCoverage,

        /// <summary>
        /// Generate tests based on boundary values
        /// </summary>
        BoundaryValue,

        /// <summary>
        /// Generate tests based on equivalence classes
        /// </summary>
        EquivalencePartitioning,

        /// <summary>
        /// Generate tests that cover error conditions
        /// </summary>
        ErrorCondition,

        /// <summary>
        /// Custom test generation strategy
        /// </summary>
        Custom
    }
}
