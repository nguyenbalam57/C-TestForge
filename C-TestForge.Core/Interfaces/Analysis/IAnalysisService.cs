using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Interface for analyzing C code
    /// </summary>
    public interface IAnalysisService
    {
        /// <summary>
        /// Analyzes a source file and extracts information
        /// </summary>
        /// <param name="sourceFile">Source file to analyze</param>
        /// <param name="options">Analysis options</param>
        /// <returns>Analysis result</returns>
        Task<AnalysisResult> AnalyzeSourceFileAsync(SourceFile sourceFile, AnalysisOptions options);

        /// <summary>
        /// Analyzes a project and extracts information
        /// </summary>
        /// <param name="project">Project to analyze</param>
        /// <param name="options">Analysis options</param>
        /// <returns>Analysis result</returns>
        Task<AnalysisResult> AnalyzeProjectAsync(Project project, AnalysisOptions options);

        /// <summary>
        /// Analyzes a function and its dependencies
        /// </summary>
        /// <param name="function">Function to analyze</param>
        /// <param name="projectContext">Project context for the analysis</param>
        /// <param name="options">Analysis options</param>
        /// <returns>Analysis result focusing on the function</returns>
        Task<AnalysisResult> AnalyzeFunctionAsync(CFunction function, Project projectContext, AnalysisOptions options);

        /// <summary>
        /// Analyzes the call graph of a function
        /// </summary>
        /// <param name="function">Function to analyze</param>
        /// <param name="projectContext">Project context for the analysis</param>
        /// <param name="maxDepth">Maximum depth of the call graph (0 for unlimited)</param>
        /// <returns>Call graph for the function</returns>
        Task<CallGraph> AnalyzeCallGraphAsync(CFunction function, Project projectContext, int maxDepth = 0);

        /// <summary>
        /// Analyzes the data flow of a function
        /// </summary>
        /// <param name="function">Function to analyze</param>
        /// <param name="projectContext">Project context for the analysis</param>
        /// <returns>Data flow graph for the function</returns>
        Task<DataFlowGraph> AnalyzeDataFlowAsync(CFunction function, Project projectContext);
    }

    /// <summary>
    /// Represents a call graph
    /// </summary>
    public class CallGraph
    {
        /// <summary>
        /// Root function of the call graph
        /// </summary>
        public string RootFunction { get; set; }

        /// <summary>
        /// Nodes in the call graph
        /// </summary>
        public List<CallGraphNode> Nodes { get; set; } = new List<CallGraphNode>();

        /// <summary>
        /// Edges in the call graph
        /// </summary>
        public List<CallGraphEdge> Edges { get; set; } = new List<CallGraphEdge>();
    }
    /// <summary>
    /// Represents a node in a call graph
    /// </summary>
    public class CallGraphNode
    {
        /// <summary>
        /// ID of the node
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Source file of the function
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Line number of the function
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Depth in the call graph
        /// </summary>
        public int Depth { get; set; }
    }

    /// <summary>
    /// Represents an edge in a call graph
    /// </summary>
    public class CallGraphEdge
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
        /// Line number of the call
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Source file of the call
        /// </summary>
        public string SourceFile { get; set; }
    }

    /// <summary>
    /// Represents a data flow graph
    /// </summary>
    public class DataFlowGraph
    {
        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Nodes in the data flow graph
        /// </summary>
        public List<DataFlowNode> Nodes { get; set; } = new List<DataFlowNode>();

        /// <summary>
        /// Edges in the data flow graph
        /// </summary>
        public List<DataFlowEdge> Edges { get; set; } = new List<DataFlowEdge>();
    }

    /// <summary>
    /// Represents a node in a data flow graph
    /// </summary>
    public class DataFlowNode
    {
        /// <summary>
        /// ID of the node
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Variable name
        /// </summary>
        public string VariableName { get; set; }

        /// <summary>
        /// Line number of the variable usage
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Type of the node (assignment, read, etc.)
        /// </summary>
        public string NodeType { get; set; }
    }

    /// <summary>
    /// Represents an edge in a data flow graph
    /// </summary>
    public class DataFlowEdge
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
        /// Type of the edge (data flow, control dependency, etc.)
        /// </summary>
        public string EdgeType { get; set; }
    }

}
