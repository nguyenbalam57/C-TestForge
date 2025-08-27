using C_TestForge.Models.Projects;
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
        Task<string> ReadFileAsync(string filePath, string encodingName = null);

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
        /// Checks if a directory exists
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>True if the file exists, false otherwise</returns>
        public bool DirectoryExists(string filePath);

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

        /// <summary>
        /// Tìm kiếm tất cả các file trong một thư mục với phần mở rộng cụ thể
        /// </summary>
        /// <param name="directoryPath">Đường đẫn thư mục</param>
        /// <param name="extension">Phần mở rộng cần tìm kiếm</param>
        /// <param name="recursive">Tìm kiếm tất cả file trong thư mục gốc hoặc tìm kiếm tất cả file trong những thư mục con</param>
        /// <returns></returns>
        public List<string> GetFiles(string directoryPath, string extension = "*", bool recursive = false);

        /// <summary>
        /// Gets the last modified time of a file
        /// </summary>
        /// <param name="filePath">Path to the file</param>
        /// <returns>Last modified time</returns>
        DateTime GetLastModifiedTime(string filePath);

        /// <summary>
        /// Creates a directory if it doesn't exist
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <returns>True if the directory exists or was created, false otherwise</returns>
        bool CreateDirectoryIfNotExists(string directoryPath);

        /// <summary>
        /// Gets all files in a directory with a specific extension
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <param name="extension">File extension to filter by (e.g., ".c")</param>
        /// <param name="recursive">Whether to search subdirectories</param>
        /// <returns>List of file paths</returns>
        List<string> GetFilesInDirectory(string directoryPath, string extension, bool recursive = false);

        /// <summary>
        /// Deletes a directory and all its contents.
        /// </summary>
        /// <param name="directoryPath">Path to the directory</param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> DeleteDirectoryAsync(string directoryPath);
    }
}