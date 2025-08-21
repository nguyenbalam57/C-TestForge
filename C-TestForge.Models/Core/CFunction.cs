using C_TestForge.Models.Base;
using C_TestForge.Models.Core.Enumerations;
using C_TestForge.Models.Core.SupportingClasses;
using C_TestForge.Models.Parse;
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
    /// <summary>
    /// Enhanced CFunction with additional properties
    /// </summary>
    public class CFunction : SourceCodeEntity, ISymbol
    {
        /// <summary>
        /// Return type of the function
        /// </summary>
        public string ReturnType { get; set; } = string.Empty;

        /// <summary>
        /// List of parameters
        /// </summary>
        public List<CParameter> Parameters { get; set; } = new List<CParameter>();

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
        /// Whether the function is variadic (accepts variable arguments)
        /// </summary>
        public bool IsVariadic { get; set; }

        /// <summary>
        /// Function visibility/linkage
        /// </summary>
        public FunctionLinkage Linkage { get; set; } = FunctionLinkage.Internal;

        /// <summary>
        /// Body of the function
        /// </summary>
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// List of functions called by this function
        /// </summary>
        public List<string> CalledFunctions { get; set; } = new List<string>();

        /// <summary>
        /// List of variables used by this function
        /// </summary>
        public List<string> UsedVariables { get; set; } = new List<string>();

        /// <summary>
        /// Local variables declared in this function
        /// </summary>
        public List<CVariable> LocalVariables { get; set; } = new List<CVariable>();

        /// <summary>
        /// Control flow paths in this function
        /// </summary>
        public List<ControlFlowPath> ControlFlowPaths { get; set; } = new List<ControlFlowPath>();

        /// <summary>
        /// Function attributes (e.g., __attribute__)
        /// </summary>
        public List<CFunctionAttribute> Attributes { get; set; } = new List<CFunctionAttribute>();

        /// <summary>
        /// Function comments/documentation
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        public int StartLineNumber { get; set; }
        public int EndLineNumber { get; set; }

        /// <summary>
        /// Cyclomatic complexity of the function
        /// </summary>
        public int CyclomaticComplexity { get; set; }

        /// <summary>
        /// Number of lines in the function
        /// </summary>
        [JsonIgnore]
        public int LineCount => EndLineNumber - StartLineNumber + 1;

        /// <summary>
        /// Whether this function is a declaration only (no body)
        /// </summary>
        public bool IsDeclarationOnly { get; set; }

        /// <summary>
        /// Whether this is a main function
        /// </summary>
        [JsonIgnore]
        public bool IsMainFunction => Name == "main";

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
                if (IsVariadic && Parameters.Count > 0)
                    paramList += ", ...";
                else if (IsVariadic)
                    paramList = "...";

                return $"{staticModifier}{inlineModifier}{ReturnType} {Name}({paramList})";
            }
        }

        // ISymbol implementation
        string ISymbol.Type => "Function";

        public override string ToString() => Signature;

        public CFunction Clone()
        {
            return new CFunction
            {
                Id = Id,
                Name = Name,
                ReturnType = ReturnType,
                Parameters = Parameters?.Select(p => p.Clone()).ToList() ?? new List<CParameter>(),
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                IsStatic = IsStatic,
                IsInline = IsInline,
                IsExternal = IsExternal,
                IsVariadic = IsVariadic,
                Linkage = Linkage,
                Body = Body,
                CalledFunctions = new List<string>(CalledFunctions ?? new List<string>()),
                UsedVariables = new List<string>(UsedVariables ?? new List<string>()),
                LocalVariables = LocalVariables?.Select(v => v.Clone()).ToList() ?? new List<CVariable>(),
                ControlFlowPaths = ControlFlowPaths?.Select(p => p.Clone()).ToList() ?? new List<ControlFlowPath>(),
                Attributes = Attributes?.Select(a => a.Clone()).ToList() ?? new List<CFunctionAttribute>(),
                Documentation = Documentation,
                StartLineNumber = StartLineNumber,
                EndLineNumber = EndLineNumber,
                CyclomaticComplexity = CyclomaticComplexity,
                IsDeclarationOnly = IsDeclarationOnly
            };
        }
    }
}