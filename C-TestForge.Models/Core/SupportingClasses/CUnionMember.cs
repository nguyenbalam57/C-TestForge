using C_TestForge.Models.Base;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core.SupportingClasses
{
    /// <summary>
    /// Represents a member of a C union
    /// </summary>
    public class CUnionMember : SourceCodeEntity
    {
        /// <summary>
        /// Data type of the union member
        /// </summary>
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// Size of this member in bytes
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Alignment requirement of this member
        /// </summary>
        public int Alignment { get; set; }

        /// <summary>
        /// Offset within the union (always 0 for union members)
        /// </summary>
        public int Offset { get; set; } = 0;

        /// <summary>
        /// Whether this member is a pointer
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Whether this member is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Array size if this member is an array
        /// </summary>
        public int ArraySize { get; set; }

        /// <summary>
        /// Whether this member is const
        /// </summary>
        public bool IsConst { get; set; }

        /// <summary>
        /// Whether this member is volatile
        /// </summary>
        public bool IsVolatile { get; set; }

        /// <summary>
        /// Whether this member is a bitfield
        /// </summary>
        public bool IsBitfield { get; set; }

        /// <summary>
        /// Bitfield width if this member is a bitfield
        /// </summary>
        public int BitfieldWidth { get; set; }

        /// <summary>
        /// Documentation for this member
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        /// <summary>
        /// Attributes applied to this member
        /// </summary>
        public List<CUnionMemberAttribute> Attributes { get; set; } = new List<CUnionMemberAttribute>();

        /// <summary>
        /// Default value or initializer for this member (if any)
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Whether this member is anonymous (unnamed)
        /// </summary>
        public bool IsAnonymous { get; set; }

        public override string ToString()
        {
            var result = new StringBuilder();

            if (IsConst) result.Append("const ");
            if (IsVolatile) result.Append("volatile ");

            result.Append(DataType);

            if (IsPointer) result.Append("*");
            if (!string.IsNullOrEmpty(Name)) result.Append($" {Name}");
            if (IsArray) result.Append($"[{ArraySize}]");
            if (IsBitfield) result.Append($" : {BitfieldWidth}");

            return result.ToString();
        }

        public CUnionMember Clone()
        {
            return new CUnionMember
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                DataType = DataType,
                Size = Size,
                Alignment = Alignment,
                Offset = Offset,
                IsPointer = IsPointer,
                IsArray = IsArray,
                ArraySize = ArraySize,
                IsConst = IsConst,
                IsVolatile = IsVolatile,
                IsBitfield = IsBitfield,
                BitfieldWidth = BitfieldWidth,
                Documentation = Documentation,
                Attributes = Attributes?.Select(a => a.Clone()).ToList() ?? new List<CUnionMemberAttribute>(),
                DefaultValue = DefaultValue,
                IsAnonymous = IsAnonymous
            };
        }
    }
}
