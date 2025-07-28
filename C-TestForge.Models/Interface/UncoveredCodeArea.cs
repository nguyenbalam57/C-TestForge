using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Interface
{
    /// <summary>
    /// Represents an uncovered code area
    /// </summary>
    public class UncoveredCodeArea
    {
        /// <summary>
        /// Gets or sets the area type
        /// </summary>
        public UncoveredAreaType AreaType { get; set; }

        /// <summary>
        /// Gets or sets the line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Gets or sets the end line number (for multi-line areas)
        /// </summary>
        public int EndLineNumber { get; set; }

        /// <summary>
        /// Gets or sets the description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the severity
        /// </summary>
        public CodeCoverageSeverity Severity { get; set; }
    }
}
