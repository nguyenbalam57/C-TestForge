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
        Task<List<string>> FindPotentialIncludeDirectoriesAsync(string rootDirectoryPath);

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

    /// <summary>
    /// ??i di?n cho m?t c?u l?nh include trong m? ngu?n
    /// </summary>
    public class IncludeStatement
    {
        /// <summary>
        /// T?n t?p ???c include
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// ???ng d?n include ??y ?? nh? trong m? ngu?n (v? d?: "stdio.h" ho?c <stdlib.h>)
        /// </summary>
        public string RawIncludePath { get; set; }

        /// <summary>
        /// ???ng d?n include ???c chu?n h?a (kh?ng c? d?u ngo?c k?p ho?c <>)
        /// </summary>
        public string NormalizedIncludePath { get; set; }

        /// <summary>
        /// ???ng d?n ??y ?? ??n t?p ???c include (n?u ???c gi?i quy?t)
        /// </summary>
        public string ResolvedPath { get; set; }

        /// <summary>
        /// True n?u ??y l? include h? th?ng (<>), false n?u l? include d? ?n ("")
        /// </summary>
        public bool IsSystemInclude { get; set; }

        /// <summary>
        /// S? d?ng trong t?p ngu?n
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Kh?i ?i?u ki?n ch?a c?u l?nh include n?y (n?u c?)
        /// </summary>
        public ConditionalBlock Conditional { get; set; }
    }

    /// <summary>
    /// ??i di?n cho m?t kh?i ?i?u ki?n ti?n x? l?
    /// </summary>
    public class ConditionalBlock
    {
        /// <summary>
        /// Lo?i directive (#if, #ifdef, #ifndef, v.v.)
        /// </summary>
        public string DirectiveType { get; set; }

        /// <summary>
        /// Bi?u th?c ?i?u ki?n (v? d?: "DEBUG" trong #ifdef DEBUG)
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// S? d?ng b?t ??u c?a kh?i
        /// </summary>
        public int StartLine { get; set; }

        /// <summary>
        /// S? d?ng k?t th?c c?a kh?i
        /// </summary>
        public int EndLine { get; set; }

        /// <summary>
        /// Danh s?ch c?c kh?i con
        /// </summary>
        public List<ConditionalBlock> NestedBlocks { get; set; } = new List<ConditionalBlock>();

        /// <summary>
        /// Danh s?ch c?c c?u l?nh include trong kh?i n?y
        /// </summary>
        public List<IncludeStatement> Includes { get; set; } = new List<IncludeStatement>();

        /// <summary>
        /// Kh?i ?i?u ki?n cha (n?u ???c l?ng gh?p)
        /// </summary>
        public ConditionalBlock Parent { get; set; }
    }

    /// <summary>
    /// ?? th? ph? thu?c include
    /// </summary>
    public class IncludeDependencyGraph
    {
        /// <summary>
        /// Danh s?ch t?t c? c?c t?p ???c ph?n t?ch
        /// </summary>
        public List<SourceFileDependency> SourceFiles { get; set; } = new List<SourceFileDependency>();

        /// <summary>
        /// Danh s?ch c?c th? m?c include ???c d?ng ?? gi?i quy?t c?c ???ng d?n
        /// </summary>
        public List<string> IncludePaths { get; set; } = new List<string>();

        /// <summary>
        /// T?m t?p ngu?n theo ???ng d?n
        /// </summary>
        public SourceFileDependency FindFile(string path)
        {
            return SourceFiles.FirstOrDefault(f => string.Equals(f.FilePath, path, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Th?m m?t t?p ngu?n v?o ?? th?
        /// </summary>
        public SourceFileDependency AddSourceFile(string path)
        {
            var existing = FindFile(path);
            if (existing != null)
                return existing;

            var newFile = new SourceFileDependency { FilePath = path };
            SourceFiles.Add(newFile);
            return newFile;
        }
    }

    /// <summary>
    /// ??i di?n cho m?t t?p ngu?n trong ?? th? ph? thu?c
    /// </summary>
    public class SourceFileDependency
    {
        /// <summary>
        /// ???ng d?n ??n t?p ngu?n
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Danh s?ch c?c c?u l?nh include trong t?p n?y
        /// </summary>
        public List<IncludeStatement> Includes { get; set; } = new List<IncludeStatement>();

        /// <summary>
        /// Danh s?ch c?c t?p ph? thu?c tr?c ti?p
        /// </summary>
        public List<SourceFileDependency> DirectDependencies { get; set; } = new List<SourceFileDependency>();

        /// <summary>
        /// Danh s?ch c?c t?p ph? thu?c v?o t?p n?y
        /// </summary>
        public List<SourceFileDependency> DependentFiles { get; set; } = new List<SourceFileDependency>();

        /// <summary>
        /// Danh s?ch c?c kh?i ?i?u ki?n ti?n x? l?
        /// </summary>
        public List<ConditionalBlock> ConditionalBlocks { get; set; } = new List<ConditionalBlock>();

        /// <summary>
        /// Lo?i t?p ngu?n (C, Header, v.v.)
        /// </summary>
        public SourceFileType FileType { get; set; }

        /// <summary>
        /// T?n t?p
        /// </summary>
        public string FileName => Path.GetFileName(FilePath);

        /// <summary>
        /// ???c ph?n t?ch ch?a
        /// </summary>
        public bool Parsed { get; set; }
    }
}