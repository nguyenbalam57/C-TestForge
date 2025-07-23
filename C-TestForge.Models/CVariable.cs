namespace C_TestForge.Models
{
    public enum VariableStorageClass
    {
        Auto,
        Register,
        Static,
        Extern,
        Typedef,
        None
    }

    public enum VariableScope
    {
        Global,
        Local,
        Parameter,
        Member
    }

    public class CVariable
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public VariableStorageClass StorageClass { get; set; }
        public VariableScope Scope { get; set; }
        public string DefaultValue { get; set; } = string.Empty;
        public bool IsConstant { get; set; }
        public bool IsArray { get; set; }
        public bool IsPointer { get; set; }
        public int LineNumber { get; set; }
        public bool IsEnabled { get; set; } = true;
        public string MinValue { get; set; } = string.Empty;
        public string MaxValue { get; set; } = string.Empty;
        public List<string> PossibleValues { get; set; } = new List<string>();
        public CFunction ParentFunction { get; set; } = new CFunction();
        public CPreprocessorDirective ParentDirective { get; set; } = new CPreprocessorDirective();
    }
}
