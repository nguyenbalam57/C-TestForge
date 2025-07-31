// Lớp ControlFlowPath.cs
using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a control flow path in a function
    /// </summary>
    public class ControlFlowPath : IModelObject
    {
        /// <summary>
        /// Unique identifier for the path
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Description of the path
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Boolean condition that must be satisfied to follow this path
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Sequence of statement IDs or line numbers in this path
        /// </summary>
        public List<int> Statements { get; set; } = new List<int>();

        /// <summary>
        /// Variables that are used in this path
        /// </summary>
        public List<string> UsedVariables { get; set; } = new List<string>();

        /// <summary>
        /// Variables that are modified in this path
        /// </summary>
        public List<string> ModifiedVariables { get; set; } = new List<string>();

        /// <summary>
        /// Functions that are called in this path
        /// </summary>
        public List<string> CalledFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Indicates if this path includes a return statement
        /// </summary>
        public bool HasReturn { get; set; }

        /// <summary>
        /// Return value expression (if HasReturn is true)
        /// </summary>
        public string ReturnExpression { get; set; } = string.Empty;

        /// <summary>
        /// Start line in the source code
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// End line in the source code
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Whether this path is feasible (can be executed)
        /// </summary>
        public bool IsFeasible { get; set; } = true;

        /// <summary>
        /// Whether this path has been covered by tests
        /// </summary>
        public bool IsCovered { get; set; } = false;

        /// <summary>
        /// Clone this control flow path
        /// </summary>
        public ControlFlowPath Clone()
        {
            return new ControlFlowPath
            {
                Id = Id,
                Description = Description,
                Condition = Condition,
                Statements = Statements != null ? new List<int>(Statements) : new List<int>(),
                UsedVariables = UsedVariables != null ? new List<string>(UsedVariables) : new List<string>(),
                ModifiedVariables = ModifiedVariables != null ? new List<string>(ModifiedVariables) : new List<string>(),
                CalledFunctions = CalledFunctions != null ? new List<string>(CalledFunctions) : new List<string>(),
                HasReturn = HasReturn,
                ReturnExpression = ReturnExpression,
                StartLine = StartLine,
                EndLine = EndLine,
                IsFeasible = IsFeasible,
                IsCovered = IsCovered
            };
        }

        /// <summary>
        /// Get a string representation of the control flow path
        /// </summary>
        public override string ToString()
        {
            return $"Path {Id}: {Description} [Condition: {Condition}]";
        }
    }
}