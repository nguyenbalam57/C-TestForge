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
    /// Represents an enumeration value
    /// </summary>
    public class CEnumValue : SourceCodeEntity
    {
        /// <summary>
        /// Integer value of the enum
        /// </summary>
        public long Value { get; set; }

        /// <summary>
        /// Whether the value was explicitly assigned
        /// </summary>
        public bool IsExplicitValue { get; set; }

        /// <summary>
        /// String representation of the assigned value (if any)
        /// </summary>
        public string ValueExpression { get; set; } = string.Empty;

        /// <summary>
        /// Documentation for this enum value
        /// </summary>
        public string Documentation { get; set; } = string.Empty;

        public override string ToString()
        {
            return IsExplicitValue ? $"{Name} = {ValueExpression}" : Name;
        }

        public CEnumValue Clone()
        {
            return new CEnumValue
            {
                Id = Id,
                Name = Name,
                LineNumber = LineNumber,
                ColumnNumber = ColumnNumber,
                SourceFile = SourceFile,
                Value = Value,
                IsExplicitValue = IsExplicitValue,
                ValueExpression = ValueExpression,
                Documentation = Documentation
            };
        }
    }
}
