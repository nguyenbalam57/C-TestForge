using C_TestForge.Models.CodeAnalysis.Functions;
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
        Task<ParseResult> ParseSourceFileParserAsync(SourceFile sourceFile, ParseOptions options);

        /// <summary>
        /// Parses C source code content with options
        /// </summary>
        /// <param name="sourceCode">Source code content</param>
        /// <param name="fileName">Name of the source file</param>
        /// <param name="options">Parse options</param>
        /// <returns>Parse result</returns>
        Task<ParseResult> ParseSourceCodeParserAsync(SourceFile sourceFile, ParseOptions options);

        /// <summary>
        /// Gets a list of files included by a source file
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>List of included file paths</returns>
        Task<List<string>> GetIncludedFilesParserAsync(SourceFile sourceFile);

        /// <summary>
        /// Analyzes a specific function in a source file
        /// </summary>
        /// <param name="functionName">Name of the function to analyze</param>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>Function analysis result</returns>
        Task<FunctionAnalysisResult> AnalyzeFunctionParserAsync(string functionName, SourceFile sourceFile);
    }
}