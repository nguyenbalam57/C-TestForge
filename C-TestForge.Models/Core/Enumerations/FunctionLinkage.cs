using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.Enumerations
{
    /// <summary>
    /// Các loại liên kết hàm (Function linkage types)
    /// </summary>
    public enum FunctionLinkage
    {
        /// <summary>
        /// Internal: Hàm chỉ được sử dụng trong cùng một tệp nguồn (file), không thể truy cập từ các tệp khác.
        /// </summary>
        Internal,

        /// <summary>
        /// External: Hàm có thể được sử dụng ở các tệp nguồn khác, cho phép liên kết ngoài.
        /// </summary>
        External,

        /// <summary>
        /// Static: Hàm chỉ có phạm vi trong tệp nguồn khai báo, không thể truy cập từ tệp khác (tương tự Internal).
        /// </summary>
        Static,

        /// <summary>
        /// Inline: Hàm có thể được trình biên dịch chèn trực tiếp mã nguồn vào nơi gọi hàm để tối ưu hiệu suất.
        /// </summary>
        Inline
    }
}
