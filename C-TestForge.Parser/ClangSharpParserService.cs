using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.Projects;
using C_TestForge.Models.CodeAnalysis.Functions;
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
        private readonly ISourceFileService _sourceFileService;
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
            ISourceFileService sourceFileService,
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
            _sourceFileService = sourceFileService ?? throw new ArgumentNullException(nameof(sourceFileService));
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

                // Xử lý code để thay đổi type biến
                if (string.IsNullOrEmpty(sourceFile.ProcessedContent))
                    _sourceFileService.ProcessTypeReplacements(sourceFile);

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

        /// <inheritdoc/>
        public async Task<SourceFile> ParseSourceCodeAsync(SourceFile sourceFile, ParseOptions options, string fileName = "inline.c")
        {

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "inline.c";
            }

            try
            {
                _logger.LogInformation($"Parsing source code with name: {fileName}");

                // Lấy mã nguồn từ source đã được raplace types
                var parseResult = await ParseSourceFileParserAsync(sourceFile, options);

                // Ghi ParseResult vào SourceFile
                sourceFile.ParseResult = parseResult;

                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing source code: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<CFunction>> ExtractFunctionsAsync(SourceFile sourceFile)
        {

            try
            {
                _logger.LogInformation($"Extracting functions from file: {sourceFile.FilePath}");

                // Parse the source file with function analysis only
                var options = new ParseOptions
                {
                    AnalyzeFunctions = true,
                    AnalyzeVariables = false,
                    ParsePreprocessorDefinitions = false
                };

                var parseResult = await ParseSourceFileParserAsync(sourceFile, options);

                _logger.LogInformation($"Extracted {parseResult.Functions.Count} functions from {sourceFile.FilePath}");
                return parseResult.Functions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting functions: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<CFunction>> ExtractFunctionsFromCodeAsync(SourceFile sourceFile, string fileName = "inline.c")
        {

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "inline.c";
            }

            try
            {
                _logger.LogInformation($"Extracting functions from code: {fileName}");

                // Parse the source code with function analysis only
                var options = new ParseOptions
                {
                    AnalyzeFunctions = true,
                    AnalyzeVariables = false,
                    ParsePreprocessorDefinitions = false
                };

                var parseResult = await ParseSourceFileParserAsync(sourceFile, options);

                _logger.LogInformation($"Extracted {parseResult.Functions.Count} functions from source code");
                return parseResult.Functions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting functions from code: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region IParserService Implementation

        /// <inheritdoc/>
        public async Task<ParseResult> ParseSourceFileParserAsync(SourceFile sourceFile, ParseOptions options)
        {

            try
            {
                _logger.LogInformation($"Starting parse of file: {sourceFile.FilePath}");

                // Parse the source code
                return await ParseSourceCodeParserAsync(sourceFile, options);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing source file: {sourceFile.FilePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ParseResult> ParseSourceCodeParserAsync(SourceFile sourceFile, ParseOptions options)
        {
            if (string.IsNullOrEmpty(sourceFile.Content))
            {
                throw new ArgumentException("Source code cannot be null or empty", nameof(sourceFile.Content));
            }

            if (string.IsNullOrEmpty(sourceFile.FileName))
            {
                sourceFile.FileName = "inline.c";
            }

            try
            {
                _logger.LogInformation($"Parsing source code for {sourceFile.FileName}");

                var result = new ParseResult
                {
                    SourceFilePath = sourceFile.FileName
                };

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
                            args.Add("-I/usr/include");
                            args.Add("-I/usr/local/include");
                        }

                        // Add macro definitions from options
                        if (options.MacroDefinitions != null && options.MacroDefinitions.Count > 0)
                        {
                            foreach (var def in options.MacroDefinitions)
                            {
                                if (string.IsNullOrEmpty(def.Value))
                                {
                                    args.Add($"-D{def.Key}");
                                }
                                else
                                {
                                    args.Add($"-D{def.Key}={def.Value}");
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
                                Length = (nuint)sourceFile.ProcessedContent.Length
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

                    // Extract preprocessor definitions
                    if (options.ParsePreprocessorDefinitions)
                    {
                        await ExtractPreprocessorDefinitionsAsync(translationUnit, sourceFile.FileName, result);
                    }

                    // Extract functions and variables from the AST - sử dụng rootCursor đã lấy từ khối unsafe
                    if (options.AnalyzeFunctions || options.AnalyzeVariables)
                    {
                        await TraverseASTAsync(rootCursor, sourceFile.FileName, sourceFile.ProcessedContent, result, options);
                    }

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

        /// <inheritdoc/>
        public async Task<List<string>> GetIncludedFilesParserAsync(SourceFile sourceFile)
        {

            try
            {
                _logger.LogInformation($"Getting included files for: {sourceFile.FilePath}");

                // Parse the source code with minimal options
                var options = new ParseOptions
                {
                    ParsePreprocessorDefinitions = true,
                    AnalyzeFunctions = false,
                    AnalyzeVariables = false
                };

                var result = await ParseSourceCodeParserAsync(sourceFile, options);

                // Extract include directives
                var includes = result.PreprocessorDirectives
                    .Where(d => d.Type == "include")
                    .Select(d => d.Value.Trim())
                    .ToList();

                _logger.LogInformation($"Found {includes.Count} included files in {sourceFile.FilePath}");

                return includes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting included files: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionAnalysisResult> AnalyzeFunctionParserAsync(string functionName, SourceFile sourceFile)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException("Function name cannot be null or empty", nameof(functionName));
            }

            try
            {
                _logger.LogInformation($"Analyzing function {functionName} in file: {sourceFile.FilePath}");

                // Parse the source file first
                var options = new ParseOptions
                {
                    AnalyzeFunctions = true,
                    AnalyzeVariables = true,
                    ParsePreprocessorDefinitions = true
                };

                var parseResult = await ParseSourceFileParserAsync(sourceFile, options);

                // Find the function in the parse result
                var function = parseResult.Functions.FirstOrDefault(f => f.Name == functionName);
                if (function == null)
                {
                    throw new InvalidOperationException($"Function '{functionName}' not found in file: {sourceFile.FilePath}");
                }

                // Create a new FunctionAnalysisResult
                var result = new FunctionAnalysisResult
                {
                    FunctionName = function.Name,
                    FilePath = sourceFile.FilePath,
                    ReturnType = function.ReturnType,
                    StartLine = function.StartLineNumber,
                    EndLine = function.EndLineNumber,
                    Body = function.Body,
                    CalledFunctions = function.CalledFunctions,
                    CyclomaticComplexity = CalculateCyclomaticComplexity(function)
                };

                // Map parameters
                foreach (var param in function.Parameters)
                {
                    result.Parameters.Add(new FunctionParameter
                    {
                        Name = param.Name,
                        Type = param.TypeName,
                        IsPointer = param.IsPointer,
                        IsArray = param.IsArray
                    });
                }

                // Analyze branches and paths
                await AnalyzeBranchesAndPathsAsync(function, result, parseResult);

                // Extract variables
                ExtractFunctionVariables(function, result, parseResult);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing function {functionName}: {ex.Message}");
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

        /// <summary>
        /// Phân tích các câu lệnh include từ một tệp mã nguồn
        /// </summary>
        /// <param name="filePath">Đường dẫn đến tệp mã nguồn</param>
        /// <returns>Danh sách các đường dẫn include từ tệp này</returns>
        public async Task<List<IncludeStatement>> ParseIncludeStatementsAsync(string filePath)
        {
            var interfaceResults = await _fileScannerService.ParseIncludeStatementsAsync(filePath);
            return ConvertIncludeStatements(interfaceResults);
        }

        /// <summary>
        /// Xây dựng đồ thị phụ thuộc include cho một tập hợp các tệp
        /// </summary>
        /// <param name="filePaths">Danh sách các đường dẫn tệp để phân tích</param>
        /// <param name="includePaths">Danh sách các thư mục include để tìm kiếm</param>
        /// <returns>Đồ thị phụ thuộc include</returns>
        public async Task<IncludeDependencyGraph> BuildIncludeDependencyGraphAsync(List<string> filePaths, List<string> includePaths)
        {
            var interfaceResult = await _fileScannerService.BuildIncludeDependencyGraphAsync(filePaths, includePaths);
            return ConvertIncludeDependencyGraph(interfaceResult);
        }

        /// <summary>
        /// Phân tích các directive tiền xử lý điều kiện (#if, #ifdef, v.v.) từ một tệp
        /// </summary>
        /// <param name="filePath">Đường dẫn đến tệp</param>
        /// <returns>Danh sách các directive tiền xử lý</returns>
        public async Task<List<ConditionalBlock>> ParsePreprocessorConditionalsAsync(string filePath)
        {
            var interfaceResults = await _fileScannerService.ParsePreprocessorConditionalsAsync(filePath);
            return ConvertConditionalBlocks(interfaceResults);
        }

        #region Conversion Methods

        /// <summary>
        /// Chuyển đổi IncludeStatement từ interface sang model
        /// </summary>
        private List<IncludeStatement> ConvertIncludeStatements(List<IncludeStatement> interfaceStatements)
        {
            return interfaceStatements.Select(s => new IncludeStatement
            {
                FileName = s.FileName,
                RawIncludePath = s.RawIncludePath,
                NormalizedIncludePath = s.NormalizedIncludePath,
                ResolvedPath = s.ResolvedPath,
                IsSystemInclude = s.IsSystemInclude,
                LineNumber = s.LineNumber,
                Conditional = ConvertConditionalBlock(s.Conditional)
            }).ToList();
        }

        /// <summary>
        /// Chuyển đổi ConditionalBlock từ interface sang model
        /// </summary>
        private ConditionalBlock ConvertConditionalBlock(ConditionalBlock interfaceBlock,
            HashSet<ConditionalBlock> visited = null)
        {
            if (interfaceBlock == null) return null;
            visited ??= new HashSet<ConditionalBlock>();

            // Nêús đã duyệt block này rồi thì trả về null để tránh vòng lặp vô hạn
            if (!visited.Add(interfaceBlock))
            {
                _logger.LogWarning($"Circular reference detected in conditional block: {interfaceBlock.DirectiveType} at line {interfaceBlock.StartLine}");
                return null;
            }

            var model = new ConditionalBlock
            {
                DirectiveType = interfaceBlock.DirectiveType,
                Condition = interfaceBlock.Condition,
                StartLine = interfaceBlock.StartLine,
                EndLine = interfaceBlock.EndLine,
                NestedBlocks = ConvertConditionalBlocks(interfaceBlock.NestedBlocks, visited),
                Includes = ConvertIncludeStatements(interfaceBlock.Includes),
                Parent = ConvertConditionalBlock(interfaceBlock.Parent, visited)
            };

            // Sau khi xử lý xong node này, có thể bỏ ra khỏi visited nếu muốn cho các nhánh khác dùng lại
            visited.Remove(interfaceBlock);

            return model;
        }

        /// <summary>
        /// Chuyển đổi danh sách ConditionalBlock từ interface sang model
        /// </summary>
        private List<ConditionalBlock> ConvertConditionalBlocks(List<ConditionalBlock> interfaceBlocks,
            HashSet<ConditionalBlock> visited = null)
        {
            if (interfaceBlocks == null) return new List<ConditionalBlock>();
            return interfaceBlocks?.Select(b => ConvertConditionalBlock(b, visited)).ToList();
        }

        /// <summary>
        /// Chuyển đổi IncludeDependencyGraph từ interface sang model
        /// </summary>
        private IncludeDependencyGraph ConvertIncludeDependencyGraph(IncludeDependencyGraph interfaceGraph)
        {
            var modelGraph = new IncludeDependencyGraph
            {
                IncludePaths = interfaceGraph.IncludePaths?.ToList() ?? new List<string>()
            };

            // Chuyển đổi source files
            if (interfaceGraph.SourceFiles != null)
            {
                foreach (var interfaceFile in interfaceGraph.SourceFiles)
                {
                    var modelFile = new SourceFileDependency
                    {
                        FilePath = interfaceFile.FilePath,
                        FileType = interfaceFile.FileType,
                        Parsed = interfaceFile.Parsed,
                        Includes = ConvertIncludeStatements(interfaceFile.Includes),
                        ConditionalBlocks = ConvertConditionalBlocks(interfaceFile.ConditionalBlocks)
                    };

                    modelGraph.SourceFiles.Add(modelFile);
                }

                // Thiết lập dependencies sau khi tất cả files đã được tạo
                for (int i = 0; i < interfaceGraph.SourceFiles.Count; i++)
                {
                    var interfaceFile = interfaceGraph.SourceFiles[i];
                    var modelFile = modelGraph.SourceFiles[i];

                    // Chuyển đổi direct dependencies
                    foreach (var interfaceDep in interfaceFile.DirectDependencies)
                    {
                        var modelDep = modelGraph.SourceFiles.FirstOrDefault(f => f.FilePath == interfaceDep.FilePath);
                        if (modelDep != null)
                        {
                            modelFile.DirectDependencies.Add(modelDep);
                        }
                    }

                    // Chuyển đổi dependent files
                    foreach (var interfaceDep in interfaceFile.DependentFiles)
                    {
                        var modelDep = modelGraph.SourceFiles.FirstOrDefault(f => f.FilePath == interfaceDep.FilePath);
                        if (modelDep != null)
                        {
                            modelFile.DependentFiles.Add(modelDep);
                        }
                    }
                }
            }

            return modelGraph;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates parse options from configuration
        /// </summary>
        /// <param name="configuration">Configuration to use</param>
        /// <returns>Parse options</returns>
        private ParseOptions CreateParseOptionsFromConfiguration(Configuration configuration)
        {
            var options = new ParseOptions
            {
                AnalyzeFunctions = true,
                AnalyzeVariables = true,
                ParsePreprocessorDefinitions = true
            };

            if (configuration != null)
            {
                options.IncludePaths = configuration.IncludePaths.ToList();
                options.MacroDefinitions = configuration.MacroDefinitions.ToDictionary(kv => kv.Key, kv => kv.Value);
                options.AdditionalClangArguments = configuration.AdditionalArguments.ToList();

                // Check if any properties affect parsing options
                if (configuration.Properties.TryGetValue("AnalyzeFunctions", out string analyzeFunctionsStr))
                {
                    if (bool.TryParse(analyzeFunctionsStr, out bool analyzeFunctions))
                    {
                        options.AnalyzeFunctions = analyzeFunctions;
                    }
                }

                if (configuration.Properties.TryGetValue("AnalyzeVariables", out string analyzeVariablesStr))
                {
                    if (bool.TryParse(analyzeVariablesStr, out bool analyzeVariables))
                    {
                        options.AnalyzeVariables = analyzeVariables;
                    }
                }

                if (configuration.Properties.TryGetValue("ParsePreprocessorDefinitions", out string parsePreprocessorStr))
                {
                    if (bool.TryParse(parsePreprocessorStr, out bool parsePreprocessor))
                    {
                        options.ParsePreprocessorDefinitions = parsePreprocessor;
                    }
                }
            }

            return options;
        }

        /// <summary>
        /// Extracts preprocessor definitions from a translation unit
        /// </summary>
        /// <param name="translationUnit">Translation unit to process</param>
        /// <param name="sourceFileName">Name of the source file</param>
        /// <param name="result">Parse result to update</param>
        /// <returns>Task</returns>
        private async Task ExtractPreprocessorDefinitionsAsync(CXTranslationUnit translationUnit, string sourceFileName, ParseResult result)
        {
            try
            {
                _logger.LogInformation($"Extracting preprocessor definitions from {sourceFileName}");

                // Use the preprocessor service to extract definitions
                var preprocessorResult = await _preprocessorService.ExtractPreprocessorDefinitionsAsync(translationUnit, sourceFileName);

                // Add the results to the parse result
                result.Definitions.AddRange(preprocessorResult.Definitions);
                result.ConditionalDirectives.AddRange(preprocessorResult.ConditionalDirectives);
                result.PreprocessorDirectives.AddRange(preprocessorResult.PreprocessorDirectives);

                // Add include directives
                if (preprocessorResult.Includes != null && preprocessorResult.Includes.Count > 0)
                {
                    foreach (var include in preprocessorResult.Includes)
                    {
                        var directive = new CPreprocessorDirective
                        {
                            Type = "include",
                            Value = include.IsSystemInclude ? $"<{include.FilePath}>" : $"\"{include.FilePath}\"",
                            LineNumber = include.LineNumber,
                            SourceFile = include.SourceFile
                        };

                        if (!result.PreprocessorDirectives.Any(d => d.LineNumber == directive.LineNumber && d.Type == directive.Type))
                        {
                            result.PreprocessorDirectives.Add(directive);
                        }
                    }
                }

                // Analyze macro relationships and dependencies
                await _macroAnalysisService.AnalyzeMacroRelationshipsAsync(result.Definitions, result.ConditionalDirectives);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting preprocessor definitions: {ex.Message}");
            }
        }

        /// <summary>
        /// Traverses the AST of a translation unit to extract functions and variables
        /// </summary>
        /// <param name="cursor">Root cursor of the translation unit</param>
        /// <param name="sourceFileName">Name of the source file</param>
        /// <param name="result">Parse result to update</param>
        /// <param name="options">Parse options</param>
        /// <returns>Task</returns>
        private async Task TraverseASTAsync(CXCursor cursor, string sourceFileName, string sourceCode, ParseResult result, ParseOptions options)
        {
            try
            {
                _logger.LogInformation($"Traversing AST for {sourceFileName}");

                // Create a visitor to process the AST
                var visitor = new ASTVisitor(
                    sourceFileName,
                    _variableAnalysisService,
                    _functionAnalysisService,
                    options);

                // Visit all the children of the translation unit
                unsafe
                {
                    cursor.VisitChildren((child, parent, clientData) =>
                    {
                        visitor.Visit(child, parent, sourceCode);
                        return CXChildVisitResult.CXChildVisit_Continue;
                    }, default(CXClientData));
                }

                // Get the extracted data from the visitor
                result.Variables.AddRange(visitor.Variables);
                result.Functions.AddRange(visitor.Functions);

                // Analyze variable relationships and constraints
                if (options.AnalyzeVariables && result.Variables.Count > 0)
                {
                    var constraints = await _variableAnalysisService.AnalyzeVariablesAsync(
                        result.Variables, result.Functions, result.Definitions);

                    // Associate constraints with variables
                    foreach (var constraint in constraints)
                    {
                        if (!string.IsNullOrEmpty(constraint.VariableName))
                        {
                            var variable = result.Variables.FirstOrDefault(v => v.Name == constraint.VariableName);
                            if (variable != null && !variable.Constraints.Any(c => c.Id == constraint.Id))
                            {
                                variable.Constraints.Add(constraint);
                            }
                        }
                    }
                }

                // Analyze function relationships
                if (options.AnalyzeFunctions && result.Functions.Count > 0)
                {
                    var relationships = await _functionAnalysisService.AnalyzeFunctionRelationshipsAsync(result.Functions);

                    // Add function relationships to the result
                    foreach (var relationship in relationships)
                    {
                        result.FunctionRelationships.Add(relationship);
                    }
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
        /// Analyzes branches and paths in a function
        /// </summary>
        private async Task AnalyzeBranchesAndPathsAsync(CFunction function, FunctionAnalysisResult result, ParseResult parseResult)
        {
            // In a real implementation, this would analyze the AST to find branches and paths
            // For this example, we'll create some placeholder branches based on if statements

            string[] lines = function.Body.Split('\n');
            int lineNumber = function.StartLineNumber;
            int blockId = 0;

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                if (trimmedLine.StartsWith("if ") || trimmedLine.Contains(" if "))
                {
                    // Extract condition from if statement
                    int startIndex = trimmedLine.IndexOf("if ") + 3;
                    int endIndex = trimmedLine.IndexOf(')', startIndex);

                    if (endIndex > startIndex)
                    {
                        string condition = trimmedLine.Substring(startIndex, endIndex - startIndex + 1);

                        // Create a branch
                        var branch = new FunctionBranch
                        {
                            LineNumber = lineNumber,
                            Condition = condition,
                            TrueBlockId = ++blockId,
                            FalseBlockId = blockId + 1,
                            BranchType = BranchType.If
                        };

                        result.Branches.Add(branch);
                        blockId++; // Increment for the false block
                    }
                }

                lineNumber++;
            }

            // Create some basic paths
            if (result.Branches.Count > 0)
            {
                // Create a path for each branch
                foreach (var branch in result.Branches)
                {
                    // True path
                    var truePath = new FunctionPath
                    {
                        PathCondition = branch.Condition,
                        IsExecutable = true,
                        BlockSequence = new List<int> { branch.TrueBlockId }
                    };

                    // False path
                    var falsePath = new FunctionPath
                    {
                        PathCondition = $"!({branch.Condition})",
                        IsExecutable = true,
                        BlockSequence = new List<int> { branch.FalseBlockId }
                    };

                    result.Paths.Add(truePath);
                    result.Paths.Add(falsePath);
                }
            }
            else
            {
                // If no branches, create a single path
                var path = new FunctionPath
                {
                    PathCondition = "true",
                    IsExecutable = true,
                    BlockSequence = new List<int> { 0 }
                };

                result.Paths.Add(path);
            }
        }

        /// <summary>
        /// Extracts variables used in a function
        /// </summary>
        private void ExtractFunctionVariables(CFunction function, FunctionAnalysisResult result, ParseResult parseResult)
        {
            // Add parameters as variables
            foreach (var param in function.Parameters)
            {
                var variable = new FunctionVariable
                {
                    Name = param.Name,
                    Type = param.TypeName,
                    IsPointer = param.IsPointer,
                    IsArray = param.IsArray,
                    Scope = VariableScope.Parameter,
                    LineNumber = function.StartLineNumber
                };

                result.Variables.Add(variable);
            }

            // Extract local variables from the function body
            // This would be better done using the AST, but for the example we'll use a simple approach
            string[] lines = function.Body.Split('\n');
            int lineNumber = function.StartLineNumber;

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();

                // Check for variable declarations (simplistic approach)
                if (!trimmedLine.StartsWith("//") && !trimmedLine.StartsWith("/*") &&
                    !trimmedLine.StartsWith("*") && !trimmedLine.StartsWith("*/"))
                {
                    // Simple check for variable declarations like "int x = 5;"
                    foreach (var type in new[] { "int", "char", "float", "double", "long", "short", "bool", "unsigned" })
                    {
                        if ((trimmedLine.StartsWith(type + " ") || trimmedLine.Contains(" " + type + " ")) &&
                            trimmedLine.Contains("=") && trimmedLine.EndsWith(";"))
                        {
                            string afterType = trimmedLine.Substring(trimmedLine.IndexOf(type) + type.Length).Trim();
                            string varName = afterType.Split('=')[0].Trim();
                            string initialValue = afterType.Split('=')[1].Trim().TrimEnd(';');

                            var variable = new FunctionVariable
                            {
                                Name = varName,
                                Type = type,
                                InitialValue = initialValue,
                                LineNumber = lineNumber,
                                Scope = VariableScope.Local,
                                IsPointer = varName.Contains("*"),
                                IsArray = varName.Contains("[")
                            };

                            // Clean up name if it's a pointer or array
                            if (variable.IsPointer)
                            {
                                variable.Name = variable.Name.Replace("*", "").Trim();
                            }

                            if (variable.IsArray)
                            {
                                variable.Name = variable.Name.Split('[')[0].Trim();
                            }

                            result.Variables.Add(variable);
                        }
                    }
                }

                lineNumber++;
            }
        }

        /// <summary>
        /// Helper class to visit the AST and collect variables and functions
        /// </summary>
        private class ASTVisitor
        {
            private readonly string _sourceFileName;
            private readonly IVariableAnalysisService _variableAnalysisService;
            private readonly IFunctionAnalysisService _functionAnalysisService;
            private readonly ParseOptions _options;

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
                IFunctionAnalysisService functionAnalysisService,
                ParseOptions options)
            {
                _sourceFileName = sourceFileName;
                _variableAnalysisService = variableAnalysisService;
                _functionAnalysisService = functionAnalysisService;
                _options = options;
            }

            /// <summary>
            /// Visit a cursor in the AST
            /// </summary>
            /// <param name="cursor">Current cursor</param>
            /// <param name="parent">Parent cursor</param>
            public void Visit(CXCursor cursor, CXCursor parent, string sourceCode)
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
                    if (_options.AnalyzeVariables && cursor.Kind == CXCursorKind.CXCursor_VarDecl)
                    {
                        var variable = _variableAnalysisService.ExtractVariable(cursor, sourceCode);
                        if (variable != null)
                        {
                            Variables.Add(variable);
                        }
                    }

                    // Process function declarations
                    if (_options.AnalyzeFunctions && cursor.Kind == CXCursorKind.CXCursor_FunctionDecl)
                    {
                        var function = _functionAnalysisService.ExtractFunction(cursor, sourceCode);
                        if (function != null)
                        {
                            Functions.Add(function);
                        }
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
        /// <param name="projectRootPath">Đường dẫn thư mục gốc của dự án</param>
        /// <returns>Kết quả phân tích dự án hoàn chỉnh bao gồm biến, hàm, macro và phụ thuộc</returns>
        public async Task<ProjectAnalysisResult> AnalyzeCompleteProjectAsync(List<string> projectRootPath)
        {
            if (projectRootPath == null)
            {
                throw new ArgumentException("Project root path cannot be null or empty", nameof(projectRootPath));
            }

            foreach (var path in projectRootPath)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    throw new ArgumentException("Project root path contains null or empty string", nameof(projectRootPath));
                }
            }

            _logger.LogInformation($"Bắt đầu phân tích toàn bộ dự án: {projectRootPath}");

            var analysisResult = new ProjectAnalysisResult
            {
                ProjectPath = projectRootPath,
                StartTime = DateTime.Now
            };

            try
            {
                // Bước 1: Quét và xác định phạm vi dự án
                _logger.LogInformation("Bước 1: Quét dự án và xác định phạm vi tệp tin");
                //var allFiles = await ScanDirectoryForCFilesAsync(projectRootPath, true);
                var allFiles = projectRootPath;
                _logger.LogInformation($"Đã tìm thấy {allFiles.Count} tệp C/C++ trong dự án");

                // Bước 2: Tìm các thư mục include tiềm năng
                _logger.LogInformation("Bước 2: Tìm các thư mục include tiềm năng");
                var includeDirs = await FindPotentialIncludeDirectoriesAsync(projectRootPath);
                _logger.LogInformation($"Đã tìm thấy {includeDirs.Count} thư mục include tiềm năng");

                // Bước 3: Lấy đường dẫn thư mục của file header (.h, .hpp, ...) duy nhất
                _logger.LogInformation("Bước 3: Lấy các thư mục chứa file header duy nhất");
                var headerDirectories = GetUniqueHeaderDirectoriesAsUnixPaths(includeDirs);
                _logger.LogInformation($"Đã tìm thấy {headerDirectories.Count} thư mục chứa file header duy nhất");
                analysisResult.DependencyGraph.IncludePaths = headerDirectories;

                _logger.LogInformation($"Bắt đầu phân tích chi tiết mã");
                foreach (var file in projectRootPath)
                {
                    try
                    {
                        _logger.LogInformation($"Bắt đầu phân tích {file}");
                        await AnalyzeSingleFileInContext(file, analysisResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Lỗi khi phân tích tệp {file}: {ex.Message}");
                        analysisResult.Errors.Add($"Error analyzing file {file}: {ex.Message}");
                    }
                }

                // Bước 7: Phân tích mối quan hệ giữa các thành phần
                _logger.LogInformation("Bước 7: Phân tích mối quan hệ giữa các thành phần");
                await AnalyzeComponentRelationships(analysisResult);

                // Bước 8: Tối ưu hóa và hoàn thành kết quả
                _logger.LogInformation("Bước 8: Tối ưu hóa và hoàn thiện kết quả");
                OptimizeAnalysisResult(analysisResult);

                analysisResult.EndTime = DateTime.Now;
                analysisResult.Duration = analysisResult.EndTime - analysisResult.StartTime;

                _logger.LogInformation($"Hoàn thành phân tích dự án trong {analysisResult.Duration.TotalSeconds:F2} giây");
                _logger.LogInformation($"Kết quả: {analysisResult.Functions.Count} hàm, {analysisResult.Variables.Count} biến, {analysisResult.Macros.Count} macro");

                return analysisResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi nghiêm trọng khi phân tích dự án: {ex.Message}");
                analysisResult.EndTime = DateTime.Now;
                analysisResult.Duration = analysisResult.EndTime - analysisResult.StartTime;
                analysisResult.Errors.Add($"Critical error: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Trích xuất tất cả các điều kiện tiền xử lý từ kết quả phân tích dự án
        /// </summary>
        /// <param name="analysisResult">Kết quả phân tích dự án</param>
        /// <returns>Danh sách các điều kiện tiền xử lý duy nhất</returns>
        public List<string> ExtractPreprocessorConditionsFromAnalysisResult(ProjectAnalysisResult analysisResult)
        {
            if (analysisResult?.DependencyGraph == null)
                return new List<string>();

            return ExtractPreprocessorConditionsFromGraph(analysisResult.DependencyGraph);
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
        private async Task AnalyzeSingleFileInContext(string filePath, ProjectAnalysisResult analysisResult)
        {
            try
            {
                var sourceFile = await ParseSourceFileAsync(filePath);

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

                // Thêm include paths từ dependency graph 
                if (analysisResult.DependencyGraph.IncludePaths.Count > 0)
                {
                    foreach (var includePath in analysisResult.DependencyGraph.IncludePaths)
                    {
                        if (!options.IncludePaths.Contains(includePath))
                        {
                            options.IncludePaths.Add(includePath);
                        }
                    }
                }

                var parseResult = await ParseSourceFileParserAsync(sourceFile, options);

                // Thu thập functions
                foreach (var function in parseResult.Functions)
                {
                    if (!analysisResult.Functions.Any(f => f.Name == function.Name && f.SourceFile == function.SourceFile))
                    {
                        analysisResult.Functions.Add(function);
                    }
                }

                // Thu thập variables
                foreach (var variable in parseResult.Variables)
                {
                    if (!analysisResult.Variables.Any(v => v.Name == variable.Name && v.SourceFile == variable.SourceFile))
                    {
                        analysisResult.Variables.Add(variable);
                    }
                }

                analysisResult.ProcessedFiles.Add(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi phân tích tệp {filePath}: {ex.Message}");
                analysisResult.Errors.Add($"Error analyzing {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Phân tích typedef từ các tệp header
        /// </summary>
        private async Task AnalyzeTypedefsFromHeadersAsync(IncludeDependencyGraph dependencyGraph)
        {
            var headerFiles = dependencyGraph.SourceFiles
                .Where(f => f.FileType == SourceFileType.CHeader || f.FileType == SourceFileType.CPPHeader)
                .Select(f => f.FilePath)
                .ToList();

            if (headerFiles.Any())
            {
                await _typeManager.AnalyzeHeaderFilesAsync(headerFiles);
                await _typeManager.SaveTypedefConfigAsync();
            }
        }

        /// <summary>
        /// Sắp xếp tệp theo thứ tự phụ thuộc
        /// </summary>
        private List<SourceFileDependency> SortFilesByDependencies(IncludeDependencyGraph dependencyGraph)
        {
            var result = new List<SourceFileDependency>();
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var processing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Tìm các tệp không có phụ thuộc hoặc chỉ phụ thuộc vào tệp hệ thống
            var independentFiles = dependencyGraph.SourceFiles
                .Where(f => f.DirectDependencies.Count == 0)
                .OrderBy(f => f.FileType) // Header files trước
                .ToList();

            // Xử lý các tệp độc lập trước
            foreach (var file in independentFiles)
            {
                if (!processed.Contains(file.FilePath))
                {
                    result.Add(file);
                    processed.Add(file.FilePath);
                }
            }

            // Xử lý các tệp còn lại bằng thuật toán topo sort
            foreach (var file in dependencyGraph.SourceFiles)
            {
                if (!processed.Contains(file.FilePath))
                {
                    TopologicalSort(file, dependencyGraph, result, processed, processing);
                }
            }

            return result;
        }

        /// <summary>
        /// Thuật toán sắp xếp topo để sắp xếp tệp theo phụ thuộc
        /// </summary>
        private void TopologicalSort(
            SourceFileDependency file,
            IncludeDependencyGraph dependencyGraph,
            List<SourceFileDependency> result,
            HashSet<string> processed,
            HashSet<string> processing)
        {
            if (processed.Contains(file.FilePath))
                return;

            if (processing.Contains(file.FilePath))
            {
                // Phát hiện vòng lặp phụ thuộc
                _logger.LogWarning($"Phát hiện vòng lặp phụ thuộc liên quan đến tệp: {file.FilePath}");
                return;
            }

            processing.Add(file.FilePath);

            // Xử lý các phụ thuộc trước
            foreach (var dependency in file.DirectDependencies)
            {
                if (!processed.Contains(dependency.FilePath))
                {
                    TopologicalSort(dependency, dependencyGraph, result, processed, processing);
                }
            }

            processing.Remove(file.FilePath);

            if (!processed.Contains(file.FilePath))
            {
                result.Add(file);
                processed.Add(file.FilePath);
            }
        }

        /// <summary>
        /// Phân tích mối quan hệ giữa các thành phần
        /// </summary>
        private async Task AnalyzeComponentRelationships(ProjectAnalysisResult analysisResult)
        {
            try
            {
                // Phân tích mối quan hệ hàm
                if (analysisResult.Functions.Count > 0)
                {
                    var functionRelationships = await _functionAnalysisService.AnalyzeFunctionRelationshipsAsync(analysisResult.Functions);
                    analysisResult.FunctionRelationships.AddRange(functionRelationships);
                }

                // Phân tích ràng buộc biến
                if (analysisResult.Variables.Count > 0)
                {
                    var variableConstraints = await _variableAnalysisService.AnalyzeVariablesAsync(
                        analysisResult.Variables, analysisResult.Functions, analysisResult.Macros);

                    // Gắn ràng buộc với biến
                    foreach (var constraint in variableConstraints)
                    {
                        if (!string.IsNullOrEmpty(constraint.VariableName))
                        {
                            var variable = analysisResult.Variables.FirstOrDefault(v => v.Name == constraint.VariableName);
                            if (variable != null && !variable.Constraints.Any(c => c.Id == constraint.Id))
                            {
                                variable.Constraints.Add(constraint);
                            }
                        }
                    }
                }

                // Phân tích mối quan hệ macro
                if (analysisResult.Macros.Count > 0)
                {
                    await _macroAnalysisService.AnalyzeMacroRelationshipsAsync(analysisResult.Macros, analysisResult.ConditionalDirectives);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi phân tích mối quan hệ thành phần: {ex.Message}");
                analysisResult.Errors.Add($"Error analyzing component relationships: {ex.Message}");
            }
        }

        /// <summary>
        /// Tối ưu hóa kết quả phân tích
        /// </summary>
        private void OptimizeAnalysisResult(ProjectAnalysisResult analysisResult)
        {
            try
            {
                // Loại bỏ các hàm trùng lặp
                var uniqueFunctions = new List<CFunction>();
                var functionSignatures = new HashSet<string>();

                foreach (var function in analysisResult.Functions)
                {
                    string signature = $"{function.Name}_{function.SourceFile}_{function.StartLineNumber}";
                    if (!functionSignatures.Contains(signature))
                    {
                        functionSignatures.Add(signature);
                        uniqueFunctions.Add(function);
                    }
                }
                analysisResult.Functions = uniqueFunctions;

                // Loại bỏ các biến trùng lặp
                var uniqueVariables = new List<CVariable>();
                var variableSignatures = new HashSet<string>();

                foreach (var variable in analysisResult.Variables)
                {
                    string signature = $"{variable.Name}_{variable.SourceFile}_{variable.LineNumber}";
                    if (!variableSignatures.Contains(signature))
                    {
                        variableSignatures.Add(signature);
                        uniqueVariables.Add(variable);
                    }
                }
                analysisResult.Variables = uniqueVariables;

                // Loại bỏ các macro trùng lặp
                var uniqueMacros = new List<CDefinition>();
                var macroSignatures = new HashSet<string>();

                foreach (var macro in analysisResult.Macros)
                {
                    string signature = $"{macro.Name}_{macro.SourceFile}";
                    if (!macroSignatures.Contains(signature))
                    {
                        macroSignatures.Add(signature);
                        uniqueMacros.Add(macro);
                    }
                }
                analysisResult.Macros = uniqueMacros;

                _logger.LogDebug($"Tối ưu hóa hoàn thành: {analysisResult.Functions.Count} hàm duy nhất, " +
                    $"{analysisResult.Variables.Count} biến duy nhất, {analysisResult.Macros.Count} macro duy nhất");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tối ưu hóa kết quả: {ex.Message}");
                analysisResult.Errors.Add($"Error optimizing results: {ex.Message}");
            }
        }

        /// <summary>
        /// Trích xuất tất cả các điều kiện tiền xử lý từ các tệp trong đồ thị phụ thuộc
        /// </summary>
        /// <param name="dependencyGraph">Đồ thị phụ thuộc</param>
        /// <returns>Danh sách các điều kiện tiền xử lý duy nhất được sử dụng trong dự án</returns>
        public List<string> ExtractPreprocessorConditionsFromGraph(IncludeDependencyGraph dependencyGraph)
        {
            _logger.LogInformation("Trích xuất các điều kiện tiền xử lý từ đồ thị phụ thuộc");

            var conditions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sourceFile in dependencyGraph.SourceFiles)
            {
                // Chỉ xử lý các tệp header vì chúng thường chứa nhiều điều kiện tiền xử lý
                if (sourceFile.FileType != SourceFileType.CHeader &&
                    sourceFile.FileType != SourceFileType.CPPHeader)
                    continue;

                foreach (var block in sourceFile.ConditionalBlocks)
                {
                    ExtractConditionsRecursively(block, conditions);
                }
            }

            _logger.LogInformation($"Đã trích xuất {conditions.Count} điều kiện tiền xử lý duy nhất");
            return conditions.ToList();
        }

        /// <summary>
        /// Trích xuất tất cả các điều kiện từ một khối điều kiện và các khối con của nó
        /// </summary>
        private void ExtractConditionsRecursively(ConditionalBlock block, HashSet<string> conditions)
        {
            if (!string.IsNullOrEmpty(block.Condition))
            {
                // Loại bỏ khoảng trắng và comment
                string cleanCondition = block.Condition.Trim();

                if (block.DirectiveType == "ifdef" || block.DirectiveType == "ifndef")
                {
                    // Đơn giản hóa cho #ifdef/#ifndef - chỉ lấy tên macro
                    conditions.Add(cleanCondition);
                }
                else if (block.DirectiveType == "if" || block.DirectiveType == "elif")
                {
                    // Cho #if/#elif, phân tách biểu thức để lấy tên các macro
                    var macros = ExtractMacrosFromExpression(cleanCondition);
                    foreach (var macro in macros)
                    {
                        conditions.Add(macro);
                    }
                }
            }

            // Đệ quy vào các khối con
            foreach (var nestedBlock in block.NestedBlocks)
            {
                ExtractConditionsRecursively(nestedBlock, conditions);
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