using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Text;
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

                SourceFile sourceFile = new SourceFile(filePath);

                sourceFile.LoadFromFile(); 

                _logger.LogInformation($"Successfully loaded source file: {filePath}");

                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading source file: {filePath}");
                throw;
            }
        }

        
    }
}