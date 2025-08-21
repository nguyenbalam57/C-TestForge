using C_TestForge.Models;
using C_TestForge.Models.Core;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using ClangSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Parser
{
    /// <summary>
    /// Interface for handling preprocessor directives
    /// </summary>
    public interface IPreprocessorService
    {
        /// <summary>
        /// Extracts preprocessor definitions from a translation unit
        /// </summary>
        /// <param name="translationUnit">Clang translation unit</param>
        /// <param name="sourceFileName">Name of the source file</param>
        /// <returns>Preprocessor definitions and conditional directives</returns>
        Task<string> ExtractPreprocessorDefinitionsAsync(CXTranslationUnit translationUnit, List<SourceFile> sourceFiles, SourceFile sourceFile);

        /// <summary>
        /// Checks if a definition is enabled based on the current configuration
        /// </summary>
        /// <param name="definition">The definition to check</param>
        /// <param name="activeDefinitions">Dictionary of active definitions</param>
        /// <returns>True if the definition is enabled, false otherwise</returns>
        bool IsDefinitionEnabled(CDefinition definition, Dictionary<string, string> activeDefinitions);

        /// <summary>
        /// Evaluates a conditional directive based on the current configuration
        /// </summary>
        /// <param name="directive">The directive to evaluate</param>
        /// <param name="activeDefinitions">Dictionary of active definitions</param>
        /// <returns>True if the condition is satisfied, false otherwise</returns>
        bool EvaluateConditionalDirective(ConditionalDirective directive, Dictionary<string, string> activeDefinitions);

        /// <summary>
        /// Extracts conditional directives from source code
        /// </summary>
        /// <param name="sourceCode">Source code as a string</param>
        /// <param name="fileName">Name of the source file (for reference)</param>
        /// <returns>List of conditional directives</returns>
        Task<List<ConditionalDirective>> ExtractConditionalDirectivesAsync(string sourceCode, string fileName);

        /// <summary>
        /// Extracts include directives from source code
        /// </summary>
        /// <param name="sourceCode">Source code as a string</param>
        /// <param name="fileName">Name of the source file (for reference)</param>
        /// <returns>List of include directives</returns>
        Task<List<IncludeDirective>> ExtractIncludeDirectivesAsync(string sourceCode, string fileName);
    }
}
