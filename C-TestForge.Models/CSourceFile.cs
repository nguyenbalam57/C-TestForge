namespace C_TestForge.Models
{
    public class CSourceFile
    {
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<CPreprocessorDirective> PreprocessorDirectives { get; set; } = new List<CPreprocessorDirective>();
        public List<CDefinition> Definitions { get; set; } = new List<CDefinition>();
        public List<CVariable> Variables { get; set; } = new List<CVariable>();
        public List<CFunction> Functions { get; set; } = new List<CFunction>();

        public CSourceFile()
        {
        }

        public CSourceFile(string filePath, string content)
        {
            FilePath = filePath;
            Content = content;
        }
    }
}
