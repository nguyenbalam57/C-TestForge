using System;
using System.Collections.Generic;
using System.Linq;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;

namespace C_TestForge.Models.Parse
{
    /// <summary>
    /// Result of analyzing a C source file or project
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// List of variables found in the analysis
        /// </summary>
        public List<CVariable> Variables { get; set; } = new List<CVariable>();

        /// <summary>
        /// List of functions found in the analysis
        /// </summary>
        public List<CFunction> Functions { get; set; } = new List<CFunction>();

        /// <summary>
        /// List of preprocessor definitions found in the analysis
        /// </summary>
        public List<CDefinition> Definitions { get; set; } = new List<CDefinition>();

        /// <summary>
        /// List of conditional directives found in the analysis
        /// </summary>
        public List<ConditionalDirective> ConditionalDirectives { get; set; } = new List<ConditionalDirective>();

        /// <summary>
        /// List of function relationships found in the analysis
        /// </summary>
        public List<FunctionRelationship> FunctionRelationships { get; set; } = new List<FunctionRelationship>();

        /// <summary>
        /// List of variable constraints found in the analysis
        /// </summary>
        public List<VariableConstraint> VariableConstraints { get; set; } = new List<VariableConstraint>();

        /// <summary>
        /// Get variable by name
        /// </summary>
        public CVariable GetVariable(string name)
        {
            return Variables.FirstOrDefault(v => v.Name == name);
        }

        /// <summary>
        /// Get function by name
        /// </summary>
        public CFunction GetFunction(string name)
        {
            return Functions.FirstOrDefault(f => f.Name == name);
        }

        /// <summary>
        /// Get definition by name
        /// </summary>
        public CDefinition GetDefinition(string name)
        {
            return Definitions.FirstOrDefault(d => d.Name == name);
        }

        /// <summary>
        /// Get functions that call the specified function
        /// </summary>
        public List<CFunction> GetCallers(string functionName)
        {
            var callerNames = FunctionRelationships
                .Where(r => r.CalleeName == functionName)
                .Select(r => r.CallerName)
                .ToList();

            return Functions
                .Where(f => callerNames.Contains(f.Name))
                .ToList();
        }

        /// <summary>
        /// Get functions called by the specified function
        /// </summary>
        public List<CFunction> GetCallees(string functionName)
        {
            var calleeNames = FunctionRelationships
                .Where(r => r.CallerName == functionName)
                .Select(r => r.CalleeName)
                .ToList();

            return Functions
                .Where(f => calleeNames.Contains(f.Name))
                .ToList();
        }

        /// <summary>
        /// Merges another analysis result into this one
        /// </summary>
        public void Merge(AnalysisResult other)
        {
            if (other == null)
                return;

            Variables.AddRange(other.Variables);
            Functions.AddRange(other.Functions);
            Definitions.AddRange(other.Definitions);
            ConditionalDirectives.AddRange(other.ConditionalDirectives);
            FunctionRelationships.AddRange(other.FunctionRelationships);
            VariableConstraints.AddRange(other.VariableConstraints);
        }
    }

    /// <summary>
    /// Kết quả phân tích toàn bộ dự án C/C++
    /// </summary>
    public class ProjectAnalysisResult : AnalysisResult
    {
        /// <summary>
        /// Đường dẫn thư mục gốc của dự án
        /// </summary>
        public string ProjectPath { get; set; } = string.Empty;

        /// <summary>
        /// Đồ thị phụ thuộc include của dự án
        /// </summary>
        public IncludeDependencyGraph DependencyGraph { get; set; } = new IncludeDependencyGraph();

        /// <summary>
        /// Danh sách macro từ tất cả các tệp
        /// </summary>
        public List<CDefinition> Macros { get; set; } = new List<CDefinition>();

        /// <summary>
        /// Danh sách directive tiền xử lý từ tất cả các tệp
        /// </summary>
        public List<CPreprocessorDirective> PreprocessorDirectives { get; set; } = new List<CPreprocessorDirective>();

        /// <summary>
        /// Danh sách các tệp đã được xử lý
        /// </summary>
        public List<string> ProcessedFiles { get; set; } = new List<string>();

        /// <summary>
        /// Danh sách lỗi gặp phải trong quá trình phân tích
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();

        /// <summary>
        /// Thời gian bắt đầu phân tích
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Thời gian kết thúc phân tích
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Thời gian phân tích
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Tổng số tệp trong dự án
        /// </summary>
        public int TotalFiles => DependencyGraph?.SourceFiles?.Count ?? 0;

        /// <summary>
        /// Số tệp đã được xử lý thành công
        /// </summary>
        public int ProcessedFileCount => ProcessedFiles?.Count ?? 0;

        /// <summary>
        /// Tỷ lệ phần trăm hoàn thành
        /// </summary>
        public double CompletionPercentage => TotalFiles > 0 ? (double)ProcessedFileCount / TotalFiles * 100 : 0;

        /// <summary>
        /// Có lỗi trong quá trình phân tích không
        /// </summary>
        public bool HasErrors => Errors?.Count > 0;

        /// <summary>
        /// Thống kê kết quả phân tích
        /// </summary>
        public ProjectAnalysisStatistics Statistics => new ProjectAnalysisStatistics
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
        public CDefinition GetMacro(string name)
        {
            return Macros?.FirstOrDefault(m => m.Name == name);
        }

        /// <summary>
        /// Lấy tất cả các điều kiện tiền xử lý duy nhất
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
                    {
                        conditions.Add(directive.Condition.Trim());
                    }
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
        /// Trích xuất điều kiện đệ quy từ conditional blocks
        /// </summary>
        private void ExtractConditionsRecursively(ConditionalBlock block, HashSet<string> conditions)
        {
            if (!string.IsNullOrEmpty(block.Condition))
            {
                conditions.Add(block.Condition.Trim());
            }

            foreach (var nestedBlock in block.NestedBlocks)
            {
                ExtractConditionsRecursively(nestedBlock, conditions);
            }
        }

        /// <summary>
        /// Tạo báo cáo tóm tắt phân tích
        /// </summary>
        public string GenerateSummaryReport()
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("=== BÁO CÁO PHÂN TÍCH DỰ ÁN C/C++ ===");
            report.AppendLine($"Dự án: {ProjectPath}");
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
                foreach (var error in Errors.Take(10)) // Chỉ hiển thị 10 lỗi đầu tiên
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