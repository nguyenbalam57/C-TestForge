using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Interface
{
    /// <summary>
    /// Represents a path through a function
    /// </summary>
    public class CFunctionPath
    {
        /// <summary>
        /// Gets or sets the path ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the branches in the path
        /// </summary>
        public List<int> Branches { get; set; } = new List<int>();

        /// <summary>
        /// Gets or sets the path condition
        /// </summary>
        public string PathCondition { get; set; }

        /// <summary>
        /// Gets or sets whether the path is feasible
        /// </summary>
        public bool IsFeasible { get; set; }
    }
}
