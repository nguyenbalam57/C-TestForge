namespace C_TestForge.Models.TestCases
{
    public class TestCaseOutput
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string VariableName { get; set; } = string.Empty;
        public string VariableType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public TestCaseOutput Clone()
        {
            return new TestCaseOutput
            {
                Id = Guid.NewGuid(),
                VariableName = this.VariableName,
                VariableType = this.VariableType,
                Value = this.Value
            };
        }
    }
}
