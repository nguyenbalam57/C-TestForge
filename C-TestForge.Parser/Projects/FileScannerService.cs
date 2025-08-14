using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.Projects;
using C_TestForge.Models.Core;
using C_TestForge.Models.Projects;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// S? d?ng alias ?? tr?nh xung ??t namespace
using InterfaceIncludeStatement = C_TestForge.Core.Interfaces.Projects.IncludeStatement;
using InterfaceConditionalBlock = C_TestForge.Core.Interfaces.Projects.ConditionalBlock;
using InterfaceIncludeDependencyGraph = C_TestForge.Core.Interfaces.Projects.IncludeDependencyGraph;
using InterfaceSourceFileDependency = C_TestForge.Core.Interfaces.Projects.SourceFileDependency;
using ModelIncludeStatement = C_TestForge.Models.Projects.IncludeStatement;
using ModelConditionalBlock = C_TestForge.Models.Projects.ConditionalBlock;
using ModelIncludeDependencyGraph = C_TestForge.Models.Projects.IncludeDependencyGraph;
using ModelSourceFileDependency = C_TestForge.Models.Projects.SourceFileDependency;

namespace C_TestForge.Parser.Projects
{
    /// <summary>
    /// D?ch v? qu?t v? ph?n t?ch c?c t?p m? ngu?n C/C++
    /// </summary>
    public class FileScannerService : IFileScannerService
    {
        private readonly ILogger<FileScannerService> _logger;
        private readonly IFileService _fileService;

        /// <summary>
        /// Kh?i t?o ??i t??ng FileScannerService
        /// </summary>
        /// <param name="logger">Logger</param>
        /// <param name="fileService">D?ch v? t?p tin</param>
        public FileScannerService(ILogger<FileScannerService> logger, IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        /// <inheritdoc/>
        public async Task<List<string>> ScanDirectoryForCFilesAsync(string directoryPath, bool recursive = true)
        {
            _logger.LogInformation($"Qu?t th? m?c {directoryPath} ?? t?m c?c t?p C/H (recursive: {recursive})");

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogWarning($"Th? m?c kh?ng t?n t?i: {directoryPath}");
                    return new List<string>();
                }

                // T?m t?p C (.c)
                var cFiles = _fileService.GetFilesInDirectory(directoryPath, ".c", recursive);
                
                // T?m t?p header (.h)
                var hFiles = _fileService.GetFilesInDirectory(directoryPath, ".h", recursive);

                // T?m t?p C++ (.cpp)
                var cppFiles = _fileService.GetFilesInDirectory(directoryPath, ".cpp", recursive);
                
                // T?m t?p header C++ (.hpp)
                var hppFiles = _fileService.GetFilesInDirectory(directoryPath, ".hpp", recursive);

                // G?p k?t qu?
                var result = new List<string>();
                result.AddRange(cFiles);
                result.AddRange(hFiles);
                result.AddRange(cppFiles);
                result.AddRange(hppFiles);

                _logger.LogInformation($"?? t?m th?y {result.Count} t?p C/C++ trong th? m?c {directoryPath}");
                _logger.LogDebug($"Chi ti?t: {cFiles.Count} .c, {hFiles.Count} .h, {cppFiles.Count} .cpp, {hppFiles.Count} .hpp");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"L?i khi qu?t th? m?c {directoryPath}: {ex.Message}");
                return new List<string>();
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> FindPotentialIncludeDirectoriesAsync(string rootDirectoryPath)
        {
            _logger.LogInformation($"T?m ki?m c?c th? m?c include ti?m n?ng trong {rootDirectoryPath}");
            
            var includeDirs = new List<string>();
            
            try
            {
                if (!Directory.Exists(rootDirectoryPath))
                {
                    _logger.LogWarning($"Th? m?c kh?ng t?n t?i: {rootDirectoryPath}");
                    return includeDirs;
                }

                // T?m t?t c? c?c th? m?c trong d? ?n
                var allDirs = Directory.GetDirectories(rootDirectoryPath, "*", SearchOption.AllDirectories);
                
                foreach (var dir in allDirs)
                {
                    // B? qua th? m?c .git, .vs, bin, obj, ...
                    if (ShouldIgnoreDirectory(dir))
                        continue;

                    // Ki?m tra xem th? m?c c? ch?a file .h kh?ng
                    if (_fileService.GetFilesInDirectory(dir, ".h", false).Any())
                    {
                        includeDirs.Add(dir);
                        continue;
                    }

                    // Ki?m tra n?u t?n th? m?c g?i ? ??y l? th? m?c include
                    if (IsPotentialIncludeDir(dir))
                    {
                        includeDirs.Add(dir);
                    }
                }

                // Th?m th? m?c g?c
                if (_fileService.GetFilesInDirectory(rootDirectoryPath, ".h", false).Any())
                {
                    includeDirs.Add(rootDirectoryPath);
                }

                _logger.LogInformation($"?? t?m th?y {includeDirs.Count} th? m?c include ti?m n?ng");
                return includeDirs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"L?i khi t?m ki?m th? m?c include: {ex.Message}");
                return includeDirs;
            }
        }

        /// <inheritdoc/>
        public async Task<string> FindIncludeFileAsync(string includePath, List<string> searchDirectories, string currentFilePath = null)
        {
            _logger.LogDebug($"T?m ki?m t?p include: {includePath}");

            try
            {
                // N?u ???ng d?n include l? tuy?t ??i, ki?m tra tr?c ti?p
                if (Path.IsPathRooted(includePath) && _fileService.FileExists(includePath))
                {
                    return includePath;
                }

                // N?u l? include d? ?n (c? ???ng d?n t??ng ??i)
                if (includePath.Contains("/") || includePath.Contains("\\"))
                {
                    // Tr??ng h?p ??c bi?t: n?u include c? d?ng "../abc.h"
                    if (includePath.StartsWith("../") || includePath.StartsWith("..\\"))
                    {
                        if (!string.IsNullOrEmpty(currentFilePath))
                        {
                            string currentDir = Path.GetDirectoryName(currentFilePath);
                            string resolvedPath = Path.GetFullPath(Path.Combine(currentDir, includePath));
                            
                            if (_fileService.FileExists(resolvedPath))
                            {
                                return resolvedPath;
                            }
                        }
                    }
                }

                // T?m ki?m trong th? m?c hi?n t?i (n?u c?)
                if (!string.IsNullOrEmpty(currentFilePath))
                {
                    string currentDir = Path.GetDirectoryName(currentFilePath);
                    string potentialPath = Path.Combine(currentDir, includePath);
                    
                    if (_fileService.FileExists(potentialPath))
                    {
                        return potentialPath;
                    }
                }

                // T?m ki?m trong c?c th? m?c include
                foreach (var dir in searchDirectories)
                {
                    string potentialPath = Path.Combine(dir, includePath);
                    
                    if (_fileService.FileExists(potentialPath))
                    {
                        return potentialPath;
                    }
                }

                _logger.LogWarning($"Kh?ng t?m th?y t?p include: {includePath}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"L?i khi t?m ki?m t?p include {includePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Ph?n t?ch c?c c?u l?nh include t? m?t t?p m? ngu?n
        /// </summary>
        /// <param name="filePath">???ng d?n ??n t?p m? ngu?n</param>
        /// <returns>Danh s?ch c?c ???ng d?n include t? t?p n?y</returns>
        public async Task<List<InterfaceIncludeStatement>> ParseIncludeStatementsAsync(string filePath)
        {
            _logger.LogDebug($"Ph?n t?ch c?c c?u l?nh include trong t?p: {filePath}");

            var includes = new List<InterfaceIncludeStatement>();
            
            try
            {
                if (!_fileService.FileExists(filePath))
                {
                    _logger.LogWarning($"T?p kh?ng t?n t?i: {filePath}");
                    return includes;
                }

                string content = await _fileService.ReadFileAsync(filePath);
                string[] lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Regex ?? ph?t hi?n c?u l?nh #include
                var includeRegex = new Regex(@"^\s*#\s*include\s+([""<])([^"">]+)(["">\s])", RegexOptions.Compiled);
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    var match = includeRegex.Match(line);
                    
                    if (match.Success)
                    {
                        var delimiter = match.Groups[1].Value;
                        var path = match.Groups[2].Value;
                        
                        var include = new InterfaceIncludeStatement
                        {
                            FileName = Path.GetFileName(path),
                            RawIncludePath = $"{delimiter}{path}{(delimiter == "<" ? ">" : "\"")}",
                            NormalizedIncludePath = path,
                            LineNumber = i + 1,
                            IsSystemInclude = delimiter == "<"
                        };
                        
                        includes.Add(include);
                    }
                }

                _logger.LogDebug($"?? t?m th?y {includes.Count} c?u l?nh include trong t?p {filePath}");
                return includes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"L?i khi ph?n t?ch c?u l?nh include trong t?p {filePath}: {ex.Message}");
                return includes;
            }
        }

        /// <inheritdoc/>
        public async Task<InterfaceIncludeDependencyGraph> BuildIncludeDependencyGraphAsync(List<string> filePaths, List<string> includePaths)
        {
            _logger.LogInformation($"X?y d?ng ?? th? ph? thu?c include cho {filePaths.Count} t?p");

            var graph = new InterfaceIncludeDependencyGraph
            {
                IncludePaths = includePaths ?? new List<string>()
            };

            try
            {
                // T?o danh s?ch t?t c? c?c t?p ngu?n
                foreach (var path in filePaths)
                {
                    if (_fileService.FileExists(path))
                    {
                        graph.AddSourceFile(path).FileType = DetermineFileType(path);
                    }
                }

                // Ph?n t?ch c?c t?p ?? t?m dependencies
                foreach (var sourceFile in graph.SourceFiles)
                {
                    await ParseFileIncludesAsync(sourceFile, graph);
                }

                // X?c ??nh quan h? ph? thu?c hai chi?u
                foreach (var sourceFile in graph.SourceFiles)
                {
                    foreach (var dependency in sourceFile.DirectDependencies)
                    {
                        if (!dependency.DependentFiles.Contains(sourceFile))
                        {
                            dependency.DependentFiles.Add(sourceFile);
                        }
                    }
                }

                _logger.LogInformation($"?? th? ph? thu?c include ?? ???c x?y d?ng v?i {graph.SourceFiles.Count} t?p");
                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"L?i khi x?y d?ng ?? th? ph? thu?c: {ex.Message}");
                return graph;
            }
        }

        /// <inheritdoc/>
        public async Task<List<InterfaceConditionalBlock>> ParsePreprocessorConditionalsAsync(string filePath)
        {
            _logger.LogDebug($"Ph?n t?ch c?c directive ti?n x? l? ?i?u ki?n trong t?p: {filePath}");

            var blocks = new List<InterfaceConditionalBlock>();
            var stack = new Stack<InterfaceConditionalBlock>();
            
            try
            {
                if (!_fileService.FileExists(filePath))
                {
                    _logger.LogWarning($"T?p kh?ng t?n t?i: {filePath}");
                    return blocks;
                }

                string content = await _fileService.ReadFileAsync(filePath);
                string[] lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Regex cho c?c directive ti?n x? l?
                var ifRegex = new Regex(@"^\s*#\s*(if|ifdef|ifndef)\s+(.+)$", RegexOptions.Compiled);
                var elifRegex = new Regex(@"^\s*#\s*elif\s+(.+)$", RegexOptions.Compiled);
                var elseRegex = new Regex(@"^\s*#\s*else", RegexOptions.Compiled);
                var endifRegex = new Regex(@"^\s*#\s*endif", RegexOptions.Compiled);
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    
                    // Ki?m tra #if, #ifdef, #ifndef
                    var ifMatch = ifRegex.Match(line);
                    if (ifMatch.Success)
                    {
                        var block = new InterfaceConditionalBlock
                        {
                            DirectiveType = ifMatch.Groups[1].Value,
                            Condition = ifMatch.Groups[2].Value.Trim(),
                            StartLine = i + 1
                        };
                        
                        if (stack.Count > 0)
                        {
                            var parent = stack.Peek();
                            block.Parent = parent;
                            parent.NestedBlocks.Add(block);
                        }
                        else
                        {
                            blocks.Add(block);
                        }
                        
                        stack.Push(block);
                        continue;
                    }
                    
                    // Ki?m tra #elif
                    var elifMatch = elifRegex.Match(line);
                    if (elifMatch.Success && stack.Count > 0)
                    {
                        var currentBlock = stack.Pop();
                        currentBlock.EndLine = i;
                        
                        var newBlock = new InterfaceConditionalBlock
                        {
                            DirectiveType = "elif",
                            Condition = elifMatch.Groups[1].Value.Trim(),
                            StartLine = i + 1,
                            Parent = currentBlock.Parent
                        };
                        
                        if (currentBlock.Parent != null)
                        {
                            currentBlock.Parent.NestedBlocks.Add(newBlock);
                        }
                        else
                        {
                            blocks.Add(newBlock);
                        }
                        
                        stack.Push(newBlock);
                        continue;
                    }
                    
                    // Ki?m tra #else
                    if (elseRegex.IsMatch(line) && stack.Count > 0)
                    {
                        var currentBlock = stack.Pop();
                        currentBlock.EndLine = i;
                        
                        var newBlock = new InterfaceConditionalBlock
                        {
                            DirectiveType = "else",
                            Condition = "true",
                            StartLine = i + 1,
                            Parent = currentBlock.Parent
                        };
                        
                        if (currentBlock.Parent != null)
                        {
                            currentBlock.Parent.NestedBlocks.Add(newBlock);
                        }
                        else
                        {
                            blocks.Add(newBlock);
                        }
                        
                        stack.Push(newBlock);
                        continue;
                    }
                    
                    // Ki?m tra #endif
                    if (endifRegex.IsMatch(line) && stack.Count > 0)
                    {
                        var currentBlock = stack.Pop();
                        currentBlock.EndLine = i + 1;
                        continue;
                    }
                }

                // X? l? nh?ng kh?i kh?ng ???c ??ng ??ng c?ch
                while (stack.Count > 0)
                {
                    var block = stack.Pop();
                    block.EndLine = lines.Length;
                    _logger.LogWarning($"Ph?t hi?n kh?i ?i?u ki?n kh?ng ??ng trong t?p {filePath}: {block.DirectiveType} {block.Condition} (d?ng {block.StartLine})");
                }

                _logger.LogDebug($"?? t?m th?y {blocks.Count} kh?i ?i?u ki?n ti?n x? l? (c?p cao nh?t) trong t?p {filePath}");
                return blocks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"L?i khi ph?n t?ch c?c directive ti?n x? l? trong t?p {filePath}: {ex.Message}");
                return blocks;
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// X?c ??nh xem c? n?n b? qua th? m?c n?y kh?ng
        /// </summary>
        private bool ShouldIgnoreDirectory(string directoryPath)
        {
            string dirName = Path.GetFileName(directoryPath).ToLowerInvariant();
            
            // B? qua c?c th? m?c th?ng d?ng kh?ng c?n thi?t
            string[] ignoreList = { ".git", ".vs", "bin", "obj", "debug", "release", ".svn", ".idea", "packages", "node_modules" };
            
            return ignoreList.Contains(dirName);
        }

        /// <summary>
        /// Ki?m tra xem t?n th? m?c c? ph?i l? th? m?c include ti?m n?ng kh?ng
        /// </summary>
        private bool IsPotentialIncludeDir(string directoryPath)
        {
            string dirName = Path.GetFileName(directoryPath).ToLowerInvariant();
            
            // C?c t?n th? m?c th??ng ???c d?ng cho th? m?c include
            string[] includeHints = { "include", "inc", "headers", "h", "interface", "api", "common" };
            
            return includeHints.Contains(dirName);
        }

        /// <summary>
        /// X?c ??nh lo?i t?p d?a tr?n ph?n m? r?ng
        /// </summary>
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
        /// Ph?n t?ch m?t t?p ?? t?m c?c c?u l?nh include v? c?p nh?t ?? th? ph? thu?c
        /// </summary>
        private async Task ParseFileIncludesAsync(InterfaceSourceFileDependency sourceFile, InterfaceIncludeDependencyGraph graph)
        {
            if (sourceFile.Parsed)
                return;
            
            try
            {
                // Ph?n t?ch c?c c?u l?nh include
                sourceFile.Includes = await ParseIncludeStatementsAsync(sourceFile.FilePath);
                
                // Ph?n t?ch c?c kh?i ?i?u ki?n
                sourceFile.ConditionalBlocks = await ParsePreprocessorConditionalsAsync(sourceFile.FilePath);
                
                // X? l? m?i c?u l?nh include
                foreach (var include in sourceFile.Includes)
                {
                    // T?m kh?i ?i?u ki?n ch?a c?u l?nh include n?y
                    include.Conditional = FindContainingConditionalBlock(include.LineNumber, sourceFile.ConditionalBlocks);
                    
                    // T?m t?p ???c include
                    include.ResolvedPath = await FindIncludeFileAsync(
                        include.NormalizedIncludePath,
                        graph.IncludePaths,
                        sourceFile.FilePath);
                    
                    // N?u t?m th?y t?p include, th?m v?o ?? th? v? x?c l?p quan h? ph? thu?c
                    if (!string.IsNullOrEmpty(include.ResolvedPath))
                    {
                        var dependencyFile = graph.FindFile(include.ResolvedPath);
                        
                        if (dependencyFile == null)
                        {
                            // Th?m t?p m?i v?o ?? th?
                            dependencyFile = graph.AddSourceFile(include.ResolvedPath);
                            dependencyFile.FileType = DetermineFileType(include.ResolvedPath);
                            
                            // Ph?n t?ch ?? quy t?p n?y
                            await ParseFileIncludesAsync(dependencyFile, graph);
                        }
                        
                        // Th?m quan h? ph? thu?c
                        if (!sourceFile.DirectDependencies.Contains(dependencyFile))
                        {
                            sourceFile.DirectDependencies.Add(dependencyFile);
                        }
                    }
                }
                
                sourceFile.Parsed = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"L?i khi ph?n t?ch include trong t?p {sourceFile.FilePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// T?m kh?i ?i?u ki?n ch?a m?t d?ng c? th?
        /// </summary>
        private InterfaceConditionalBlock FindContainingConditionalBlock(int lineNumber, List<InterfaceConditionalBlock> blocks)
        {
            foreach (var block in blocks)
            {
                if (lineNumber >= block.StartLine && lineNumber <= block.EndLine)
                {
                    // Ki?m tra c?c kh?i con tr??c
                    var nestedBlock = FindContainingConditionalBlock(lineNumber, block.NestedBlocks);
                    return nestedBlock ?? block;
                }
            }
            
            return null;
        }

        #endregion
    }
}