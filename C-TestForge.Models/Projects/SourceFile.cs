using C_TestForge.Models.Base;
using C_TestForge.Models.Parse;
using C_TestForge.Models.CodeAnalysis;
using C_TestForge.Models.CodeAnalysis.BaseClasss;
using C_TestForge.Models.CodeAnalysis.ClangASTNodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace C_TestForge.Models.Projects
{
    /// <summary>
    /// Represents a source file with comprehensive code analysis capabilities
    /// </summary>
    public class SourceFile : IModelObject
    {
        #region Basic Properties

        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Path to the source file
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Name of the source file
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Directory containing the source file
        /// </summary>
        public string Directory { get; set; } = string.Empty;

        /// <summary>
        /// Extension of the source file
        /// </summary>
        public string Extension { get; set; } = string.Empty;

        /// <summary>
        /// Content of the source file
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Processed content after type replacements and preprocessing
        /// </summary>
        public string ProcessedContent { get; set; } = string.Empty;

        /// <summary>
        /// Lines of the source file
        /// </summary>
        public List<string> Lines { get; set; } = new List<string>();

        /// <summary>
        /// Processed lines after type replacements and preprocessing
        /// </summary>
        public List<string> ProcessedLines { get; set; } = new List<string>();

        /// <summary>
        /// Hash of the content for change detection
        /// </summary>
        public string ContentHash { get; set; } = string.Empty;

        /// <summary>
        /// Type of the source file
        /// </summary>
        public SourceFileType FileType { get; set; }

        /// <summary>
        /// Last modified time of the file
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// File size in bytes
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// Encoding of the source file
        /// </summary>
        public string EncodingText { get; set; } 

        #endregion

        #region Analysis Properties

        /// <summary>
        /// Dictionary of includes in the source file
        /// </summary>
        public Dictionary<string, IncludeInfo> Includes { get; set; } = new Dictionary<string, IncludeInfo>();

        /// <summary>
        /// Parse result for this source file
        /// </summary>
        [JsonIgnore]
        public ParseResult ParseResult { get; set; } = new ParseResult();

        /// <summary>
        /// Code analysis results
        /// </summary>
        [JsonIgnore]
        public SourceFileAnalysisResult AnalysisResult { get; set; } = new SourceFileAnalysisResult();

        /// <summary>
        /// List of preprocessor directives
        /// </summary>
        public List<PreprocessorDirective> PreprocessorDirectives { get; set; } = new List<PreprocessorDirective>();

        /// <summary>
        /// List of comments in the file
        /// </summary>
        public List<CommentBlock> Comments { get; set; } = new List<CommentBlock>();

        /// <summary>
        /// Statistics about the source file
        /// </summary>
        public SourceFileStatistics Statistics { get; set; } = new SourceFileStatistics();

        #endregion

        #region State Properties

        /// <summary>
        /// Whether the file has been modified since the last save
        /// </summary>
        [JsonIgnore]
        public bool IsDirty { get; set; }

        /// <summary>
        /// Whether the file has been parsed
        /// </summary>
        [JsonIgnore]
        public bool IsParsed { get; set; }

        /// <summary>
        /// Whether the file has been analyzed
        /// </summary>
        [JsonIgnore]
        public bool IsAnalyzed { get; set; }

        /// <summary>
        /// Whether the file exists on disk
        /// </summary>
        [JsonIgnore]
        public bool Exists => File.Exists(FilePath);

        /// <summary>
        /// Whether the file is read-only
        /// </summary>
        [JsonIgnore]
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Whether the file is binary
        /// </summary>
        [JsonIgnore]
        public bool IsBinary { get; set; }

        /// <summary>
        /// Last analysis timestamp
        /// </summary>
        public DateTime LastAnalyzed { get; set; }

        #endregion

        #region Dependencies

        /// <summary>
        /// Files that this file depends on (includes)
        /// </summary>
        public List<string> Dependencies { get; set; } = new List<string>();

        /// <summary>
        /// Files that depend on this file
        /// </summary>
        [JsonIgnore]
        public List<string> Dependents { get; set; } = new List<string>();

        /// <summary>
        /// External dependencies (system headers, libraries)
        /// </summary>
        public List<string> ExternalDependencies { get; set; } = new List<string>();

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
        public SourceFile()
        {
            Id = Guid.NewGuid().ToString();
            Lines = new List<string>();
            ProcessedLines = new List<string>();
            Includes = new Dictionary<string, IncludeInfo>();
            Dependencies = new List<string>();
            Dependents = new List<string>();
            ExternalDependencies = new List<string>();
            PreprocessorDirectives = new List<PreprocessorDirective>();
            Comments = new List<CommentBlock>();
            Statistics = new SourceFileStatistics();
            AnalysisResult = new SourceFileAnalysisResult();
        }

        /// <summary>
        /// Constructor with file path
        /// </summary>
        public SourceFile(string filePath) : this()
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            Directory = Path.GetDirectoryName(filePath) ?? string.Empty;
            Extension = Path.GetExtension(filePath);
            FileType = DetermineFileType(Extension);
        }

        #endregion

        #region Content Management

        /// <summary>
        /// Load content from file
        /// </summary>
        public void LoadFromFile()
        {
            if (!File.Exists(FilePath))
                throw new FileNotFoundException($"Source file not found: {FilePath}");

            var fileInfo = new FileInfo(FilePath);
            LastModified = fileInfo.LastWriteTime;
            FileSize = fileInfo.Length;

            // Detect if file is binary
            if (IsBinaryFile(FilePath))
            {
                IsBinary = true;
                Content = string.Empty;
                Lines = new List<string>();
                return;
            }

            ReadFileAsync(FilePath);
            UpdateContent(Content);
        }

        /// <summary>
        /// Update the content of the source file
        /// </summary>
        public void UpdateContent(string newContent)
        {
            Lines = Content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();
            ContentHash = ComputeContentHash(Content);
            // Sử dụng EncodingText để lấy Encoding phù hợp khi tính FileSize
            Encoding encoding;
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                encoding = !string.IsNullOrEmpty(EncodingText)
                    ? Encoding.GetEncoding(EncodingText)
                    : Encoding.UTF8;
            }
            catch
            {
                encoding = Encoding.UTF8;
            }
            FileSize = encoding.GetByteCount(Content);

            IsDirty = true;

            // Reset analysis flags
            IsParsed = false;
            IsAnalyzed = false;

            // Update statistics
            UpdateStatistics();

            // Extract basic elements
            ExtractPreprocessorDirectives();
            ExtractComments();
            ExtractIncludes();
        }

        /// <summary>
        /// Update a specific line in the source file
        /// </summary>
        public void UpdateLine(int lineNumber, string newContent)
        {
            if (lineNumber < 1 || lineNumber > Lines.Count)
                throw new ArgumentOutOfRangeException(nameof(lineNumber));

            Lines[lineNumber - 1] = newContent ?? string.Empty;
            Content = string.Join(Environment.NewLine, Lines);
            ContentHash = ComputeContentHash(Content);
            IsDirty = true;
            IsParsed = false;
            IsAnalyzed = false;

            UpdateStatistics();
        }

        /// <summary>
        /// Insert a line at the specified position
        /// </summary>
        public void InsertLine(int lineNumber, string content)
        {
            if (lineNumber < 1 || lineNumber > Lines.Count + 1)
                throw new ArgumentOutOfRangeException(nameof(lineNumber));

            Lines.Insert(lineNumber - 1, content ?? string.Empty);
            Content = string.Join(Environment.NewLine, Lines);
            ContentHash = ComputeContentHash(Content);
            IsDirty = true;
            IsParsed = false;
            IsAnalyzed = false;

            UpdateStatistics();
        }

        /// <summary>
        /// Delete a line at the specified position
        /// </summary>
        public void DeleteLine(int lineNumber)
        {
            if (lineNumber < 1 || lineNumber > Lines.Count)
                throw new ArgumentOutOfRangeException(nameof(lineNumber));

            Lines.RemoveAt(lineNumber - 1);
            Content = string.Join(Environment.NewLine, Lines);
            ContentHash = ComputeContentHash(Content);
            IsDirty = true;
            IsParsed = false;
            IsAnalyzed = false;

            UpdateStatistics();
        }

        /// <summary>
        /// Save content to file
        /// </summary>
        public void SaveToFile()
        {
            if (string.IsNullOrEmpty(FilePath))
                throw new InvalidOperationException("FilePath is not set");

            File.WriteAllText(FilePath, Content);
            IsDirty = false;
            LastModified = File.GetLastWriteTime(FilePath);
        }

        #endregion

        #region ReadFile Methods

        /// <inheritdoc/>
        /// <summary>
        /// Đọc nội dung file với phát hiện encoding tự động hoặc theo chỉ định
        /// </summary>
        /// <param name="filePath">Đường dẫn file</param>
        /// <param name="encodingName">Tên encoding, null để tự động phát hiện</param>
        /// <returns>Nội dung file dưới dạng chuỗi</returns>
        private async void ReadFileAsync(string filePath, string encodingName = null)
        {
            try
            {
                // Đảm bảo các encoding bổ sung được đăng ký
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                string encoding = string.Empty;

                // Sử dụng encoding được chỉ định
                if (!string.IsNullOrEmpty(encodingName))
                {
                    try
                    {
                        encoding = encoding;
                    }
                    catch (ArgumentException)
                    {
                        encoding = null;
                    }
                }

                // Nếu không có encoding được chỉ định, tự động phát hiện
                if (string.IsNullOrEmpty(encoding))
                {
                    // Đọc một phần file để phát hiện encoding
                    int bufferSize = 4096; // 4KB là đủ cho hầu hết trường hợp
                    byte[] buffer = new byte[bufferSize];

                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                    {
                        int bytesRead = await fileStream.ReadAsync(buffer, 0, bufferSize);
                        if (bytesRead < bufferSize)
                        {
                            Array.Resize(ref buffer, bytesRead);
                        }
                    }

                    encoding = DetectTextEncoding(buffer);
                }

                // Đọc file với encoding đã xác định

                EncodingText = encoding;
                Content = await File.ReadAllTextAsync(filePath, Encoding.GetEncoding(encoding));

            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Kiểm tra xem buffer có chứa dữ liệu nhị phân không
        /// </summary>
        private bool ContainsBinaryData(byte[] buffer)
        {
            // Giới hạn số byte cần kiểm tra để tăng hiệu suất
            int bytesToCheck = Math.Min(buffer.Length, 512);
            int nullCount = 0;

            for (int i = 0; i < bytesToCheck; i++)
            {
                byte b = buffer[i];

                // Nếu là byte null
                if (b == 0x00)
                {
                    nullCount++;
                    // Nếu có hơn 5% là byte null, có thể là file nhị phân
                    if (nullCount > bytesToCheck * 0.05)
                        return true;
                }

                // Kiểm tra các ký tự điều khiển không phải whitespace
                if (b < 0x20 && b != 0x09 && b != 0x0A && b != 0x0D)
                {
                    // Nếu gặp ký tự điều khiển không phải tab, LF, CR
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra xem buffer có chứa chuỗi UTF-8 hợp lệ không
        /// </summary>
        private bool IsValidUtf8(byte[] buffer)
        {
            int i = 0;
            int length = buffer.Length;

            while (i < length)
            {
                // Byte đầu tiên của chuỗi UTF-8
                byte b = buffer[i++];

                // ASCII (0xxx xxxx)
                if ((b & 0x80) == 0)
                    continue;

                // Xác định số byte của ký tự UTF-8
                int extraBytes;

                if ((b & 0xE0) == 0xC0) // 2 byte (110x xxxx)
                    extraBytes = 1;
                else if ((b & 0xF0) == 0xE0) // 3 byte (1110 xxxx)
                    extraBytes = 2;
                else if ((b & 0xF8) == 0xF0) // 4 byte (1111 0xxx)
                    extraBytes = 3;
                else
                    return false; // Không phải định dạng UTF-8 hợp lệ

                // Kiểm tra các byte tiếp theo (10xx xxxx)
                while (extraBytes > 0 && i < length)
                {
                    b = buffer[i++];
                    if ((b & 0xC0) != 0x80)
                        return false; // Không phải định dạng UTF-8 hợp lệ

                    extraBytes--;
                }

                if (extraBytes > 0)
                    return false; // Chuỗi UTF-8 không hoàn chỉnh
            }

            return true;
        }

        /// <summary>
        /// Kiểm tra xem buffer có chứa ký tự tiếng Nhật không (theo Shift-JIS)
        /// </summary>
        private bool ContainsJapaneseCharacters(byte[] buffer)
        {
            int length = buffer.Length;

            for (int i = 0; i < length - 1; i++)
            {
                byte b1 = buffer[i];
                byte b2 = buffer[i + 1];

                // Kiểm tra dải Shift-JIS cho Hiragana (0x82A0-0x8393)
                if (b1 == 0x82 && b2 >= 0xA0)
                    return true;

                // Kiểm tra dải Shift-JIS cho Katakana (0x83A0-0x8393)
                if (b1 == 0x83 && b2 <= 0x93)
                    return true;

                // Kiểm tra dải Shift-JIS cho Kanji (0x889F-0x9FFC)
                if ((b1 >= 0x88 && b1 <= 0x9F) || (b1 >= 0xE0 && b1 <= 0xEA))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Kiểm tra xem buffer có chứa ký tự tiếng Việt không
        /// </summary>
        private bool ContainsVietnameseCharacters(byte[] buffer)
        {
            // Kiểm tra Windows-1258 encoding
            bool hasVietnameseDiacritics = false;

            for (int i = 0; i < buffer.Length; i++)
            {
                byte b = buffer[i];

                // Các byte đặc trưng cho dấu tiếng Việt trong Windows-1258
                if (b >= 0xC0 && b <= 0xCF) // Â, Ă, Đ, Ê, Ô, Ơ, Ư
                    hasVietnameseDiacritics = true;
                else if (b >= 0xD0 && b <= 0xDC) // à, á, â, ã...
                    hasVietnameseDiacritics = true;
                else if (b >= 0xE0 && b <= 0xEF) // è, é, ê...
                    hasVietnameseDiacritics = true;
            }

            // Kiểm tra UTF-8 với các ký tự tiếng Việt
            if (IsValidUtf8(buffer))
            {
                string content = Encoding.UTF8.GetString(buffer);

                // Tìm các ký tự đặc trưng tiếng Việt
                if (content.Contains('đ') || content.Contains('Đ') ||
                    content.Contains('ă') || content.Contains('Ă') ||
                    content.Contains('â') || content.Contains('Â') ||
                    content.Contains('ê') || content.Contains('Ê') ||
                    content.Contains('ô') || content.Contains('Ô') ||
                    content.Contains('ơ') || content.Contains('Ơ') ||
                    content.Contains('ư') || content.Contains('Ư'))
                {
                    return true;
                }
            }

            return hasVietnameseDiacritics;
        }

        /// <summary>
        /// Phương pháp toàn diện hơn để phát hiện encoding, trả về tên encoding (string)
        /// </summary>
        private string DetectTextEncoding(byte[] buffer)
        {
            try
            {
                // Đảm bảo các encoding bổ sung được đăng ký
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                // Kiểm tra BOM
                if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                    return "utf-8";
                if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
                    return "utf-16BE";
                if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
                    return "utf-16LE";
                if (buffer.Length >= 4 && buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xFE && buffer[3] == 0xFF)
                    return "utf-32BE";

                // Nếu là file nhị phân
                if (ContainsBinaryData(buffer))
                    return Encoding.Default.WebName;

                // Kiểm tra tiếng Nhật
                if (ContainsJapaneseCharacters(buffer))
                {
                    try
                    {
                        return Encoding.GetEncoding("shift_jis").WebName;
                    }
                    catch (ArgumentException)
                    {
                        return "utf-8";
                    }
                }

                // Kiểm tra UTF-8 không có BOM
                if (IsValidUtf8(buffer))
                    return "utf-8";

                // Kiểm tra tiếng Việt
                if (ContainsVietnameseCharacters(buffer))
                {
                    try
                    {
                        return Encoding.GetEncoding("windows-1258").WebName;
                    }
                    catch (ArgumentException)
                    {
                        return "utf-8";
                    }
                }

                // Thử phân tích dựa trên tần suất xuất hiện của các byte
                int[] byteCount = new int[256];
                foreach (byte b in buffer)
                {
                    byteCount[b]++;
                }

                // Nếu có nhiều byte >127 có thể là một bảng mã mở rộng
                int extendedAsciiCount = 0;
                for (int i = 128; i < 256; i++)
                {
                    extendedAsciiCount += byteCount[i];
                }

                if (extendedAsciiCount > buffer.Length * 0.1) // Nếu >10% là byte mở rộng
                {
                    try
                    {
                        return Encoding.GetEncoding(1252).WebName; // Windows-1252
                    }
                    catch (ArgumentException)
                    {
                        return Encoding.Default.WebName;
                    }
                }

                // Mặc định UTF-8
                return "utf-8";
            }
            catch (Exception)
            {
                return "utf-8"; // Fallback to UTF-8
            }
        }

        #endregion

        #region Analysis Methods

        /// <summary>
        /// Extract preprocessor directives from the source
        /// </summary>
        private void ExtractPreprocessorDirectives()
        {
            PreprocessorDirectives.Clear();

            var directivePattern = @"^\s*#\s*(\w+)(.*)$";
            var regex = new Regex(directivePattern, RegexOptions.Multiline);

            var matches = regex.Matches(Content);
            foreach (Match match in matches)
            {
                var directive = new PreprocessorDirective
                {
                    Type = match.Groups[1].Value,
                    Value = match.Groups[2].Value.Trim(),
                    LineNumber = Content.Substring(0, match.Index).Count(c => c == '\n') + 1,
                    Position = match.Index
                };

                PreprocessorDirectives.Add(directive);
            }
        }

        /// <summary>
        /// Extract comments from the source
        /// </summary>
        private void ExtractComments()
        {
            Comments.Clear();

            // Single line comments
            var singleLinePattern = @"//.*$";
            var singleLineRegex = new Regex(singleLinePattern, RegexOptions.Multiline);

            foreach (Match match in singleLineRegex.Matches(Content))
            {
                var comment = new CommentBlock
                {
                    Type = CommentType.SingleLine,
                    Content = match.Value,
                    StartLine = Content.Substring(0, match.Index).Count(c => c == '\n') + 1,
                    StartPosition = match.Index,
                    Length = match.Length
                };
                comment.EndLine = comment.StartLine;
                comment.EndPosition = comment.StartPosition + comment.Length;

                Comments.Add(comment);
            }

            // Multi-line comments
            var multiLinePattern = @"/\*.*?\*/";
            var multiLineRegex = new Regex(multiLinePattern, RegexOptions.Singleline);

            foreach (Match match in multiLineRegex.Matches(Content))
            {
                var startLine = Content.Substring(0, match.Index).Count(c => c == '\n') + 1;
                var endLine = Content.Substring(0, match.Index + match.Length).Count(c => c == '\n') + 1;

                var comment = new CommentBlock
                {
                    Type = CommentType.MultiLine,
                    Content = match.Value,
                    StartLine = startLine,
                    EndLine = endLine,
                    StartPosition = match.Index,
                    EndPosition = match.Index + match.Length,
                    Length = match.Length
                };

                Comments.Add(comment);
            }
        }

        /// <summary>
        /// Extract include statements
        /// </summary>
        private void ExtractIncludes()
        {
            Includes.Clear();
            Dependencies.Clear();
            ExternalDependencies.Clear();

            var includePattern = @"^\s*#\s*include\s*[<""]([^>""]+)[>""]";
            var regex = new Regex(includePattern, RegexOptions.Multiline);

            foreach (Match match in regex.Matches(Content))
            {
                var includePath = match.Groups[1].Value;
                var lineNumber = Content.Substring(0, match.Index).Count(c => c == '\n') + 1;
                var isSystem = match.Value.Contains("<");

                var includeInfo = new IncludeInfo
                {
                    Path = includePath,
                    IsSystemInclude = isSystem,
                    LineNumber = lineNumber,
                    Position = match.Index
                };

                Includes[includePath] = includeInfo;

                if (isSystem)
                {
                    ExternalDependencies.Add(includePath);
                }
                else
                {
                    Dependencies.Add(includePath);
                }
            }
        }

        /// <summary>
        /// Update file statistics
        /// </summary>
        private void UpdateStatistics()
        {
            Statistics.TotalLines = Lines.Count;
            Statistics.NonEmptyLines = Lines.Count(line => !string.IsNullOrWhiteSpace(line));
            Statistics.EmptyLines = Statistics.TotalLines - Statistics.NonEmptyLines;
            Statistics.CommentLines = Comments.Sum(c => c.EndLine - c.StartLine + 1);
            Statistics.CodeLines = Statistics.NonEmptyLines - Statistics.CommentLines;
            Statistics.CharacterCount = Content.Length;
            Statistics.WordCount = Content.Split(new char[] { ' ', '\t', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries).Length;
            Statistics.IncludeCount = Includes.Count;
            Statistics.PreprocessorDirectiveCount = PreprocessorDirectives.Count;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Compute SHA256 hash of content
        /// </summary>
        private string ComputeContentHash(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            using var sha256 = SHA256.Create();
            // Sử dụng EncodingText để lấy Encoding phù hợp khi tính FileSize
            Encoding encoding;
            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                encoding = !string.IsNullOrEmpty(EncodingText)
                    ? Encoding.GetEncoding(EncodingText)
                    : Encoding.UTF8;
            }
            catch
            {
                encoding = Encoding.UTF8;
            }
            var bytes = encoding.GetBytes(content);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Determine file type based on extension
        /// </summary>
        private SourceFileType DetermineFileType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".c" => SourceFileType.CSource,
                ".h" => SourceFileType.CHeader,
                ".cpp" or ".cxx" or ".cc" => SourceFileType.CPPSource,
                ".hpp" or ".hxx" or ".hh" => SourceFileType.CPPHeader,
                ".s" or ".asm" => SourceFileType.Assembly,
                ".inc" => SourceFileType.Include,
                _ => SourceFileType.Unknown
            };
        }

        /// <summary>
        /// Check if file is binary
        /// </summary>
        private bool IsBinaryFile(string filePath)
        {
            try
            {
                var buffer = new byte[8192];
                using var stream = File.OpenRead(filePath);
                var bytesRead = stream.Read(buffer, 0, buffer.Length);

                // Check for null bytes (common in binary files)
                for (int i = 0; i < bytesRead; i++)
                {
                    if (buffer[i] == 0)
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get line at specified number (1-based)
        /// </summary>
        public string GetLine(int lineNumber)
        {
            if (lineNumber < 1 || lineNumber > Lines.Count)
                return string.Empty;

            return Lines[lineNumber - 1];
        }

        /// <summary>
        /// Get lines in specified range (1-based, inclusive)
        /// </summary>
        public List<string> GetLines(int startLine, int endLine)
        {
            if (startLine < 1 || endLine > Lines.Count || startLine > endLine)
                return new List<string>();

            return Lines.Skip(startLine - 1).Take(endLine - startLine + 1).ToList();
        }

        /// <summary>
        /// Search for text in the source file
        /// </summary>
        public List<SearchResult> Search(string searchText, bool caseSensitive = false, bool useRegex = false)
        {
            var results = new List<SearchResult>();
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            if (useRegex)
            {
                try
                {
                    var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    var regex = new Regex(searchText, options);

                    for (int i = 0; i < Lines.Count; i++)
                    {
                        var matches = regex.Matches(Lines[i]);
                        foreach (Match match in matches)
                        {
                            results.Add(new SearchResult
                            {
                                LineNumber = i + 1,
                                ColumnNumber = match.Index + 1,
                                Text = match.Value,
                                Line = Lines[i]
                            });
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Invalid regex, fall back to simple search
                    return Search(searchText, caseSensitive, false);
                }
            }
            else
            {
                for (int i = 0; i < Lines.Count; i++)
                {
                    var line = Lines[i];
                    int index = 0;

                    while ((index = line.IndexOf(searchText, index, comparison)) != -1)
                    {
                        results.Add(new SearchResult
                        {
                            LineNumber = i + 1,
                            ColumnNumber = index + 1,
                            Text = searchText,
                            Line = line
                        });

                        index += searchText.Length;
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Check if content has changed since last hash
        /// </summary>
        public bool HasChanged()
        {
            var currentHash = ComputeContentHash(Content);
            return currentHash != ContentHash;
        }

        /// <summary>
        /// Refresh file information from disk
        /// </summary>
        public void Refresh()
        {
            if (!File.Exists(FilePath))
                return;

            var fileInfo = new FileInfo(FilePath);
            var diskModified = fileInfo.LastWriteTime;

            if (diskModified > LastModified || HasChanged())
            {
                LoadFromFile();
            }
        }

        #endregion

        #region Object Methods

        /// <summary>
        /// Create a deep copy of the source file
        /// </summary>
        public SourceFile Clone()
        {
            var clone = new SourceFile
            {
                Id = Id,
                FilePath = FilePath,
                FileName = FileName,
                Directory = Directory,
                Extension = Extension,
                Content = Content,
                ProcessedContent = ProcessedContent,
                Lines = new List<string>(Lines),
                ProcessedLines = new List<string>(ProcessedLines),
                ContentHash = ContentHash,
                FileType = FileType,
                LastModified = LastModified,
                FileSize = FileSize,
                EncodingText = EncodingText,
                Includes = new Dictionary<string, IncludeInfo>(Includes),
                Dependencies = new List<string>(Dependencies),
                ExternalDependencies = new List<string>(ExternalDependencies),
                PreprocessorDirectives = new List<PreprocessorDirective>(PreprocessorDirectives),
                Comments = new List<CommentBlock>(Comments),
                Statistics = Statistics.Clone(),
                IsDirty = IsDirty,
                IsReadOnly = IsReadOnly,
                LastAnalyzed = LastAnalyzed
            };

            return clone;
        }

        /// <summary>
        /// Get a string representation of the source file
        /// </summary>
        public override string ToString()
        {
            var status = IsDirty ? " (modified)" : "";
            return $"{FileName} ({FileType}){status}";
        }

        /// <summary>
        /// Check equality with another source file
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is SourceFile other)
            {
                return Id == other.Id && ContentHash == other.ContentHash;
            }
            return false;
        }

        /// <summary>
        /// Get hash code
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, ContentHash);
        }

        #endregion
    }

    #region Supporting Classes

    /// <summary>
    /// Information about an include statement
    /// </summary>
    public class IncludeInfo
    {
        public string Path { get; set; } = string.Empty;
        public bool IsSystemInclude { get; set; }
        public int LineNumber { get; set; }
        public int Position { get; set; }
        public bool IsResolved { get; set; }
        public string ResolvedPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Preprocessor directive information
    /// </summary>
    public class PreprocessorDirective
    {
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int Position { get; set; }
    }

    /// <summary>
    /// Comment block information
    /// </summary>
    public class CommentBlock
    {
        public CommentType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public int Length { get; set; }
    }

    /// <summary>
    /// Comment type enumeration
    /// </summary>
    public enum CommentType
    {
        SingleLine,
        MultiLine,
        Documentation
    }

    /// <summary>
    /// Source file statistics
    /// </summary>
    public class SourceFileStatistics
    {
        public int TotalLines { get; set; }
        public int CodeLines { get; set; }
        public int CommentLines { get; set; }
        public int EmptyLines { get; set; }
        public int NonEmptyLines { get; set; }
        public int CharacterCount { get; set; }
        public int WordCount { get; set; }
        public int IncludeCount { get; set; }
        public int PreprocessorDirectiveCount { get; set; }

        public double CommentRatio => TotalLines > 0 ? (double)CommentLines / TotalLines : 0.0;
        public double CodeRatio => TotalLines > 0 ? (double)CodeLines / TotalLines : 0.0;

        public SourceFileStatistics Clone()
        {
            return new SourceFileStatistics
            {
                TotalLines = TotalLines,
                CodeLines = CodeLines,
                CommentLines = CommentLines,
                EmptyLines = EmptyLines,
                NonEmptyLines = NonEmptyLines,
                CharacterCount = CharacterCount,
                WordCount = WordCount,
                IncludeCount = IncludeCount,
                PreprocessorDirectiveCount = PreprocessorDirectiveCount
            };
        }
    }

    /// <summary>
    /// Search result information
    /// </summary>
    public class SearchResult
    {
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }
        public string Text { get; set; } = string.Empty;
        public string Line { get; set; } = string.Empty;
    }

    /// <summary>
    /// Source file analysis result
    /// </summary>
    public class SourceFileAnalysisResult
    {
        public List<Function> Functions { get; set; } = new List<Function>();
        public List<Variable> Variables { get; set; } = new List<Variable>();
        public List<StructDefinition> Structs { get; set; } = new List<StructDefinition>();
        public List<UnionDefinition> Unions { get; set; } = new List<UnionDefinition>();
        public List<EnumDefinition> Enums { get; set; } = new List<EnumDefinition>();
        public List<MacroDefinition> Macros { get; set; } = new List<MacroDefinition>();
        public List<TypedefDefinition> Typedefs { get; set; } = new List<TypedefDefinition>();
        public List<CodeElement> AllElements { get; set; } = new List<CodeElement>();

        public DateTime AnalysisTimestamp { get; set; }
        public bool IsComplete { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    #endregion
}