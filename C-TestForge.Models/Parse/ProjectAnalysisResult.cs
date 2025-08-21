using System;
using System.Collections.Generic;
using System.Linq;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;

namespace C_TestForge.Models.Parse
{
    /// <summary>
    /// Kết quả phân tích toàn bộ dự án C/C++
    /// </summary>
    public class ProjectAnalysisResult : AnalysisResult
    {
        /// <summary>
        /// Danh sách đường dẫn thư mục gốc của dự án
        /// </summary>
        public List<string> ProjectPath { get; set; } = new();

        /// <summary>
        /// Danh sách macro từ tất cả các tệp trong dự án
        /// </summary>
        public List<CDefinition> Macros { get; set; } = new();

        /// <summary>
        /// Danh sách directive tiền xử lý từ tất cả các tệp
        /// </summary>
        public List<CPreprocessorDirective> PreprocessorDirectives { get; set; } = new();

        /// <summary>
        /// Danh sách các tệp đã được xử lý thành công
        /// </summary>
        public List<string> ProcessedFiles { get; set; } = new();

        /// <summary>
        /// Danh sách lỗi gặp phải trong quá trình phân tích
        /// </summary>
        public List<string> Errors { get; set; } = new();

        /// <summary>
        /// Thời gian bắt đầu phân tích dự án
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Thời gian kết thúc phân tích dự án
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Tổng thời gian phân tích dự án
        /// </summary>
        public TimeSpan Duration => EndTime > StartTime ? EndTime - StartTime : TimeSpan.Zero;

        /// <summary>
        /// Đồ thị phụ thuộc giữa các tệp nguồn trong dự án
        /// </summary>
        public DependencyGraph DependencyGraph { get; set; } = new();

        /// <summary>
        /// Tổng số tệp trong dự án
        /// </summary>
        public int TotalFiles => DependencyGraph?.SourceFiles?.Count ?? 0;

        /// <summary>
        /// Số tệp đã được xử lý thành công
        /// </summary>
        public int ProcessedFileCount => ProcessedFiles?.Count ?? 0;

        /// <summary>
        /// Tỷ lệ phần trăm hoàn thành phân tích
        /// </summary>
        public double CompletionPercentage => TotalFiles > 0 ? (double)ProcessedFileCount / TotalFiles * 100 : 0;

        /// <summary>
        /// Có lỗi trong quá trình phân tích không?
        /// </summary>
        public bool HasErrors => Errors?.Count > 0;

        /// <summary>
        /// Thống kê kết quả phân tích dự án
        /// </summary>
        public ProjectAnalysisStatistics Statistics => new()
        {
            TotalFiles = TotalFiles,
            ProcessedFiles = ProcessedFileCount,
            TotalFunctions = Functions?.Count ?? 0,
            TotalVariables = Variables?.Count ?? 0,
            TotalMacros = Macros?.Count ?? 0,
            TotalErrors = Errors?.Count ?? 0,
            Duration = Duration,
            CompletionPercentage = CompletionPercentage
        };

        /// <summary>
        /// Lấy tất cả các tệp header trong dự án
        /// </summary>
        public List<SourceFileDependency> GetHeaderFiles()
        {
            return DependencyGraph?.SourceFiles?
                .Where(f => f.FileType == SourceFileType.CHeader || f.FileType == SourceFileType.CPPHeader)
                .ToList() ?? new List<SourceFileDependency>();
        }

        /// <summary>
        /// Lấy tất cả các tệp source trong dự án
        /// </summary>
        public List<SourceFileDependency> GetSourceFiles()
        {
            return DependencyGraph?.SourceFiles?
                .Where(f => f.FileType == SourceFileType.CSource || f.FileType == SourceFileType.CPPSource)
                .ToList() ?? new List<SourceFileDependency>();
        }

        /// <summary>
        /// Lấy macro theo tên
        /// </summary>
        public CDefinition? GetMacro(string name)
        {
            return Macros?.FirstOrDefault(m => m.Name == name);
        }

        /// <summary>
        /// Lấy tất cả các điều kiện tiền xử lý duy nhất trong dự án
        /// </summary>
        public List<string> GetUniquePreprocessorConditions()
        {
            var conditions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Từ conditional directives
            if (ConditionalDirectives != null)
            {
                foreach (var directive in ConditionalDirectives)
                {
                    if (!string.IsNullOrEmpty(directive.Condition))
                        conditions.Add(directive.Condition.Trim());
                }
            }

            // Từ dependency graph conditional blocks
            if (DependencyGraph?.SourceFiles != null)
            {
                foreach (var file in DependencyGraph.SourceFiles)
                {
                    foreach (var block in file.ConditionalBlocks)
                    {
                        ExtractConditionsRecursively(block, conditions);
                    }
                }
            }

            return conditions.ToList();
        }

        /// <summary>
        /// Đệ quy trích xuất điều kiện từ các block điều kiện
        /// </summary>
        private void ExtractConditionsRecursively(ConditionalBlock block, HashSet<string> conditions)
        {
            if (!string.IsNullOrEmpty(block.Condition))
                conditions.Add(block.Condition.Trim());

            foreach (var nestedBlock in block.NestedBlocks)
                ExtractConditionsRecursively(nestedBlock, conditions);
        }

        /// <summary>
        /// Tạo báo cáo tóm tắt phân tích dự án
        /// </summary>
        public string GenerateSummaryReport()
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== BÁO CÁO PHÂN TÍCH DỰ ÁN C/C++ ===");
            report.AppendLine($"Số thư mục dự án: {ProjectPath.Count}");
            report.AppendLine($"Thời gian phân tích: {Duration.TotalSeconds:F2} giây");
            report.AppendLine($"Hoàn thành: {CompletionPercentage:F1}%");
            report.AppendLine();

            report.AppendLine("=== THỐNG KÊ ===");
            report.AppendLine($"Tổng số tệp: {TotalFiles}");
            report.AppendLine($"Tệp đã xử lý: {ProcessedFileCount}");
            report.AppendLine($"Tệp header: {GetHeaderFiles().Count}");
            report.AppendLine($"Tệp source: {GetSourceFiles().Count}");
            report.AppendLine();

            report.AppendLine("=== THÀNH PHẦN ===");
            report.AppendLine($"Hàm: {Functions?.Count ?? 0}");
            report.AppendLine($"Biến: {Variables?.Count ?? 0}");
            report.AppendLine($"Macro: {Macros?.Count ?? 0}");
            report.AppendLine($"Điều kiện tiền xử lý: {GetUniquePreprocessorConditions().Count}");
            report.AppendLine();

            if (HasErrors)
            {
                report.AppendLine("=== LỖI ===");
                foreach (var error in Errors.Take(10))
                {
                    report.AppendLine($"- {error}");
                }
                if (Errors.Count > 10)
                {
                    report.AppendLine($"... và {Errors.Count - 10} lỗi khác");
                }
            }

            return report.ToString();
        }
    }

    /// <summary>
    /// Thống kê kết quả phân tích dự án
    /// </summary>
    public class ProjectAnalysisStatistics
    {
        public int TotalFiles { get; set; }
        public int ProcessedFiles { get; set; }
        public int TotalFunctions { get; set; }
        public int TotalVariables { get; set; }
        public int TotalMacros { get; set; }
        public int TotalErrors { get; set; }
        public TimeSpan Duration { get; set; }
        public double CompletionPercentage { get; set; }
    }
}