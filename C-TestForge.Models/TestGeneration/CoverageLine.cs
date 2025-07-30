using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestGeneration
{
    /// <summary>
    /// Represents a line for coverage tracking
    /// </summary>
    public class CoverageLine : IModelObject
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
        /// Line content
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Whether the line is executable
        /// </summary>
        public bool IsExecutable { get; set; } = true;

        /// <summary>
        /// List of test cases that cover this line
        /// </summary>
        public List<string> CoveringTestCases { get; set; } = new List<string>();

        /// <summary>
        /// Creates a clone of the coverage line
        /// </summary>
        public CoverageLine Clone()
        {
            return new CoverageLine
            {
                Id = Id,
                SourceFile = SourceFile,
                LineNumber = LineNumber,
                FunctionName = FunctionName,
                Content = Content,
                IsExecutable = IsExecutable,
                CoveringTestCases = CoveringTestCases != null ? new List<string>(CoveringTestCases) : new List<string>()
            };
        }
    }
}
