using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a conditional directive in C code
    /// </summary>
    public class ConditionalDirective : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Type of the conditional directive
        /// </summary>
        public ConditionalType Type { get; set; }

        /// <summary>
        /// Condition of the directive
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Line number in the source file
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// End line number in the source file
        /// </summary>
        public int EndLineNumber { get; set; }

        /// <summary>
        /// Source file where the directive is defined
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Parent directive (for else/elif)
        /// </summary>
        [JsonIgnore]
        public ConditionalDirective ParentDirective { get; set; }

        /// <summary>
        /// List of branch directives (else/elif)
        /// </summary>
        public List<ConditionalDirective> Branches { get; set; } = new List<ConditionalDirective>();

        /// <summary>
        /// List of definitions that this directive depends on
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Whether the condition is currently satisfied
        /// </summary>
        [JsonIgnore]
        public bool IsConditionSatisfied { get; set; }

        /// <summary>
        /// ID of the parent directive
        /// </summary>
        public string ParentDirectiveId => ParentDirective?.Id;

        /// <summary>
        /// Get a string representation of the conditional directive
        /// </summary>
        public override string ToString()
        {
            switch (Type)
            {
                case ConditionalType.If:
                    return $"#if {Condition}";
                case ConditionalType.IfDef:
                    return $"#ifdef {Condition}";
                case ConditionalType.IfNDef:
                    return $"#ifndef {Condition}";
                case ConditionalType.ElseIf:
                    return $"#elif {Condition}";
                case ConditionalType.Else:
                    return "#else";
                default:
                    return "Unknown conditional";
            }
        }

        /// <summary>
        /// Create a clone of the conditional directive
        /// </summary>
        public ConditionalDirective Clone()
        {
            var clone = new ConditionalDirective
            {
                Id = Id,
                Type = Type,
                Condition = Condition,
                LineNumber = LineNumber,
                EndLineNumber = EndLineNumber,
                SourceFile = SourceFile,
                // Don't clone parent to avoid circular references
                IsConditionSatisfied = IsConditionSatisfied,
                Dependencies = Dependencies != null ? new List<string>(Dependencies) : new List<string>()
            };

            // Clone branches
            clone.Branches = Branches?.Select(b => b.Clone()).ToList() ?? new List<ConditionalDirective>();
            foreach (var branch in clone.Branches)
            {
                branch.ParentDirective = clone;
            }

            return clone;
        }
    }
}
