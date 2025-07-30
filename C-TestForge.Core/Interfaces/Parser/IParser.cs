using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Parser
{
    /// <summary>
    /// Interface for parsing C source code
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// Parses a C source file
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>Parsed source file model</returns>
        Task<SourceFile> ParseSourceFileAsync(string filePath);

        /// <summary>
        /// Parses C source code content
        /// </summary>
        /// <param name="sourceCode">Source code content</param>
        /// <param name="fileName">Optional file name for the source code</param>
        /// <returns>Parsed source file model</returns>
        Task<SourceFile> ParseSourceCodeAsync(string sourceCode, string fileName = "inline.c");
    }
}
