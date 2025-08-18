using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Projects
{
    /// <summary>
    /// Giao di?n d?ch v? qu?t t?p C/C++ v? x?y d?ng ?? th? ph? thu?c
    /// </summary>
    public interface IFileScannerService
    {
        /// <summary>
        /// Qu?t m?t th? m?c ?? t?m t?t c? c?c t?p C (.c) v? header (.h)
        /// </summary>
        /// <param name="directoryPath">???ng d?n th? m?c c?n qu?t</param>
        /// <param name="recursive">Qu?t ?? quy c?c th? m?c con</param>
        /// <returns>Danh s?ch t?t c? c?c t?p C v? header ???c t?m th?y</returns>
        Task<List<string>> ScanDirectoryForCFilesAsync(string directoryPath, bool recursive = true);

        /// <summary>
        /// T?m t?t c? c?c th? m?c include ti?m n?ng trong m?t d? ?n
        /// </summary>
        /// <param name="rootDirectoryPath">???ng d?n th? m?c g?c c?a d? ?n</param>
        /// <returns>Danh s?ch c?c th? m?c c? ch?a t?p header</returns>
        Task<List<string>> FindPotentialIncludeDirectoriesAsync(List<string> rootDirectoryPath);

        /// <summary>
        /// T?m m?t t?p header d?a v?o ???ng d?n include
        /// </summary>
        /// <param name="includePath">???ng d?n include (v? d?: "stdio.h" ho?c "mylib/utils.h")</param>
        /// <param name="searchDirectories">Danh s?ch c?c th? m?c c?n t?m ki?m</param>
        /// <param name="currentFilePath">???ng d?n c?a t?p hi?n t?i, ?? t?m header t??ng ??i</param>
        /// <returns>???ng d?n ??y ?? ??n t?p header, null n?u kh?ng t?m th?y</returns>
        Task<string> FindIncludeFileAsync(string includePath, List<string> searchDirectories, string currentFilePath = null);

        /// <summary>
        /// Ph?n t?ch c?c c?u l?nh include t? m?t t?p m? ngu?n
        /// </summary>
        /// <param name="filePath">???ng d?n ??n t?p m? ngu?n</param>
        /// <returns>Danh s?ch c?c ???ng d?n include t? t?p n?y</returns>
        Task<List<IncludeStatement>> ParseIncludeStatementsAsync(string filePath);

        /// <summary>
        /// X?y d?ng ?? th? ph? thu?c include cho m?t t?p h?p c?c t?p
        /// </summary>
        /// <param name="filePaths">Danh s?ch c?c ???ng d?n t?p ?? ph?n t?ch</param>
        /// <param name="includePaths">Danh s?ch c?c th? m?c include ?? t?m ki?m</param>
        /// <returns>?? th? ph? thu?c include</returns>
        Task<IncludeDependencyGraph> BuildIncludeDependencyGraphAsync(List<string> filePaths, List<string> includePaths);

        /// <summary>
        /// Ph?n t?ch c?c directive ti?n x? l? ?i?u ki?n (#if, #ifdef, v.v.) t? m?t t?p
        /// </summary>
        /// <param name="filePath">???ng d?n ??n t?p</param>
        /// <returns>Danh s?ch c?c directive ti?n x? l?</returns>
        Task<List<ConditionalBlock>> ParsePreprocessorConditionalsAsync(string filePath);
    }

}