using C_TestForge.Models.Base;
using C_TestForge.Models.CodeAnalysis;
using C_TestForge.Models.Core.Enumerations;
using C_TestForge.Models.Core.SupportingClasses;
using C_TestForge.Models.Parse;
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
    /// Enhanced CVariable with additional properties
    /// </summary>
    public class CVariable : SourceCodeEntity, ISymbol
    {
        /// <summary>
        /// Type of the variable as a string
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Original type name before typedef resolution
        /// </summary>
        public string OriginalTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Type of the variable
        /// </summary>
        public VariableType VariableType { get; set; }

        /// <summary>
        /// Whether this is a custom/user-defined type
        /// </summary>
        public bool IsCustomType { get; set; }

        /// <summary>
        /// Scope of the variable
        /// </summary>
        public VariableScope Scope { get; set; }

        /// <summary>
        /// Storage class (auto, register, static, extern)
        /// </summary>
        public StorageClass StorageClass { get; set; }

        /// <summary>
        /// Default/initial value of the variable
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Whether the variable is constant
        /// </summary>
        public bool IsConst { get; set; }

        /// <summary>
        /// Whether the variable is volatile
        /// </summary>
        public bool IsVolatile { get; set; }

        /// <summary>
        /// Whether the variable is a pointer
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Pointer depth (0 = not pointer, 1 = *, 2 = **, etc.)
        /// </summary>
        public int PointerDepth { get; set; }

        /// <summary>
        /// Whether the variable is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Array dimensions if it's an array
        /// </summary>
        public List<int> ArrayDimensions { get; set; } = new List<int>();

        /// <summary>
        /// Size of the variable in bytes
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Whether this is a global variable
        /// </summary>
        [JsonIgnore]
        public bool IsGlobal => Scope == VariableScope.Global;

        /// <summary>
        /// Whether this is a local variable
        /// </summary>
        [JsonIgnore]
        public bool IsLocal => Scope == VariableScope.Local;

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
        /// Variable attributes
        /// </summary>
        public List<CVariableAttribute> Attributes { get; set; } = new List<CVariableAttribute>();

        // ISymbol implementation
        string ISymbol.Type => "Variable";

        public override string ToString()
        {
            string constModifier = IsConst ? "const " : "";
            string volatileModifier = IsVolatile ? "volatile " : "";
            string pointerMarker = new string('*', PointerDepth);
            string arrayMarker = IsArray ? $"[{string.Join("][", ArrayDimensions)}]" : "";
            string defaultValueStr = !string.IsNullOrEmpty(DefaultValue) ? $" = {DefaultValue}" : "";

            return $"{constModifier}{volatileModifier}{TypeName} {pointerMarker}{Name}{arrayMarker}{defaultValueStr}";
        }

        public CVariable Clone()
        {
            return new CVariable
            {
                Id = Id,
                Name = Name,
                TypeName = TypeName,
                OriginalTypeName = OriginalTypeName,
                VariableType = VariableType,
                IsCustomType = IsCustomType,
                Scope = Scope,
                StorageClass = StorageClass,
                DefaultValue = DefaultValue,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                IsConst = IsConst,
                IsVolatile = IsVolatile,
                IsPointer = IsPointer,
                PointerDepth = PointerDepth,
                IsArray = IsArray,
                ArrayDimensions = new List<int>(ArrayDimensions ?? new List<int>()),
                Size = Size,
                Constraints = Constraints?.Select(c => c.Clone()).ToList() ?? new List<VariableConstraint>(),
                UsedByFunctions = new List<string>(UsedByFunctions ?? new List<string>()),
                Attributes = Attributes?.Select(a => a.Clone()).ToList() ?? new List<CVariableAttribute>()
            };
        }
    }
}
