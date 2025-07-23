using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models
{
    public enum PreprocessorType
    {
        Include,
        Define,
        Undef,
        If,
        Ifdef,
        Ifndef,
        Else,
        Elif,
        Endif,
        Pragma,
        Error,
        Warning
    }

    public class CPreprocessorDirective
    {
        public PreprocessorType Type { get; set; }
        public int LineNumber { get; set; }
        public string RawText { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public List<CPreprocessorDirective> Children { get; set; } = new List<CPreprocessorDirective>();
    }
}
