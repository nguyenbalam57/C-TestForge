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
    /// Represents a C structure
    /// </summary>
    public class CStruct : SourceCodeEntity, ISymbol
    {
        /// <summary>
        /// List of fields/members in the structure
        /// </summary>
        public List<CStructField> Fields { get; set; } = new List<CStructField>();

        /// <summary>
        /// Size of the structure in bytes
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Alignment requirement of the structure
        /// </summary>
        public int Alignment { get; set; }

        /// <summary>
        /// Whether this is a packed structure
        /// </summary>
        public bool IsPacked { get; set; }

        /// <summary>
        /// Whether this is an anonymous structure
        /// </summary>
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// Whether this is a forward declaration
        /// </summary>
        public bool IsForwardDeclaration { get; set; }

        /// <summary>
        /// Documentation for the structure
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// Attributes applied to the structure
        /// </summary>
        public List<CStructAttribute> Attributes { get; set; } = new List<CStructAttribute>();

        /// <summary>
        /// Structures that this structure depends on
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        // ISymbol implementation
        string ISymbol.Type => "Struct";

        public override string ToString()
        {
            return $"struct {Name}";
        }

        public CStruct Clone()
        {
            return new CStruct
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                Fields = Fields?.Select(f => f.Clone()).ToList() ?? new List<CStructField>(),
                Size = Size,
                Alignment = Alignment,
                IsPacked = IsPacked,
                IsAnonymous = IsAnonymous,
                IsForwardDeclaration = IsForwardDeclaration,
                Documentation = Documentation,
                Attributes = Attributes?.Select(a => a.Clone()).ToList() ?? new List<CStructAttribute>(),
                Dependencies = new List<string>(Dependencies ?? new List<string>())
            };
        }
    }
}
