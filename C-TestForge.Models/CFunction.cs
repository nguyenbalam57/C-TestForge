namespace C_TestForge.Models
{
    public enum FunctionStorageClass
    {
        Static,
        Extern,
        Inline,
        None
    }

    public class CFunction
    {
        public string Name { get; set; } = string.Empty;
        public string ReturnType { get; set; } = string.Empty;
        public FunctionStorageClass StorageClass { get; set; }
        public List<CVariable> Parameters { get; set; } = new List<CVariable>();
        public List<CVariable> LocalVariables { get; set; } = new List<CVariable>();
        public List<string> CalledFunctions { get; set; } = new List<string>();
        public List<string> CalledByFunctions { get; set; } = new List<string>();
        public int LineNumber { get; set; }
        public int EndLineNumber { get; set; }
        public string Body { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public CPreprocessorDirective ParentDirective { get; set; } = new CPreprocessorDirective();
    }
}
