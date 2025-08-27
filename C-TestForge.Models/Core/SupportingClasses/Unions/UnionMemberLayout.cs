using C_TestForge.Models.Base;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core.SupportingClasses.Unions
{
    /// <summary>
    /// Represents the layout of a union member
    /// </summary>
    public class UnionMemberLayout : SourceCodeEntity
    {
        /// <summary>
        /// Name of the member
        /// </summary>
        public string MemberName { get; set; } = string.Empty;

        /// <summary>
        /// Data type of the member
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
        /// Whether this member is a bit field
        /// </summary>
        public bool IsBitField { get; set; }

        /// <summary>
        /// Bit width if this is a bit field
        /// </summary>
        public int BitWidth { get; set; }

        /// <summary>
        /// Bit offset within the storage unit
        /// </summary>
        public int BitOffset { get; set; }

        /// <summary>
        /// Whether this is the largest member in the union
        /// </summary>
        public bool IsLargestMember { get; set; }

        public override string ToString()
        {
            var result = $"{DataType} {MemberName}: Size={Size}, Alignment={Alignment}";
            if (IsBitField)
                result += $", BitField({BitWidth})";
            if (IsLargestMember)
                result += " [Largest]";
            return result;
        }

        public UnionMemberLayout Clone()
        {
            return new UnionMemberLayout
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                MemberName = MemberName,
                DataType = DataType,
                Size = Size,
                Alignment = Alignment,
                Offset = Offset,
                IsBitField = IsBitField,
                BitWidth = BitWidth,
                BitOffset = BitOffset,
                IsLargestMember = IsLargestMember
            };
        }
    }
}
