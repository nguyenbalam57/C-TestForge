using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Service for importing test cases
    /// </summary>
    public interface IImportService
    {
        /// <summary>
        /// Imports test cases from a .tst file
        /// </summary>
        Task<IEnumerable<TestCaseModels>> ImportFromTstFileAsync(string filePath);

        /// <summary>
        /// Imports test cases from a .csv file
        /// </summary>
        Task<IEnumerable<TestCaseModels>> ImportFromCsvFileAsync(string filePath);

        /// <summary>
        /// Imports test cases from an Excel file
        /// </summary>
        Task<IEnumerable<TestCaseModels>> ImportFromExcelFileAsync(string filePath, string sheetName = null);
    }
}
