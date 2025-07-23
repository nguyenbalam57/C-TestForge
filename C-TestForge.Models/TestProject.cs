namespace C_TestForge.Models
{
    public class TestProject
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourceDirectory { get; set; } = string.Empty;
        public List<string> SourceFiles { get; set; } = new List<string>();
        public List<string> IncludeDirectories { get; set; } = new List<string>();
        public Dictionary<string, string> PreprocessorDefinitions { get; set; } = new Dictionary<string, string>();
        public List<TestCase> TestCases { get; set; } = new List<TestCase>();
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }
}
