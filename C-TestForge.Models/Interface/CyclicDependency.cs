using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Interface
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
