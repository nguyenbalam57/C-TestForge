using C_TestForge.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Parse
{
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
}
