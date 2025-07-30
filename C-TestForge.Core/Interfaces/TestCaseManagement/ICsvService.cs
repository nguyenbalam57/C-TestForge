using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;
using C_TestForge.Models.TestCases;

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
        Task<IEnumerable<TestCase>> ReadTestCasesAsync(string filePath);

        /// <summary>
        /// Writes test cases to a CSV file
        /// </summary>
        Task<bool> WriteTestCasesAsync(IEnumerable<TestCase> testCases, string filePath);
    }
}
