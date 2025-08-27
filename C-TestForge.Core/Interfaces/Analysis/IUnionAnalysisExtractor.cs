using C_TestForge.Models.Core;
using C_TestForge.Models.Core.SupportingClasses.Unions;
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
    /// Interface for union analysis and extraction service
    /// </summary>
    public interface IUnionAnalysisExtractor
    {
        /// <summary>
        /// Extract union definition from cursor
        /// </summary>
        /// <param name="cursor">Clang cursor representing the union</param>
        /// <param name="result">Parse result to add the union to</param>
        void ExtractUnionDefinition(CXCursor cursor, ParseResult result);

        /// <summary>
        /// Analyze union dependencies asynchronously
        /// </summary>
        /// <param name="unions">List of union definitions to analyze</param>
        /// <param name="result">Parse result for warnings and errors</param>
        /// <returns>List of union dependencies</returns>
        Task<List<UnionDependency>> AnalyzeUnionDependenciesAsync(
            List<CUnion> unions,
            ParseResult result);

        /// <summary>
        /// Validate union definition
        /// </summary>
        /// <param name="unionDef">Union definition to validate</param>
        /// <returns>List of validation errors</returns>
        List<ParseError> ValidateUnionDefinition(CUnion unionDef);

        /// <summary>
        /// Extract union constraints from source code
        /// </summary>
        /// <param name="unionDef">Union definition</param>
        /// <param name="sourceCode">Source code to analyze</param>
        /// <returns>List of union constraints</returns>
        Task<List<UnionConstraint>> ExtractUnionConstraintsAsync(
            CUnion unionDef,
            string sourceCode);

        /// <summary>
        /// Calculate union memory layout
        /// </summary>
        /// <param name="unionDef">Union definition</param>
        /// <returns>Memory layout information</returns>
        UnionMemoryLayout CalculateMemoryLayout(CUnion unionDef);
    }
}
