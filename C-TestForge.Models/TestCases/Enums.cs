namespace C_TestForge.Models.TestCases
{
    public enum TestCaseType 
    { 
        UnitTest, 
        IntegrationTest,
        Regression,
        Performance,
        Security
    }
    public enum TestCaseStatus 
    {
        NotRun,
        Passed,
        Failed,
        Error,
        Skipped
    }
    public enum StubParameterAction
    {
        SetValue,
        CopyBuffer,
        CallCallback
    }
}
