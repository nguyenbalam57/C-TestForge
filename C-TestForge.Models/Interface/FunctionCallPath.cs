using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Interface
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
