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
        /// Phân tích toàn bộ dự án C/C++ theo quy trình hoàn chỉnh
        /// </summary>
        /// <param name="sourceFiles">Đường dẫn thư mục gốc của dự án</param>
        /// <returns>Kết quả phân tích dự án hoàn chỉnh bao gồm biến, hàm, macro và phụ thuộc</returns>
        Task<ProjectAnalysisResult> AnalyzeCompleteProjectAsync(Project project, List<SourceFile> sourceFile);

    }

}
