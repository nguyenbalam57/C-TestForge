using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for exporting test cases
    /// </summary>
    public interface IExportService
    {
        /// <summary>
        /// Exports test cases to a .tst file
        /// </summary>
        Task<bool> ExportToTstFileAsync(IEnumerable<TestCaseModels> testCases, string filePath);

        /// <summary>
        /// Exports test cases to a .csv file
        /// </summary>
        Task<bool> ExportToCsvFileAsync(IEnumerable<TestCaseModels> testCases, string filePath);

        /// <summary>
        /// Exports test cases to an Excel file
        /// </summary>
        Task<bool> ExportToExcelFileAsync(IEnumerable<TestCaseModels> testCases, string filePath, string sheetName = null);
    }
}
