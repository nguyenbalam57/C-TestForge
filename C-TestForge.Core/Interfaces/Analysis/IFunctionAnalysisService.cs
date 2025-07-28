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
    /// Interface for analyzing functions
    /// </summary>
    public interface IFunctionAnalysisService
    {
        /// <summary>
        /// Extracts function information from a cursor
        /// </summary>
        /// <param name="cursor">Clang cursor</param>
        /// <returns>Function object</returns>
        CFunction ExtractFunction(CXCursor cursor);

        /// <summary>
        /// Analyzes relationships between functions
        /// </summary>
        /// <param name="functions">List of functions to analyze</param>
        /// <returns>List of function relationships</returns>
        Task<List<FunctionRelationship>> AnalyzeFunctionRelationshipsAsync(List<CFunction> functions);

        /// <summary>
        /// Extracts the control flow graph of a function
        /// </summary>
        /// <param name="function">Function to analyze</param>
        /// <param name="sourceFile">Source file containing the function</param>
        /// <returns>Control flow graph</returns>
        Task<ControlFlowGraph> ExtractControlFlowGraphAsync(CFunction function, SourceFile sourceFile);

        /// <summary>
        /// Analyzes the complexity of a function
        /// </summary>
        /// <param name="function">Function to analyze</param>
        /// <param name="sourceFile">Source file containing the function</param>
        /// <returns>Complexity metrics</returns>
        Task<FunctionComplexity> AnalyzeFunctionComplexityAsync(CFunction function, SourceFile sourceFile);

        /// <summary>
        /// Determines which variables are used in a function
        /// </summary>
        /// <param name="function">Function to analyze</param>
        /// <param name="allVariables">All available variables</param>
        /// <returns>List of variables used in the function</returns>
        Task<List<CVariable>> AnalyzeFunctionVariableUsageAsync(CFunction function, List<CVariable> allVariables);
    }

    /// <summary>
    /// Represents a control flow graph
    /// </summary>
    public class ControlFlowGraph
    {
        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Nodes in the control flow graph
        /// </summary>
        public List<ControlFlowNode> Nodes { get; set; } = new List<ControlFlowNode>();

        /// <summary>
        /// Edges in the control flow graph
        /// </summary>
        public List<ControlFlowEdge> Edges { get; set; } = new List<ControlFlowEdge>();
    }

    /// <summary>
    /// Represents a node in a control flow graph
    /// </summary>
    public class ControlFlowNode
    {
        /// <summary>
        /// ID of the node
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Type of the node (statement, condition, loop, etc.)
        /// </summary>
        public string NodeType { get; set; }

        /// <summary>
        /// Line number in the source file
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Code associated with the node
        /// </summary>
        public string Code { get; set; }
    }

    /// <summary>
    /// Represents an edge in a control flow graph
    /// </summary>
    public class ControlFlowEdge
    {
        /// <summary>
        /// ID of the edge
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Source node ID
        /// </summary>
        public string SourceId { get; set; }

        /// <summary>
        /// Target node ID
        /// </summary>
        public string TargetId { get; set; }

        /// <summary>
        /// Type of the edge (true branch, false branch, unconditional, etc.)
        /// </summary>
        public string EdgeType { get; set; }

        /// <summary>
        /// Condition for the edge (if applicable)
        /// </summary>
        public string Condition { get; set; }
    }

    /// <summary>
    /// Represents complexity metrics for a function
    /// </summary>
    public class FunctionComplexity
    {
        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Cyclomatic complexity
        /// </summary>
        public int CyclomaticComplexity { get; set; }

        /// <summary>
        /// Number of lines of code
        /// </summary>
        public int LinesOfCode { get; set; }

        /// <summary>
        /// Number of parameters
        /// </summary>
        public int ParameterCount { get; set; }

        /// <summary>
        /// Nesting depth
        /// </summary>
        public int NestingDepth { get; set; }

        /// <summary>
        /// Number of statements
        /// </summary>
        public int StatementCount { get; set; }

        /// <summary>
        /// Number of conditions
        /// </summary>
        public int ConditionCount { get; set; }
    }
}
