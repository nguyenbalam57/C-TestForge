using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.Projects;
using C_TestForge.Models.Core;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using ClangSharp;
using ClangSharp.Interop;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;


namespace C_TestForge.Parser
{
    /// <summary>
    /// Service chính phân tích mã nguồn C sử dụng ClangSharp
    /// </summary>
    public class ClangSharpParserService : IParserService, IClangSharpParserService
    {
        private readonly ILogger<ClangSharpParserService> _logger;
        private readonly IPreprocessorService _preprocessorService;
        private readonly IFunctionAnalysisService _functionAnalysisService;
        private readonly IVariableAnalysisService _variableAnalysisService;
        private readonly IMacroAnalysisService _macroAnalysisService;
        private readonly IFileService _fileService;
        private readonly IConfigurationService _configurationService;
        private readonly ITypeManager _typeManager;
        private readonly IFileScannerService _fileScannerService;

        /// <summary>
        /// Constructor for ClangSharpParserService
        /// </summary>
        public ClangSharpParserService(
            ILogger<ClangSharpParserService> logger,
            IPreprocessorService preprocessorService,
            IFunctionAnalysisService functionAnalysisService,
            IVariableAnalysisService variableAnalysisService,
            IMacroAnalysisService macroAnalysisService,
            IFileService fileService,
            ITypeManager typeManager,
            IFileScannerService fileScannerService,
            IConfigurationService configurationService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _preprocessorService = preprocessorService ?? throw new ArgumentNullException(nameof(preprocessorService));
            _functionAnalysisService = functionAnalysisService ?? throw new ArgumentNullException(nameof(functionAnalysisService));
            _variableAnalysisService = variableAnalysisService ?? throw new ArgumentNullException(nameof(variableAnalysisService));
            _macroAnalysisService = macroAnalysisService ?? throw new ArgumentNullException(nameof(macroAnalysisService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _typeManager = typeManager ?? throw new ArgumentNullException(nameof(typeManager));
            _fileScannerService = fileScannerService ?? throw new ArgumentNullException(nameof(fileScannerService));
            _configurationService = configurationService;

            // Đảm bảo hỗ trợ các bảng mã cho xử lý file
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
        }

        #region IClangSharpParserService Implementation

        /// <inheritdoc/>
        public async Task<SourceFile> ParseSourceFileAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            if (!_fileService.FileExists(filePath))
            {
                throw new FileNotFoundException($"Source file not found: {filePath}");
            }

            try
            {
                _logger.LogInformation($"Parsing source file: {filePath}");

                // Đọc nội dung file
                string sourceContent = await _fileService.ReadFileAsync(filePath);

                // Convert ParseResult to SourceFile
                var sourceFile = new SourceFile
                {
                    FilePath = filePath,
                    FileName = _fileService.GetFileName(filePath),
                    Content = sourceContent,
                    Lines = sourceContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList(),
                    ContentHash = GetContentHash(sourceContent),
                    FileType = DetermineFileType(filePath),
                    LastModified = _fileService.GetLastModifiedTime(filePath),
                    //ParseResult = parseResult,
                    IsDirty = false
                };

                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing source file: {filePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<SourceFile>> ParseSourceFilesAsync(IEnumerable<string> filePaths)
        {
            if (filePaths == null)
            {
                throw new ArgumentNullException(nameof(filePaths));
            }

            var sourceFiles = new List<SourceFile>();
            var tasks = new List<Task<SourceFile>>();

            foreach (var filePath in filePaths)
            {
                tasks.Add(ParseSourceFileAsync(filePath));
            }

            try
            {
                // Wait for all parsing tasks to complete
                var results = await Task.WhenAll(tasks);
                sourceFiles.AddRange(results);

                _logger.LogInformation($"Successfully parsed {sourceFiles.Count} source files");
                return sourceFiles;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing source files");
                throw;
            }
        }

        #endregion

        #region IParserService Implementation

        /// <inheritdoc/>
        public async Task<ParseResult> ParseSourceFileParserAsync(List<SourceFile> sourceFiles, SourceFile sourceFile, ParseOptions options)
        {

            try
            {
                _logger.LogInformation($"Starting parse of file: {sourceFile.FilePath}");

                // Parse the source code
                return await ParseSourceCodeParserAsync(sourceFiles, sourceFile, options);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing source file: {sourceFile.FilePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ParseResult> ParseSourceCodeParserAsync(List<SourceFile> sourceFiles, SourceFile sourceFile, ParseOptions options)
        {
            if (string.IsNullOrEmpty(sourceFile.Content))
            {
                throw new ArgumentException("Source code cannot be null or empty", nameof(sourceFile.Content));
            }

            if (string.IsNullOrEmpty(sourceFile.FileName))
            {
                sourceFile.FileName = "inline.c";
            }

            // Initialize parse result - this was missing in original code
            var result = new ParseResult
            {
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation($"Parsing source code for {sourceFile.FileName}");

                // Khai báo biến ở phạm vi ngoài cùng để có thể truy cập từ khối finally
                CXIndex index = default;
                CXTranslationUnit translationUnit = default;
                List<IntPtr> allocatedPointers = new List<IntPtr>();
                CXCursor rootCursor = default;

                try
                {
                    unsafe
                    {
                        // Khởi tạo CXIndex
                        index = (CXIndex)clang.createIndex(1, 1);

                        // Set compilation options
                        CXTranslationUnit_Flags flags = CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord |
                                                      CXTranslationUnit_Flags.CXTranslationUnit_IncludeAttributedTypes |
                                                      CXTranslationUnit_Flags.CXTranslationUnit_VisitImplicitAttributes |
                                                      CXTranslationUnit_Flags.CXTranslationUnit_KeepGoing;

                        // Standard include paths and arguments
                        var args = new List<string>();

                        // Add include paths from options
                        if (options.IncludePaths != null && options.IncludePaths.Count > 0)
                        {
                            foreach (var includePath in options.IncludePaths)
                            {
                                args.Add($"-I{includePath}");
                            }
                        }
                        else
                        {
                            // Default include paths
                            //args.Add("-I/usr/include");
                            //args.Add("-I/usr/local/include");
                        }

                        // Add macro definitions from options
                        if (options.MacroDefinitions != null && options.MacroDefinitions.Count > 0)
                        {
                            foreach (var def in options.MacroDefinitions)
                            {
                                if (string.IsNullOrEmpty(def))
                                {
                                    args.Add($"-D{def}");
                                }
                            }
                        }

                        // Add additional arguments
                        if (options.AdditionalClangArguments != null && options.AdditionalClangArguments.Count > 0)
                        {
                            args.AddRange(options.AdditionalClangArguments);
                        }
                        else
                        {
                            // Default C standard
                            args.Add("-std=c99");
                        }

                        // Parse the translation unit
                        // Convert filename and source code to byte arrays
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(sourceFile.FileName + "\0");
                        byte[] sourceCodeBytes = System.Text.Encoding.UTF8.GetBytes(sourceFile.Content);

                        // Allocate memory for arguments
                        var argPtrs = new List<IntPtr>();
                        foreach (var arg in args)
                        {
                            IntPtr ptr = Marshal.StringToHGlobalAnsi(arg);
                            argPtrs.Add(ptr);
                            allocatedPointers.Add(ptr); // Track for cleanup
                        }

                        fixed (byte* fileNamePtr = fileNameBytes)
                        fixed (byte* sourceCodePtr = sourceCodeBytes)
                        {
                            // Create unsaved file for in-memory parsing
                            var unsavedFile = new CXUnsavedFile
                            {
                                Filename = (sbyte*)fileNamePtr,
                                Contents = (sbyte*)sourceCodePtr,
                                Length = (nuint)sourceFile.Content.Length
                            };

                            // Lấy con trỏ trực tiếp thay vì sử dụng khối fixed thứ hai
                            CXUnsavedFile* unsavedFilePtr = &unsavedFile;

                            // Convert the managed array to a native array of pointers
                            sbyte** argArray = stackalloc sbyte*[args.Count];
                            for (int i = 0; i < args.Count; i++)
                            {
                                argArray[i] = (sbyte*)argPtrs[i].ToPointer();
                            }

                            _logger.LogInformation("Clang arguments: " + string.Join(" ", args));


                            translationUnit = clang.parseTranslationUnit(
                        index,
                        (sbyte*)fileNamePtr,
                        argArray,
                        args.Count,
                        unsavedFilePtr,
                        1,
                        (uint)flags);

                            if (translationUnit.Handle == IntPtr.Zero)
                            {
                                throw new Exception("Failed to parse translation unit");
                            }
                        }

                        // Process diagnostics
                        uint numDiagnostics = clang.getNumDiagnostics(translationUnit);
                        for (uint i = 0; i < numDiagnostics; i++)
                        {
                            var diagnostic = clang.getDiagnostic(translationUnit, i);
                            var severity = clang.getDiagnosticSeverity(diagnostic);
                            var text = clang.getDiagnosticSpelling(diagnostic).ToString();
                            var location = clang.getDiagnosticLocation(diagnostic);

                            CXFile file;
                            uint line, column, offset;
                            location.GetFileLocation(out file, out line, out column, out offset);

                            var parseError = new ParseError
                            {
                                Message = text,
                                LineNumber = (int)line,
                                ColumnNumber = (int)column,
                                Severity = ConvertSeverity(severity)
                            };

                            result.ParseErrors.Add(parseError);
                        }

                        // Lấy cursor từ translation unit trong khối unsafe
                        rootCursor = clang.getTranslationUnitCursor(translationUnit);
                    }

                    sourceFile.IsParsed = true;
                    sourceFile.ParseResult = result;

                    _logger.LogInformation($"Extract define, functions, variables, enum, union, struct, typedef, v.v.. from the AST - sử dụng rootCursor đã lấy từ khối unsafe from {sourceFile.FileName}");
                    // Extract functions and variables from the AST - sử dụng rootCursor đã lấy từ khối unsafe
                    await TraverseASTAsync(rootCursor, sourceFile);

                    sourceFile.LastAnalyzed = DateTime.UtcNow;


                    _logger.LogInformation($"Successfully parsed {sourceFile.FileName}: {result.Functions.Count} functions, {result.Variables.Count} variables, {result.Definitions.Count} definitions");

                    return result;
                }
                finally
                {
                    unsafe
                    {
                        // Clean up Clang resources
                        if (translationUnit.Handle != IntPtr.Zero)
                        {
                            clang.disposeTranslationUnit(translationUnit);
                        }

                        if (index.Handle != IntPtr.Zero)
                        {
                            clang.disposeIndex(index);
                        }
                    }

                    // Free allocated memory
                    foreach (var ptr in allocatedPointers)
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing source code: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Quét một thư mục để tìm tất cả các tệp C (.c) và header (.h)
        /// </summary>
        /// <param name="directoryPath">Đường dẫn thư mục cần quét</param>
        /// <param name="recursive">Quét đệ quy các thư mục con</param>
        /// <returns>Danh sách tất cả các tệp C và header được tìm thấy</returns>
        public async Task<List<string>> ScanDirectoryForCFilesAsync(string directoryPath, bool recursive = true)
        {
            return await _fileScannerService.ScanDirectoryForCFilesAsync(directoryPath, recursive);
        }

        /// <summary>
        /// Tìm tất cả các thư mục include tiềm năng trong một dự án
        /// </summary>
        /// <param name="rootDirectoryPath">Đường dẫn tất cả các file</param>
        /// <returns>Danh sách các thư mục có chứa tệp header</returns>
        public async Task<List<string>> FindPotentialIncludeDirectoriesAsync(List<string> files)
        {
            var headerFiles = files.Where(f => f.EndsWith(".h", StringComparison.OrdinalIgnoreCase)).ToList();
            return headerFiles;
        }

        #region Helper Methods

        /// <summary>
        /// Traverses the AST of a translation unit to extract functions and variables
        /// </summary>
        /// <param name="cursor">Root cursor of the translation unit</param>
        /// <param name="sourceFileName">Name of the source file</param>
        /// <param name="result">Parse result to update</param>
        /// <param name="options">Parse options</param>
        /// <returns>Task</returns>
        private async Task TraverseASTAsync(CXCursor cursor, SourceFile sourceFile)
        {
            try
            {
                _logger.LogInformation($"Traversing AST for {sourceFile.FileName}");

                // Create a visitor to process the AST
                var visitor = new ASTVisitor(
                    sourceFile.FileName,
                    _variableAnalysisService,
                    _functionAnalysisService);

                // Visit all the children of the translation unit
                unsafe
                {
                    cursor.VisitChildren((child, parent, clientData) =>
                    {
                        visitor.Visit(child, parent, sourceFile.ParseResult);
                        return CXChildVisitResult.CXChildVisit_Continue;
                    }, default(CXClientData));
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error traversing AST: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts a Clang diagnostic severity to an error severity
        /// </summary>
        /// <param name="severity">Clang diagnostic severity</param>
        /// <returns>Error severity</returns>
        private ErrorSeverity ConvertSeverity(CXDiagnosticSeverity severity)
        {
            switch (severity)
            {
                case CXDiagnosticSeverity.CXDiagnostic_Ignored:
                    return ErrorSeverity.Info;
                case CXDiagnosticSeverity.CXDiagnostic_Note:
                    return ErrorSeverity.Info;
                case CXDiagnosticSeverity.CXDiagnostic_Warning:
                    return ErrorSeverity.Warning;
                case CXDiagnosticSeverity.CXDiagnostic_Error:
                    return ErrorSeverity.Error;
                case CXDiagnosticSeverity.CXDiagnostic_Fatal:
                    return ErrorSeverity.Critical;
                default:
                    return ErrorSeverity.Info;
            }
        }

        /// <summary>
        /// Calculate hash for content to detect changes
        /// </summary>
        /// <param name="content">Content to hash</param>
        /// <returns>Content hash as string</returns>
        private string GetContentHash(string content)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
                byte[] hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Determine file type based on file extension
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Source file type</returns>
        private SourceFileType DetermineFileType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".c":
                    return SourceFileType.CSource;
                case ".h":
                    return SourceFileType.CHeader;
                case ".cpp":
                case ".cc":
                case ".cxx":
                    return SourceFileType.CPPSource;
                case ".hpp":
                case ".hxx":
                    return SourceFileType.CPPHeader;
                default:
                    return SourceFileType.Unknown;
            }
        }

        /// <summary>
        /// Calculates the cyclomatic complexity of a function
        /// </summary>
        private int CalculateCyclomaticComplexity(CFunction function)
        {
            // Basic complexity is 1
            int complexity = 1;

            // Count decision points in the body (if, for, while, switch statements)
            // This is a simple implementation - a real one would parse the AST
            string body = function.Body.ToLower();
            complexity += CountOccurrences(body, "if ");
            complexity += CountOccurrences(body, "for ");
            complexity += CountOccurrences(body, "while ");
            complexity += CountOccurrences(body, "case ");
            complexity += CountOccurrences(body, "&&");
            complexity += CountOccurrences(body, "||");

            return complexity;
        }

        /// <summary>
        /// Counts occurrences of a substring in a string
        /// </summary>
        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int position = 0;

            while ((position = text.IndexOf(pattern, position)) != -1)
            {
                count++;
                position += pattern.Length;
            }

            return count;
        }

        /// <summary>
        /// Helper class to visit the AST and collect variables and functions
        /// </summary>
        private class ASTVisitor
        {
            private readonly string _sourceFileName;
            private readonly IVariableAnalysisService _variableAnalysisService;
            private readonly IFunctionAnalysisService _functionAnalysisService;

            /// <summary>
            /// List of variables found during traversal
            /// </summary>
            public List<CVariable> Variables { get; } = new List<CVariable>();

            /// <summary>
            /// List of functions found during traversal
            /// </summary>
            public List<CFunction> Functions { get; } = new List<CFunction>();

            /// <summary>
            /// Constructor for ASTVisitor
            /// </summary>
            /// <param name="sourceFileName">Name of the source file</param>
            /// <param name="variableAnalysisService">Variable analysis service</param>
            /// <param name="functionAnalysisService">Function analysis service</param>
            /// <param name="options">Parse options</param>
            public ASTVisitor(
                string sourceFileName,
                IVariableAnalysisService variableAnalysisService,
                IFunctionAnalysisService functionAnalysisService)
            {
                _sourceFileName = sourceFileName;
                _variableAnalysisService = variableAnalysisService;
                _functionAnalysisService = functionAnalysisService;
            }

            /// <summary>
            /// Visit a cursor in the AST
            /// </summary>
            /// <param name="cursor">Current cursor</param>
            /// <param name="parent">Parent cursor</param>
            public void Visit(CXCursor cursor, CXCursor parent, ParseResult parseResult)
            {
                try
                {
                    // Get the cursor's location
                    CXFile file;
                    uint line, column, offset;
                    cursor.Location.GetFileLocation(out file, out line, out column, out offset);

                    // Skip if the cursor is not from the source file we're analyzing
                    if (file.Handle != IntPtr.Zero)
                    {
                        string cursorFile = file.Name.ToString();
                        if (!cursorFile.EndsWith(_sourceFileName))
                        {
                            return;
                        }
                    }

                    // Process variable declarations
                    if ( cursor.Kind == CXCursorKind.CXCursor_VarDecl)
                    {
                        _variableAnalysisService.ExtractVariable(cursor, parseResult);
                    }

                    // Process function declarations
                    if ( cursor.Kind == CXCursorKind.CXCursor_FunctionDecl)
                    {
                        //var function = _functionAnalysisService.ExtractFunction(cursor, sourceCode);
                        //if (function != null)
                        //{
                        //    Functions.Add(function);
                        //}
                    }
                }
                catch (Exception ex)
                {
                    // Log the error but continue processing
                    // In a real implementation, we would want to properly log this
                    Console.Error.WriteLine($"Error visiting AST node: {ex.Message}");
                }
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// Phân tích toàn bộ dự án C/C++ theo quy trình hoàn chỉnh
        /// </summary>
        /// <param name="sourceFiles"> file dự án</param>
        /// <returns>Kết quả phân tích dự án hoàn chỉnh bao gồm biến, hàm, macro và phụ thuộc</returns>
        public async Task<ProjectAnalysisResult> AnalyzeCompleteProjectAsync(Project project, List<SourceFile> sourceFiles)
        {
            if (sourceFiles == null)
            {
                throw new ArgumentException("Project root path cannot be null or empty", nameof(sourceFiles));
            }

            _logger.LogInformation($"Bắt đầu phân tích toàn bộ file: {sourceFiles.Count}");

            try
            {
                foreach (var file in sourceFiles)
                {
                    _logger.LogInformation($"- {file.FileName}");

                    file.ParseResult.StartTime = DateTime.Now;

                    try
                    {
                        _logger.LogInformation($"Phân tích {file.FileName}");
                        await AnalyzeSingleFileInContext(project, sourceFiles, file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi phân tích tệp {file.FileName}: {ex.Message}");
                    }

                    //_logger.LogInformation($"Hoàn thành phân tích dự án trong {analysisResult.Duration.TotalSeconds:F2} giây");
                    //_logger.LogInformation($"Kết quả: {analysisResult.Functions.Count} hàm, {analysisResult.Variables.Count} biến, {analysisResult.Macros.Count} macro");

                    return new ProjectAnalysisResult();
                }
                return new ProjectAnalysisResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi nghiêm trọng khi phân tích dự án: {ex.Message}");
                //analysisResult.EndTime = DateTime.Now;
                //analysisResult.Duration = analysisResult.EndTime - analysisResult.StartTime;
                //analysisResult.Errors.Add($"Critical error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lấy danh sách các thư mục chứa file header (.h, .hpp, ...) từ sortedFiles, chuẩn hóa về absolute path kiểu Unix, loại bỏ trùng lặp.
        /// </summary>
        /// <param name="sortedFiles">Danh sách các file đã sắp xếp</param>
        /// <returns>Danh sách đường dẫn thư mục duy nhất, dạng /usr/include</returns>
        private List<string> GetUniqueHeaderDirectoriesAsUnixPaths(List<string> pathFiles)
        {
            var dirSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in pathFiles)
            {
                var dir = _fileService.GetDirectoryName(file);
                if (!string.IsNullOrEmpty(dir))
                {
                    string unixPath = ToUnixStylePath(Path.GetFullPath(dir));
                    dirSet.Add(unixPath);
                }
            }

            return dirSet.ToList();
        }

        private string ToUnixStylePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            // Nếu là UNC path (bắt đầu bằng \\), chuyển thành /server/share/...
            if (path.StartsWith(@"\\"))
            {
                // Bỏ 2 dấu \\ đầu, thay \ thành /
                string unix = path.TrimStart('\\').Replace('\\', '/');
                // Thêm dấu / đầu cho đúng chuẩn Unix
                return "/" + unix;
            }
            // Nếu là path Windows thông thường (C:\...), cũng chuyển \ thành /
            return path.Replace('\\', '/');
        }


        #region Project Analysis Helper Methods

        /// <summary>
        /// Phân tích một tệp đơn lẻ trong ngữ cảnh của toàn bộ dự án
        /// </summary>
        /// <param name="filePath">Đường dẫn tệp cần phân tích</param>
        /// <param name="analysisResult">Kết quả phân tích dự án để cập nhật</param>
        /// <returns>Task</returns>
        private async Task AnalyzeSingleFileInContext(Project project, List<SourceFile> sourceFiles, SourceFile sourceFile)
        {
            try
            {
                // Get active configuration if available
                ParseOptions options = ParseOptions.Default;
                if (_configurationService != null)
                {
                    var config = _configurationService.GetActiveConfiguration();
                    if (config != null)
                    {
                        options = _configurationService.CreateParseOptionsFromConfiguration(config);
                    }
                }

                // Thêm include paths từ Project 
                if (project.IncludePaths.Count > 0)
                {
                    foreach (var includePath in project.IncludePaths)
                    {
                        if (!options.IncludePaths.Contains(includePath))
                        {
                            options.IncludePaths.Add(includePath);
                        }
                    }
                }

                // Thêm macro definitions từ Project
                if (project.MacroDefinitions.Count > 0)
                {
                    foreach (var macro in project.MacroDefinitions)
                    {
                        if (!options.MacroDefinitions.Contains(macro))
                        {
                            options.MacroDefinitions.Add(macro);
                        }
                    }
                }

                var parseResult = await ParseSourceFileParserAsync(sourceFiles, sourceFile, options);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi phân tích tệp {sourceFile.FilePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Trích xuất tên các macro từ một biểu thức điều kiện
        /// </summary>
        private List<string> ExtractMacrosFromExpression(string expression)
        {
            var macros = new List<string>();

            // Tìm các định danh trong biểu thức
            var matches = System.Text.RegularExpressions.Regex.Matches(
                expression,
                @"[A-Za-z_][A-Za-z0-9_]*",
                System.Text.RegularExpressions.RegexOptions.Compiled);

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                string potential = match.Value;

                // Bỏ qua các từ khóa C
                if (!IsKeyword(potential))
                {
                    macros.Add(potential);
                }
            }

            return macros;
        }

        /// <summary>
        /// Kiểm tra xem một chuỗi có phải là từ khóa C không
        /// </summary>
        private bool IsKeyword(string word)
        {
            string[] keywords = {
                "if", "else", "for", "while", "do", "switch", "case", "default",
                "break", "continue", "return", "goto", "typedef", "sizeof", "struct",
                "union", "enum", "extern", "static", "auto", "register", "void",
                "char", "short", "int", "long", "float", "double", "signed", "unsigned",
                "const", "volatile", "defined", "ifdef", "ifndef", "endif", "elif", "true", "false"
            };

            return keywords.Contains(word.ToLowerInvariant());
        }

        #endregion
    }
}