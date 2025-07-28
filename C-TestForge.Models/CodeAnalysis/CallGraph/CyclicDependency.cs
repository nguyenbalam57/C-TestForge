using System.Collections.Generic;

namespace C_TestForge.Models.CodeAnalysis.CallGraph
{
    /// <summary>
    /// Represents a cyclic dependency in a function call graph
    /// </summary>
    public class CyclicDependency
    {
        /// <summary>
        /// Gets or sets the function names in the cycle
        /// </summary>
        public List<string> FunctionNames { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the cycle length
        /// </summary>
        public int CycleLength => FunctionNames?.Count ?? 0;
    }
}