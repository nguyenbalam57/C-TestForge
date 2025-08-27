using C_TestForge.Models.Core;
using C_TestForge.Models.Core.Enumerations;
using C_TestForge.Models.Parse;
using ClangSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Interface for macro definition analysis service
    /// </summary>
    public interface IMacroDefineExtractor
    {
        /// <summary>
        /// Extract macro definition from a cursor
        /// </summary>
        /// <param name="cursor">The cursor pointing to the macro definition</param>
        /// <param name="result">Parse result to store the extracted macro</param>
        void ExtractMacroDefine(CXCursor cursor, ParseResult result);

        /// <summary>
        /// Analyze macro dependencies and relationships
        /// </summary>
        /// <param name="definitions">List of all macro definitions</param>
        /// <param name="result">Parse result to update with dependency information</param>
        Task<List<MacroDependency>> AnalyzeMacroDependenciesAsync(List<CDefinition> definitions, ParseResult result);

        /// <summary>
        /// Validate macro definition syntax and semantics
        /// </summary>
        /// <param name="definition">The macro definition to validate</param>
        /// <returns>List of validation errors and warnings</returns>
        List<ParseError> ValidateMacroDefinition(CDefinition definition);

        /// <summary>
        /// Extract constraints from macro usage patterns
        /// </summary>
        /// <param name="definition">Macro definition to analyze</param>
        /// <param name="sourceCode">Source code content</param>
        /// <returns>List of usage-based constraints</returns>
        Task<List<MacroConstraint>> ExtractMacroConstraintsAsync(CDefinition definition, string sourceCode);
    }
}
