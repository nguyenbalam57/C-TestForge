using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.ProjectManagement
{
    /// <summary>
    /// Interface for file system operations
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Reads a file from disk
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>File content</returns>
        Task<string> ReadFileAsync(string filePath);

        /// <summary>
        /// Writes content to a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="content">Content to write</param>
        /// <returns>Task</returns>
        Task WriteFileAsync(string filePath, string content);

        /// <summary>
        /// Copies a file
        /// </summary>
        /// <param name="sourceFilePath">Source file path</param>
        /// <param name="destinationFilePath">Destination file path</param>
        /// <returns>Task</returns>
        Task CopyFileAsync(string sourceFilePath, string destinationFilePath);

        /// <summary>
        /// Deletes a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteFileAsync(string filePath);

        /// <summary>
        /// Checks if a file exists
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if the file exists, false otherwise</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// Reads a file as bytes
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>File content as bytes</returns>
        Task<byte[]> ReadFileBytesAsync(string filePath);

        /// <summary>
        /// Writes bytes to a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <param name="content">Content to write</param>
        /// <returns>Task</returns>
        Task WriteFileBytesAsync(string filePath, byte[] content);

        /// <summary>
        /// Gets the filename from a path
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Filename</returns>
        string GetFileName(string filePath);

        /// <summary>
        /// Gets the filename without extension from a path
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Filename without extension</returns>
        string GetFileNameWithoutExtension(string filePath);

        /// <summary>
        /// Gets the file extension from a path
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>File extension</returns>
        string GetFileExtension(string filePath);

        /// <summary>
        /// Gets the directory name from a path
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <returns>Directory name</returns>
        string GetDirectoryName(string filePath);
    }
}
