using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using C_TestForge.Models.TestCases;

namespace C_TestForge.TestCase.Repositories
{
    public interface ITestCaseRepository
    {
        Task<List<Models.TestCases.TestCase>> GetAllAsync();
        Task<Models.TestCases.TestCase> GetByIdAsync(Guid id);
        Task<List<Models.TestCases.TestCase>> GetByFunctionNameAsync(string functionName);
        Task<Models.TestCases.TestCase> CreateAsync(Models.TestCases.TestCase testCase);
        Task<Models.TestCases.TestCase> UpdateAsync(Models.TestCases.TestCase testCase);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> DeleteAllAsync();
    }
}
