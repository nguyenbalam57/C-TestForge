using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for CSV file operations
    /// </summary>
    public interface ICsvService
    {
        /// <summary>
        /// Reads test cases from a CSV file
        /// </summary>
        Task<IEnumerable<TestCaseModels>> ReadTestCasesAsync(string filePath);

        /// <summary>
        /// Writes test cases to a CSV file
        /// </summary>
        Task<bool> WriteTestCasesAsync(IEnumerable<TestCaseModels> testCases, string filePath);
    }
}
