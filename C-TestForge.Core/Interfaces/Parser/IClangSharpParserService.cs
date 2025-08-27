using C_TestForge.Models;
using C_TestForge.Models.Core;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Parser
{
    /// <summary>
    /// Interface for the ClangSharp parser service
    /// </summary>
    public interface IClangSharpParserService
    {
        /// <summary>
        /// Parses a C source file
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>The parsed source file model</returns>
        Task<SourceFile> ParseSourceFileAsync(string filePath);

        /// <summary>
        /// Parses multiple C source files
        /// </summary>
        /// <param name="filePaths">Paths to the source files</param>
        /// <returns>List of parsed source file models</returns>
        Task<List<SourceFile>> ParseSourceFilesAsync(IEnumerable<string> filePaths);

        /// <summary>
        /// Phân tích toàn bộ dự án C/C++ theo quy trình hoàn chỉnh, hỗ trợ báo cáo tiến độ và hủy bỏ.
        /// </summary>
        /// <param name="project">Dự án</param>
        /// <param name="sourceFiles">Danh sách file nguồn</param>
        /// <param name="progress">Báo cáo tiến độ (0-100%)</param>
        /// <param name="cancellationToken">Token để hủy bỏ</param>
        /// <returns>Kết quả phân tích dự án</returns>
        Task<ProjectAnalysisResult> AnalyzeCompleteProjectAsync(
            Project project,
            List<SourceFile> sourceFiles,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default);
    }

}
