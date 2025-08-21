using C_TestForge.Models.Base;
using C_TestForge.Models.Core.Enumerations;
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
    /// Represents a C constant
    /// </summary>
    public class CConstant : SourceCodeEntity, ISymbol
    {
        /// <summary>
        /// Type of the constant
        /// </summary>
        public string ConstantType { get; set; } = string.Empty;

        /// <summary>
        /// Value of the constant
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Category of the constant
        /// </summary>
        public ConstantCategory Category { get; set; }

        /// <summary>
        /// Whether this constant is defined via #define
        /// </summary>
        public bool IsDefine { get; set; }

        /// <summary>
        /// Whether this constant is defined via const keyword
        /// </summary>
        public bool IsConstKeyword { get; set; }

        /// <summary>
        /// Whether this constant is defined via enum
        /// </summary>
        public bool IsEnumValue { get; set; }

        /// <summary>
        /// Documentation for the constant
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// Usage count of this constant
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// Scope of the constant
        /// </summary>
        public ConstantScope Scope { get; set; }

        // ISymbol implementation
        string ISymbol.Type => "Constant";

        public override string ToString()
        {
            return $"{ConstantType} {Name} = {Value}";
        }

        public CConstant Clone()
        {
            return new CConstant
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                ConstantType = ConstantType,
                Value = Value,
                Category = Category,
                IsDefine = IsDefine,
                IsConstKeyword = IsConstKeyword,
                IsEnumValue = IsEnumValue,
                Documentation = Documentation,
                UsageCount = UsageCount,
                Scope = Scope
            };
        }
    }
}
