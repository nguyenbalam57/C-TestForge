using C_TestForge.Models.Core;
using C_TestForge.Models.Core.SupportingClasses.Structs;
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
    /// Interface for struct analysis and extraction service
    /// </summary>
    public interface IStructAnalysisExtractor
    {
        /// <summary>
        /// Extract struct definition from cursor
        /// </summary>
        /// <param name="cursor">Clang cursor representing the struct</param>
        /// <param name="result">Parse result to add the struct to</param>
        void ExtractStructDefinition(CXCursor cursor, ParseResult result);

        /// <summary>
        /// Analyze struct dependencies asynchronously
        /// </summary>
        /// <param name="structs">List of struct definitions to analyze</param>
        /// <param name="result">Parse result for warnings and errors</param>
        /// <returns>List of struct dependencies</returns>
        Task<List<StructDependency>> AnalyzeStructDependenciesAsync(
            List<CStruct> structs,
            ParseResult result);

        /// <summary>
        /// Validate struct definition
        /// </summary>
        /// <param name="structDef">Struct definition to validate</param>
        /// <returns>List of validation errors</returns>
        List<ParseError> ValidateStructDefinition(CStruct structDef);

        /// <summary>
        /// Extract struct constraints from source code
        /// </summary>
        /// <param name="structDef">Struct definition</param>
        /// <param name="sourceCode">Source code to analyze</param>
        /// <returns>List of struct constraints</returns>
        Task<List<StructConstraint>> ExtractStructConstraintsAsync(
            CStruct structDef,
            string sourceCode);

        /// <summary>
        /// Calculate struct memory layout
        /// </summary>
        /// <param name="structDef">Struct definition</param>
        /// <returns>Memory layout information</returns>
        StructMemoryLayout CalculateMemoryLayout(CStruct structDef);
    }
}
