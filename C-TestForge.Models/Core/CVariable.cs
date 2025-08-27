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

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Đại diện cho biến C với các thuộc tính mở rộng
    /// </summary>
    public class CVariable : SourceCodeEntity, ISymbol
    {
        /// <summary>
        /// Tên kiểu dữ liệu của biến (sau khi resolve typedef)
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Tên kiểu gốc trước khi resolve typedef
        /// </summary>
        public string OriginalTypeName { get; set; } = string.Empty;

        /// <summary>
        /// Loại biến (enum: int, float, struct, ...)
        /// </summary>
        public VariableType VariableType { get; set; }

        /// <summary>
        /// Có phải kiểu do người dùng định nghĩa không (struct, union, enum, ...)
        /// </summary>
        public bool IsCustomType { get; set; }

        /// <summary>
        /// Phạm vi của biến (toàn cục, cục bộ, static, tham số, ...)
        /// </summary>
        public VariableScope Scope { get; set; }

        /// <summary>
        /// Lớp lưu trữ (auto, register, static, extern)
        /// </summary>
        public StorageClass StorageClass { get; set; }

        /// <summary>
        /// Giá trị khởi tạo mặc định của biến (nếu có)
        /// </summary>
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// Biến có phải là hằng số không (const)
        /// </summary>
        public bool IsConst { get; set; }

        /// <summary>
        /// Biến có phải volatile không
        /// </summary>
        public bool IsVolatile { get; set; }

        /// <summary>
        /// Biến có phải là con trỏ không
        /// </summary>
        public bool IsPointer { get; set; }

        /// <summary>
        /// Độ sâu con trỏ (0 = không phải con trỏ, 1 = *, 2 = **, ...)
        /// </summary>
        public int PointerDepth { get; set; }

        /// <summary>
        /// Biến có phải là mảng không
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Kích thước các chiều mảng (nếu là mảng)
        /// </summary>
        public List<int> ArrayDimensions { get; set; } = new List<int>();

        /// <summary>
        /// Kích thước biến (byte)
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Có phải biến toàn cục không
        /// </summary>
        [JsonIgnore]
        public bool IsGlobal => Scope == VariableScope.Global;

        /// <summary>
        /// Có phải biến cục bộ không
        /// </summary>
        [JsonIgnore]
        public bool IsLocal => Scope == VariableScope.Local;

        /// <summary>
        /// Danh sách ràng buộc áp dụng lên biến
        /// </summary>
        public List<VariableConstraint> Constraints { get; set; } = new List<VariableConstraint>();

        /// <summary>
        /// Danh sách tên các hàm sử dụng biến này
        /// </summary>
        [JsonIgnore]
        public List<string> UsedByFunctions { get; set; } = new List<string>();

        /// <summary>
        /// Danh sách thuộc tính đặc biệt của biến (ví dụ: __attribute__((...)))
        /// </summary>
        public List<CVariableAttribute> Attributes { get; set; } = new List<CVariableAttribute>();

        // Triển khai ISymbol
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

        /// <summary>
        /// Tạo bản sao sâu của biến
        /// </summary>
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