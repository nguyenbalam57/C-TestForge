using C_TestForge.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestExecution
{
    /// <summary>
    /// Represents a parameter value in a function call
    /// </summary>
    public class ParameterValue : IModelObject
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Name of the parameter
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type of the parameter
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Value of the parameter
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Whether the parameter is an array
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Array values (if IsArray is true)
        /// </summary>
        public List<string> ArrayValues { get; set; } = new List<string>();

        /// <summary>
        /// Creates a clone of the parameter value
        /// </summary>
        public ParameterValue Clone()
        {
            return new ParameterValue
            {
                Id = Id,
                Name = Name,
                Type = Type,
                Value = Value,
                IsArray = IsArray,
                ArrayValues = ArrayValues != null ? new List<string>(ArrayValues) : new List<string>()
            };
        }
    }
}
