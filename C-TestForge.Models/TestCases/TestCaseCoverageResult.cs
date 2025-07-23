namespace C_TestForge.Models.TestCases
{
    public class TestCaseCoverageResult
    {
        public CFunction Function { get; set; } = new CFunction();
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
        public double CoveragePercentage { get; set; }
        public List<string> UncoveredPaths { get; set; } = new List<string>();
        public List<string> CoveredPaths { get; set; } = new List<string>();
    }
}
