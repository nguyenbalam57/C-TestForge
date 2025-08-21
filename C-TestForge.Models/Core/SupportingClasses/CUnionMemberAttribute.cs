using C_TestForge.Models.Base;
using C_TestForge.Models.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace C_TestForge.Models.Core.SupportingClasses
{
    /// <summary>
    /// Represents an attribute applied to a union member
    /// </summary>
    public class CUnionMemberAttribute : SourceCodeEntity
    {
        /// <summary>
        /// Name of the attribute
        /// </summary>
        public string AttributeName { get; set; } = string.Empty;

        /// <summary>
        /// Parameters/arguments for the attribute
        /// </summary>
        public List<string> Parameters { get; set; } = new List<string>();

        /// <summary>
        /// Raw attribute text as it appears in source
        /// </summary>
        public string RawText { get; set; } = string.Empty;

        public override string ToString()
        {
            if (Parameters.Any())
            {
                return $"__attribute__(({AttributeName}({string.Join(", ", Parameters)})))";
            }
            return $"__attribute__(({AttributeName}))";
        }

        public CUnionMemberAttribute Clone()
        {
            return new CUnionMemberAttribute
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                AttributeName = AttributeName,
                Parameters = Parameters?.ToList() ?? new List<string>(),
                RawText = RawText
            };
        }
    }
}
