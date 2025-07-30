using C_TestForge.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Parse
{
    /// <summary>
    /// Result of preprocessing analysis
    /// </summary>
    public class PreprocessorResult
    {
        /// <summary>
        /// List of extracted preprocessor definitions
        /// </summary>
        public List<CDefinition> Definitions { get; set; } = new List<CDefinition>();

        /// <summary>
        /// List of extracted conditional directives
        /// </summary>
        public List<ConditionalDirective> ConditionalDirectives { get; set; } = new List<ConditionalDirective>();

        /// <summary>
        /// List of extracted include directives
        /// </summary>
        public List<IncludeDirective> Includes { get; set; } = new List<IncludeDirective>();

        /// <summary>
        /// List of all preprocessor directives (includes, conditional directives, etc.)
        /// </summary>
        public List<CPreprocessorDirective> PreprocessorDirectives { get; set; } = new List<CPreprocessorDirective>();

        /// <summary>
        /// Merge another preprocessor result into this one
        /// </summary>
        /// <param name="other">Result to merge</param>
        public void Merge(PreprocessorResult other)
        {
            if (other == null)
                return;

            Definitions.AddRange(other.Definitions);
            ConditionalDirectives.AddRange(other.ConditionalDirectives);
            Includes.AddRange(other.Includes);
            PreprocessorDirectives.AddRange(other.PreprocessorDirectives);
        }
    }
}