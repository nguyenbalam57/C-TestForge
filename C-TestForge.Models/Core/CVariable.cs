using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents a variable in C code
    /// </summary>
    public class CVariable : SourceCodeEntity
    {
        /// <summary>
        /// Type of the variable as a string
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Type of the variable
        /// </summary>
        public VariableType VariableType { get; set; }

        // Thuộc tính mới thêm
        public string OriginalTypeName { get; set; } = string.Empty; // Type hiện tại của biến, có thể là typedef hoặc alias
        public bool IsCustomType { get; set; } // Biến có phải là kiểu dữ liệu tùy chỉnh hay không

        /// <summary>
        /// Scope of the variable
        /// </summary>
        public VariableScope Scope { get; set; }

        /// <summary>
        /// Default value of the variable
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Whether the variable is constant
        /// </summary>
        public bool IsConst { get; set; }

        /// <summary>
        /// Whether the variable is read-only
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Whether the variable is volatile
        /// </summary>
        public bool IsVolatile { get; set; }
        public bool IsPointer { get; set; }
        public bool IsArray { get; set; }

        /// <summary>
        /// Size of the variable in bytes
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Constraints on the variable
        /// </summary>
        public List<VariableConstraint> Constraints { get; set; } = new List<VariableConstraint>();

        /// <summary>
        /// Functions that use this variable
        /// </summary>
        [JsonIgnore]
        public List<string> UsedByFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Get a string representation of the variable
        /// </summary>
        public override string ToString()
        {
            string constModifier = IsConst ? "const " : "";
            string volatileModifier = IsVolatile ? "volatile " : "";
            string defaultValueStr = DefaultValue != null ? $" = {DefaultValue}" : "";

            return $"{constModifier}{volatileModifier}{TypeName} {Name}{defaultValueStr}";
        }

        /// <summary>
        /// Create a clone of the variable
        /// </summary>
        public CVariable Clone()
        {
            return new CVariable
            {
                Id = Id,
                Name = Name,
                TypeName = TypeName,
                VariableType = VariableType,
                Scope = Scope,
                DefaultValue = DefaultValue,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                IsConst = IsConst,
                IsReadOnly = IsReadOnly,
                IsVolatile = IsVolatile,
                Size = Size,
                Constraints = Constraints?.Select(c => c.Clone()).ToList() ?? new List<VariableConstraint>(),
                UsedByFunctions = UsedByFunctions != null ? new List<string>(UsedByFunctions) : new List<string>()
            };
        }
    }
}
