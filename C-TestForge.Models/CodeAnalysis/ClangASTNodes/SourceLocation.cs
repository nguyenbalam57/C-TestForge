using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.ClangASTNodes
{
    // Class để lưu thông tin vị trí trong source code (compatible với Clang SourceLocation)
    public class SourceLocation
    {
        public string FileName { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
        public int Offset { get; set; }
        public bool IsValid { get; set; }
        public bool IsMacroExpansion { get; set; }
        public SourceLocation MacroExpansionLocation { get; set; }
        public SourceLocation SpellingLocation { get; set; }

        public SourceLocation()
        {
            IsValid = true;
        }

        public override string ToString()
        {
            return $"{FileName}:{Line}:{Column}";
        }

        public static SourceLocation Invalid()
        {
            return new SourceLocation { IsValid = false };
        }

        public override bool Equals(object obj)
        {
            if (obj is SourceLocation other)
            {
                return FileName == other.FileName &&
                       Line == other.Line &&
                       Column == other.Column;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FileName, Line, Column);
        }
    }
}
