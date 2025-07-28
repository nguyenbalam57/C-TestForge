using C_TestForge.Models.Base;
using System.Collections.Generic;

namespace C_TestForge.Models.TestGeneration
{
    /// <summary>
    /// Criteria for generating test cases
    /// </summary>
    public class TestGenerationCriteria : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the criteria
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the criteria
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Strategy for generating test cases
        /// </summary>
        public TestGenerationStrategy Strategy { get; set; }

        /// <summary>
        /// Target coverage percentage (0-100)
        /// </summary>
        public double TargetCoverage { get; set; } = 100;

        /// <summary>
        /// Maximum number of test cases to generate
        /// </summary>
        public int MaxTestCases { get; set; } = 100;

        /// <summary>
        /// Whether to include error conditions
        /// </summary>
        public bool IncludeErrorConditions { get; set; } = true;

        /// <summary>
        /// Whether to include boundary values
        /// </summary>
        public bool IncludeBoundaryValues { get; set; } = true;

        /// <summary>
        /// Whether to include null/empty values
        /// </summary>
        public bool IncludeNullValues { get; set; } = true;

        /// <summary>
        /// Custom properties for the criteria
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a clone of the test generation criteria
        /// </summary>
        public TestGenerationCriteria Clone()
        {
            return new TestGenerationCriteria
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Strategy = Strategy,
                TargetCoverage = TargetCoverage,
                MaxTestCases = MaxTestCases,
                IncludeErrorConditions = IncludeErrorConditions,
                IncludeBoundaryValues = IncludeBoundaryValues,
                IncludeNullValues = IncludeNullValues,
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }
}