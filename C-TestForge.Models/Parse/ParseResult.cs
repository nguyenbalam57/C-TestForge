using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using C_TestForge.Models.Core;

namespace C_TestForge.Models.Parse
{
    /// <summary>
    /// Result of parsing a C source file
    /// </summary>
    public class ParseResult
    {
        /// <summary>
        /// Path to the source file that was parsed
        /// </summary>
        public string SourceFilePath { get; set; } = string.Empty;

        /// <summary>
        /// List of preprocessor directives in the file
        /// </summary>
        public List<CPreprocessorDirective> PreprocessorDirectives { get; set; } = new List<CPreprocessorDirective>();


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
        /// List of function relationships found in the analysis
        /// </summary>
        public List<FunctionRelationship> FunctionRelationships { get; set; } = new List<FunctionRelationship>();

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

            PreprocessorDirectives.AddRange(other.PreprocessorDirectives);
            Definitions.AddRange(other.Definitions);
            Variables.AddRange(other.Variables);
            Functions.AddRange(other.Functions);
            ConditionalDirectives.AddRange(other.ConditionalDirectives);
            ParseErrors.AddRange(other.ParseErrors);
        }
    }
}