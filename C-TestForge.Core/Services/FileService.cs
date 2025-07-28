using C_TestForge.Core.Interfaces.ProjectManagement;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Services
{
    #region FileService Implementation

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
        public async Task<string> ReadFileAsync(string filePath)
        {
            try
            {
                _logger.LogDebug($"Reading file: {filePath}");

                if (string.IsNullOrEmpty(filePath))
                {
                    throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
                }

                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"File not found: {filePath}");
                }

                return await File.ReadAllTextAsync(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reading file: {filePath}");
                throw;
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
    }

    #endregion
}
