using System;
using System.Collections.Generic;

namespace C_TestForge.Models.Parse
{
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
        public List<string> MacroDefinitions { get; set; } = new List<string>();

        /// <summary>
        /// Additional command-line arguments to pass to clang
        /// </summary>
        public List<string> AdditionalClangArguments { get; set; } = new List<string>();

        /// <summary>
        /// Gets a default parse options object
        /// </summary>
        /// <returns>Default parse options</returns>
        public static ParseOptions Default => new ParseOptions
        {
            IncludePaths = new List<string>
            {
                //"/usr/include",
                //"/usr/local/include"
            },
            MacroDefinitions = new List<string>(),
            AdditionalClangArguments = new List<string>
            {
                "-std=c99"
            }
        };

        /// <summary>
        /// Creates a clone of the parse options
        /// </summary>
        public ParseOptions Clone()
        {
            return new ParseOptions
            {
                IncludePaths = new List<string>(IncludePaths),
                MacroDefinitions = new List<string>(MacroDefinitions),
                AdditionalClangArguments = new List<string>(AdditionalClangArguments),
            };
        }
    }
}