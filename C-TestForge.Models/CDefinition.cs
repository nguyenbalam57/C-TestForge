using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models
{
    public enum DefinitionType
    {
        Simple,
        Macro,
        FunctionLike
    }

    public class CDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public DefinitionType Type { get; set; }
        public int LineNumber { get; set; }
        public string RawText { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public List<string> Parameters { get; set; } = new List<string>();
        public CPreprocessorDirective ParentDirective { get; set; } = new CPreprocessorDirective();
    }
}
