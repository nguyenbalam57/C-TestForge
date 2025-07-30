using System.Collections.Generic;
using C_TestForge.Models.Base;
using C_TestForge.Models.Core;

namespace C_TestForge.Models.TestGeneration
{
    /// <summary>
    /// Stub for a function in a test case
    /// </summary>
    public class FunctionStub : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the function to stub
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// Return type of the function
        /// </summary>
        public string ReturnType { get; set; } = string.Empty;

        /// <summary>
        /// Parameters of the function
        /// </summary>
        public List<CVariable> Parameters { get; set; } = new List<CVariable>();

        /// <summary>
        /// Body of the stub implementation
        /// </summary>
        public string StubBody { get; set; } = string.Empty;

        /// <summary>
        /// Return value to use in the stub
        /// </summary>
        public string ReturnValue { get; set; } = string.Empty;

        /// <summary>
        /// Expected call count for this stub
        /// </summary>
        public int ExpectedCallCount { get; set; } = 1;

        /// <summary>
        /// Whether to verify call count
        /// </summary>
        public bool VerifyCallCount { get; set; } = true;

        /// <summary>
        /// Whether to verify parameter values
        /// </summary>
        public bool VerifyParameters { get; set; } = true;

        /// <summary>
        /// Custom validations for the stub
        /// </summary>
        public List<string> CustomValidations { get; set; } = new List<string>();

        /// <summary>
        /// Custom properties for the stub
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Get a string representation of the function stub
        /// </summary>
        public override string ToString()
        {
            string paramList = string.Join(", ", Parameters.Select(p => p.ToString()));
            return $"{ReturnType} {FunctionName}({paramList}) => {ReturnValue}";
        }

        /// <summary>
        /// Creates a clone of the function stub
        /// </summary>
        public FunctionStub Clone()
        {
            return new FunctionStub
            {
                Id = Id,
                FunctionName = FunctionName,
                ReturnType = ReturnType,
                Parameters = Parameters?.Select(p => p.Clone()).ToList() ?? new List<CVariable>(),
                StubBody = StubBody,
                ReturnValue = ReturnValue,
                ExpectedCallCount = ExpectedCallCount,
                VerifyCallCount = VerifyCallCount,
                VerifyParameters = VerifyParameters,
                CustomValidations = CustomValidations != null ? new List<string>(CustomValidations) : new List<string>(),
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }
}