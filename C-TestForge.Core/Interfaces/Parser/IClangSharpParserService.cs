using C_TestForge.Models;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Parser
{
    /// <summary>
    /// Interface for the ClangSharp parser service
    /// </summary>
    public interface IClangSharpParserService
    {
        /// <summary>
        /// Parses a C source file
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>The parsed source file model</returns>
        Task<SourceFile> ParseSourceFileAsync(string filePath);

        /// <summary>
        /// Parses multiple C source files
        /// </summary>
        /// <param name="filePaths">Paths to the source files</param>
        /// <returns>List of parsed source file models</returns>
        Task<List<SourceFile>> ParseSourceFilesAsync(IEnumerable<string> filePaths);

        /// <summary>
        /// Parses C source code string
        /// </summary>
        /// <param name="sourceCode">The source code to parse</param>
        /// <param name="fileName">Optional file name for the source</param>
        /// <returns>The parsed source file model</returns>
        Task<SourceFile> ParseSourceCodeAsync(string sourceCode, string fileName = "inline.c");

        /// <summary>
        /// Extracts all functions from a C source file
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>List of extracted functions</returns>
        Task<List<CFunction>> ExtractFunctionsAsync(string filePath);

        /// <summary>
        /// Extracts all functions from C source code string
        /// </summary>
        /// <param name="sourceCode">The source code to parse</param>
        /// <param name="fileName">Optional file name for the source</param>
        /// <returns>List of extracted functions</returns>
        Task<List<CFunction>> ExtractFunctionsFromCodeAsync(string sourceCode, string fileName = "inline.c");
    }

}
