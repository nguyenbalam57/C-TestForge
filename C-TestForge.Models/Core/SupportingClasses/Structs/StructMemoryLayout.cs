using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses.Structs
{
    /// <summary>
    /// Represents struct memory layout information
    /// </summary>
    public class StructMemoryLayout
    {
        public string StructName { get; set; } = string.Empty;
        public int TotalSize { get; set; }
        public int Alignment { get; set; }
        public List<FieldLayout> FieldLayouts { get; set; } = new List<FieldLayout>();
        public int PaddingBytes { get; set; }
        public bool HasBitFields { get; set; }
    }

    /// <summary>
    /// Represents field layout information
    /// </summary>
    public class FieldLayout
    {
        public string FieldName { get; set; } = string.Empty;
        public int Offset { get; set; }
        public int Size { get; set; }
        public int PaddingAfter { get; set; }
        public bool IsBitField { get; set; }
        public int BitOffset { get; set; }
        public int BitWidth { get; set; }
    }
}
