using C_TestForge.Models.Base;
using System.Collections.Generic;

namespace C_TestForge.Models.TestGeneration
{
    /// <summary>
    /// Configuration for test generator
    /// </summary>
    public class TestGeneratorConfig : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the configuration
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the configuration
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// List of function names to generate tests for
        /// </summary>
        public List<string> TargetFunctions { get; set; } = new List<string>();

        /// <summary>
        /// List of generation criteria
        /// </summary>
        public List<TestGenerationCriteria> Criteria { get; set; } = new List<TestGenerationCriteria>();

        /// <summary>
        /// Whether to generate stubs for called functions
        /// </summary>
        public bool GenerateStubs { get; set; } = true;

        /// <summary>
        /// Whether to use solver for constraint resolution
        /// </summary>
        public bool UseSolver { get; set; } = true;

        /// <summary>
        /// Maximum timeout for solver in milliseconds
        /// </summary>
        public int SolverTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// Whether to optimize test cases by removing redundant ones
        /// </summary>
        public bool OptimizeTestCases { get; set; } = true;

        /// <summary>
        /// Custom properties for the configuration
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Creates a clone of the test generator configuration
        /// </summary>
        public TestGeneratorConfig Clone()
        {
            return new TestGeneratorConfig
            {
                Id = Id,
                Name = Name,
                Description = Description,
                TargetFunctions = TargetFunctions != null ? new List<string>(TargetFunctions) : new List<string>(),
                Criteria = Criteria?.Select(c => c.Clone()).ToList() ?? new List<TestGenerationCriteria>(),
                GenerateStubs = GenerateStubs,
                UseSolver = UseSolver,
                SolverTimeoutMs = SolverTimeoutMs,
                OptimizeTestCases = OptimizeTestCases,
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }
}