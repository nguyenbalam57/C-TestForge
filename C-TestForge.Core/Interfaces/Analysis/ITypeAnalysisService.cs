using C_TestForge.Models.Core;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using ClangSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Interface for typedef analysis service
    /// </summary>
    public interface ITypeAnalysisService
    {
        /// <summary>
        /// Extracts typedef information from a Clang cursor
        /// </summary>
        /// <param name="cursor">Clang cursor representing a typedef</param>
        /// <param name="result">Parse result to add typedef to</param>
        void ExtractTypedef(CXCursor cursor, ParseResult result);

        /// <summary>
        /// Analyzes typedefs and extracts constraints and relationships
        /// </summary>
        /// <param name="typedefs">List of typedefs to analyze</param>
        /// <param name="functions">List of functions for usage analysis</param>
        /// <param name="structures">List of structures for relationship analysis</param>
        /// <param name="enumerations">List of enumerations for relationship analysis</param>
        /// <returns>List of typedef constraints</returns>
        Task<List<TypedefConstraint>> AnalyzeTypedefsAsync(
            List<CTypedef> typedefs,
            List<CFunction> functions,
            List<CStruct> structures,
            List<CEnum> enumerations);

        /// <summary>
        /// Extracts constraints for a specific typedef
        /// </summary>
        /// <param name="typedef">Typedef to analyze</param>
        /// <param name="sourceFile">Source file for analysis</param>
        /// <returns>List of constraints for the typedef</returns>
        Task<List<TypedefConstraint>> ExtractConstraintsAsync(CTypedef typedef, SourceFile sourceFile);

        /// <summary>
        /// Analyzes typedef usage patterns across functions
        /// </summary>
        /// <param name="typedef">Typedef to analyze</param>
        /// <param name="functions">List of functions to check for usage</param>
        /// <returns>Usage statistics and patterns</returns>
        Task<TypedefUsageAnalysis> AnalyzeTypedefUsageAsync(CTypedef typedef, List<CFunction> functions);

        /// <summary>
        /// Resolves typedef chains to find the ultimate underlying type
        /// </summary>
        /// <param name="typedef">Typedef to resolve</param>
        /// <param name="allTypedefs">All available typedefs for resolution</param>
        /// <returns>The ultimate underlying type information</returns>
        TypedefResolution ResolveTypedefChain(CTypedef typedef, List<CTypedef> allTypedefs);

        /// <summary>
        /// Validates typedef definitions for potential issues
        /// </summary>
        /// <param name="typedefs">List of typedefs to validate</param>
        /// <returns>List of validation issues found</returns>
        List<TypedefValidationIssue> ValidateTypedefs(List<CTypedef> typedefs);
    }
}
