using System.Collections.Generic;

namespace C_TestForge.Models.CodeAnalysis.CallGraph
{
    /// <summary>
    /// Represents a path in a function call graph
    /// </summary>
    public class FunctionCallPath
    {
        /// <summary>
        /// Gets or sets the path ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the function names in the path
        /// </summary>
        public List<string> FunctionNames { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the call site line numbers
        /// </summary>
        public List<int> CallSiteLineNumbers { get; set; } = new List<int>();
    }
}