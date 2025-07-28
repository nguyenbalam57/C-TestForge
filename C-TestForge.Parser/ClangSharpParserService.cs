using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models;
using ClangSharp.Interop;
using Microsoft.Extensions.Logging;

namespace C_TestForge.Parser
{
    #region ClangSharpParserService Implementation

    /// <summary>
    /// Implementation of the parser service using ClangSharp
    /// </summary>
    public class ClangSharpParserService : IParserService
    {
        private readonly ILogger<ClangSharpParserService> _logger;
        private readonly IPreprocessorService _preprocessorService;
        private readonly IFunctionAnalysisService _functionAnalysisService;
        private readonly IVariableAnalysisService _variableAnalysisService;
        private readonly IMacroAnalysisService _macroAnalysisService;
        private readonly IFileService _fileService;

        public ClangSharpParserService(
            ILogger<ClangSharpParserService> logger,
            IPreprocessorService preprocessorService,
            IFunctionAnalysisService functionAnalysisService,
            IVariableAnalysisService variableAnalysisService,
            IMacroAnalysisService macroAnalysisService,
            IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _preprocessorService = preprocessorService ?? throw new ArgumentNullException(nameof(preprocessorService));
            _functionAnalysisService = functionAnalysisService ?? throw new ArgumentNullException(nameof(functionAnalysisService));
            _variableAnalysisService = variableAnalysisService ?? throw new ArgumentNullException(nameof(variableAnalysisService));
            _macroAnalysisService = macroAnalysisService ?? throw new ArgumentNullException(nameof(macroAnalysisService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

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
                throw new ArgumentException("File name cannot be null or empty", nameof(fileName));
            }

            try
            {
                _logger.LogInformation($"Starting parse of source code for: {fileName}");

                var result = new ParseResult
                {
                    SourceFilePath = fileName,
                    Definitions = new List<CDefinition>(),
                    Variables = new List<CVariable>(),
                    Functions = new List<CFunction>(),
                    ConditionalDirectives = new List<ConditionalDirective>(),
                    ParseErrors = new List<ParseError>()
                };

                // Create a temporary file to store the source code
                string tempPath = Path.GetTempFileName();
                try
                {
                    await _fileService.WriteFileAsync(tempPath, sourceCode);

                    // Create Clang index
                    using var index = CXIndex.Create();

                    // Prepare command-line arguments for clang
                    var clangArgs = PrepareClangArguments(options);

                    // Parse the translation unit
                    var translationFlags = CXTranslationUnit_Flags.CXTranslationUnit_DetailedPreprocessingRecord |
                                          CXTranslationUnit_Flags.CXTranslationUnit_KeepGoing;

                    // Sửa lỗi: Thay đổi cách tạo translation unit
                    CXTranslationUnit translationUnit = CXTranslationUnit.Parse(
                        index,
                        tempPath,
                        clangArgs,
                        Array.Empty<CXUnsavedFile>(),
                        translationFlags);

                    // Kiểm tra lỗi
                    if (translationUnit.Handle == IntPtr.Zero)
                    {
                        _logger.LogError($"Failed to parse source code for: {fileName}");
                        result.ParseErrors.Add(new ParseError
                        {
                            Message = "Failed to parse source code. Could not create translation unit.",
                            Severity = ErrorSeverity.Critical,
                            FileName = fileName
                        });
                        return result;
                    }

                    using (translationUnit)
                    {
                        // Process diagnostics and collect any errors
                        CollectDiagnostics(translationUnit, result);

                        if (result.HasCriticalErrors)
                        {
                            _logger.LogError($"Critical errors found during parsing: {fileName}");
                            return result;
                        }

                        var cursor = translationUnit.Cursor;

                        // Process preprocessor definitions
                        if (options.ParsePreprocessorDefinitions)
                        {
                            await ProcessPreprocessorDefinitions(translationUnit, fileName, result, options);
                        }

                        // Traverse the AST to extract variables and functions
                        await TraverseASTAsync(cursor, fileName, result, options);
                    }

                    _logger.LogInformation($"Completed parsing source code for: {fileName}");
                    _logger.LogInformation($"Found {result.Definitions.Count} definitions, {result.Variables.Count} variables, {result.Functions.Count} functions");

                    return result;
                }
                finally
                {
                    // Clean up the temporary file
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing source code for: {fileName}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ParseResult> ParseMultipleSourceFilesAsync(IEnumerable<string> filePaths, ParseOptions options)
        {
            if (filePaths == null)
            {
                throw new ArgumentNullException(nameof(filePaths));
            }

            try
            {
                _logger.LogInformation($"Starting parse of multiple source files");

                var result = new ParseResult
                {
                    SourceFilePath = "Multiple Files",
                    Definitions = new List<CDefinition>(),
                    Variables = new List<CVariable>(),
                    Functions = new List<CFunction>(),
                    ConditionalDirectives = new List<ConditionalDirective>(),
                    ParseErrors = new List<ParseError>()
                };

                // Parse each file and merge the results
                foreach (var filePath in filePaths)
                {
                    try
                    {
                        var fileResult = await ParseSourceFileAsync(filePath, options);
                        result.Merge(fileResult);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error parsing file: {filePath}");
                        result.ParseErrors.Add(new ParseError
                        {
                            Message = $"Error parsing file: {ex.Message}",
                            Severity = ErrorSeverity.Error,
                            FileName = filePath
                        });
                    }
                }

                _logger.LogInformation($"Completed parsing multiple source files");
                _logger.LogInformation($"Found {result.Definitions.Count} definitions, {result.Variables.Count} variables, {result.Functions.Count} functions");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing multiple source files");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ParseResult> ParseHeaderFileAsync(string headerPath, ParseOptions options)
        {
            if (string.IsNullOrEmpty(headerPath))
            {
                throw new ArgumentException("Header path cannot be null or empty", nameof(headerPath));
            }

            if (!_fileService.FileExists(headerPath))
            {
                throw new FileNotFoundException($"Header file not found: {headerPath}");
            }

            try
            {
                _logger.LogInformation($"Starting parse of header file: {headerPath}");

                // Create a temporary C file that includes the header
                string tempPath = Path.GetTempFileName() + ".c";
                try
                {
                    string headerName = _fileService.GetFileName(headerPath);
                    string headerDir = _fileService.GetDirectoryName(headerPath);

                    // Create a simple C file that includes the header
                    string sourceCode = $"#include \"{headerName}\"\n";
                    await _fileService.WriteFileAsync(tempPath, sourceCode);

                    // Add the header directory to include paths
                    var headerOptions = options.Clone();
                    if (!headerOptions.IncludePaths.Contains(headerDir))
                    {
                        headerOptions.IncludePaths.Add(headerDir);
                    }

                    // Parse the temporary file
                    var result = await ParseSourceFileAsync(tempPath, headerOptions);
                    result.SourceFilePath = headerPath;

                    return result;
                }
                finally
                {
                    // Clean up the temporary file
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing header file: {headerPath}");
                throw;
            }
        }

        private string[] PrepareClangArguments(ParseOptions options)
        {
            var args = new List<string>();

            // Add include paths
            foreach (var includePath in options.IncludePaths)
            {
                args.Add($"-I{includePath}");
            }

            // Add macro definitions
            foreach (var define in options.MacroDefinitions)
            {
                args.Add($"-D{define.Key}={define.Value}");
            }

            // Add standard C options
            args.Add("-std=c99"); // Default to C99, can be made configurable

            // Add additional clang arguments
            args.AddRange(options.AdditionalClangArguments);

            return args.ToArray();
        }

        private void CollectDiagnostics(CXTranslationUnit translationUnit, ParseResult result)
        {
            uint diagnosticCount = translationUnit.NumDiagnostics;

            for (uint i = 0; i < diagnosticCount; i++)
            {
                using var diagnostic = translationUnit.GetDiagnostic(i);
                var severity = diagnostic.Severity;
                var message = diagnostic.Spelling.ToString();

                diagnostic.Location.GetFileLocation(out CXFile file, out uint line, out uint column, out _);

                var parseError = new ParseError
                {
                    Message = message,
                    LineNumber = (int)line,
                    ColumnNumber = (int)column,
                    FileName = file != null ? file.Name.ToString() : string.Empty,
                    Severity = ConvertSeverity(severity)
                };

                result.ParseErrors.Add(parseError);

                if (severity >= CXDiagnosticSeverity.CXDiagnostic_Error)
                {
                    _logger.LogError($"Error in {parseError.FileName} at {parseError.LineNumber}:{parseError.ColumnNumber}: {message}");
                }
                else if (severity == CXDiagnosticSeverity.CXDiagnostic_Warning)
                {
                    _logger.LogWarning($"Warning in {parseError.FileName} at {parseError.LineNumber}:{parseError.ColumnNumber}: {message}");
                }
            }
        }

        private ErrorSeverity ConvertSeverity(CXDiagnosticSeverity severity)
        {
            switch (severity)
            {
                case CXDiagnosticSeverity.CXDiagnostic_Fatal:
                    return ErrorSeverity.Critical;
                case CXDiagnosticSeverity.CXDiagnostic_Error:
                    return ErrorSeverity.Error;
                case CXDiagnosticSeverity.CXDiagnostic_Warning:
                    return ErrorSeverity.Warning;
                case CXDiagnosticSeverity.CXDiagnostic_Note:
                case CXDiagnosticSeverity.CXDiagnostic_Ignored:
                default:
                    return ErrorSeverity.Info;
            }
        }

        private async Task ProcessPreprocessorDefinitions(CXTranslationUnit translationUnit, string sourceFileName, ParseResult result, ParseOptions options)
        {
            var preprocessorResult = await _preprocessorService.ExtractPreprocessorDefinitionsAsync(translationUnit, sourceFileName);

            foreach (var definition in preprocessorResult.Definitions)
            {
                // Check if definition is enabled according to the configuration
                if (_preprocessorService.IsDefinitionEnabled(definition, options.MacroDefinitions))
                {
                    result.Definitions.Add(definition);
                }
            }

            // Add conditional directives
            result.ConditionalDirectives = preprocessorResult.ConditionalDirectives;

            // Analyze macros for dependencies and usages
            await _macroAnalysisService.AnalyzeMacroRelationshipsAsync(result.Definitions, result.ConditionalDirectives);
        }

        private async Task TraverseASTAsync(CXCursor cursor, string sourceFileName, ParseResult result, ParseOptions options)
        {
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
                var constraints = await _variableAnalysisService.AnalyzeVariablesAsync(result.Variables, result.Functions, result.Definitions);

                // Associate constraints with variables
                foreach (var constraint in constraints)
                {
                    if (constraint.Source != null && constraint.Source.StartsWith("Variable:"))
                    {
                        string variableName = constraint.Source.Substring("Variable:".Length);
                        var variable = result.Variables.FirstOrDefault(v => v.Name == variableName);
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

                // Build function relationships based on called functions
                foreach (var function in result.Functions)
                {
                    foreach (var calledFunctionName in function.CalledFunctions)
                    {
                        var calledFunction = result.Functions.FirstOrDefault(f => f.Name == calledFunctionName);
                        if (calledFunction != null)
                        {
                            var relationship = new FunctionRelationship
                            {
                                CallerName = function.Name,
                                CalleeName = calledFunctionName,
                                SourceFile = function.SourceFile,
                                LineNumber = function.LineNumber
                            };

                            if (!relationships.Any(r => r.CallerName == relationship.CallerName && r.CalleeName == relationship.CalleeName))
                            {
                                relationships.Add(relationship);
                            }
                        }
                    }
                }
            }

            await Task.CompletedTask;
        }

        // Helper class to visit the AST and collect variables and functions
        private class ASTVisitor
        {
            private readonly string _sourceFileName;
            private readonly IVariableAnalysisService _variableAnalysisService;
            private readonly IFunctionAnalysisService _functionAnalysisService;
            private readonly ParseOptions _options;

            public List<CVariable> Variables { get; } = new List<CVariable>();
            public List<CFunction> Functions { get; } = new List<CFunction>();

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

            public void Visit(CXCursor cursor, CXCursor parent)
            {
                // Skip if not from the main file
                if (!IsFromMainFile(cursor))
                {
                    return;
                }

                switch (cursor.Kind)
                {
                    case CXCursorKind.CXCursor_VarDecl:
                        if (_options.AnalyzeVariables)
                        {
                            ProcessVariableDeclaration(cursor);
                        }
                        break;

                    case CXCursorKind.CXCursor_FunctionDecl:
                        if (_options.AnalyzeFunctions)
                        {
                            ProcessFunctionDeclaration(cursor);
                        }
                        break;

                    // Handle other relevant cursor kinds as needed
                    case CXCursorKind.CXCursor_EnumDecl:
                        if (_options.AnalyzeVariables)
                        {
                            ProcessEnumDeclaration(cursor);
                        }
                        break;

                    case CXCursorKind.CXCursor_StructDecl:
                        if (_options.AnalyzeVariables)
                        {
                            ProcessStructDeclaration(cursor);
                        }
                        break;

                    case CXCursorKind.CXCursor_TypedefDecl:
                        if (_options.AnalyzeVariables)
                        {
                            ProcessTypedefDeclaration(cursor);
                        }
                        break;
                }
            }

            private bool IsFromMainFile(CXCursor cursor)
            {
                var location = cursor.Location;
                if (location.IsFromMainFile)
                {
                    return true;
                }

                // Check if the file matches our source file name
                // Đồng thời thay thế dấu * bằng out _ (out discard)
                location.GetFileLocation(out CXFile file, out _, out _, out _);
                if (file != null)
                {
                    var fileName = Path.GetFileName(file.Name.ToString());
                    return string.Equals(fileName, _sourceFileName, StringComparison.OrdinalIgnoreCase);
                }

                return false;
            }

            private void ProcessVariableDeclaration(CXCursor cursor)
            {
                var variable = _variableAnalysisService.ExtractVariable(cursor);
                if (variable != null)
                {
                    Variables.Add(variable);
                }
            }

            private void ProcessFunctionDeclaration(CXCursor cursor)
            {
                var function = _functionAnalysisService.ExtractFunction(cursor);
                if (function != null)
                {
                    Functions.Add(function);
                }
            }

            private unsafe void ProcessEnumDeclaration(CXCursor cursor)
            {
                // Process enum declaration and its constants
                cursor.VisitChildren((child, parent, clientData) =>
                {
                    if (child.Kind == CXCursorKind.CXCursor_EnumConstantDecl)
                    {
                        string name = child.Spelling.ToString();
                        var value = child.EnumConstantDeclValue.ToString();

                        child.Location.GetFileLocation(out var file, out uint line, out uint column, out _);
                        string sourceFile = file != null ? Path.GetFileName(file.Name.ToString()) : _sourceFileName;

                        // Create a variable for the enum constant
                        var enumConstant = new CVariable
                        {
                            Name = name,
                            TypeName = "enum " + parent.Spelling.ToString(),
                            VariableType = VariableType.Enum,
                            Scope = VariableScope.Global,
                            DefaultValue = value,
                            LineNumber = (int)line,
                            ColumnNumber = (int)column,
                            SourceFile = sourceFile,
                            IsConst = true,
                            IsReadOnly = true
                        };

                        Variables.Add(enumConstant);
                    }

                    return CXChildVisitResult.CXChildVisit_Continue;
                }, default(CXClientData));
            }

            private void ProcessStructDeclaration(CXCursor cursor)
            {
                // Process struct members as needed
                // This is a placeholder - in a real implementation, you would extract struct members
            }

            private void ProcessTypedefDeclaration(CXCursor cursor)
            {
                // Process typedef declarations as needed
                // This is a placeholder - in a real implementation, you would extract typedef information
            }
        }
    }

    #endregion
}
