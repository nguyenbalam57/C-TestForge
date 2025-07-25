using C_TestForge.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces
{
    public interface IZ3SolverService : IDisposable
    {
        /// <summary>
        /// Finds values for variables that satisfy the given constraints
        /// </summary>
        /// <param name="variables">List of variables to find values for</param>
        /// <param name="constraints">List of constraints in string format</param>
        /// <returns>Dictionary of variable names and their satisfying values</returns>
        Dictionary<string, object> FindSatisfyingValues(List<CVariable> variables, List<string> constraints);
    }
}
