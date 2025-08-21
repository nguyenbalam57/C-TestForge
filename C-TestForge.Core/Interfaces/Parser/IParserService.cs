
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Parser
{
    /// <summary>
    /// Interface for parsing C source code with more detailed options
    /// </summary>
    public interface IParserService
    {
        /// <summary>
        /// Parses a C source file with options
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <param name="options">Parse options</param>
        /// <returns>Parse result</returns>
        Task<ParseResult> ParseSourceFileParserAsync(List<SourceFile> sourceFiles, SourceFile sourceFile, ParseOptions options);

        /// <summary>
        /// Parses C source code content with options
        /// </summary>
        /// <param name="sourceCode">Source code content</param>
        /// <param name="fileName">Name of the source file</param>
        /// <param name="options">Parse options</param>
        /// <returns>Parse result</returns>
        Task<ParseResult> ParseSourceCodeParserAsync(List<SourceFile> sourceFiles, SourceFile sourceFile, ParseOptions options);

    }
}