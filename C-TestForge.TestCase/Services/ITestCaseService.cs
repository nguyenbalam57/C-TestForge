using C_TestForge.Models;
using C_TestForge.Models.TestCases;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace C_TestForge.TestCase.Services
{
    public interface ITestCaseService
    {
        // CRUD operations
        Task<List<Models.TestCases.TestCase>> GetAllTestCasesAsync();
        Task<Models.TestCases.TestCase> GetTestCaseByIdAsync(Guid id);
        Task<List<Models.TestCases.TestCase>> GetTestCasesByFunctionNameAsync(string functionName);
        Task<Models.TestCases.TestCase> CreateTestCaseAsync(Models.TestCases.TestCase testCase);
        Task<Models.TestCases.TestCase> UpdateTestCaseAsync(Models.TestCases.TestCase testCase);
        Task<bool> DeleteTestCaseAsync(Guid id);
        Task<bool> DeleteAllTestCasesAsync();

        // Import/Export
        Task<List<Models.TestCases.TestCase>> ImportFromTstFileAsync(string filePath);
        Task<List<Models.TestCases.TestCase>> ImportFromCsvFileAsync(string filePath);
        Task<List<Models.TestCases.TestCase>> ImportFromExcelFileAsync(string filePath);

        Task ExportToTstFileAsync(List<Models.TestCases.TestCase> testCases, string filePath);
        Task ExportToCsvFileAsync(List<Models.TestCases.TestCase> testCases, string filePath);
        Task ExportToExcelFileAsync(List<Models.TestCases.TestCase> testCases, string filePath);

        // Analysis
        Task<TestCaseComparisonResult> CompareTestCasesAsync(Models.TestCases.TestCase testCase1, Models.TestCases.TestCase testCase2);
        Task<TestCaseCoverageResult> AnalyzeTestCaseCoverageAsync(List<Models.TestCases.TestCase> testCases, CFunction function);

        // Test Case Generation
        Task<Models.TestCases.TestCase> GenerateUnitTestCaseAsync(CFunction function);
        Task<Models.TestCases.TestCase> GenerateIntegrationTestCaseAsync(List<CFunction> functions);
    }
}