using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Parser
{
    /// <summary>
    /// Interface for managing source code files
    /// </summary>
    public interface ISourceCodeService
    {
        /// <summary>
        /// Loads a source file from disk
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>Source file object</returns>
        Task<SourceFile> LoadSourceFileAsync(string filePath);

        /// <summary>
        /// Saves a source file to disk
        /// </summary>
        /// <param name="sourceFile">Source file to save</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> SaveSourceFileAsync(SourceFile sourceFile);

        /// <summary>
        /// Creates a backup of a source file
        /// </summary>
        /// <param name="sourceFile">Source file to backup</param>
        /// <returns>Backup source file object</returns>
        Task<SourceFile> CreateBackupAsync(SourceFile sourceFile);

        /// <summary>
        /// Extracts includes from a source file
        /// </summary>
        /// <param name="sourceFile">Source file to analyze</param>
        /// <returns>Dictionary of include paths and their types</returns>
        Task<Dictionary<string, string>> ExtractIncludesAsync(SourceFile sourceFile);

        /// <summary>
        /// Gets a specific line of code from a source file
        /// </summary>
        /// <param name="sourceFile">Source file</param>
        /// <param name="lineNumber">Line number (1-based)</param>
        /// <returns>The line of code</returns>
        string GetLineOfCode(SourceFile sourceFile, int lineNumber);

        /// <summary>
        /// Finds all usages of a variable in a source file
        /// </summary>
        /// <param name="sourceFile">Source file to search in</param>
        /// <param name="variable">Variable to find</param>
        /// <returns>Dictionary of source files and line numbers</returns>
        Task<Dictionary<string, HashSet<int>>> FindVariableUsagesAsync(SourceFile sourceFile, CVariable variable);

        /// <summary>
        /// Finds all usages of a function in a source file
        /// </summary>
        /// <param name="sourceFile">Source file to search in</param>
        /// <param name="function">Function to find</param>
        /// <returns>Dictionary of source files and line numbers</returns>
        Task<Dictionary<string, HashSet<int>>> FindFunctionUsagesAsync(SourceFile sourceFile, CFunction function);

        /// <summary>
        /// Updates the content of a source file
        /// </summary>
        /// <param name="sourceFile">Source file to update</param>
        /// <param name="newContent">New content for the source file</param>
        /// <returns>Updated source file</returns>
        Task<SourceFile> UpdateSourceFileContentAsync(SourceFile sourceFile, string newContent);

        /// <summary>
        /// Finds definitions, declarations, and usages of a symbol in a source file
        /// </summary>
        /// <param name="sourceFile">Source file to search in</param>
        /// <param name="symbolName">Name of the symbol to find</param>
        /// <returns>Dictionary of source files and line numbers</returns>
        Task<Dictionary<string, List<SymbolOccurrence>>> FindSymbolOccurrencesAsync(SourceFile sourceFile, string symbolName);
    }

    /// <summary>
    /// Types of symbol occurrences
    /// </summary>
    public enum SymbolOccurrenceType
    {
        Definition,
        Declaration,
        Usage
    }

    /// <summary>
    /// Represents an occurrence of a symbol in source code
    /// </summary>
    public class SymbolOccurrence
    {
        /// <summary>
        /// Type of occurrence
        /// </summary>
        public SymbolOccurrenceType Type { get; set; }

        /// <summary>
        /// Line number of the occurrence
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Column number of the occurrence
        /// </summary>
        public int ColumnNumber { get; set; }

        /// <summary>
        /// Context of the occurrence (the line of code)
        /// </summary>
        public string Context { get; set; }
    }
}
