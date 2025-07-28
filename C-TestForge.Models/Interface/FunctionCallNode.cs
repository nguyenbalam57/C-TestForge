using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Interface
{
    /// <summary>
    /// Represents a node in a function call graph
    /// </summary>
    public class FunctionCallNode
    {
        /// <summary>
        /// Gets or sets the node ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the function name
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Gets or sets the file path
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the depth in the call graph
        /// </summary>
        public int Depth { get; set; }
    }
}
