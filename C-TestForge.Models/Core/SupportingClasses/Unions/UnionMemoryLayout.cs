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
    /// Represents the memory layout of a union
    /// </summary>
    public class UnionMemoryLayout : SourceCodeEntity
    {
        /// <summary>
        /// Name of the union
        /// </summary>
        public string UnionName { get; set; } = string.Empty;

        /// <summary>
        /// Total size of the union in bytes (size of largest member)
        /// </summary>
        public int TotalSize { get; set; }

        /// <summary>
        /// Alignment requirement of the union
        /// </summary>
        public int Alignment { get; set; }

        /// <summary>
        /// Layout information for each member
        /// </summary>
        public List<UnionMemberLayout> MemberLayouts { get; set; } = new List<UnionMemberLayout>();

        /// <summary>
        /// Name of the largest member
        /// </summary>
        public string LargestMember { get; set; } = string.Empty;

        /// <summary>
        /// Whether the union contains bit fields
        /// </summary>
        public bool HasBitFields { get; set; }

        /// <summary>
        /// Whether the union has padding for alignment
        /// </summary>
        public bool HasPadding => TotalSize > MemberLayouts.Max(m => m.Size);

        /// <summary>
        /// Amount of padding bytes for alignment
        /// </summary>
        public int PaddingBytes => HasPadding ? TotalSize - MemberLayouts.Max(m => m.Size) : 0;

        public override string ToString()
        {
            return $"Union {UnionName}: Size={TotalSize}, Alignment={Alignment}, Members={MemberLayouts.Count}";
        }

        public UnionMemoryLayout Clone()
        {
            return new UnionMemoryLayout
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                UnionName = UnionName,
                TotalSize = TotalSize,
                Alignment = Alignment,
                MemberLayouts = MemberLayouts?.Select(m => m.Clone()).ToList() ?? new List<UnionMemberLayout>(),
                LargestMember = LargestMember,
                HasBitFields = HasBitFields
            };
        }
    }
}
