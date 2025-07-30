using C_TestForge.Models;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Interface for the macro analysis service
    /// </summary>
    public interface IMacroAnalysisService
    {
        /// <summary>
        /// Analyzes relationships between macros
        /// </summary>
        /// <param name="definitions">List of definitions to analyze</param>
        /// <param name="conditionalDirectives">List of conditional directives to analyze</param>
        /// <returns>Task</returns>
        Task AnalyzeMacroRelationshipsAsync(List<CDefinition> definitions, List<ConditionalDirective> conditionalDirectives);

        /// <summary>
        /// Extracts dependencies for a macro
        /// </summary>
        /// <param name="definition">Definition to analyze</param>
        /// <param name="allDefinitions">All available definitions</param>
        /// <returns>List of dependencies</returns>
        Task<List<CDefinition>> ExtractMacroDependenciesAsync(CDefinition definition, List<CDefinition> allDefinitions);

        /// <summary>
        /// Evaluates a macro expression
        /// </summary>
        /// <param name="expression">Expression to evaluate</param>
        /// <param name="activeDefinitions">Dictionary of active definitions</param>
        /// <returns>Result of the evaluation</returns>
        Task<string> EvaluateMacroExpressionAsync(string expression, Dictionary<string, string> activeDefinitions);

        /// <summary>
        /// Determines which files include a specific macro
        /// </summary>
        /// <param name="definition">Definition to analyze</param>
        /// <param name="projectContext">Project context for the analysis</param>
        /// <returns>List of files that include the macro</returns>
        Task<List<string>> FindMacroUsagesAsync(CDefinition definition, Project projectContext);
    }
}
