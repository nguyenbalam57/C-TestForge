using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace C_TestForge.Models
{
    #region Test Generation Models

    /// <summary>
    /// Strategy for generating test cases
    /// </summary>
    public enum TestGenerationStrategy
    {
        /// <summary>
        /// Generate tests that cover all branches
        /// </summary>
        BranchCoverage,

        /// <summary>
        /// Generate tests that cover all statements
        /// </summary>
        StatementCoverage,

        /// <summary>
        /// Generate tests that cover all paths
        /// </summary>
        PathCoverage,

        /// <summary>
        /// Generate tests based on boundary values
        /// </summary>
        BoundaryValue,

        /// <summary>
        /// Generate tests based on equivalence classes
        /// </summary>
        EquivalencePartitioning,

        /// <summary>
        /// Generate tests that cover error conditions
        /// </summary>
        ErrorCondition,

        /// <summary>
        /// Custom test generation strategy
        /// </summary>
        Custom
    }

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
        public string FunctionName { get; set; }

        /// <summary>
        /// Return type of the function
        /// </summary>
        public string ReturnType { get; set; }

        /// <summary>
        /// Parameters of the function
        /// </summary>
        public List<CVariable> Parameters { get; set; } = new List<CVariable>();

        /// <summary>
        /// Body of the stub implementation
        /// </summary>
        public string StubBody { get; set; }

        /// <summary>
        /// Return value to use in the stub
        /// </summary>
        public string ReturnValue { get; set; }

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

    /// <summary>
    /// Type of coverage metric
    /// </summary>
    public enum CoverageType
    {
        /// <summary>
        /// Statement coverage
        /// </summary>
        Statement,

        /// <summary>
        /// Branch coverage
        /// </summary>
        Branch,

        /// <summary>
        /// Path coverage
        /// </summary>
        Path,

        /// <summary>
        /// Function coverage
        /// </summary>
        Function,

        /// <summary>
        /// Line coverage
        /// </summary>
        Line,

        /// <summary>
        /// Condition coverage
        /// </summary>
        Condition,

        /// <summary>
        /// Modified Condition/Decision Coverage
        /// </summary>
        MCDC
    }

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
        public string Name { get; set; }

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

    /// <summary>
    /// Represents a line for coverage tracking
    /// </summary>
    public class CoverageLine : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Source file
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Line content
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Whether the line is executable
        /// </summary>
        public bool IsExecutable { get; set; } = true;

        /// <summary>
        /// List of test cases that cover this line
        /// </summary>
        public List<string> CoveringTestCases { get; set; } = new List<string>();

        /// <summary>
        /// Creates a clone of the coverage line
        /// </summary>
        public CoverageLine Clone()
        {
            return new CoverageLine
            {
                Id = Id,
                SourceFile = SourceFile,
                LineNumber = LineNumber,
                FunctionName = FunctionName,
                Content = Content,
                IsExecutable = IsExecutable,
                CoveringTestCases = CoveringTestCases != null ? new List<string>(CoveringTestCases) : new List<string>()
            };
        }
    }

    /// <summary>
    /// Represents a branch for coverage tracking
    /// </summary>
    public class CoverageBranch : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Source file
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// Line number
        /// </summary>
        public int LineNumber { get; set; }

        /// <summary>
        /// Function name
        /// </summary>
        public string FunctionName { get; set; }

        /// <summary>
        /// Branch condition
        /// </summary>
        public string Condition { get; set; }

        /// <summary>
        /// Whether the branch is true (true branch) or false (false branch)
        /// </summary>
        public bool IsTrueBranch { get; set; }

        /// <summary>
        /// List of test cases that cover this branch
        /// </summary>
        public List<string> CoveringTestCases { get; set; } = new List<string>();

        /// <summary>
        /// Creates a clone of the coverage branch
        /// </summary>
        public CoverageBranch Clone()
        {
            return new CoverageBranch
            {
                Id = Id,
                SourceFile = SourceFile,
                LineNumber = LineNumber,
                FunctionName = FunctionName,
                Condition = Condition,
                IsTrueBranch = IsTrueBranch,
                CoveringTestCases = CoveringTestCases != null ? new List<string>(CoveringTestCases) : new List<string>()
            };
        }
    }

    #endregion
}