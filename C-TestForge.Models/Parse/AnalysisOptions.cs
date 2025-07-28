namespace C_TestForge.Models.Parse
{
    /// <summary>
    /// Options for analyzing C source files
    /// </summary>
    public class AnalysisOptions
    {
        /// <summary>
        /// Whether to analyze variables
        /// </summary>
        public bool AnalyzeVariables { get; set; } = true;

        /// <summary>
        /// Whether to analyze functions
        /// </summary>
        public bool AnalyzeFunctions { get; set; } = true;

        /// <summary>
        /// Whether to analyze preprocessor definitions
        /// </summary>
        public bool AnalyzePreprocessorDefinitions { get; set; } = true;

        /// <summary>
        /// Whether to analyze function relationships
        /// </summary>
        public bool AnalyzeFunctionRelationships { get; set; } = true;

        /// <summary>
        /// Whether to analyze variable constraints
        /// </summary>
        public bool AnalyzeVariableConstraints { get; set; } = true;

        /// <summary>
        /// Level of detail for analysis
        /// </summary>
        public AnalysisLevel DetailLevel { get; set; } = AnalysisLevel.Normal;

        /// <summary>
        /// Creates a clone of the analysis options
        /// </summary>
        public AnalysisOptions Clone()
        {
            return new AnalysisOptions
            {
                AnalyzeVariables = AnalyzeVariables,
                AnalyzeFunctions = AnalyzeFunctions,
                AnalyzePreprocessorDefinitions = AnalyzePreprocessorDefinitions,
                AnalyzeFunctionRelationships = AnalyzeFunctionRelationships,
                AnalyzeVariableConstraints = AnalyzeVariableConstraints,
                DetailLevel = DetailLevel
            };
        }
    }
}