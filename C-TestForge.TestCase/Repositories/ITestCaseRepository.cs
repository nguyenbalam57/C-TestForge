
using C_TestForge.Models.TestCases;

namespace C_TestForge.TestCase.Repositories
{
    public interface ITestCaseRepository
    {
        Task<List<TestCaseUser>> GetAllAsync();
        Task<TestCaseUser> GetByIdAsync(Guid id);
        Task<List<TestCaseUser>> GetByFunctionNameAsync(string functionName);
        Task<TestCaseUser> CreateAsync(TestCaseUser testCase);
        Task<TestCaseUser> UpdateAsync(TestCaseUser testCase);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> DeleteAllAsync();
    }
}
