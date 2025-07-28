using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.CallGraph
{
    /// <summary>
    /// Represents an edge in a function call graph
    /// </summary>
    public class FunctionCallEdge
    {
        /// <summary>
        /// Gets or sets the source node ID
        /// </summary>
        public int SourceNodeId { get; set; }

        /// <summary>
        /// Gets or sets the target node ID
        /// </summary>
        public int TargetNodeId { get; set; }

        /// <summary>
        /// Gets or sets the call site line number
        /// </summary>
        public int CallSiteLineNumber { get; set; }
    }
}
