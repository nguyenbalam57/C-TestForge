using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace C_TestForge.UI.Utilities
{
    /// <summary>
    /// L?p ti?n ?ch qu?n l? font ch? cho ?ng d?ng, h? tr? ?a ng?n ng?
    /// </summary>
    public static class FontUtils
    {
        // Danh s?ch c?c font t?t h? tr? Unicode, ti?ng Vi?t v? ti?ng Nh?t
        private static readonly string[] MultilanguageFonts = new[]
        {
            "Segoe UI",         // H? tr? t?t ti?ng Vi?t v? ti?ng Nh?t (Windows m?c ??nh)
            "Arial",            // H? tr? t?t ti?ng Vi?t
            "Microsoft Sans Serif",
            "Yu Gothic UI",     // Font ti?ng Nh?t c?a Microsoft
            "Meiryo UI",        // Font ti?ng Nh?t c?a Microsoft
            "MS Gothic",        // Font ti?ng Nh?t c? b?n
            "Noto Sans CJK JP", // Google Noto font h? tr? Nh?t 
            "Noto Sans CJK",    // Google Noto font cho CJK
            "Noto Sans",        // Google Noto font c? b?n
            "Tahoma",           // H? tr? t?t ti?ng Vi?t
            "Verdana"           // H? tr? t?t ti?ng Vi?t
        };

        /// <summary>
        /// Font fallback string s? ???c s? d?ng l?m m?c ??nh cho ?ng d?ng
        /// </summary>
        public static readonly string DefaultFontFamily = CreateFontFallbackString();

        /// <summary>
        /// Font cho m? ngu?n (monospace)
        /// </summary>
        public static readonly string CodeFontFamily = CreateCodeFontFallbackString();

        /// <summary>
        /// Ki?m tra v? t?o chu?i font fallback
        /// </summary>
        public static string CreateFontFallbackString()
        {
            List<string> availableFonts = new List<string>();
            
            // Ki?m tra t?ng font trong danh s?ch
            foreach (string fontName in MultilanguageFonts)
            {
                if (IsFontAvailable(fontName))
                {
                    availableFonts.Add(fontName);
                }
            }

            // Th?m font m?c ??nh cu?i c?ng
            if (!availableFonts.Contains("Global User Interface"))
            {
                availableFonts.Add("Global User Interface");
            }

            // T?o chu?i font fallback
            return string.Join(", ", availableFonts);
        }

        /// <summary>
        /// T?o danh s?ch font monospace cho code
        /// </summary>
        public static string CreateCodeFontFallbackString()
        {
            List<string> codeFonts = new List<string>
            {
                "Consolas", "Courier New", "Monospace"
            };

            return string.Join(", ", codeFonts.Where(IsFontAvailable));
        }

        /// <summary>
        /// Ki?m tra xem font c? kh? d?ng tr?n h? th?ng hay kh?ng
        /// </summary>
        public static bool IsFontAvailable(string fontName)
        {
            try
            {
                // Th? t?o FontFamily ?? ki?m tra font c? t?n t?i
                var fontFamily = new FontFamily(fontName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ki?m tra xem h? th?ng c? h? tr? c?c font c?n thi?t cho ti?ng Vi?t v? ti?ng Nh?t kh?ng
        /// </summary>
        public static void ValidateFontSupport()
        {
            bool hasVietnameseSupport = false;
            bool hasJapaneseSupport = false;

            // Ki?m tra font ti?ng Vi?t
            foreach (var font in new[] { "Segoe UI", "Arial", "Tahoma" })
            {
                if (IsFontAvailable(font))
                {
                    hasVietnameseSupport = true;
                    break;
                }
            }

            // Ki?m tra font ti?ng Nh?t
            foreach (var font in new[] { "Yu Gothic UI", "Meiryo UI", "MS Gothic", "Noto Sans CJK JP" })
            {
                if (IsFontAvailable(font))
                {
                    hasJapaneseSupport = true;
                    break;
                }
            }

            // Hi?n th? c?nh b?o n?u kh?ng c? font h? tr?
            if (!hasVietnameseSupport)
            {
                MessageBox.Show(
                    "Kh?ng t?m th?y font h? tr? t?t cho ti?ng Vi?t tr?n h? th?ng. " +
                    "M?t s? k? t? c? th? kh?ng hi?n th? ch?nh x?c.",
                    "C?nh b?o font ch?",
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }

            if (!hasJapaneseSupport)
            {
                MessageBox.Show(
                    "Kh?ng t?m th?y font h? tr? t?t cho ti?ng Nh?t tr?n h? th?ng. " +
                    "K? t? ti?ng Nh?t c? th? kh?ng hi?n th? ch?nh x?c.",
                    "C?nh b?o font ch?",
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
            }
        }
    }
}