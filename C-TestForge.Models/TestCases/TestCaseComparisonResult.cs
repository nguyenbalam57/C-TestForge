namespace C_TestForge.Models.TestCases
{
    public class TestCaseComparisonResult
    {
        public TestCaseUser TestCase1 { get; set; } = new TestCaseUser();
        public TestCaseUser TestCase2 { get; set; } = new TestCaseUser();
        public List<TestCaseDifference> Differences { get; set; } = new List<TestCaseDifference>();
    }
}
