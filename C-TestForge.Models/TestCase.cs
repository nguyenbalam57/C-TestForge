namespace C_TestForge.Models
{
    public enum TestCaseType
    {
        UnitTest,
        IntegrationTest
    }

    public enum TestCaseStatus
    {
        NotRun,
        Passed,
        Failed,
        Error
    }

    public class TestCase
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TestCaseType Type { get; set; }
        public string TargetFunction { get; set; } = string.Empty;
        public List<TestCaseVariable> InputVariables { get; set; } = new List<TestCaseVariable>();
        public List<TestCaseVariable> OutputVariables { get; set; } = new List<TestCaseVariable>();
        public List<TestCaseStub> Stubs { get; set; } = new List<TestCaseStub>();
        public TestCaseStatus Status { get; set; } = TestCaseStatus.NotRun;
        public string ActualResult { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }

    public class TestCaseVariable
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsPointer { get; set; }
        public bool IsArray { get; set; }
        public int ArraySize { get; set; }
    }

    public class TestCaseStub
    {
        public string FunctionName { get; set; } = string.Empty;
        public string ReturnValue { get; set; } = string.Empty;
        public List<TestCaseVariable> Parameters { get; set; } = new List<TestCaseVariable>();
    }
}
