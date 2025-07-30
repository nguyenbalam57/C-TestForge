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
    /// Repository for storing and retrieving test cases
    /// </summary>
    public interface ITestCaseRepository
    {
        /// <summary>
        /// Gets all test cases
        /// </summary>
        Task<IEnumerable<TestCase>> GetAllAsync();

        /// <summary>
        /// Gets test cases by function name
        /// </summary>
        Task<IEnumerable<TestCase>> GetByFunctionNameAsync(string functionName);

        /// <summary>
        /// Gets a test case by ID
        /// </summary>
        Task<TestCase> GetByIdAsync(int id);

        /// <summary>
        /// Adds a new test case
        /// </summary>
        Task<TestCase> AddAsync(TestCase testCase);

        /// <summary>
        /// Updates an existing test case
        /// </summary>
        Task<bool> UpdateAsync(TestCase testCase);

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
