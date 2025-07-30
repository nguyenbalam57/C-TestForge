using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    /// <summary>
    /// Represents a variable constraint
    /// </summary>
    public class VariableConstraint
    {
        /// <summary>
        /// Gets or sets the variable name
        /// </summary>
        public string VariableName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the minimum value
        /// </summary>
        public string MinValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum value
        /// </summary>
        public string MaxValue { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the enum values
        /// </summary>
        public List<string> EnumValues { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the allowed values
        /// </summary>
        public List<string> AllowedValues { get; set; } = new List<string>();

        /// <summary>
        /// Creates a deep clone of the variable constraint
        /// </summary>
        /// <returns>A new instance of VariableConstraint with the same values</returns>
        public VariableConstraint Clone()
        {
            return new VariableConstraint
            {
                VariableName = this.VariableName,
                MinValue = this.MinValue,
                MaxValue = this.MaxValue,
                // Tạo danh sách mới và copy các phần tử để tránh tham chiếu cùng đối tượng
                EnumValues = this.EnumValues != null ? new List<string>(this.EnumValues) : new List<string>(),
                AllowedValues = this.AllowedValues != null ? new List<string>(this.AllowedValues) : new List<string>()
            };
        }
    }
}