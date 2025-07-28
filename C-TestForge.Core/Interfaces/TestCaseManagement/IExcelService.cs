using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for Excel file operations
    /// </summary>
    public interface IExcelService
    {
        /// <summary>
        /// Reads test cases from an Excel file
        /// </summary>
        Task<IEnumerable<TestCaseModels>> ReadTestCasesAsync(string filePath, string sheetName = null);

        /// <summary>
        /// Writes test cases to an Excel file
        /// </summary>
        Task<bool> WriteTestCasesAsync(IEnumerable<TestCaseModels> testCases, string filePath, string sheetName = null);
    }
}
