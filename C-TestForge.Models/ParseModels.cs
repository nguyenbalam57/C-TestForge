using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace C_TestForge.Models
{
    #region Parse Models

    /// <summary>
    /// Options for parsing C source files
    /// </summary>
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

        /// <summary>
        /// Creates a clone of the parse options
        /// </summary>
        public ParseOptions Clone()
        {
            return new ParseOptions
            {
                IncludePaths = new List<string>(IncludePaths),
                MacroDefinitions = new Dictionary<string, string>(MacroDefinitions),
                AdditionalClangArguments = new List<string>(AdditionalClangArguments),
                ParsePreprocessorDefinitions = ParsePreprocessorDefinitions,
                AnalyzeVariables = AnalyzeVariables,
                AnalyzeFunctions = AnalyzeFunctions
            };
        }
    }

    /// <summary>
    /// Result of parsing a C source file
    /// </summary>
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
        [JsonIgnore]
        public bool HasCriticalErrors => ParseErrors.Any(e => e.Severity == ErrorSeverity.Critical);

        /// <summary>
        /// Gets whether the parse result contains any errors
        /// </summary>
        [JsonIgnore]
        public bool HasErrors => ParseErrors.Any(e => e.Severity >= ErrorSeverity.Error);

        /// <summary>
        /// Gets whether the parse result contains any warnings
        /// </summary>
        [JsonIgnore]
        public bool HasWarnings => ParseErrors.Any(e => e.Severity == ErrorSeverity.Warning);

        /// <summary>
        /// Merges another parse result into this one
        /// </summary>
        public void Merge(ParseResult other)
        {
            if (other == null)
                return;

            Definitions.AddRange(other.Definitions);
            Variables.AddRange(other.Variables);
            Functions.AddRange(other.Functions);
            ConditionalDirectives.AddRange(other.ConditionalDirectives);
            ParseErrors.AddRange(other.ParseErrors);
        }
    }

    /// <summary>
    /// Result of preprocessing a C source file
    /// </summary>
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

    /// <summary>
    /// Level of detail for analysis
    /// </summary>
    public enum AnalysisLevel
    {
        Basic,
        Normal,
        Detailed,
        Comprehensive
    }

    /// <summary>
    /// Result of analyzing a C source file or project
    /// </summary>
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

        /// <summary>
        /// Get variable by name
        /// </summary>
        public CVariable GetVariable(string name)
        {
            return Variables.FirstOrDefault(v => v.Name == name);
        }

        /// <summary>
        /// Get function by name
        /// </summary>
        public CFunction GetFunction(string name)
        {
            return Functions.FirstOrDefault(f => f.Name == name);
        }

        /// <summary>
        /// Get definition by name
        /// </summary>
        public CDefinition GetDefinition(string name)
        {
            return Definitions.FirstOrDefault(d => d.Name == name);
        }

        /// <summary>
        /// Get functions that call the specified function
        /// </summary>
        public List<CFunction> GetCallers(string functionName)
        {
            var callerNames = FunctionRelationships
                .Where(r => r.CalleeName == functionName)
                .Select(r => r.CallerName)
                .ToList();

            return Functions
                .Where(f => callerNames.Contains(f.Name))
                .ToList();
        }

        /// <summary>
        /// Get functions called by the specified function
        /// </summary>
        public List<CFunction> GetCallees(string functionName)
        {
            var calleeNames = FunctionRelationships
                .Where(r => r.CallerName == functionName)
                .Select(r => r.CalleeName)
                .ToList();

            return Functions
                .Where(f => calleeNames.Contains(f.Name))
                .ToList();
        }

        /// <summary>
        /// Merges another analysis result into this one
        /// </summary>
        public void Merge(AnalysisResult other)
        {
            if (other == null)
                return;

            Variables.AddRange(other.Variables);
            Functions.AddRange(other.Functions);
            Definitions.AddRange(other.Definitions);
            ConditionalDirectives.AddRange(other.ConditionalDirectives);
            FunctionRelationships.AddRange(other.FunctionRelationships);
            VariableConstraints.AddRange(other.VariableConstraints);
        }
    }

    #endregion
}
