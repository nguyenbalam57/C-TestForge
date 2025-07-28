using C_TestForge.Models;
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
    public interface IParserService
    {
        /// <summary>
        /// Parses a C source file and extracts information about definitions, variables, and functions
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <param name="options">Parsing options</param>
        /// <returns>Result of the parsing operation</returns>
        Task<ParseResult> ParseSourceFileAsync(string filePath, ParseOptions options);

        /// <summary>
        /// Parses C source code directly from a string
        /// </summary>
        /// <param name="sourceCode">Source code as a string</param>
        /// <param name="fileName">Name of the source file (for reference)</param>
        /// <param name="options">Parsing options</param>
        /// <returns>Result of the parsing operation</returns>
        Task<ParseResult> ParseSourceCodeAsync(string sourceCode, string fileName, ParseOptions options);

        /// <summary>
        /// Parses multiple C source files and merges the results
        /// </summary>
        /// <param name="filePaths">Paths to the source files</param>
        /// <param name="options">Parsing options</param>
        /// <returns>Combined result of the parsing operations</returns>
        Task<ParseResult> ParseMultipleSourceFilesAsync(IEnumerable<string> filePaths, ParseOptions options);

        /// <summary>
        /// Parses a header file to extract function declarations and type definitions
        /// </summary>
        /// <param name="headerPath">Path to the header file</param>
        /// <param name="options">Parsing options</param>
        /// <returns>Result of the parsing operation</returns>
        Task<ParseResult> ParseHeaderFileAsync(string headerPath, ParseOptions options);
    }
}
