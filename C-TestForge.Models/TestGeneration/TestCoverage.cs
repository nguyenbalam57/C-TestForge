using C_TestForge.Models.Base;
using C_TestForge.Models.CodeAnalysis.Coverage;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace C_TestForge.Models.TestGeneration
{
    /// <summary>
    /// Coverage metrics for tests
    /// </summary>
    public class TestCoverage : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the coverage
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Dictionary of coverage metrics by type
        /// </summary>
        public Dictionary<CoverageType, double> CoverageMetrics { get; set; } = new Dictionary<CoverageType, double>();

        /// <summary>
        /// Coverage metrics by function
        /// </summary>
        public Dictionary<string, Dictionary<CoverageType, double>> FunctionCoverage { get; set; } = new Dictionary<string, Dictionary<CoverageType, double>>();

        /// <summary>
        /// List of covered lines
        /// </summary>
        public List<CoverageLine> CoveredLines { get; set; } = new List<CoverageLine>();

        /// <summary>
        /// List of uncovered lines
        /// </summary>
        public List<CoverageLine> UncoveredLines { get; set; } = new List<CoverageLine>();

        /// <summary>
        /// List of covered branches
        /// </summary>
        public List<CoverageBranch> CoveredBranches { get; set; } = new List<CoverageBranch>();

        /// <summary>
        /// List of uncovered branches
        /// </summary>
        public List<CoverageBranch> UncoveredBranches { get; set; } = new List<CoverageBranch>();

        /// <summary>
        /// Total coverage percentage
        /// </summary>
        [JsonIgnore]
        public double TotalCoverage
        {
            get
            {
                if (CoverageMetrics.Count == 0)
                    return 0;

                return CoverageMetrics.Values.Average();
            }
        }

        /// <summary>
        /// Creates a clone of the test coverage
        /// </summary>
        public TestCoverage Clone()
        {
            var clone = new TestCoverage
            {
                Id = Id,
                Name = Name,
                CoverageMetrics = CoverageMetrics != null ? new Dictionary<CoverageType, double>(CoverageMetrics) : new Dictionary<CoverageType, double>(),
                CoveredLines = CoveredLines?.Select(l => l.Clone()).ToList() ?? new List<CoverageLine>(),
                UncoveredLines = UncoveredLines?.Select(l => l.Clone()).ToList() ?? new List<CoverageLine>(),
                CoveredBranches = CoveredBranches?.Select(b => b.Clone()).ToList() ?? new List<CoverageBranch>(),
                UncoveredBranches = UncoveredBranches?.Select(b => b.Clone()).ToList() ?? new List<CoverageBranch>()
            };

            // Clone function coverage
            clone.FunctionCoverage = new Dictionary<string, Dictionary<CoverageType, double>>();
            if (FunctionCoverage != null)
            {
                foreach (var kvp in FunctionCoverage)
                {
                    clone.FunctionCoverage[kvp.Key] = new Dictionary<CoverageType, double>(kvp.Value);
                }
            }

            return clone;
        }
    }
}