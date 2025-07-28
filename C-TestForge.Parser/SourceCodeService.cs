using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.RegularExpressions;

namespace C_TestForge.Parser
{
    #region SourceCodeService Implementation

    /// <summary>
    /// Implementation of the source code service
    /// </summary>
    public class SourceCodeService : ISourceCodeService
    {
        private readonly ILogger<SourceCodeService> _logger;
        private readonly IFileService _fileService;
        private readonly IPreprocessorService _preprocessorService;

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
                    Lines = lines.ToList(),
                    LastModified = File.GetLastWriteTime(filePath)
                };

                // Pre-process the source file to get some basic information
                await PreProcessSourceFileAsync(sourceFile);

                _logger.LogInformation($"Successfully loaded source file: {filePath}");

                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error loading source file: {filePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> SaveSourceFileAsync(SourceFile sourceFile)
        {
            try
            {
                _logger.LogInformation($"Saving source file: {sourceFile.FilePath}");

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                if (string.IsNullOrEmpty(sourceFile.FilePath))
                {
                    throw new ArgumentException("File path cannot be null or empty");
                }

                // Create the content from the lines
                string content;
                if (sourceFile.Lines != null && sourceFile.Lines.Any())
                {
                    content = string.Join(Environment.NewLine, sourceFile.Lines);
                }
                else if (!string.IsNullOrEmpty(sourceFile.Content))
                {
                    content = sourceFile.Content;
                }
                else
                {
                    throw new InvalidOperationException("Source file has no content to save");
                }

                await _fileService.WriteFileAsync(sourceFile.FilePath, content);

                // Update last modified time
                sourceFile.LastModified = File.GetLastWriteTime(sourceFile.FilePath);
                sourceFile.IsDirty = false;

                // Update content hash
                sourceFile.ContentHash = CalculateMD5(content);

                _logger.LogInformation($"Successfully saved source file: {sourceFile.FilePath}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving source file: {sourceFile.FilePath}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<SourceFile> CreateBackupAsync(SourceFile sourceFile)
        {
            try
            {
                _logger.LogInformation($"Creating backup of source file: {sourceFile.FilePath}");

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                if (string.IsNullOrEmpty(sourceFile.FilePath))
                {
                    throw new ArgumentException("File path cannot be null or empty");
                }

                string backupPath = Path.Combine(
                    _fileService.GetDirectoryName(sourceFile.FilePath),
                    $"{_fileService.GetFileNameWithoutExtension(sourceFile.FilePath)}_backup_{DateTime.Now:yyyyMMddHHmmss}{_fileService.GetFileExtension(sourceFile.FilePath)}");

                await _fileService.CopyFileAsync(sourceFile.FilePath, backupPath);

                // Create a new SourceFile for the backup
                var backupFile = new SourceFile
                {
                    FilePath = backupPath,
                    FileName = _fileService.GetFileName(backupPath),
                    Content = sourceFile.Content,
                    Lines = sourceFile.Lines != null ? new List<string>(sourceFile.Lines) : null,
                    LastModified = File.GetLastWriteTime(backupPath),
                    FileType = sourceFile.FileType,
                    ContentHash = sourceFile.ContentHash
                };

                _logger.LogInformation($"Successfully created backup: {backupPath}");

                return backupFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating backup of source file: {sourceFile.FilePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, string>> ExtractIncludesAsync(SourceFile sourceFile)
        {
            try
            {
                _logger.LogInformation($"Extracting includes from source file: {sourceFile.FilePath}");

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                var includes = new Dictionary<string, string>();

                // Use the preprocessor service to extract includes
                var includeDirectives = await _preprocessorService.ExtractIncludeDirectivesAsync(sourceFile.Content, sourceFile.FileName);

                foreach (var include in includeDirectives)
                {
                    includes[include.FilePath] = include.IsSystemInclude ? "system" : "local";
                }

                _logger.LogInformation($"Found {includes.Count} includes in source file: {sourceFile.FilePath}");

                return includes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting includes from source file: {sourceFile.FilePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public string GetLineOfCode(SourceFile sourceFile, int lineNumber)
        {
            if (sourceFile == null)
            {
                throw new ArgumentNullException(nameof(sourceFile));
            }

            if (lineNumber < 1 || lineNumber > sourceFile.Lines.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(lineNumber), $"Line number must be between 1 and {sourceFile.Lines.Count}");
            }

            return sourceFile.Lines[lineNumber - 1];
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, HashSet<int>>> FindVariableUsagesAsync(SourceFile sourceFile, CVariable variable)
        {
            try
            {
                _logger.LogInformation($"Finding usages of variable {variable.Name} in source file: {sourceFile.FilePath}");

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                if (variable == null)
                {
                    throw new ArgumentNullException(nameof(variable));
                }

                var usages = new Dictionary<string, HashSet<int>>();

                // Add the declaration line
                if (!string.IsNullOrEmpty(variable.SourceFile) && variable.LineNumber > 0)
                {
                    string sourceFileName = variable.SourceFile;
                    if (!usages.ContainsKey(sourceFileName))
                    {
                        usages[sourceFileName] = new HashSet<int>();
                    }

                    usages[sourceFileName].Add(variable.LineNumber);
                }

                // Find usages in the current file
                string variableName = variable.Name;

                for (int i = 0; i < sourceFile.Lines.Count; i++)
                {
                    string line = sourceFile.Lines[i];

                    // Skip comments
                    if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("/*"))
                    {
                        continue;
                    }

                    // Check if the line contains the variable name
                    if (ContainsVariableName(line, variableName))
                    {
                        if (!usages.ContainsKey(sourceFile.FileName))
                        {
                            usages[sourceFile.FileName] = new HashSet<int>();
                        }

                        usages[sourceFile.FileName].Add(i + 1);
                    }
                }

                _logger.LogInformation($"Found {usages.Values.Sum(v => v.Count)} usages of variable {variable.Name}");

                return usages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding usages of variable {variable.Name} in source file: {sourceFile.FilePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, HashSet<int>>> FindFunctionUsagesAsync(SourceFile sourceFile, CFunction function)
        {
            try
            {
                _logger.LogInformation($"Finding usages of function {function.Name} in source file: {sourceFile.FilePath}");

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                if (function == null)
                {
                    throw new ArgumentNullException(nameof(function));
                }

                var usages = new Dictionary<string, HashSet<int>>();

                // Add the declaration line
                if (!string.IsNullOrEmpty(function.SourceFile) && function.LineNumber > 0)
                {
                    string sourceFileName = function.SourceFile;
                    if (!usages.ContainsKey(sourceFileName))
                    {
                        usages[sourceFileName] = new HashSet<int>();
                    }

                    usages[sourceFileName].Add(function.LineNumber);
                }

                // Find usages in the current file
                string functionName = function.Name;

                for (int i = 0; i < sourceFile.Lines.Count; i++)
                {
                    string line = sourceFile.Lines[i];

                    // Skip comments
                    if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("/*"))
                    {
                        continue;
                    }

                    // Check if the line contains the function name followed by a parenthesis
                    if (ContainsFunctionCall(line, functionName))
                    {
                        if (!usages.ContainsKey(sourceFile.FileName))
                        {
                            usages[sourceFile.FileName] = new HashSet<int>();
                        }

                        usages[sourceFile.FileName].Add(i + 1);
                    }
                }

                _logger.LogInformation($"Found {usages.Values.Sum(v => v.Count)} usages of function {function.Name}");

                return usages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding usages of function {function.Name} in source file: {sourceFile.FilePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<SourceFile> UpdateSourceFileContentAsync(SourceFile sourceFile, string newContent)
        {
            try
            {
                _logger.LogInformation($"Updating content of source file: {sourceFile.FilePath}");

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                if (string.IsNullOrEmpty(newContent))
                {
                    throw new ArgumentException("New content cannot be null or empty", nameof(newContent));
                }

                // Update content and lines
                sourceFile.Content = newContent;
                sourceFile.Lines = newContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
                sourceFile.IsDirty = true;

                // Update content hash
                sourceFile.ContentHash = CalculateMD5(newContent);

                // Re-process the source file to update information
                await PreProcessSourceFileAsync(sourceFile);

                _logger.LogInformation($"Successfully updated content of source file: {sourceFile.FilePath}");

                return sourceFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating content of source file: {sourceFile.FilePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<Dictionary<string, List<SymbolOccurrence>>> FindSymbolOccurrencesAsync(SourceFile sourceFile, string symbolName)
        {
            try
            {
                _logger.LogInformation($"Finding occurrences of symbol {symbolName} in source file: {sourceFile.FilePath}");

                if (sourceFile == null)
                {
                    throw new ArgumentNullException(nameof(sourceFile));
                }

                if (string.IsNullOrEmpty(symbolName))
                {
                    throw new ArgumentException("Symbol name cannot be null or empty", nameof(symbolName));
                }

                var occurrences = new Dictionary<string, List<SymbolOccurrence>>();

                // Initialize the result dictionary
                occurrences[sourceFile.FileName] = new List<SymbolOccurrence>();

                // Regular expressions for matching different kinds of occurrences
                var definitionRegex = new Regex($@"(^|\s)((typedef|struct|enum|union)\s+)+{Regex.Escape(symbolName)}\s*[{{;]");
                var declarationRegex = new Regex($@"^[^=/]*?\b\w+\s+{Regex.Escape(symbolName)}\s*(\(|\[|;)");

                // Scan the file for occurrences
                for (int i = 0; i < sourceFile.Lines.Count; i++)
                {
                    string line = sourceFile.Lines[i];

                    // Skip comments
                    if (line.TrimStart().StartsWith("//") || line.TrimStart().StartsWith("/*"))
                    {
                        continue;
                    }

                    // Check for different types of occurrences
                    if (definitionRegex.IsMatch(line))
                    {
                        // This is likely a definition
                        occurrences[sourceFile.FileName].Add(new SymbolOccurrence
                        {
                            Type = SymbolOccurrenceType.Definition,
                            LineNumber = i + 1,
                            ColumnNumber = line.IndexOf(symbolName) + 1,
                            Context = line.Trim()
                        });
                    }
                    else if (declarationRegex.IsMatch(line))
                    {
                        // This is likely a declaration
                        occurrences[sourceFile.FileName].Add(new SymbolOccurrence
                        {
                            Type = SymbolOccurrenceType.Declaration,
                            LineNumber = i + 1,
                            ColumnNumber = line.IndexOf(symbolName) + 1,
                            Context = line.Trim()
                        });
                    }
                    else if (ContainsSymbolUsage(line, symbolName))
                    {
                        // This is likely a usage
                        occurrences[sourceFile.FileName].Add(new SymbolOccurrence
                        {
                            Type = SymbolOccurrenceType.Usage,
                            LineNumber = i + 1,
                            ColumnNumber = line.IndexOf(symbolName) + 1,
                            Context = line.Trim()
                        });
                    }
                }

                _logger.LogInformation($"Found {occurrences[sourceFile.FileName].Count} occurrences of symbol {symbolName}");

                return occurrences;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding occurrences of symbol {symbolName} in source file: {sourceFile.FilePath}");
                throw;
            }
        }

        private async Task PreProcessSourceFileAsync(SourceFile sourceFile)
        {
            // Extract file type (header or implementation)
            string extension = _fileService.GetFileExtension(sourceFile.FilePath).ToLowerInvariant();
            if (extension == ".h" || extension == ".hpp")
            {
                sourceFile.FileType = SourceFileType.Header;
            }
            else if (extension == ".c" || extension == ".cpp")
            {
                sourceFile.FileType = SourceFileType.Implementation;
            }
            else
            {
                sourceFile.FileType = SourceFileType.Unknown;
            }

            // Extract includes
            sourceFile.Includes = await ExtractIncludesAsync(sourceFile);

            // Calculate an MD5 hash of the content for change detection
            sourceFile.ContentHash = CalculateMD5(sourceFile.Content);
        }

        private string CalculateMD5(string input)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to a hex string
                var sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        private bool ContainsVariableName(string line, string variableName)
        {
            // Simple check if the line contains the variable name as a whole word
            var regex = new Regex($@"\b{Regex.Escape(variableName)}\b");
            return regex.IsMatch(line);
        }

        private bool ContainsFunctionCall(string line, string functionName)
        {
            // Check if the line contains the function name followed by a parenthesis
            var regex = new Regex($@"\b{Regex.Escape(functionName)}\s*\(");
            return regex.IsMatch(line);
        }

        private bool ContainsSymbolUsage(string line, string symbolName)
        {
            // Check if the line contains the symbol name as a whole word
            var regex = new Regex($@"\b{Regex.Escape(symbolName)}\b");
            return regex.IsMatch(line);
        }
    }

    #endregion
}
