using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using C_TestForge.Models;
using C_TestForge.Models.TestCases;

namespace C_TestForge.TestCase.Services
{
    public interface ITestCaseService
    {
        // CRUD operations
        Task<List<TestCaseUser>> GetAllTestCasesAsync();
        Task<TestCaseUser> GetTestCaseByIdAsync(Guid id);
        Task<List<TestCaseUser>> GetTestCasesByFunctionNameAsync(string functionName);
        Task<TestCaseUser> CreateTestCaseAsync(TestCaseUser testCase);
        Task<TestCaseUser> UpdateTestCaseAsync(TestCaseUser testCase);
        Task<bool> DeleteTestCaseAsync(Guid id);
        Task<bool> DeleteAllTestCasesAsync();

        // Import/Export
        Task<List<TestCaseUser>> ImportFromTstFileAsync(string filePath);
        Task<List<TestCaseUser>> ImportFromCsvFileAsync(string filePath);
        Task<List<TestCaseUser>> ImportFromExcelFileAsync(string filePath);

        Task ExportToTstFileAsync(List<TestCaseUser> testCases, string filePath);
        Task ExportToCsvFileAsync(List<TestCaseUser> testCases, string filePath);
        Task ExportToExcelFileAsync(List<TestCaseUser> testCases, string filePath);

        // Analysis
        Task<TestCaseComparisonResult> CompareTestCasesAsync(TestCaseUser testCase1, TestCaseUser testCase2);
        Task<TestCaseCoverageResult> AnalyzeTestCaseCoverageAsync(List<TestCaseUser> testCases, CFunction function);

        // Test Case Generation
        Task<TestCaseUser> GenerateUnitTestCaseAsync(CFunction function);
        Task<TestCaseUser> GenerateIntegrationTestCaseAsync(List<CFunction> functions);
    }
}