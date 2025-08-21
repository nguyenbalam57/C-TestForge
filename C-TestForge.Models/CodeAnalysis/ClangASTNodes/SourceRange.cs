using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.CodeAnalysis.ClangASTNodes
{
    // Class để lưu source range (begin và end location)
    public class SourceRange
    {
        public SourceLocation Begin { get; set; }
        public SourceLocation End { get; set; }
        public bool IsValid => Begin?.IsValid == true && End?.IsValid == true;

        public SourceRange()
        {
            Begin = new SourceLocation();
            End = new SourceLocation();
        }

        public SourceRange(SourceLocation begin, SourceLocation end)
        {
            Begin = begin;
            End = end;
        }

        public bool Contains(SourceLocation location)
        {
            if (!IsValid || location?.IsValid != true)
                return false;

            return location.Line >= Begin.Line && location.Line <= End.Line &&
                   (location.Line != Begin.Line || location.Column >= Begin.Column) &&
                   (location.Line != End.Line || location.Column <= End.Column);
        }

        public bool Overlaps(SourceRange other)
        {
            if (!IsValid || !other.IsValid)
                return false;

            return Contains(other.Begin) || Contains(other.End) ||
                   other.Contains(Begin) || other.Contains(End);
        }

        public int GetLineCount()
        {
            return IsValid ? End.Line - Begin.Line + 1 : 0;
        }

        public override string ToString()
        {
            return $"{Begin} - {End}";
        }

        public override bool Equals(object obj)
        {
            if (obj is SourceRange other)
            {
                return Begin.Equals(other.Begin) && End.Equals(other.End);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Begin, End);
        }
    }
}
