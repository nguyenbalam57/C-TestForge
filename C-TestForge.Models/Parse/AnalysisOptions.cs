namespace C_TestForge.Models.Parse
{
    /// <summary>
    /// Tuỳ chọn cho quá trình phân tích mã nguồn C/C++
    /// </summary>
    public class AnalysisOptions
    {
        /// <summary>
        /// Phân tích biến (variables)
        /// </summary>
        public bool AnalyzeVariables { get; set; } = true;

        /// <summary>
        /// Phân tích hàm (functions)
        /// </summary>
        public bool AnalyzeFunctions { get; set; } = true;

        /// <summary>
        /// Phân tích macro/định nghĩa tiền xử lý (#define)
        /// </summary>
        public bool AnalyzePreprocessorDefinitions { get; set; } = true;

        /// <summary>
        /// Phân tích mối quan hệ gọi hàm (function call relationships)
        /// </summary>
        public bool AnalyzeFunctionRelationships { get; set; } = true;

        /// <summary>
        /// Phân tích ràng buộc biến (variable constraints)
        /// </summary>
        public bool AnalyzeVariableConstraints { get; set; } = true;

        /// <summary>
        /// Phân tích mối quan hệ giữa các tệp (cross-file relationships)
        /// </summary>
        public bool AnalyzeCrossFileRelationships { get; set; } = false;

        /// <summary>
        /// Phân tích struct
        /// </summary>
        public bool AnalyzeStructures { get; set; } = true;

        /// <summary>
        /// Phân tích union
        /// </summary>
        public bool AnalyzeUnions { get; set; } = true;

        /// <summary>
        /// Phân tích enum
        /// </summary>
        public bool AnalyzeEnumerations { get; set; } = true;

        /// <summary>
        /// Phân tích typedef
        /// </summary>
        public bool AnalyzeTypedefs { get; set; } = true;

        /// <summary>
        /// Phân tích chỉ thị include (#include)
        /// </summary>
        public bool AnalyzeIncludes { get; set; } = true;

        /// <summary>
        /// Phân tích hằng số (constant)
        /// </summary>
        public bool AnalyzeConstants { get; set; } = true;

        /// <summary>
        /// Phân tích phụ thuộc kiểu dữ liệu (type dependencies)
        /// </summary>
        public bool AnalyzeTypeDependencies { get; set; } = false;

        /// <summary>
        /// Phân tích tham chiếu ký hiệu (symbol references)
        /// </summary>
        public bool AnalyzeSymbolReferences { get; set; } = false;

        /// <summary>
        /// Phân tích đồ thị gọi hàm (call graph)
        /// </summary>
        public bool AnalyzeCallGraph { get; set; } = false;

        /// <summary>
        /// Phân tích lỗi và cảnh báo (parse errors & warnings)
        /// </summary>
        public bool AnalyzeErrorsAndWarnings { get; set; } = true;

        /// <summary>
        /// Mức độ chi tiết của phân tích
        /// </summary>
        public AnalysisLevel DetailLevel { get; set; } = AnalysisLevel.Normal;

        /// <summary>
        /// Tạo bản sao tuỳ chọn phân tích hiện tại
        /// </summary>
        public AnalysisOptions Clone()
        {
            return new AnalysisOptions
            {
                AnalyzeVariables = AnalyzeVariables,
                AnalyzeFunctions = AnalyzeFunctions,
                AnalyzePreprocessorDefinitions = AnalyzePreprocessorDefinitions,
                AnalyzeFunctionRelationships = AnalyzeFunctionRelationships,
                AnalyzeVariableConstraints = AnalyzeVariableConstraints,
                AnalyzeCrossFileRelationships = AnalyzeCrossFileRelationships,
                AnalyzeStructures = AnalyzeStructures,
                AnalyzeUnions = AnalyzeUnions,
                AnalyzeEnumerations = AnalyzeEnumerations,
                AnalyzeTypedefs = AnalyzeTypedefs,
                AnalyzeIncludes = AnalyzeIncludes,
                AnalyzeConstants = AnalyzeConstants,
                AnalyzeTypeDependencies = AnalyzeTypeDependencies,
                AnalyzeSymbolReferences = AnalyzeSymbolReferences,
                AnalyzeCallGraph = AnalyzeCallGraph,
                AnalyzeErrorsAndWarnings = AnalyzeErrorsAndWarnings,
                DetailLevel = DetailLevel
            };
        }
    }
}