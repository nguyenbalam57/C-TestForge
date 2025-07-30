using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.Functions
{
    /// <summary>
    /// Represents an execution path through a function
    /// </summary>
    public class FunctionPath
    {
        /// <summary>
        /// Gets or sets the path condition
        /// </summary>
        public string PathCondition { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets whether the path is executable
        /// </summary>
        public bool IsExecutable { get; set; }

        /// <summary>
        /// Gets or sets the sequence of block IDs in the path
        /// </summary>
        public List<int> BlockSequence { get; set; } = new List<int>();
    }
}
