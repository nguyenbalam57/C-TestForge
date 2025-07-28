using System;
using System.Collections.Generic;
using System.Linq;
using C_TestForge.Models.Core;

namespace C_TestForge.Models.Parse
{
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
}