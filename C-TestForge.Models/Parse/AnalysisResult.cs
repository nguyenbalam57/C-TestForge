using C_TestForge.Models.Core;
using C_TestForge.Models.Core.Enumerations;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C_TestForge.Models.Parse
{
    /// <summary>
    /// Kết quả phân tích một tệp hoặc dự án mã nguồn C/C++
    /// </summary>
    public class AnalysisResult
    {
        #region Danh sách phần tử phân tích cơ bản

        /// <summary>
        /// Danh sách biến được phát hiện trong quá trình phân tích
        /// </summary>
        public List<CVariable> Variables { get; set; } = new List<CVariable>();

        /// <summary>
        /// Danh sách hàm được phát hiện trong quá trình phân tích
        /// </summary>
        public List<CFunction> Functions { get; set; } = new List<CFunction>();

        /// <summary>
        /// Danh sách macro/định nghĩa tiền xử lý được phát hiện
        /// </summary>
        public List<CDefinition> Definitions { get; set; } = new List<CDefinition>();

        /// <summary>
        /// Danh sách directive điều kiện (#ifdef, #ifndef, #if, ...)
        /// </summary>
        public List<ConditionalDirective> ConditionalDirectives { get; set; } = new List<ConditionalDirective>();

        /// <summary>
        /// Danh sách mối quan hệ gọi hàm (ai gọi ai)
        /// </summary>
        public List<FunctionRelationship> FunctionRelationships { get; set; } = new List<FunctionRelationship>();

        /// <summary>
        /// Danh sách ràng buộc giá trị biến (ví dụ: biến chỉ nhận giá trị trong khoảng nào đó)
        /// </summary>
        public List<VariableConstraint> VariableConstraints { get; set; } = new List<VariableConstraint>();

        #endregion

        #region Phần tử mở rộng (theo ParseResult)

        /// <summary>
        /// Danh sách struct được phát hiện
        /// </summary>
        public List<CStruct> Structures { get; set; } = new List<CStruct>();

        /// <summary>
        /// Danh sách union được phát hiện
        /// </summary>
        public List<CUnion> Unions { get; set; } = new List<CUnion>();

        /// <summary>
        /// Danh sách enum được phát hiện
        /// </summary>
        public List<CEnum> Enumerations { get; set; } = new List<CEnum>();

        /// <summary>
        /// Danh sách typedef được phát hiện
        /// </summary>
        public List<CTypedef> Typedefs { get; set; } = new List<CTypedef>();

        /// <summary>
        /// Danh sách chỉ thị include (#include)
        /// </summary>
        public List<CInclude> Includes { get; set; } = new List<CInclude>();

        /// <summary>
        /// Danh sách phụ thuộc kiểu dữ liệu (giữa struct, union, typedef, ...)
        /// </summary>
        public List<TypeDependency> TypeDependencies { get; set; } = new List<TypeDependency>();

        /// <summary>
        /// Danh sách tham chiếu ký hiệu (symbol reference)
        /// </summary>
        public List<SymbolReference> SymbolReferences { get; set; } = new List<SymbolReference>();

        /// <summary>
        /// Đồ thị gọi hàm (call graph)
        /// </summary>
        public CallGraph CallGraph { get; set; } = new CallGraph();

        #endregion

        #region Trạng thái và thống kê phân tích

        /// <summary>
        /// Danh sách lỗi phát hiện trong quá trình phân tích
        /// </summary>
        public List<ParseError> ParseErrors { get; set; } = new List<ParseError>();

        /// <summary>
        /// Danh sách cảnh báo phát hiện trong quá trình phân tích
        /// </summary>
        public List<ParseWarning> ParseWarnings { get; set; } = new List<ParseWarning>();

        /// <summary>
        /// Thời điểm bắt đầu phân tích
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Thời điểm kết thúc phân tích
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Tổng thời gian phân tích
        /// </summary>
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Phân tích thành công hay không
        /// </summary>
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// Kết quả phân tích đã đầy đủ chưa
        /// </summary>
        public bool IsComplete { get; set; } = true;

        /// <summary>
        /// Đường dẫn tệp nguồn được phân tích
        /// </summary>
        public string SourceFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Hash nội dung tệp để kiểm tra tính nhất quán
        /// </summary>
        public string ContentHash { get; set; } = string.Empty;

        /// <summary>
        /// Phiên bản parser sử dụng
        /// </summary>
        public string ParserVersion { get; set; } = "1.0";

        /// <summary>
        /// Phiên bản Clang sử dụng để phân tích
        /// </summary>
        public string ClangVersion { get; set; } = string.Empty;

        /// <summary>
        /// Thống kê chi tiết quá trình phân tích
        /// </summary>
        public ParseStatistics Statistics { get; set; } = new ParseStatistics();

        #endregion

        #region Thuộc tính tính toán nhanh

        /// <summary>
        /// Có lỗi nghiêm trọng không?
        /// </summary>
        public bool HasCriticalErrors => ParseErrors.Any(e => e.Severity == ErrorSeverity.Critical);

        /// <summary>
        /// Có lỗi (mức error trở lên) không?
        /// </summary>
        public bool HasErrors => ParseErrors.Any(e => e.Severity >= ErrorSeverity.Error);

        /// <summary>
        /// Có cảnh báo không?
        /// </summary>
        public bool HasWarnings => ParseErrors.Any(e => e.Severity == ErrorSeverity.Warning) || ParseWarnings.Count > 0;

        /// <summary>
        /// Tổng số phần tử được phân tích
        /// </summary>
        public int TotalElementCount => Functions.Count + Variables.Count + Structures.Count +
                                       Unions.Count + Enumerations.Count + Typedefs.Count +
                                       Definitions.Count;

        /// <summary>
        /// Danh sách symbol toàn cục (hàm, biến toàn cục, hằng số)
        /// </summary>
        public List<ISymbol> GlobalSymbols
        {
            get
            {
                var symbols = new List<ISymbol>();
                symbols.AddRange(Functions.Cast<ISymbol>());
                symbols.AddRange(Variables.Where(v => v.Scope == VariableScope.Global).Cast<ISymbol>());
                return symbols.OrderBy(s => s.Name).ToList();
            }
        }

        /// <summary>
        /// Thống kê độ phức tạp mã nguồn
        /// </summary>
        public CodeComplexity ComplexityMetrics
        {
            get
            {
                return new CodeComplexity
                {
                    CyclomaticComplexity = Functions.Sum(f => f.CyclomaticComplexity),
                    TotalFunctions = Functions.Count,
                    TotalVariables = Variables.Count,
                    TotalStructures = Structures.Count,
                    AverageFunctionLength = Functions.Count > 0 ? Functions.Average(f => f.LineCount) : 0,
                    MaxFunctionComplexity = Functions.Count > 0 ? Functions.Max(f => f.CyclomaticComplexity) : 0
                };
            }
        }

        #endregion

        #region Các phương thức truy vấn nhanh

        public CVariable GetVariable(string name) => Variables.FirstOrDefault(v => v.Name == name);
        public CFunction GetFunction(string name) => Functions.FirstOrDefault(f => f.Name == name);
        public CDefinition GetDefinition(string name) => Definitions.FirstOrDefault(d => d.Name == name);
        public CStruct GetStructure(string name) => Structures.FirstOrDefault(s => s.Name == name);
        public CUnion GetUnion(string name) => Unions.FirstOrDefault(u => u.Name == name);
        public CEnum GetEnumeration(string name) => Enumerations.FirstOrDefault(e => e.Name == name);
        public CTypedef GetTypedef(string name) => Typedefs.FirstOrDefault(t => t.Name == name);
        public CInclude GetInclude(string includePath) => Includes.FirstOrDefault(i => i.IncludePath == includePath);

        /// <summary>
        /// Lấy danh sách hàm gọi tới một hàm cụ thể
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
        /// Lấy danh sách hàm được gọi bởi một hàm cụ thể
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
        /// Tìm kiếm symbol theo tên (có thể dùng regex)
        /// </summary>
        public List<ISymbol> FindSymbols(string pattern, bool useRegex = false)
        {
            var allSymbols = new List<ISymbol>();
            allSymbols.AddRange(Functions.Cast<ISymbol>());
            allSymbols.AddRange(Variables.Cast<ISymbol>());
            allSymbols.AddRange(Structures.Cast<ISymbol>());
            allSymbols.AddRange(Unions.Cast<ISymbol>());
            allSymbols.AddRange(Enumerations.Cast<ISymbol>());
            allSymbols.AddRange(Typedefs.Cast<ISymbol>());

            if (useRegex)
            {
                try
                {
                    var regex = new System.Text.RegularExpressions.Regex(pattern,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    return allSymbols.Where(s => regex.IsMatch(s.Name)).ToList();
                }
                catch
                {
                    // Nếu regex lỗi, fallback về so khớp chuỗi thường
                    return allSymbols.Where(s => s.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
                }
            }

            return allSymbols.Where(s => s.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        /// <summary>
        /// Lấy tất cả hàm sử dụng một biến cụ thể
        /// </summary>
        public List<CFunction> GetFunctionsUsingVariable(string variableName)
        {
            return Functions
                .Where(f => f.UsedVariables != null && f.UsedVariables.Contains(variableName))
                .ToList();
        }

        /// <summary>
        /// Lấy tất cả biến được sử dụng trong một hàm cụ thể
        /// </summary>
        public List<CVariable> GetVariablesUsedByFunction(string functionName)
        {
            var function = GetFunction(functionName);
            if (function?.UsedVariables == null)
                return new List<CVariable>();

            return Variables
                .Where(v => function.UsedVariables.Contains(v.Name))
                .ToList();
        }

        #endregion

        #region Gộp và kiểm tra hợp lệ

        /// <summary>
        /// Gộp kết quả phân tích khác vào đối tượng hiện tại
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

            Structures.AddRange(other.Structures);
            Unions.AddRange(other.Unions);
            Enumerations.AddRange(other.Enumerations);
            Typedefs.AddRange(other.Typedefs);
            Includes.AddRange(other.Includes);
            TypeDependencies.AddRange(other.TypeDependencies);
            SymbolReferences.AddRange(other.SymbolReferences);

            ParseErrors.AddRange(other.ParseErrors);
            ParseWarnings.AddRange(other.ParseWarnings);

            Statistics.Merge(other.Statistics);

            IsSuccess = IsSuccess && other.IsSuccess;
            IsComplete = IsComplete && other.IsComplete;
        }

        /// <summary>
        /// Kiểm tra hợp lệ dữ liệu phân tích (ví dụ: trùng tên, tham chiếu chưa giải quyết, ...)
        /// </summary>
        public List<string> Validate()
        {
            var issues = new List<string>();

            // Kiểm tra trùng tên hàm
            var functionNames = Functions.Select(f => f.Name).ToList();
            var duplicateFunctions = functionNames.GroupBy(n => n).Where(g => g.Count() > 1);
            foreach (var dup in duplicateFunctions)
            {
                issues.Add($"Trùng tên hàm: {dup.Key}");
            }

            // Kiểm tra symbol chưa giải quyết
            foreach (var reference in SymbolReferences)
            {
                if (!reference.IsResolved)
                {
                    issues.Add($"Symbol chưa giải quyết: {reference.Name} tại dòng {reference.LineNumber}");
                }
            }

            // Kiểm tra mối quan hệ gọi hàm hợp lệ
            foreach (var relationship in FunctionRelationships)
            {
                if (GetFunction(relationship.CallerName) == null)
                {
                    issues.Add($"Quan hệ gọi hàm tham chiếu caller không tồn tại: {relationship.CallerName}");
                }
                if (GetFunction(relationship.CalleeName) == null)
                {
                    issues.Add($"Quan hệ gọi hàm tham chiếu callee không tồn tại: {relationship.CalleeName}");
                }
            }

            return issues;
        }

        #endregion
    }
}