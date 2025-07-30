using C_TestForge.Models.CodeAnalysis.BranchAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.Functions
{
    /// <summary>
    /// Represents the result of a function analysis
    /// </summary>
    public class FunctionAnalysisResult
    {
        /// <summary>
        /// Gets or sets the function name
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the file path
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the return type
        /// </summary>
        public string ReturnType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the function parameters
        /// </summary>
        public List<FunctionParameter> Parameters { get; set; } = new List<FunctionParameter>();

        /// <summary>
        /// Gets or sets the function variables
        /// </summary>
        public List<FunctionVariable> Variables { get; set; } = new List<FunctionVariable>();

        /// <summary>
        /// Gets or sets the branches in the function
        /// </summary>
        public List<FunctionBranch> Branches { get; set; } = new List<FunctionBranch>();

        /// <summary>
        /// Gets or sets the paths through the function
        /// </summary>
        public List<FunctionPath> Paths { get; set; } = new List<FunctionPath>();

        /// <summary>
        /// Gets or sets the functions called by this function
        /// </summary>
        public List<string> CalledFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the start line of the function
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// Gets or sets the end line of the function
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Gets or sets the cyclomatic complexity
        /// </summary>
        public int CyclomaticComplexity { get; set; }

        /// <summary>
        /// Gets or sets the function body text
        /// </summary>
        public string Body { get; set; } = string.Empty;
    }
}
