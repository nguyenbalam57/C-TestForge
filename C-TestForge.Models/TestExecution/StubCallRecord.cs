using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestExecution
{
    /// <summary>
    /// Record of a stub function call during test execution
    /// </summary>
    public class StubCallRecord : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the stubbed function
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// Sequence number of the call (1-based)
        /// </summary>
        public int CallSequence { get; set; }

        /// <summary>
        /// Timestamp of the call
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Parameter values passed to the function
        /// </summary>
        public List<ParameterValue> ParameterValues { get; set; } = new List<ParameterValue>();

        /// <summary>
        /// Return value from the stub
        /// </summary>
        public string ReturnValue { get; set; } = string.Empty;

        /// <summary>
        /// Creates a clone of the stub call record
        /// </summary>
        public StubCallRecord Clone()
        {
            return new StubCallRecord
            {
                Id = Id,
                FunctionName = FunctionName,
                CallSequence = CallSequence,
                Timestamp = Timestamp,
                ParameterValues = ParameterValues?.Select(p => p.Clone()).ToList() ?? new List<ParameterValue>(),
                ReturnValue = ReturnValue
            };
        }
    }
}
