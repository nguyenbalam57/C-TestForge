using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;

namespace C_TestForge.Core.Interfaces.TestCaseManagement
{
    /// <summary>
    /// Repository for storing and retrieving test cases
    /// </summary>
    public interface ITestCaseRepository
    {
        /// <summary>
        /// Gets all test cases
        /// </summary>
        Task<IEnumerable<TestCaseModels>> GetAllAsync();

        /// <summary>
        /// Gets test cases by function name
        /// </summary>
        Task<IEnumerable<TestCaseModels>> GetByFunctionNameAsync(string functionName);

        /// <summary>
        /// Gets a test case by ID
        /// </summary>
        Task<TestCaseModels> GetByIdAsync(int id);

        /// <summary>
        /// Adds a new test case
        /// </summary>
        Task<TestCaseModels> AddAsync(TestCaseModels testCase);

        /// <summary>
        /// Updates an existing test case
        /// </summary>
        Task<bool> UpdateAsync(TestCaseModels testCase);

        /// <summary>
        /// Deletes a test case
        /// </summary>
        Task<bool> DeleteAsync(int id);

        /// <summary>
        /// Saves all pending changes
        /// </summary>
        Task<int> SaveChangesAsync();
    }
}
