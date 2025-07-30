using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestGeneration
{
    /// <summary>
    /// Represents a branch for coverage tracking
    /// </summary>
    public class CoverageBranch : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Source file
        /// </summary>
        public string SourceFile { get; set; } = string.Empty;

        /// <summary>
        /// Line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// Branch condition
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Whether the branch is true (true branch) or false (false branch)
        /// </summary>
        public bool IsTrueBranch { get; set; }

        /// <summary>
        /// List of test cases that cover this branch
        /// </summary>
        public List<string> CoveringTestCases { get; set; } = new List<string>();

        /// <summary>
        /// Creates a clone of the coverage branch
        /// </summary>
        public CoverageBranch Clone()
        {
            return new CoverageBranch
            {
                Id = Id,
                SourceFile = SourceFile,
                LineNumber = LineNumber,
                FunctionName = FunctionName,
                Condition = Condition,
                IsTrueBranch = IsTrueBranch,
                CoveringTestCases = CoveringTestCases != null ? new List<string>(CoveringTestCases) : new List<string>()
            };
        }
    }
}
