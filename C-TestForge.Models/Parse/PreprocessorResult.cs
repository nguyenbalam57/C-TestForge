using C_TestForge.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C_TestForge.Models.Parse
{
    /// <summary>
    /// Kết quả phân tích các chỉ thị tiền xử lý (preprocessor) trong mã nguồn C/C++
    /// </summary>
    public class PreprocessorResult
    {
        /// <summary>
        /// Danh sách macro/định nghĩa (#define) được trích xuất
        /// </summary>
        public List<CDefinition> Definitions { get; set; } = new List<CDefinition>();

        /// <summary>
        /// Danh sách chỉ thị điều kiện (#if, #ifdef, #ifndef, #elif, #else, #endif)
        /// </summary>
        public List<ConditionalDirective> ConditionalDirectives { get; set; } = new List<ConditionalDirective>();

        /// <summary>
        /// Danh sách chỉ thị include (#include)
        /// </summary>
        public List<IncludeDirective> Includes { get; set; } = new List<IncludeDirective>();

        /// <summary>
        /// Danh sách tất cả các chỉ thị tiền xử lý (bao gồm macro, include, điều kiện, ...)
        /// </summary>
        public List<CPreprocessorDirective> PreprocessorDirectives { get; set; } = new List<CPreprocessorDirective>();

        /// <summary>
        /// Tổng số macro/định nghĩa
        /// </summary>
        public int MacroCount => Definitions.Count;

        /// <summary>
        /// Tổng số chỉ thị điều kiện
        /// </summary>
        public int ConditionalCount => ConditionalDirectives.Count;

        /// <summary>
        /// Tổng số chỉ thị include
        /// </summary>
        public int IncludeCount => Includes.Count;

        /// <summary>
        /// Gộp kết quả phân tích tiền xử lý khác vào đối tượng hiện tại
        /// </summary>
        /// <param name="other">Kết quả cần gộp</param>
        public void Merge(PreprocessorResult other)
        {
            if (other == null)
                return;

            Definitions.AddRange(other.Definitions);
            ConditionalDirectives.AddRange(other.ConditionalDirectives);
            Includes.AddRange(other.Includes);
            PreprocessorDirectives.AddRange(other.PreprocessorDirectives);
        }

        /// <summary>
        /// Tìm macro theo tên
        /// </summary>
        public CDefinition GetDefinition(string name)
            => Definitions.FirstOrDefault(d => d.Name == name);

        /// <summary>
        /// Tìm chỉ thị include theo đường dẫn file
        /// </summary>
        public IncludeDirective GetInclude(string filePath)
            => Includes.FirstOrDefault(i => i.FilePath == filePath);

        /// <summary>
        /// Lọc chỉ thị tiền xử lý theo loại (ví dụ: "define", "include", "if", ...)
        /// </summary>
        public List<CPreprocessorDirective> FilterDirectivesByType(string type)
            => PreprocessorDirectives.Where(d => d.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();

        /// <summary>
        /// Kiểm tra có macro trùng tên không
        /// </summary>
        public bool HasDuplicateDefinitions()
            => Definitions.GroupBy(d => d.Name).Any(g => g.Count() > 1);

        /// <summary>
        /// Thống kê nhanh các loại chỉ thị
        /// </summary>
        public override string ToString()
        {
            return $"PreprocessorResult: {MacroCount} macro, {ConditionalCount} conditional, {IncludeCount} include";
        }
    }
}