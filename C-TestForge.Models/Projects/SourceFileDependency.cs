using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Projects
{
    /// <summary>
    /// Đại diện cho một tệp nguồn trong đồ thị phụ thuộc
    /// </summary>
    public class SourceFileDependency
    {
        /// <summary>
        /// Tên file (không có đường dẫn)
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Đường dẫn đầy đủ tới file
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Loại file (header/source)
        /// </summary>
        public SourceFileType FileType { get; set; }

        /// <summary>
        /// Danh sách tên file mà file này include trực tiếp
        /// </summary>
        public List<string> DirectDependencies { get; set; } = new();

        /// <summary>
        /// Danh sách tên file phụ thuộc ngược (các file include file này)
        /// </summary>
        public List<string> DependentFiles { get; set; } = new();

        /// <summary>
        /// Danh sách các khối điều kiện tiền xử lý trong file
        /// </summary>
        public List<ConditionalBlock> ConditionalBlocks { get; set; } = new();

        /// <summary>
        /// Danh sách các chỉ thị include trong file
        /// </summary>
        public List<IncludeStatement> Includes { get; set; } = new();
    }

    /// <summary>
    /// Đại diện cho một chỉ thị include trong file
    /// </summary>
    public class IncludeStatement
    {
        /// <summary>
        /// Đường dẫn file được include
        /// </summary>
        public string IncludePath { get; set; } = string.Empty;

        /// <summary>
        /// Dòng số trong file
        /// </summary>
        public int LineNumber { get; set; }
    }

    /// <summary>
    /// Đại diện cho một khối điều kiện tiền xử lý
    /// </summary>
    public class ConditionalBlock
    {
        /// <summary>
        /// Điều kiện của khối (ví dụ: #ifdef DEBUG)
        /// </summary>
        public string Condition { get; set; } = string.Empty;

        /// <summary>
        /// Danh sách khối con lồng nhau
        /// </summary>
        public List<ConditionalBlock> NestedBlocks { get; set; } = new();

        /// <summary>
        /// Danh sách chỉ thị include trong khối này
        /// </summary>
        public List<IncludeStatement> Includes { get; set; } = new();
    }
}
