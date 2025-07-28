using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    /// <summary>
    /// Status of a test case
    /// </summary>
    public enum TestCaseStatus
    {
        NotRun,
        Passed,
        Failed,
        Error,
        Skipped
    }
}
