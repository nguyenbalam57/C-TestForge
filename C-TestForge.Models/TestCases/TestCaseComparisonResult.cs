namespace C_TestForge.Models.TestCases
{
    public class TestCaseComparisonResult
    {
        public TestCase TestCase1 { get; set; } = new TestCase();
        public TestCase TestCase2 { get; set; } = new TestCase();
        public List<TestCaseDifference> Differences { get; set; } = new List<TestCaseDifference>();
    }

    public class TestCaseDifference
    {
        public string PropertyName { get; set; } = string.Empty;
        public string Value1 { get; set; } = string.Empty;
        public string Value2 { get; set; } = string.Empty;
    }
}
