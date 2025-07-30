using C_TestForge.Models;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Interface for the analysis service
    /// </summary>
    public interface IAnalysisService
    {
        /// <summary>
        /// Analyzes a source file
        /// </summary>
        /// <param name="sourceFile">Source file to analyze</param>
        /// <param name="options">Analysis options</param>
        /// <returns>Analysis result</returns>
        Task<AnalysisResult> AnalyzeSourceFileAsync(SourceFile sourceFile, AnalysisOptions options);

        /// <summary>
        /// Analyzes a project
        /// </summary>
        /// <param name="project">Project to analyze</param>
        /// <param name="options">Analysis options</param>
        /// <returns>Analysis result</returns>
        Task<AnalysisResult> AnalyzeProjectAsync(Project project, AnalysisOptions options);

        /// <summary>
        /// Checks if a source file has been modified since it was last analyzed
        /// </summary>
        /// <param name="sourceFile">Source file to check</param>
        /// <returns>True if the file has been modified, false otherwise</returns>
        Task<bool> IsSourceFileModifiedAsync(SourceFile sourceFile);
    }
}
