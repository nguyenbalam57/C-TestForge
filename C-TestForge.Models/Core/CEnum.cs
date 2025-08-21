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
    /// Represents a C enumeration
    /// </summary>
    public class CEnum : SourceCodeEntity, ISymbol
    {
        /// <summary>
        /// List of enum values
        /// </summary>
        public List<CEnumValue> Values { get; set; } = new List<CEnumValue>();

        /// <summary>
        /// Underlying type of the enum (if specified)
        /// </summary>
        public string UnderlyingType { get; set; } = "int";

        /// <summary>
        /// Whether this is a scoped enum (C++11 feature, but might be relevant)
        /// </summary>
        public bool IsScoped { get; set; }

        /// <summary>
        /// Whether this is an anonymous enum
        /// </summary>
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// Documentation for the enumeration
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// Size of the enum type in bytes
        /// </summary>
        public int Size { get; set; } = 4; // Default int size

        // ISymbol implementation
        string ISymbol.Type => "Enum";

        public override string ToString()
        {
            return $"enum {Name}";
        }

        public CEnum Clone()
        {
            return new CEnum
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                Values = Values?.Select(v => v.Clone()).ToList() ?? new List<CEnumValue>(),
                UnderlyingType = UnderlyingType,
                IsScoped = IsScoped,
                IsAnonymous = IsAnonymous,
                Documentation = Documentation,
                Size = Size
            };
        }
    }
}
