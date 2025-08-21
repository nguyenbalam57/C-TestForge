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
    /// Represents a function parameter
    /// </summary>
    public class CParameter : SourceCodeEntity
    {
        /// <summary>
        /// Type of the parameter
        /// </summary>
        public string ParameterType { get; set; } = string.Empty;

        /// <summary>
        /// Default value of the parameter (C++ feature, might be relevant)
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Whether the parameter is const
        /// </summary>
        public bool IsConst { get; set; }

        /// <summary>
        /// Whether the parameter is volatile
        /// </summary>
        public bool IsVolatile { get; set; }

        /// <summary>
        /// Whether the parameter is a pointer
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Pointer depth
        /// </summary>
        public int PointerDepth { get; set; }

        /// <summary>
        /// Whether the parameter is an array
        /// </summary>
        public bool IsArray { get; set; }

        public override string ToString()
        {
            string constModifier = IsConst ? "const " : "";
            string volatileModifier = IsVolatile ? "volatile " : "";
            string pointerMarker = new string('*', PointerDepth);
            string arrayMarker = IsArray ? "[]" : "";

            return $"{constModifier}{volatileModifier}{ParameterType} {pointerMarker}{Name}{arrayMarker}";
        }

        public CParameter Clone()
        {
            return new CParameter
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                ParameterType = ParameterType,
                DefaultValue = DefaultValue,
                IsConst = IsConst,
                IsVolatile = IsVolatile,
                IsPointer = IsPointer,
                PointerDepth = PointerDepth,
                IsArray = IsArray
            };
        }
    }
}
