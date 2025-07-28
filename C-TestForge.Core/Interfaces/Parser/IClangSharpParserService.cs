using C_TestForge.Models;
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
        Task<CSourceFile> ParseSourceFileAsync(string filePath);

        /// <summary>
        /// Parses multiple C source files
        /// </summary>
        /// <param name="filePaths">Paths to the source files</param>
        /// <returns>List of parsed source file models</returns>
        Task<List<CSourceFile>> ParseSourceFilesAsync(IEnumerable<string> filePaths);

        /// <summary>
        /// Parses C source code string
        /// </summary>
        /// <param name="sourceCode">The source code to parse</param>
        /// <param name="fileName">Optional file name for the source</param>
        /// <returns>The parsed source file model</returns>
        Task<CSourceFile> ParseSourceCodeAsync(string sourceCode, string fileName = "inline.c");
    }

    /// <summary>
    /// Interface for preprocessor analysis service
    /// </summary>
    public interface IPreprocessorService
    {
        /// <summary>
        /// Extracts preprocessor directives from source file
        /// </summary>
        /// <param name="sourceFile">The source file to analyze</param>
        /// <returns>List of preprocessor definitions</returns>
        Task<List<CDefinition>> ExtractDefinitionsAsync(CSourceFile sourceFile);

        /// <summary>
        /// Analyzes preprocessor conditions and updates definition states
        /// </summary>
        /// <param name="sourceFile">The source file to analyze</param>
        /// <param name="activeDefinitions">List of actively defined macros</param>
        /// <returns>Updated source file with definition states</returns>
        Task<CSourceFile> AnalyzePreprocessorConditionsAsync(CSourceFile sourceFile, List<string> activeDefinitions);
    }
}
