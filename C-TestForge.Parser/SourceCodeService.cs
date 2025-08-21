using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Implementation of the source code service
    /// </summary>
    public class SourceCodeService : ISourceCodeService
    {
        private readonly ILogger<SourceCodeService> _logger;
        private readonly IFileService _fileService;
        private readonly IPreprocessorService _preprocessorService;

        /// <summary>
        /// Constructor for SourceCodeService
        /// </summary>
        /// <param name="logger">Logger instance</param>
        /// <param name="fileService">File service for reading files</param>
        /// <param name="preprocessorService">Preprocessor service for analyzing preprocessor directives</param>
        public SourceCodeService(
            ILogger<SourceCodeService> logger,
            IFileService fileService,
            IPreprocessorService preprocessorService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _preprocessorService = preprocessorService ?? throw new ArgumentNullException(nameof(preprocessorService));
        }

        /// <inheritdoc/>
        public async Task<SourceFile> LoadSourceFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"Loading source file: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!_fileService.FileExists(filePath))
                {
                    throw new FileNotFoundException($"Source file not found: {filePath}");
                }

                string fileContent = await _fileService.ReadFileAsync(filePath);
                string[] lines = fileContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                var sourceFile = new SourceFile
                {
                    FilePath = filePath,
                    FileName = _fileService.GetFileName(filePath),
                    Content = fileContent,
                    Lines = new List<string>(lines),
                    LastModified = File.GetLastWriteTime(filePath)
                };

                // Pre-process the source file to get some basic information
                // Chưa thực hiện tiền xử lý file mã nguồn
                //await PreProcessSourceFileAsync(sourceFile);

                _logger.LogInformation($"Successfully loaded source file: {filePath}");

                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading source file: {filePath}");
                throw;
            }
        }

        /// <summary>
        /// Pre-processes a source file to extract basic information
        /// Tiền xử lý file mã nguồn để trích xuất các thông tin cơ bản như: chỉ thị include, define, và các chỉ thị điều kiện (#if, #ifdef, ...).
        /// </summary>
        /// <param name="sourceFile">Source file to pre-process</param>
        /// <returns>Task</returns>
        private async Task PreProcessSourceFileAsync(SourceFile sourceFile)
        {
            try
            {
                if (sourceFile == null || string.IsNullOrEmpty(sourceFile.Content))
                {
                    return;
                }

                // Extract include directives
                var includeRegex = new Regex(@"^\s*#\s*include\s+[<""]([^>""]*)[\>""]", RegexOptions.Multiline);
                var includeMatches = includeRegex.Matches(sourceFile.Content);

                foreach (Match match in includeMatches)
                {
                    string includePath = match.Groups[1].Value;
                    // You might want to do something with the include paths here
                }

                // Extract define directives
                var defineRegex = new Regex(@"^\s*#\s*define\s+(\w+)(?:\(([^)]*)\))?\s*(.*)?$", RegexOptions.Multiline);
                var defineMatches = defineRegex.Matches(sourceFile.Content);

                foreach (Match match in defineMatches)
                {
                    string macroName = match.Groups[1].Value;
                    string parameters = match.Groups[2].Value;
                    string value = match.Groups[3].Value;
                    // You might want to do something with the defines here
                }

                // Extract conditional directives
                var conditionalDirectives = await _preprocessorService.ExtractConditionalDirectivesAsync(
                    sourceFile.Content, sourceFile.FileName);

                // You might want to do something with the conditional directives here

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error pre-processing source file: {sourceFile.FileName}");
                // Continue with partial information
            }
        }
    }
}