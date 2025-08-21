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
    /// Represents a C typedef
    /// </summary>
    public class CTypedef : SourceCodeEntity, ISymbol
    {
        /// <summary>
        /// The original type that this typedef aliases
        /// </summary>
        public string OriginalType { get; set; } = string.Empty;

        /// <summary>
        /// The aliased name (new type name)
        /// </summary>
        public string AliasName { get; set; } = string.Empty;

        /// <summary>
        /// Whether this typedef creates a pointer type
        /// </summary>
        public bool IsPointerType { get; set; }

        /// <summary>
        /// Whether this typedef creates an array type
        /// </summary>
        public bool IsArrayType { get; set; }

        /// <summary>
        /// Whether this typedef creates a function pointer type
        /// </summary>
        public bool IsFunctionPointer { get; set; }

        /// <summary>
        /// Function signature if this is a function pointer typedef
        /// </summary>
        public CFunctionSignature FunctionSignature { get; set; }

        /// <summary>
        /// Documentation for the typedef
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// Usage count of this typedef
        /// </summary>
        public int UsageCount { get; set; }

        // ISymbol implementation
        string ISymbol.Type => "Typedef";

        public override string ToString()
        {
            return $"typedef {OriginalType} {Name}";
        }

        public CTypedef Clone()
        {
            return new CTypedef
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                OriginalType = OriginalType,
                AliasName = AliasName,
                IsPointerType = IsPointerType,
                IsArrayType = IsArrayType,
                IsFunctionPointer = IsFunctionPointer,
                FunctionSignature = FunctionSignature?.Clone(),
                Documentation = Documentation,
                UsageCount = UsageCount
            };
        }
    }
}
