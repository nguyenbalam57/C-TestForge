using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Models.Core;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using Microsoft.Extensions.Logging;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Implementation of the parser interface that uses ClangSharp for C/C++ parsing
    /// </summary>
    public class Parser : IParser
    {
        private readonly IParserService _parserService;
        private readonly ILogger<Parser> _logger;

        /// <summary>
        /// Constructor for Parser
        /// </summary>
        /// <param name="parserService">Parser service to use</param>
        /// <param name="logger">Logger instance</param>
        public Parser(IParserService parserService, ILogger<Parser> logger)
        {
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<SourceFile> ParseSourceFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"Parsing source file: {filePath}");

                // Create parse options
                var parseOptions = new ParseOptions
                {
                    ParsePreprocessorDefinitions = true,
                    AnalyzeVariables = true,
                    AnalyzeFunctions = true
                };

                // Parse the source file
                var parseResult = await _parserService.ParseSourceFileAsync(filePath, parseOptions);

                // Create a source file instance
                var sourceFile = new SourceFile
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    FileType = DetermineSourceFileType(filePath),
                    LastModified = File.GetLastWriteTime(filePath),
                    IsDirty = false
                };

                // Read file content
                string content = await File.ReadAllTextAsync(filePath);
                sourceFile.Content = content;
                sourceFile.Lines = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
                sourceFile.ContentHash = ComputeHash(content);

                // Extract includes
                await ExtractIncludesAsync(sourceFile, parseResult);

                // Set parse result
                sourceFile.ParseResult = ConvertToInternalParseResult(parseResult);

                _logger.LogInformation($"Successfully parsed file: {filePath}");

                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing source file {filePath}: {ex.Message}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<SourceFile> ParseSourceCodeAsync(string sourceCode, string fileName = "inline.c")
        {
            try
            {
                _logger.LogInformation($"Parsing source code with name: {fileName}");

                // Create parse options
                var parseOptions = new ParseOptions
                {
                    ParsePreprocessorDefinitions = true,
                    AnalyzeVariables = true,
                    AnalyzeFunctions = true
                };

                // Parse the source code
                var parseResult = await _parserService.ParseSourceCodeAsync(sourceCode, fileName, parseOptions);

                // Create a source file instance
                var sourceFile = new SourceFile
                {
                    FilePath = fileName, // Virtual path
                    FileName = fileName,
                    Content = sourceCode,
                    Lines = sourceCode.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList(),
                    FileType = DetermineSourceFileType(fileName),
                    LastModified = DateTime.Now,
                    ContentHash = ComputeHash(sourceCode),
                    IsDirty = false
                };

                // Extract includes
                await ExtractIncludesAsync(sourceFile, parseResult);

                // Set parse result
                sourceFile.ParseResult = ConvertToInternalParseResult(parseResult);

                _logger.LogInformation($"Successfully parsed source code with name: {fileName}");

                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing source code: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Determines the type of a source file based on its extension
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>Source file type</returns>
        private SourceFileType DetermineSourceFileType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            switch (extension)
            {
                case ".c":
                    return SourceFileType.C;
                case ".h":
                    return SourceFileType.Header;
                case ".cpp":
                case ".cc":
                case ".cxx":
                    return SourceFileType.CPP;
                case ".hpp":
                    return SourceFileType.CPPHeader;
                default:
                    return SourceFileType.Unknown;
            }
        }

        /// <summary>
        /// Computes a hash for a string
        /// </summary>
        /// <param name="content">Content to hash</param>
        /// <returns>Hash string</returns>
        private string ComputeHash(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(content);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

        /// <summary>
        /// Extracts includes from the parse result and updates the source file
        /// </summary>
        /// <param name="sourceFile">Source file to update</param>
        /// <param name="parseResult">Parse result to extract includes from</param>
        /// <returns>Task</returns>
        private async Task ExtractIncludesAsync(SourceFile sourceFile, Models.Parse.ParseResult parseResult)
        {
            try
            {
                // Clear current includes
                sourceFile.Includes.Clear();

                // Get includes from the parser service
                var includedFiles = await _parserService.GetIncludedFilesAsync(sourceFile.FilePath);

                foreach (var includePath in includedFiles)
                {
                    string includeContent = string.Empty;

                    // Try to determine if this is a system include
                    bool isSystemInclude = includePath.StartsWith('<') && includePath.EndsWith('>');
                    string cleanPath = includePath.Trim('<', '>', '"');

                    // Try to read include content if it's a local include and the file exists
                    if (!isSystemInclude)
                    {
                        string baseDir = Path.GetDirectoryName(sourceFile.FilePath);
                        string fullPath = Path.Combine(baseDir, cleanPath);

                        if (File.Exists(fullPath))
                        {
                            try
                            {
                                includeContent = await File.ReadAllTextAsync(fullPath);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"Could not read include file: {fullPath}");
                            }
                        }
                    }

                    // Add the include to the dictionary
                    sourceFile.Includes[cleanPath] = includeContent;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting includes from parse result: {ex.Message}");
            }
        }

        /// <summary>
        /// Converts an external parse result to our internal parse result model
        /// </summary>
        /// <param name="parseResult">External parse result</param>
        /// <returns>Internal parse result</returns>
        private ParseResult ConvertToInternalParseResult(ParseResult parseResult)
        {
            var result = new ParseResult
            {
                SourceFilePath = parseResult.SourceFilePath
            };

            // Copy definitions and create preprocessor directives for them
            foreach (var definition in parseResult.Definitions)
            {
                var directive = new CPreprocessorDirective
                {
                    Type = "define",
                    Value = definition.ToString(),
                    LineNumber = definition.LineNumber,
                    SourceFile = definition.SourceFile
                };
                result.PreprocessorDirectives.Add(directive);
                result.Definitions.Add(definition);
            }

            // Copy variables and functions
            result.Variables.AddRange(parseResult.Variables);
            result.Functions.AddRange(parseResult.Functions);

            // Process conditional directives
            foreach (var conditional in parseResult.ConditionalDirectives)
            {
                // Add to conditional directives collection
                result.ConditionalDirectives.Add(conditional);

                // Create a preprocessor directive representation
                var directive = new CPreprocessorDirective
                {
                    Type = conditional.Type.ToString().ToLowerInvariant(),
                    Value = conditional.ToString(),
                    LineNumber = conditional.LineNumber,
                    SourceFile = conditional.SourceFile
                };
                result.PreprocessorDirectives.Add(directive);
            }

            // Separately process include directives from the parser service (if available)
            // In a real implementation, you would need to make sure this matches the includes
            // extracted in the ExtractIncludesAsync method.

            // Copy parse errors
            result.ParseErrors.AddRange(parseResult.ParseErrors);

            return result;
        }
    }
}