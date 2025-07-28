using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a function in C code
    /// </summary>
    public class CFunction : SourceCodeEntity
    {
        /// <summary>
        /// Return type of the function
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// List of parameters
        /// </summary>
        public List<CVariable> Parameters { get; set; } = new List<CVariable>();

        /// <summary>
        /// Whether the function is static
        /// </summary>
        public bool IsStatic { get; set; }

        /// <summary>
        /// Whether the function is inline
        /// </summary>
        public bool IsInline { get; set; }

        /// <summary>
        /// Whether the function is external
        /// </summary>
        public bool IsExternal { get; set; }

        /// <summary>
        /// Body of the function
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// List of functions called by this function
        /// </summary>
        public List<string> CalledFunctions { get; set; } = new List<string>();

        /// <summary>
        /// List of variables used by this function
        /// </summary>
        public List<string> UsedVariables { get; set; } = new List<string>();

        /// <summary>
        /// Get the function signature
        /// </summary>
        [JsonIgnore]
        public string Signature
        {
            get
            {
                string staticModifier = IsStatic ? "static " : "";
                string inlineModifier = IsInline ? "inline " : "";
                string paramList = string.Join(", ", Parameters.Select(p => p.ToString()));

                return $"{staticModifier}{inlineModifier}{ReturnType} {Name}({paramList})";
            }
        }

        /// <summary>
        /// Get a string representation of the function
        /// </summary>
        public override string ToString()
        {
            return Signature;
        }

        /// <summary>
        /// Create a clone of the function
        /// </summary>
        public CFunction Clone()
        {
            return new CFunction
            {
                Id = Id,
                Name = Name,
                ReturnType = ReturnType,
                Parameters = Parameters?.Select(p => p.Clone()).ToList() ?? new List<CVariable>(),
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                IsStatic = IsStatic,
                IsInline = IsInline,
                IsExternal = IsExternal,
                Body = Body,
                CalledFunctions = CalledFunctions != null ? new List<string>(CalledFunctions) : new List<string>(),
                UsedVariables = UsedVariables != null ? new List<string>(UsedVariables) : new List<string>()
            };
        }
    }
}
