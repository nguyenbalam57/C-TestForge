using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.CodeAnalysis.Functions;
using C_TestForge.Models.Core;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using ClangSharp;
using ClangSharp.Interop;
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
    /// Implementation of the parser service using ClangSharp
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
            IConfigurationService configurationService = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _preprocessorService = preprocessorService ?? throw new ArgumentNullException(nameof(preprocessorService));
            _functionAnalysisService = functionAnalysisService ?? throw new ArgumentNullException(nameof(functionAnalysisService));
            _variableAnalysisService = variableAnalysisService ?? throw new ArgumentNullException(nameof(variableAnalysisService));
            _macroAnalysisService = macroAnalysisService ?? throw new ArgumentNullException(nameof(macroAnalysisService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _configurationService = configurationService;
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

                var parseResult = await ParseSourceFileAsync(filePath, options);

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
                    ParseResult = parseResult,
                    IsDirty = false
                };

                // Xây dựng từ điển includes
                var includeDirectives = parseResult.PreprocessorDirectives
                    .Where(d => d.Type == "include")
                    .ToList();

                foreach (var directive in includeDirectives)
                {
                    string value = directive.Value;
                    // Loại bỏ ngoặc " hoặc <> để lấy đường dẫn
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    else if (value.StartsWith("<") && value.EndsWith(">"))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (!string.IsNullOrEmpty(value))
                    {
                        sourceFile.Includes[value] = $"#include {directive.Value}";
                    }
                }

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
        public async Task<SourceFile> ParseSourceCodeAsync(string sourceCode, string fileName = "inline.c")
        {
            if (string.IsNullOrEmpty(sourceCode))
            {
                throw new ArgumentException("Source code cannot be null or empty", nameof(sourceCode));
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "inline.c";
            }

            try
            {
                _logger.LogInformation($"Parsing source code with name: {fileName}");

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

                var parseResult = await ParseSourceCodeAsync(sourceCode, fileName, options);

                // Convert ParseResult to SourceFile
                var sourceFile = new SourceFile
                {
                    FilePath = fileName,
                    FileName = fileName,
                    Content = sourceCode,
                    Lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList(),
                    ContentHash = GetContentHash(sourceCode),
                    FileType = DetermineFileType(fileName),
                    LastModified = DateTime.Now,
                    ParseResult = parseResult,
                    IsDirty = false
                };

                // Xây dựng từ điển includes
                var includeDirectives = parseResult.PreprocessorDirectives
                    .Where(d => d.Type == "include")
                    .ToList();

                foreach (var directive in includeDirectives)
                {
                    string value = directive.Value;
                    // Loại bỏ ngoặc " hoặc <> để lấy đường dẫn
                    if (value.StartsWith("\"") && value.EndsWith("\""))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }
                    else if (value.StartsWith("<") && value.EndsWith(">"))
                    {
                        value = value.Substring(1, value.Length - 2);
                    }

                    if (!string.IsNullOrEmpty(value))
                    {
                        sourceFile.Includes[value] = $"#include {directive.Value}";
                    }
                }

                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing source code: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<CFunction>> ExtractFunctionsAsync(string filePath)
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
                _logger.LogInformation($"Extracting functions from file: {filePath}");

                // Parse the source file with function analysis only
                var options = new ParseOptions
                {
                    AnalyzeFunctions = true,
                    AnalyzeVariables = false,
                    ParsePreprocessorDefinitions = false
                };

                var parseResult = await ParseSourceFileAsync(filePath, options);

                _logger.LogInformation($"Extracted {parseResult.Functions.Count} functions from {filePath}");
                return parseResult.Functions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting functions: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<List<CFunction>> ExtractFunctionsFromCodeAsync(string sourceCode, string fileName = "inline.c")
        {
            if (string.IsNullOrEmpty(sourceCode))
            {
                throw new ArgumentException("Source code cannot be null or empty", nameof(sourceCode));
            }

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

                var parseResult = await ParseSourceCodeAsync(sourceCode, fileName, options);

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
        public async Task<ParseResult> ParseSourceFileAsync(string filePath, ParseOptions options)
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
                _logger.LogInformation($"Starting parse of file: {filePath}");

                // Read the source file
                string sourceCode = await _fileService.ReadFileAsync(filePath);
                string fileName = _fileService.GetFileName(filePath);

                // Parse the source code
                return await ParseSourceCodeAsync(sourceCode, fileName, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing source file: {filePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ParseResult> ParseSourceCodeAsync(string sourceCode, string fileName, ParseOptions options)
        {
            if (string.IsNullOrEmpty(sourceCode))
            {
                throw new ArgumentException("Source code cannot be null or empty", nameof(sourceCode));
            }

            if (string.IsNullOrEmpty(fileName))
            {
                fileName = "inline.c";
            }

            try
            {
                _logger.LogInformation($"Parsing source code for {fileName}");

                var result = new ParseResult
                {
                    SourceFilePath = fileName
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
                        byte[] fileNameBytes = System.Text.Encoding.UTF8.GetBytes(fileName + "\0");
                        byte[] sourceCodeBytes = System.Text.Encoding.UTF8.GetBytes(sourceCode);

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
                                Length = (nuint)sourceCode.Length
                            };

                            // Lấy con trỏ trực tiếp thay vì sử dụng khối fixed thứ hai
                            CXUnsavedFile* unsavedFilePtr = &unsavedFile;

                            // Convert the managed array to a native array of pointers
                            sbyte** argArray = stackalloc sbyte*[args.Count];
                            for (int i = 0; i < args.Count; i++)
                            {
                                argArray[i] = (sbyte*)argPtrs[i].ToPointer();
                            }

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
                        await ExtractPreprocessorDefinitionsAsync(translationUnit, fileName, result);
                    }

                    // Extract functions and variables from the AST - sử dụng rootCursor đã lấy từ khối unsafe
                    if (options.AnalyzeFunctions || options.AnalyzeVariables)
                    {
                        await TraverseASTAsync(rootCursor, fileName, result, options);
                    }

                    _logger.LogInformation($"Successfully parsed {fileName}: {result.Functions.Count} functions, {result.Variables.Count} variables, {result.Definitions.Count} definitions");

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
        public async Task<List<string>> GetIncludedFilesAsync(string filePath)
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
                _logger.LogInformation($"Getting included files for: {filePath}");

                // Read the source file
                string sourceCode = await _fileService.ReadFileAsync(filePath);
                string fileName = _fileService.GetFileName(filePath);

                // Parse the source code with minimal options
                var options = new ParseOptions
                {
                    ParsePreprocessorDefinitions = true,
                    AnalyzeFunctions = false,
                    AnalyzeVariables = false
                };

                var result = await ParseSourceCodeAsync(sourceCode, fileName, options);

                // Extract include directives
                var includes = result.PreprocessorDirectives
                    .Where(d => d.Type == "include")
                    .Select(d => d.Value.Trim())
                    .ToList();

                _logger.LogInformation($"Found {includes.Count} included files in {filePath}");

                return includes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting included files: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<FunctionAnalysisResult> AnalyzeFunctionAsync(string functionName, string filePath)
        {
            if (string.IsNullOrEmpty(functionName))
            {
                throw new ArgumentException("Function name cannot be null or empty", nameof(functionName));
            }

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
                _logger.LogInformation($"Analyzing function {functionName} in file: {filePath}");

                // Parse the source file first
                var options = new ParseOptions
                {
                    AnalyzeFunctions = true,
                    AnalyzeVariables = true,
                    ParsePreprocessorDefinitions = true
                };

                var parseResult = await ParseSourceFileAsync(filePath, options);

                // Find the function in the parse result
                var function = parseResult.Functions.FirstOrDefault(f => f.Name == functionName);
                if (function == null)
                {
                    throw new InvalidOperationException($"Function '{functionName}' not found in file: {filePath}");
                }

                // Create a new FunctionAnalysisResult
                var result = new FunctionAnalysisResult
                {
                    FunctionName = function.Name,
                    FilePath = filePath,
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
        private async Task TraverseASTAsync(CXCursor cursor, string sourceFileName, ParseResult result, ParseOptions options)
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
                        visitor.Visit(child, parent);
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
            public void Visit(CXCursor cursor, CXCursor parent)
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
                        var variable = _variableAnalysisService.ExtractVariable(cursor);
                        if (variable != null)
                        {
                            Variables.Add(variable);
                        }
                    }

                    // Process function declarations
                    if (_options.AnalyzeFunctions && cursor.Kind == CXCursorKind.CXCursor_FunctionDecl)
                    {
                        var function = _functionAnalysisService.ExtractFunction(cursor);
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
    }
}