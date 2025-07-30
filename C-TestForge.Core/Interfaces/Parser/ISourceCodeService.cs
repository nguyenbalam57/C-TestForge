using C_TestForge.Models.Projects;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Parser
{
    /// <summary>
    /// Interface for source code service
    /// </summary>
    public interface ISourceCodeService
    {
        /// <summary>
        /// Loads a source file from the specified path
        /// </summary>
        /// <param name="filePath">Path to the source file</param>
        /// <returns>Source file object containing content, lines, and metadata</returns>
        Task<SourceFile> LoadSourceFileAsync(string filePath);
    }
}