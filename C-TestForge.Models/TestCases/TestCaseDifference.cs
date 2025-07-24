namespace C_TestForge.Models.TestCases
{
    /// <summary>
    /// Represents a difference between two test cases
    /// </summary>
    public class TestCaseDifference
    {
        /// <summary>
        /// The name of the property that differs
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// The value of the property in the first test case
        /// </summary>
        public string Value1 { get; set; } = string.Empty;

        /// <summary>
        /// The value of the property in the second test case
        /// </summary>
        public string Value2 { get; set; } = string.Empty;
    }
}
