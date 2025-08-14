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
        /// Parses C source code string
        /// </summary>
        /// <param name="sourceCode">The source code to parse</param>
        /// <param name="fileName">Optional file name for the source</param>
        /// <returns>The parsed source file model</returns>
        Task<SourceFile> ParseSourceCodeAsync(SourceFile sourceFile, string fileName = "inline.c" );

        /// <summary>
        /// Extracts all functions from a C source file
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>List of extracted functions</returns>
        Task<List<CFunction>> ExtractFunctionsAsync(SourceFile sourceFile);

        /// <summary>
        /// Extracts all functions from C source code string
        /// </summary>
        /// <param name="sourceCode">The source code to parse</param>
        /// <param name="fileName">Optional file name for the source</param>
        /// <returns>List of extracted functions</returns>
        Task<List<CFunction>> ExtractFunctionsFromCodeAsync(SourceFile sourceFile, string fileName = "inline.c");

        /// <summary>
        /// Phân tích toàn bộ dự án C/C++ theo quy trình hoàn chỉnh
        /// </summary>
        /// <param name="projectRootPath">Đường dẫn thư mục gốc của dự án</param>
        /// <returns>Kết quả phân tích dự án hoàn chỉnh bao gồm biến, hàm, macro và phụ thuộc</returns>
        Task<ProjectAnalysisResult> AnalyzeCompleteProjectAsync(string projectRootPath);

        /// <summary>
        /// Trích xuất tất cả các điều kiện tiền xử lý từ kết quả phân tích dự án
        /// </summary>
        /// <param name="analysisResult">Kết quả phân tích dự án</param>
        /// <returns>Danh sách các điều kiện tiền xử lý duy nhất</returns>
        List<string> ExtractPreprocessorConditionsFromAnalysisResult(ProjectAnalysisResult analysisResult);
    }

}
