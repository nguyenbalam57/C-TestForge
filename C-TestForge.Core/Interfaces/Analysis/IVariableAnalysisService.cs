using C_TestForge.Models;
using ClangSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Interface for analyzing variables
    /// </summary>
    public interface IVariableAnalysisService
    {
        /// <summary>
        /// Extracts variable information from a cursor
        /// </summary>
        /// <param name="cursor">Clang cursor</param>
        /// <returns>Variable object</returns>
        CVariable ExtractVariable(CXCursor cursor);

        /// <summary>
        /// Analyzes variables for constraints and relationships
        /// </summary>
        /// <param name="variables">List of variables to analyze</param>
        /// <param name="functions">List of functions to analyze</param>
        /// <param name="definitions">List of definitions to analyze</param>
        /// <returns>List of variable constraints</returns>
        Task<List<VariableConstraint>> AnalyzeVariablesAsync(List<CVariable> variables, List<CFunction> functions, List<CDefinition> definitions);

        /// <summary>
        /// Extracts constraints for a variable from the source code
        /// </summary>
        /// <param name="variable">Variable to analyze</param>
        /// <param name="sourceFile">Source file containing the variable</param>
        /// <returns>List of constraints</returns>
        Task<List<VariableConstraint>> ExtractConstraintsAsync(CVariable variable, SourceFile sourceFile);

        /// <summary>
        /// Determines valid value ranges for a variable
        /// </summary>
        /// <param name="variable">Variable to analyze</param>
        /// <returns>Value range information</returns>
        Task<ValueRange> DetermineValueRangeAsync(CVariable variable);

        /// <summary>
        /// Analyzes data flow for a variable in a function
        /// </summary>
        /// <param name="variable">Variable to analyze</param>
        /// <param name="function">Function containing the variable</param>
        /// <param name="sourceFile">Source file containing the function</param>
        /// <returns>Data flow information</returns>
        Task<VariableDataFlow> AnalyzeVariableDataFlowAsync(CVariable variable, CFunction function, SourceFile sourceFile);
    }

    /// <summary>
    /// Represents a value range for a variable
    /// </summary>
    public class ValueRange
    {
        /// <summary>
        /// Variable name
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// Minimum value (as a string, to support different types)
        /// </summary>
        public string MinValue { get; set; }

        /// <summary>
        /// Maximum value (as a string, to support different types)
        /// </summary>
        public string MaxValue { get; set; }

        /// <summary>
        /// List of allowed values (for enums)
        /// </summary>
        public List<string> AllowedValues { get; set; }

        /// <summary>
        /// Whether the value is constrained to be positive
        /// </summary>
        public bool MustBePositive { get; set; }

        /// <summary>
        /// Whether the value is constrained to be non-zero
        /// </summary>
        public bool MustBeNonZero { get; set; }
    }

    /// <summary>
    /// Represents data flow for a variable
    /// </summary>
    public class VariableDataFlow
    {
        /// <summary>
        /// Variable name
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// List of assignments to the variable
        /// </summary>
        public List<VariableAssignment> Assignments { get; set; } = new List<VariableAssignment>();

        /// <summary>
        /// List of usages of the variable
        /// </summary>
        public List<VariableUsage> Usages { get; set; } = new List<VariableUsage>();
    }

    /// <summary>
    /// Represents an assignment to a variable
    /// </summary>
    public class VariableAssignment
    {
        /// <summary>
        /// Line number of the assignment
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Expression used in the assignment
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Variables used in the expression
        /// </summary>
        public List<string> UsedVariables { get; set; } = new List<string>();
    }

    /// <summary>
    /// Represents a usage of a variable
    /// </summary>
    public class VariableUsage
    {
        /// <summary>
        /// Line number of the usage
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Context of the usage (statement, expression, etc.)
        /// </summary>
        public string Context { get; set; }

        /// <summary>
        /// Type of usage (read, write, both)
        /// </summary>
        public string UsageType { get; set; }
    }
}
