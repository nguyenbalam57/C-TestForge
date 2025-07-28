using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestGeneration
{
    /// <summary>
    /// Type of coverage metric
    /// </summary>
    public enum CoverageType
    {
        /// <summary>
        /// Statement coverage
        /// </summary>
        Statement,

        /// <summary>
        /// Branch coverage
        /// </summary>
        Branch,

        /// <summary>
        /// Path coverage
        /// </summary>
        Path,

        /// <summary>
        /// Function coverage
        /// </summary>
        Function,

        /// <summary>
        /// Line coverage
        /// </summary>
        Line,

        /// <summary>
        /// Condition coverage
        /// </summary>
        Condition,

        /// <summary>
        /// Modified Condition/Decision Coverage
        /// </summary>
        MCDC
    }
}
