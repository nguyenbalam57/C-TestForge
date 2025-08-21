using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core
{
    /// <summary>
    /// Represents an error that occurred during parsing
    /// </summary>
    public class ParseError : SourceCodeEntity, IModelObject
    {
        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Source file where the error occurred
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Severity of the error
        /// </summary>
        public ErrorSeverity Severity { get; set; }

        /// <summary>
        /// Get a string representation of the parse error
        /// </summary>
        public override string ToString()
        {
            return $"{Severity}: {FileName}({LineNumber},{ColumnNumber}): {Message}";
        }

        /// <summary>
        /// Create a clone of the parse error
        /// </summary>
        public ParseError Clone()
        {
            return new ParseError
            {
                Id = Id,
                Message = Message,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                FileName = FileName,
                Severity = Severity
            };
        }
    }
}
