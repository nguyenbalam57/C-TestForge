using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models
{
    public class ParseOptions
    {
        /// <summary>
        /// List of include paths to add to the compilation
        /// </summary>
        public List<string> IncludePaths { get; set; } = new List<string>();

        /// <summary>
        /// Dictionary of macro definitions to add to the compilation
        /// </summary>
        public Dictionary<string, string> MacroDefinitions { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Additional command-line arguments to pass to clang
        /// </summary>
        public List<string> AdditionalClangArguments { get; set; } = new List<string>();

        /// <summary>
        /// Whether to parse preprocessor definitions
        /// </summary>
        public bool ParsePreprocessorDefinitions { get; set; } = true;

        /// <summary>
        /// Whether to analyze variables
        /// </summary>
        public bool AnalyzeVariables { get; set; } = true;

        /// <summary>
        /// Whether to analyze functions
        /// </summary>
        public bool AnalyzeFunctions { get; set; } = true;
    }

    public class ParseResult
    {
        /// <summary>
        /// Path to the source file that was parsed
        /// </summary>
        public string SourceFilePath { get; set; }

        /// <summary>
        /// List of preprocessor definitions found in the source file
        /// </summary>
        public List<CDefinition> Definitions { get; set; } = new List<CDefinition>();

        /// <summary>
        /// List of variables found in the source file
        /// </summary>
        public List<CVariable> Variables { get; set; } = new List<CVariable>();

        /// <summary>
        /// List of functions found in the source file
        /// </summary>
        public List<CFunction> Functions { get; set; } = new List<CFunction>();

        /// <summary>
        /// List of conditional directives found in the source file
        /// </summary>
        public List<ConditionalDirective> ConditionalDirectives { get; set; } = new List<ConditionalDirective>();

        /// <summary>
        /// List of errors that occurred during parsing
        /// </summary>
        public List<ParseError> ParseErrors { get; set; } = new List<ParseError>();

        /// <summary>
        /// Gets whether the parse result contains critical errors
        /// </summary>
        public bool HasCriticalErrors => ParseErrors.Any(e => e.Severity == ErrorSeverity.Critical);
    }

    public class PreprocessorResult
    {
        /// <summary>
        /// List of preprocessor definitions found in the source file
        /// </summary>
        public List<CDefinition> Definitions { get; set; } = new List<CDefinition>();

        /// <summary>
        /// List of conditional directives found in the source file
        /// </summary>
        public List<ConditionalDirective> ConditionalDirectives { get; set; } = new List<ConditionalDirective>();

        /// <summary>
        /// List of include directives found in the source file
        /// </summary>
        public List<IncludeDirective> Includes { get; set; } = new List<IncludeDirective>();
    }

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
    }

    public class AnalysisResult
    {
        /// <summary>
        /// List of variables found in the analysis
        /// </summary>
        public List<CVariable> Variables { get; set; } = new List<CVariable>();

        /// <summary>
        /// List of functions found in the analysis
        /// </summary>
        public List<CFunction> Functions { get; set; } = new List<CFunction>();

        /// <summary>
        /// List of preprocessor definitions found in the analysis
        /// </summary>
        public List<CDefinition> Definitions { get; set; } = new List<CDefinition>();

        /// <summary>
        /// List of conditional directives found in the analysis
        /// </summary>
        public List<ConditionalDirective> ConditionalDirectives { get; set; } = new List<ConditionalDirective>();

        /// <summary>
        /// List of function relationships found in the analysis
        /// </summary>
        public List<FunctionRelationship> FunctionRelationships { get; set; } = new List<FunctionRelationship>();

        /// <summary>
        /// List of variable constraints found in the analysis
        /// </summary>
        public List<VariableConstraint> VariableConstraints { get; set; } = new List<VariableConstraint>();
    }
}
