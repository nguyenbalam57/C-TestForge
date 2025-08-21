using C_TestForge.Models.Base;
using C_TestForge.Models.Core.SupportingClasses;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a C union
    /// </summary>
    public class CUnion : SourceCodeEntity, ISymbol
    {
        /// <summary>
        /// List of members in the union
        /// </summary>
        public List<CUnionMember> Members { get; set; } = new List<CUnionMember>();

        /// <summary>
        /// Size of the union in bytes (size of largest member)
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Alignment requirement of the union
        /// </summary>
        public int Alignment { get; set; }

        /// <summary>
        /// Whether this is an anonymous union
        /// </summary>
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// Whether this is a forward declaration
        /// </summary>
        public bool IsForwardDeclaration { get; set; }

        /// <summary>
        /// Documentation for the union
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// Attributes applied to the union
        /// </summary>
        public List<CUnionAttribute> Attributes { get; set; } = new List<CUnionAttribute>();

        // ISymbol implementation
        string ISymbol.Type => "Union";

        public override string ToString()
        {
            return $"union {Name}";
        }

        public CUnion Clone()
        {
            return new CUnion
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                Members = Members?.Select(m => m.Clone()).ToList() ?? new List<CUnionMember>(),
                Size = Size,
                Alignment = Alignment,
                IsAnonymous = IsAnonymous,
                IsForwardDeclaration = IsForwardDeclaration,
                Documentation = Documentation,
                Attributes = Attributes?.Select(a => a.Clone()).ToList() ?? new List<CUnionAttribute>()
            };
        }
    }
}
