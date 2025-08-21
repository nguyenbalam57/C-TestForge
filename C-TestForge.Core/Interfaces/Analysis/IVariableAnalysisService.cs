using C_TestForge.Models;
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
    /// Interface for the variable analysis service
    /// </summary>
    public interface IVariableAnalysisService
    {
        /// <summary>
        /// Extracts variable information from a cursor
        /// </summary>
        /// <param name="cursor">Clang cursor</param>
        /// <returns>Variable object</returns>
        void ExtractVariable(CXCursor cursor, ParseResult result);

        /// <summary>
        /// Analyzes variables for constraints and relationships
        /// </summary>
        /// <param name="variables">List of variables to analyze</param>
        /// <param name="functions">List of functions to analyze</param>
        /// <param name="definitions">List of definitions to analyze</param>
        /// <returns>List of variable constraints</returns>
        Task<List<VariableConstraint>> AnalyzeVariablesAsync(List<CVariable> variables, List<CFunction> functions, List<CDefinition> definitions);

        /// <summary>
        /// Extracts constraints for a variable from the source code
        /// </summary>
        /// <param name="variable">Variable to analyze</param>
        /// <param name="sourceFile">Source file containing the variable</param>
        /// <returns>List of constraints</returns>
        Task<List<VariableConstraint>> ExtractConstraintsAsync(CVariable variable, SourceFile sourceFile);
    }

}
