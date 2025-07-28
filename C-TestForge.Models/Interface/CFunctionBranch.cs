using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Interface
{
    /// <summary>
    /// Represents a branch in a function
    /// </summary>
    public class CFunctionBranch
    {
        /// <summary>
        /// Gets or sets the branch ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the branch condition
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Gets or sets the true branch target
        /// </summary>
        public int TrueBranchTarget { get; set; }

        /// <summary>
        /// Gets or sets the false branch target
        /// </summary>
        public int FalseBranchTarget { get; set; }

        /// <summary>
        /// Gets or sets whether the branch is feasible
        /// </summary>
        public bool IsFeasible { get; set; }
    }
}
