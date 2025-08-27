using C_TestForge.Models.Core;
using C_TestForge.Models.Core.SupportingClasses.Enums;
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
    /// Interface for enum analysis and extraction service
    /// </summary>
    public interface IEnumAnalysisExtractor
    {
        /// <summary>
        /// Extract enum definition from a cursor
        /// </summary>
        /// <param name="cursor">The cursor pointing to the enum declaration</param>
        /// <param name="result">Parse result to store the extracted enum</param>
        void ExtractEnum(CXCursor cursor, ParseResult result);

        /// <summary>
        /// Analyze enum dependencies and relationships
        /// </summary>
        /// <param name="enums">List of all enum definitions</param>
        /// <param name="result">Parse result to update with dependency information</param>
        /// <returns>List of enum dependencies found</returns>
        Task<List<EnumDependency>> AnalyzeEnumDependenciesAsync(List<CEnum> enums, ParseResult result);

        /// <summary>
        /// Validate enum definition syntax and semantics
        /// </summary>
        /// <param name="enumEntity">The enum definition to validate</param>
        /// <returns>List of validation errors and warnings</returns>
        List<ParseError> ValidateEnum(CEnum enumEntity);

        /// <summary>
        /// Extract constraints from enum usage patterns and structure
        /// </summary>
        /// <param name="enumEntity">Enum definition to analyze</param>
        /// <param name="sourceCode">Source code content for usage analysis</param>
        /// <returns>List of structural and usage-based constraints</returns>
        Task<List<EnumConstraint>> ExtractEnumConstraintsAsync(CEnum enumEntity, string sourceCode);
    }
}
