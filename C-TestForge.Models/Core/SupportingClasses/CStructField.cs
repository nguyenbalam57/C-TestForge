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
    /// Represents a structure field
    /// </summary>
    public class CStructField : SourceCodeEntity
    {
        /// <summary>
        /// Type of the field
        /// </summary>
        public string FieldType { get; set; } = string.Empty;

        /// <summary>
        /// Size of the field in bytes
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Offset of the field within the structure
        /// </summary>
        public int Offset { get; set; }

        /// <summary>
        /// Whether the field is a bit field
        /// </summary>
        public bool IsBitField { get; set; }

        /// <summary>
        /// Bit width if this is a bit field
        /// </summary>
        public int BitWidth { get; set; }

        /// <summary>
        /// Whether the field is const
        /// </summary>
        public bool IsConst { get; set; }

        /// <summary>
        /// Whether the field is volatile
        /// </summary>
        public bool IsVolatile { get; set; }

        /// <summary>
        /// Whether the field is a pointer
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Whether the field is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Array dimensions if it's an array
        /// </summary>
        public List<int> ArrayDimensions { get; set; } = new List<int>();

        public CStructField Clone()
        {
            return new CStructField
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                FieldType = FieldType,
                Size = Size,
                Offset = Offset,
                IsBitField = IsBitField,
                BitWidth = BitWidth,
                IsConst = IsConst,
                IsVolatile = IsVolatile,
                IsPointer = IsPointer,
                IsArray = IsArray,
                ArrayDimensions = new List<int>(ArrayDimensions ?? new List<int>())
            };
        }
    }
}
