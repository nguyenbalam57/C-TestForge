using System.Collections.Generic;

namespace C_TestForge.Models.CodeAnalysis.CallGraph
{
    /// <summary>
    /// Represents a function call graph
    /// </summary>
    public class FunctionCallGraph
    {
        /// <summary>
        /// Gets or sets the root function name
        /// </summary>
        public string RootFunctionName { get; set; } = string.Empty;

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