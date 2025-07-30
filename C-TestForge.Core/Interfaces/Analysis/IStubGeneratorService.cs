using C_TestForge.Models;
using C_TestForge.Models.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.Analysis
{
    /// <summary>
    /// Service for generating stubs for unit tests
    /// </summary>
    public interface IStubGeneratorService
    {
        /// <summary>
        /// Generates stubs for the given function
        /// </summary>
        /// <param name="functionName">The function name</param>
        /// <param name="filePath">The file path</param>
        /// <returns>Dictionary of function names and their stub implementations</returns>
        Task<Dictionary<string, string>> GenerateStubsAsync(
            string functionName,
            string filePath);

        /// <summary>
        /// Generates a single stub for a function
        /// </summary>
        /// <param name="function">The function to stub</param>
        /// <returns>The stub implementation</returns>
        Task<string> GenerateStubAsync(CFunction function);

        /// <summary>
        /// Generates stub headers for the given functions
        /// </summary>
        /// <param name="functions">The functions to generate headers for</param>
        /// <returns>The stub header file content</returns>
        Task<string> GenerateStubHeadersAsync(IEnumerable<CFunction> functions);
    }
}
