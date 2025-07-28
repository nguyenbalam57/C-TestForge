using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestExecution
{
    /// <summary>
    /// Status of a test execution
    /// </summary>
    public enum TestExecutionStatus
    {
        /// <summary>
        /// Test execution has not started
        /// </summary>
        NotStarted,

        /// <summary>
        /// Test execution is in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Test execution completed successfully
        /// </summary>
        Completed,

        /// <summary>
        /// Test execution failed
        /// </summary>
        Failed,

        /// <summary>
        /// Test execution was cancelled
        /// </summary>
        Cancelled,

        /// <summary>
        /// Test execution timed out
        /// </summary>
        Timeout
    }

}
