using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace C_TestForge.UI.Utilities
{
    /// <summary>
    /// L?p ti?n ?ch x? l? encoding cho ti?ng Vi?t v? ti?ng Nh?t
    /// </summary>
    public static class EncodingHelper
    {
        // Danh s?ch c?c encoding th??ng d?ng
        private static readonly Dictionary<string, Encoding> CommonEncodings = new Dictionary<string, Encoding>
        {
            { "UTF-8", Encoding.UTF8 },
            { "Unicode", Encoding.Unicode }, // UTF-16 Little Endian
            { "UTF-16BE", Encoding.BigEndianUnicode },
            { "Windows-1252", Encoding.GetEncoding(1252) }, // Western European
            { "Windows-1258", Encoding.GetEncoding(1258) }, // Vietnamese
            { "Shift-JIS", Encoding.GetEncoding(932) },    // Japanese
            { "EUC-JP", Encoding.GetEncoding(51932) },     // Japanese
            { "ISO-2022-JP", Encoding.GetEncoding(50220) } // Japanese
        };

        /// <summary>
        /// Ph?t hi?n encoding c?a m?t file
        /// </summary>
        /// <param name="filePath">???ng d?n ??n file</param>
        /// <returns>Encoding ???c ph?t hi?n, m?c ??nh l? UTF-8 n?u kh?ng x?c ??nh ???c</returns>
        public static Encoding DetectFileEncoding(string filePath)
        {
            // ??c bytes t? file
            byte[] bytes;
            try
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                bytes = new byte[Math.Min(fileStream.Length, 4096)]; // ??c t?i ?a 4KB
                fileStream.Read(bytes, 0, bytes.Length);
            }
            catch (Exception)
            {
                return Encoding.UTF8; // M?c ??nh n?u kh?ng ??c ???c file
            }

            return DetectEncoding(bytes);
        }

        /// <summary>
        /// Ph?t hi?n encoding t? m?ng bytes
        /// </summary>
        /// <param name="bytes">M?ng bytes c?n ph?t hi?n encoding</param>
        /// <returns>Encoding ???c ph?t hi?n, m?c ??nh l? UTF-8</returns>
        public static Encoding DetectEncoding(byte[] bytes)
        {
            // Ph?t hi?n BOM (Byte Order Mark)
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8;
            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode;
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode;
            if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF)
                return Encoding.UTF32;

            // Ki?m tra UTF-8 h?p l?
            if (IsValidUtf8(bytes))
                return Encoding.UTF8;

            // Ph?t hi?n Shift-JIS (ti?ng Nh?t)
            if (ContainsJapaneseCharacters(bytes))
                return Encoding.GetEncoding(932); // Shift-JIS

            // Ph?t hi?n Windows-1258 (ti?ng Vi?t)
            if (ContainsVietnameseCharacters(bytes))
                return Encoding.GetEncoding(1258);

            // M?c ??nh l? UTF-8
            return Encoding.UTF8;
        }

        /// <summary>
        /// Ki?m tra chu?i bytes c? ph?i UTF-8 h?p l? kh?ng
        /// </summary>
        private static bool IsValidUtf8(byte[] bytes)
        {
            int i = 0;
            while (i < bytes.Length)
            {
                if (bytes[i] <= 0x7F)
                {
                    // ASCII
                    i++;
                    continue;
                }
                
                // UTF-8 multi-byte character
                int extraBytes;
                if ((bytes[i] & 0xE0) == 0xC0) // 2 bytes
                    extraBytes = 1;
                else if ((bytes[i] & 0xF0) == 0xE0) // 3 bytes
                    extraBytes = 2;
                else if ((bytes[i] & 0xF8) == 0xF0) // 4 bytes
                    extraBytes = 3;
                else
                    return false; // Invalid UTF-8 start byte
                
                // Check continuation bytes (should be 10xxxxxx)
                for (int j = 1; j <= extraBytes; j++)
                {
                    if (i + j >= bytes.Length)
                        return false; // Unexpected end
                    
                    if ((bytes[i + j] & 0xC0) != 0x80)
                        return false; // Invalid continuation byte
                }
                
                i += 1 + extraBytes;
            }
            
            return true;
        }

        /// <summary>
        /// Ki?m tra n?u d? li?u c? th? ch?a k? t? ti?ng Nh?t
        /// </summary>
        private static bool ContainsJapaneseCharacters(byte[] bytes)
        {
            try
            {
                // Th? decode v?i Shift-JIS v? t?m c?c k? t? trong range c?a ti?ng Nh?t
                string text = Encoding.GetEncoding(932).GetString(bytes);
                return text.Any(c => 
                    (c >= 0x3040 && c <= 0x309F) || // Hiragana
                    (c >= 0x30A0 && c <= 0x30FF) || // Katakana
                    (c >= 0x4E00 && c <= 0x9FFF));  // Kanji
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ki?m tra n?u d? li?u c? th? ch?a k? t? ti?ng Vi?t
        /// </summary>
        private static bool ContainsVietnameseCharacters(byte[] bytes)
        {
            try
            {
                // Th? decode v?i Windows-1258 v? t?m c?c k? t? ??c tr?ng ti?ng Vi?t
                string text = Encoding.GetEncoding(1258).GetString(bytes);
                return text.Any(c => "????????????????????????????????????????????????????????????????????".Contains(c));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// ??c n?i dung file text v?i encoding t? ??ng ph?t hi?n
        /// </summary>
        /// <param name="filePath">???ng d?n ??n file</param>
        /// <returns>N?i dung c?a file d??i d?ng chu?i</returns>
        public static string ReadAllText(string filePath)
        {
            Encoding encoding = DetectFileEncoding(filePath);
            return File.ReadAllText(filePath, encoding);
        }

        /// <summary>
        /// ??c n?i dung file text kh?ng ??ng b? v?i encoding t? ??ng ph?t hi?n
        /// </summary>
        /// <param name="filePath">???ng d?n ??n file</param>
        /// <returns>N?i dung c?a file d??i d?ng chu?i</returns>
        public static async System.Threading.Tasks.Task<string> ReadAllTextAsync(string filePath)
        {
            Encoding encoding = DetectFileEncoding(filePath);
            return await File.ReadAllTextAsync(filePath, encoding);
        }

        /// <summary>
        /// Ghi n?i dung v?o file v?i encoding UTF-8 c? BOM
        /// </summary>
        /// <param name="filePath">???ng d?n ??n file</param>
        /// <param name="content">N?i dung c?n ghi</param>
        public static void WriteAllText(string filePath, string content)
        {
            File.WriteAllText(filePath, content, new UTF8Encoding(true));
        }

        /// <summary>
        /// Ghi n?i dung v?o file kh?ng ??ng b? v?i encoding UTF-8 c? BOM
        /// </summary>
        /// <param name="filePath">???ng d?n ??n file</param>
        /// <param name="content">N?i dung c?n ghi</param>
        public static async System.Threading.Tasks.Task WriteAllTextAsync(string filePath, string content)
        {
            await File.WriteAllTextAsync(filePath, content, new UTF8Encoding(true));
        }
    }
}