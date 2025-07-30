using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.Functions
{
    /// <summary>
    /// Represents a branch in a function
    /// </summary>
    public class FunctionBranch
    {
        /// <summary>
        /// Gets or sets the line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the branch condition
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the ID of the true block
        /// </summary>
        public int TrueBlockId { get; set; }

        /// <summary>
        /// Gets or sets the ID of the false block
        /// </summary>
        public int FalseBlockId { get; set; }

        /// <summary>
        /// Gets or sets the branch type
        /// </summary>
        public BranchType BranchType { get; set; }
    }
}
