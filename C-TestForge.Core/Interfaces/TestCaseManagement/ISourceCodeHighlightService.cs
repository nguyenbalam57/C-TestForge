using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for highlighting source code
    /// </summary>
    public interface ISourceCodeHighlightService
    {
        /// <summary>
        /// Highlights C source code
        /// </summary>
        string HighlightCSourceCode(string sourceCode);

        /// <summary>
        /// Gets highlighted source code with line numbers
        /// </summary>
        string GetHighlightedSourceCodeWithLineNumbers(string sourceCode);

        /// <summary>
        /// Highlights a specific part of the source code
        /// </summary>
        string HighlightSourceCodeSection(string sourceCode, int startLine, int endLine);
    }
}
