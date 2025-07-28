using C_TestForge.Models.Base;
using C_TestForge.Models.TestCase;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace C_TestForge.Models.TestGeneration
{
    /// <summary>
    /// Result of a test generation operation
    /// </summary>
    public class TestGenerationResult : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the generation result
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the generation result
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Configuration used for generation
        /// </summary>
        public TestGeneratorConfig Configuration { get; set; }

        /// <summary>
        /// Test suite containing generated test cases
        /// </summary>
        public TestSuite GeneratedTests { get; set; }

        /// <summary>
        /// Generated stubs for called functions
        /// </summary>
        public List<FunctionStub> GeneratedStubs { get; set; } = new List<FunctionStub>();

        /// <summary>
        /// Coverage achieved by the generated tests
        /// </summary>
        public TestCoverage Coverage { get; set; }

        /// <summary>
        /// Start time of the generation process
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// End time of the generation process
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Whether the generation was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Error message if generation failed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Detailed log of the generation process
        /// </summary>
        public List<string> GenerationLog { get; set; } = new List<string>();

        /// <summary>
        /// Custom properties for the result
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Duration of the generation process
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration => EndTime - StartTime;

        /// <summary>
        /// Creates a clone of the test generation result
        /// </summary>
        public TestGenerationResult Clone()
        {
            return new TestGenerationResult
            {
                Id = Id,
                Name = Name,
                Description = Description,
                Configuration = Configuration?.Clone(),
                GeneratedTests = GeneratedTests?.Clone(),
                GeneratedStubs = GeneratedStubs?.Select(s => s.Clone()).ToList() ?? new List<FunctionStub>(),
                Coverage = Coverage?.Clone(),
                StartTime = StartTime,
                EndTime = EndTime,
                Success = Success,
                ErrorMessage = ErrorMessage,
                GenerationLog = GenerationLog != null ? new List<string>(GenerationLog) : new List<string>(),
                Properties = Properties != null ? new Dictionary<string, string>(Properties) : new Dictionary<string, string>()
            };
        }
    }
}