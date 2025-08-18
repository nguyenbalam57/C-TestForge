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

// Sử dụng alias để tránh xung đột namespace
using ModelIncludeStatement = C_TestForge.Models.Projects.IncludeStatement;
using ModelConditionalBlock = C_TestForge.Models.Projects.ConditionalBlock;
using ModelIncludeDependencyGraph = C_TestForge.Models.Projects.IncludeDependencyGraph;
using ModelSourceFileDependency = C_TestForge.Models.Projects.SourceFileDependency;

namespace C_TestForge.Parser.Projects
{
    /// <summary>
    /// Dịch vụ quét và phân tích các tệp mã nguồn C/C++
    /// </summary>
    public class FileScannerService : IFileScannerService
    {
        private readonly ILogger<FileScannerService> _logger;
        private readonly IFileService _fileService;

        /// <summary>
        /// Khởi tạo đối tượng FileScannerService
        /// </summary>
        /// <param name="logger">Đối tượng ghi log</param>
        /// <param name="fileService">Dịch vụ thao tác tệp tin</param>
        public FileScannerService(ILogger<FileScannerService> logger, IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        /// <inheritdoc/>
        public async Task<List<string>> ScanDirectoryForCFilesAsync(string directoryPath, bool recursive = true)
        {
            _logger.LogInformation($"Quét thư mục {directoryPath} để tìm các tệp C/H (đệ quy: {recursive})");

            try
            {
                if (!Directory.Exists(directoryPath))
                {
                    _logger.LogWarning($"Thư mục không tồn tại: {directoryPath}");
                    return new List<string>();
                }

                // Tìm tệp C (.c)
                var cFiles = _fileService.GetFilesInDirectory(directoryPath, ".c", recursive);
                
                // Tìm tệp header (.h)
                var hFiles = _fileService.GetFilesInDirectory(directoryPath, ".h", recursive);

                // Tìm tệp C++ (.cpp)
                var cppFiles = _fileService.GetFilesInDirectory(directoryPath, ".cpp", recursive);
                
                // Tìm tệp header C++ (.hpp)
                var hppFiles = _fileService.GetFilesInDirectory(directoryPath, ".hpp", recursive);

                // Gộp kết quả
                var result = new List<string>();
                result.AddRange(cFiles);
                result.AddRange(hFiles);
                result.AddRange(cppFiles);
                result.AddRange(hppFiles);

                _logger.LogInformation($"Đã tìm thấy {result.Count} tệp C/C++ trong thư mục {directoryPath}");
                _logger.LogDebug($"Chi tiết: {cFiles.Count} .c, {hFiles.Count} .h, {cppFiles.Count} .cpp, {hppFiles.Count} .hpp");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi quét thư mục {directoryPath}: {ex.Message}");
                return new List<string>();
            }
        }

        /// <inheritdoc/>
        public async Task<List<string>> FindPotentialIncludeDirectoriesAsync(List<string> rootDirectoryPath)
        {
            _logger.LogInformation($"Tìm kiếm các thư mục include tiềm năng trong {rootDirectoryPath}");
            
            var includeDirs = new List<string>();
            
            try
            {
                
                foreach (var dir in rootDirectoryPath)
                {
                    // Bỏ qua thư mục .git, .vs, bin, obj, ...
                    if (ShouldIgnoreDirectory(dir))
                        continue;

                    // Kiểm tra xem thư mục có chứa file .h không
                    if (_fileService.GetFilesInDirectory(dir, ".h", false).Any())
                    {
                        includeDirs.Add(dir);
                        continue;
                    }

                    // Kiểm tra nếu tên thư mục gợi ý đây là thư mục include
                    if (IsPotentialIncludeDir(dir))
                    {
                        includeDirs.Add(dir);
                    }
                }

                _logger.LogInformation($"Đã tìm thấy {includeDirs.Count} thư mục include tiềm năng");
                return includeDirs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tìm kiếm thư mục include: {ex.Message}");
                return includeDirs;
            }
        }

        /// <inheritdoc/>
        public async Task<string> FindIncludeFileAsync(string includePath, List<string> searchDirectories, string currentFilePath = null)
        {
            _logger.LogDebug($"Tìm kiếm tệp include: {includePath}");

            try
            {
                // Tìm kiếm trong các thư mục include
                foreach (var dir in searchDirectories)
                {
                    if (_fileService.FileExists(dir) && Path.GetFileName(dir).ToString() == includePath)
                    {
                        return dir;
                    }
                }

                _logger.LogWarning($"Không tìm thấy tệp include: {includePath}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi tìm kiếm tệp include {includePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Phân tích các câu lệnh include từ một tệp mã nguồn
        /// </summary>
        /// <param name="filePath">Đường dẫn đến tệp mã nguồn</param>
        /// <returns>Danh sách các đường dẫn include từ tệp này</returns>
        public async Task<List<IncludeStatement>> ParseIncludeStatementsAsync(string filePath)
        {
            _logger.LogDebug($"Phân tích các câu lệnh include trong tệp: {filePath}");

            var includes = new List<IncludeStatement>();
            
            try
            {
                if (!_fileService.FileExists(filePath))
                {
                    _logger.LogWarning($"Tệp không tồn tại: {filePath}");
                    return includes;
                }

                string content = await _fileService.ReadFileAsync(filePath);
                string[] lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Regex để phát hiện câu lệnh #include
                var includeRegex = new Regex(@"^\s*#\s*include\s+([\""<])([^\"">]+)([\"">\s])", RegexOptions.Compiled);
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    var match = includeRegex.Match(line);
                    
                    if (match.Success)
                    {
                        var delimiter = match.Groups[1].Value;
                        var path = match.Groups[2].Value;
                        
                        var include = new IncludeStatement
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

                _logger.LogDebug($"Đã tìm thấy {includes.Count} câu lệnh include trong tệp {filePath}");
                return includes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi phân tích câu lệnh include trong tệp {filePath}: {ex.Message}");
                return includes;
            }
        }

        /// <inheritdoc/>
        public async Task<IncludeDependencyGraph> BuildIncludeDependencyGraphAsync(List<string> filePaths, List<string> includePaths)
        {
            _logger.LogInformation($"Xây dựng đồ thị phụ thuộc include cho {filePaths.Count} tệp");

            var graph = new IncludeDependencyGraph
            {
                IncludePaths = includePaths ?? new List<string>()
            };

            try
            {
                // Tạo danh sách tất cả các tệp nguồn
                foreach (var path in filePaths)
                {
                    if (_fileService.FileExists(path))
                    {
                        graph.AddSourceFile(path).FileType = DetermineFileType(path);
                    }
                }

                // Phân tích các tệp để tìm dependencies
                foreach (var sourceFile in graph.SourceFiles)
                {
                    await ParseFileIncludesAsync(sourceFile, graph);
                }

                // Xác định quan hệ phụ thuộc hai chiều
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

                _logger.LogInformation($"Đồ thị phụ thuộc include đã được xây dựng với {graph.SourceFiles.Count} tệp");
                return graph;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xây dựng đồ thị phụ thuộc: {ex.Message}");
                return graph;
            }
        }

        /// <inheritdoc/>
        public async Task<List<ConditionalBlock>> ParsePreprocessorConditionalsAsync(string filePath)
        {
            _logger.LogDebug($"Phân tích các directive tiền xử lý điều kiện trong tệp: {filePath}");

            var blocks = new List<ConditionalBlock>();
            var stack = new Stack<ConditionalBlock>();
            
            try
            {
                if (!_fileService.FileExists(filePath))
                {
                    _logger.LogWarning($"Tệp không tồn tại: {filePath}");
                    return blocks;
                }

                string content = await _fileService.ReadFileAsync(filePath);
                string[] lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                
                // Regex cho các directive tiền xử lý
                var ifRegex = new Regex(@"^\s*#\s*(if|ifdef|ifndef)\s+(.+)$", RegexOptions.Compiled);
                var elifRegex = new Regex(@"^\s*#\s*elif\s+(.+)$", RegexOptions.Compiled);
                var elseRegex = new Regex(@"^\s*#\s*else", RegexOptions.Compiled);
                var endifRegex = new Regex(@"^\s*#\s*endif", RegexOptions.Compiled);
                
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    
                    // Kiểm tra #if, #ifdef, #ifndef
                    var ifMatch = ifRegex.Match(line);
                    if (ifMatch.Success)
                    {
                        var block = new ConditionalBlock
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
                    
                    // Kiểm tra #elif
                    var elifMatch = elifRegex.Match(line);
                    if (elifMatch.Success && stack.Count > 0)
                    {
                        var currentBlock = stack.Pop();
                        currentBlock.EndLine = i;
                        
                        var newBlock = new ConditionalBlock
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
                    
                    // Kiểm tra #else
                    if (elseRegex.IsMatch(line) && stack.Count > 0)
                    {
                        var currentBlock = stack.Pop();
                        currentBlock.EndLine = i;
                        
                        var newBlock = new ConditionalBlock
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
                    
                    // Kiểm tra #endif
                    if (endifRegex.IsMatch(line) && stack.Count > 0)
                    {
                        var currentBlock = stack.Pop();
                        currentBlock.EndLine = i + 1;
                        continue;
                    }
                }

                // Xử lý những khối chưa được đóng đúng cách
                while (stack.Count > 0)
                {
                    var block = stack.Pop();
                    block.EndLine = lines.Length;
                    _logger.LogWarning($"Phát hiện khối điều kiện chưa đóng trong tệp {filePath}: {block.DirectiveType} {block.Condition} (dòng {block.StartLine})");
                }

                _logger.LogDebug($"Đã tìm thấy {blocks.Count} khối điều kiện tiền xử lý (cấp cao nhất) trong tệp {filePath}");
                return blocks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi phân tích các directive tiền xử lý trong tệp {filePath}: {ex.Message}");
                return blocks;
            }
        }

        #region Các phương thức hỗ trợ riêng

        /// <summary>
        /// Xác định xem có nên bỏ qua thư mục này không
        /// </summary>
        private bool ShouldIgnoreDirectory(string directoryPath)
        {
            string dirName = Path.GetFileName(directoryPath).ToLowerInvariant();
            
            // Bỏ qua các thư mục thông dụng không cần thiết
            string[] ignoreList = { ".git", ".vs", "bin", "obj", "debug", "release", ".svn", ".idea", "packages", "node_modules" };
            
            return ignoreList.Contains(dirName);
        }

        /// <summary>
        /// Kiểm tra xem tên thư mục có phải là thư mục include tiềm năng không
        /// </summary>
        private bool IsPotentialIncludeDir(string directoryPath)
        {
            string dirName = Path.GetFileName(directoryPath).ToLowerInvariant();
            
            // Các tên thư mục thường được dùng cho thư mục include
            string[] includeHints = { "include", "inc", "headers", "h", "interface", "api", "common" };
            
            return includeHints.Contains(dirName);
        }

        /// <summary>
        /// Xác định loại tệp dựa trên phần mở rộng
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
        /// Phân tích một tệp để tìm các câu lệnh include và cập nhật đồ thị phụ thuộc
        /// </summary>
        private async Task ParseFileIncludesAsync(SourceFileDependency sourceFile, IncludeDependencyGraph graph)
        {
            if (sourceFile.Parsed)
                return;
            
            try
            {
                // Phân tích các câu lệnh include
                sourceFile.Includes = await ParseIncludeStatementsAsync(sourceFile.FilePath);
                
                // Phân tích các khối điều kiện
                sourceFile.ConditionalBlocks = await ParsePreprocessorConditionalsAsync(sourceFile.FilePath);
                
                // Xử lý mỗi câu lệnh include
                foreach (var include in sourceFile.Includes)
                {
                    // Tìm khối điều kiện chứa câu lệnh include này
                    include.Conditional = FindContainingConditionalBlock(include.LineNumber, sourceFile.ConditionalBlocks);
                    
                    // Tìm tệp được include
                    include.ResolvedPath = await FindIncludeFileAsync(
                        include.NormalizedIncludePath,
                        graph.IncludePaths,
                        sourceFile.FilePath);
                    
                    // Nếu tìm thấy tệp include, thêm vào đồ thị và xác lập quan hệ phụ thuộc
                    if (!string.IsNullOrEmpty(include.ResolvedPath))
                    {
                        var dependencyFile = graph.FindFile(include.ResolvedPath);
                        
                        if (dependencyFile == null)
                        {
                            // Thêm tệp mới vào đồ thị
                            dependencyFile = graph.AddSourceFile(include.ResolvedPath);
                            dependencyFile.FileType = DetermineFileType(include.ResolvedPath);
                            
                            // Phân tích đệ quy tệp này
                            await ParseFileIncludesAsync(dependencyFile, graph);
                        }
                        
                        // Thêm quan hệ phụ thuộc
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
                _logger.LogError(ex, $"Lỗi khi phân tích include trong tệp {sourceFile.FilePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Tìm khối điều kiện chứa một dòng cụ thể
        /// </summary>
        private ConditionalBlock FindContainingConditionalBlock(int lineNumber, List<ConditionalBlock> blocks)
        {
            foreach (var block in blocks)
            {
                if (lineNumber >= block.StartLine && lineNumber <= block.EndLine)
                {
                    // Kiểm tra các khối con trước
                    var nestedBlock = FindContainingConditionalBlock(lineNumber, block.NestedBlocks);
                    return nestedBlock ?? block;
                }
            }
            
            return null;
        }

        #endregion
    }
}