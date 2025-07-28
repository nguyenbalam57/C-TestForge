using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Interface
{
    /// <summary>
    /// Represents a function call graph
    /// </summary>
    public class FunctionCallGraph
    {
        /// <summary>
        /// Gets or sets the root function name
        /// </summary>
        public string RootFunctionName { get; set; }

        /// <summary>
        /// Gets or sets the nodes in the graph
        /// </summary>
        public List<FunctionCallNode> Nodes { get; set; } = new List<FunctionCallNode>();

        /// <summary>
        /// Gets or sets the edges in the graph
        /// </summary>
        public List<FunctionCallEdge> Edges { get; set; } = new List<FunctionCallEdge>();
    }
}
