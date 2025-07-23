using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models.TestCases;

namespace C_TestForge.TestCase.Services
{
    public interface ITestCaseService
    {
        // CRUD operations
        Task<List<TestCaseCustom>> GetAllTestCasesAsync();
        Task<TestCaseCustom> GetTestCaseByIdAsync(Guid id);
        Task<TestCaseCustom> CreateTestCaseAsync(TestCaseCustom testCase);
        Task<TestCaseCustom> UpdateTestCaseAsync(TestCaseCustom testCase);
        Task<bool> DeleteTestCaseAsync(Guid id);

        // Import/Export
        Task<List<TestCaseCustom>> ImportFromTstFileAsync(string filePath);
        Task<List<TestCaseCustom>> ImportFromCsvFileAsync(string filePath);
        Task<List<TestCaseCustom>> ImportFromExcelFileAsync(string filePath);

        Task ExportToTstFileAsync(List<TestCaseCustom> testCases, string filePath);
        Task ExportToCsvFileAsync(List<TestCaseCustom> testCases, string filePath);
        Task ExportToExcelFileAsync(List<TestCaseCustom> testCases, string filePath);

        // Comparison
        Task<TestCaseComparisonResult> CompareTestCasesAsync(TestCaseCustom testCase1, TestCaseCustom testCase2);
        Task<TestCaseCoverageResult> AnalyzeTestCaseCoverageAsync(List<TestCaseCustom> testCases, CFunction function);
    }
}
