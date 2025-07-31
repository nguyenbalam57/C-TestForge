using C_TestForge.Core.Interfaces.ProjectManagement;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Parser
{
    /// <summary>
    /// Implementation of the file service
    /// </summary>
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;

        public FileService(ILogger<FileService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        /// <summary>
        /// Đọc nội dung file với phát hiện encoding tự động hoặc theo chỉ định
        /// </summary>
        /// <param name="filePath">Đường dẫn file</param>
        /// <param name="encodingName">Tên encoding, null để tự động phát hiện</param>
        /// <returns>Nội dung file dưới dạng chuỗi</returns>
        public async Task<string> ReadFileAsync(string filePath, string encodingName = null)
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

                Encoding encoding = null;

                // Sử dụng encoding được chỉ định
                if (!string.IsNullOrEmpty(encodingName))
                {
                    try
                    {
                        encoding = Encoding.GetEncoding(encodingName);
                        _logger.LogDebug($"Reading file: {filePath} with specified encoding: {encodingName}");
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogWarning($"Invalid encoding name: {encodingName}. Using auto-detection.");
                        encoding = null;
                    }
                }

                // Nếu không có encoding được chỉ định, tự động phát hiện
                if (encoding == null)
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
                    _logger.LogDebug($"Reading file: {filePath} with auto-detected encoding: {encoding.WebName}");
                }

                // Đọc file với encoding đã xác định
                return await File.ReadAllTextAsync(filePath, encoding);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading file: {filePath}");
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
        /// Phương pháp toàn diện hơn để phát hiện encoding
        /// </summary>
        private Encoding DetectTextEncoding(byte[] buffer)
        {
            try
            {
                // Đảm bảo các encoding bổ sung được đăng ký
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                // Kiểm tra BOM
                if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                    return Encoding.UTF8;
                if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
                    return Encoding.BigEndianUnicode;
                if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
                    return Encoding.Unicode;
                if (buffer.Length >= 4 && buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xFE && buffer[3] == 0xFF)
                    return Encoding.UTF32;

                // Nếu là file nhị phân
                if (ContainsBinaryData(buffer))
                    return Encoding.Default;

                // Kiểm tra tiếng Nhật
                if (ContainsJapaneseCharacters(buffer))
                {
                    try
                    {
                        return Encoding.GetEncoding("shift_jis");
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogWarning("Shift-JIS encoding not available, using UTF-8 instead");
                        return Encoding.UTF8;
                    }
                }

                // Kiểm tra UTF-8 không có BOM
                if (IsValidUtf8(buffer))
                    return Encoding.UTF8;

                // Kiểm tra tiếng Việt
                if (ContainsVietnameseCharacters(buffer))
                {
                    try
                    {
                        return Encoding.GetEncoding("windows-1258");
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogWarning("Windows-1258 encoding not available, using UTF-8 instead");
                        return Encoding.UTF8;
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
                        return Encoding.GetEncoding(1252); // Windows-1252
                    }
                    catch (ArgumentException)
                    {
                        return Encoding.Default;
                    }
                }

                // Mặc định UTF-8
                return Encoding.UTF8;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting text encoding");
                return Encoding.UTF8; // Fallback to UTF-8
            }
        }

        /// <inheritdoc/>
        public async Task WriteFileAsync(string filePath, string content)
        {
            try
            {
                _logger.LogDebug($"Writing file: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    _logger.LogDebug($"Creating directory: {directory}");
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(filePath, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing file: {filePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task CopyFileAsync(string sourceFilePath, string destinationFilePath)
        {
            try
            {
                _logger.LogDebug($"Copying file from {sourceFilePath} to {destinationFilePath}");

                if (string.IsNullOrEmpty(sourceFilePath))
                {
                    throw new ArgumentException("Source file path cannot be null or empty", nameof(sourceFilePath));
                }

                if (string.IsNullOrEmpty(destinationFilePath))
                {
                    throw new ArgumentException("Destination file path cannot be null or empty", nameof(destinationFilePath));
                }

                if (!File.Exists(sourceFilePath))
                {
                    throw new FileNotFoundException($"Source file not found: {sourceFilePath}");
                }

                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(destinationFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    _logger.LogDebug($"Creating directory: {directory}");
                    Directory.CreateDirectory(directory);
                }

                // Read the source file
                string content = await ReadFileAsync(sourceFilePath);

                // Write to the destination file
                await WriteFileAsync(destinationFilePath, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error copying file from {sourceFilePath} to {destinationFilePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteFileAsync(string filePath)
        {
            try
            {
                _logger.LogDebug($"Deleting file: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"File not found for deletion: {filePath}");
                    return false;
                }

                File.Delete(filePath);

                // Verify the file was deleted
                bool deleted = !File.Exists(filePath);

                if (deleted)
                {
                    _logger.LogDebug($"Successfully deleted file: {filePath}");
                }
                else
                {
                    _logger.LogWarning($"Failed to delete file: {filePath}");
                }

                return deleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file: {filePath}");
                return false;
            }
        }

        /// <inheritdoc/>
        public bool FileExists(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                return File.Exists(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if file exists: {filePath}");
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> ReadFileBytesAsync(string filePath)
        {
            try
            {
                _logger.LogDebug($"Reading file bytes: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                return await File.ReadAllBytesAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading file bytes: {filePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task WriteFileBytesAsync(string filePath, byte[] content)
        {
            try
            {
                _logger.LogDebug($"Writing file bytes: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (content == null)
                {
                    throw new ArgumentNullException(nameof(content), "Content cannot be null");
                }

                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    _logger.LogDebug($"Creating directory: {directory}");
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(filePath, content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error writing file bytes: {filePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public string GetFileName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            return Path.GetFileName(filePath);
        }

        /// <inheritdoc/>
        public string GetFileNameWithoutExtension(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            return Path.GetFileNameWithoutExtension(filePath);
        }

        /// <inheritdoc/>
        public string GetFileExtension(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            return Path.GetExtension(filePath);
        }

        /// <inheritdoc/>
        public string GetDirectoryName(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }

            return Path.GetDirectoryName(filePath);
        }

        /// <inheritdoc/>
        public DateTime GetLastModifiedTime(string filePath)
        {
            try
            {
                _logger.LogDebug($"Getting last modified time: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                return File.GetLastWriteTime(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting last modified time: {filePath}");
                throw;
            }
        }

        /// <inheritdoc/>
        public bool CreateDirectoryIfNotExists(string directoryPath)
        {
            try
            {
                _logger.LogDebug($"Creating directory if not exists: {directoryPath}");

                if (string.IsNullOrEmpty(directoryPath))
                {
                    throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));
                }

                if (Directory.Exists(directoryPath))
                {
                    return true;
                }

                Directory.CreateDirectory(directoryPath);
                return Directory.Exists(directoryPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating directory: {directoryPath}");
                return false;
            }
        }

        /// <inheritdoc/>
        public List<string> GetFilesInDirectory(string directoryPath, string extension, bool recursive = false)
        {
            try
            {
                _logger.LogDebug($"Getting files in directory: {directoryPath} with extension {extension}, recursive: {recursive}");

                if (string.IsNullOrEmpty(directoryPath))
                {
                    throw new ArgumentException("Directory path cannot be null or empty", nameof(directoryPath));
                }

                if (!Directory.Exists(directoryPath))
                {
                    throw new DirectoryNotFoundException($"Directory not found: {directoryPath}");
                }

                if (string.IsNullOrEmpty(extension))
                {
                    extension = ".*";
                }
                else if (!extension.StartsWith("."))
                {
                    extension = "." + extension;
                }

                SearchOption searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(directoryPath, $"*{extension}", searchOption).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting files in directory: {directoryPath}");
                return new List<string>();
            }
        }
    }
}