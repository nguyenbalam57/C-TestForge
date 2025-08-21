using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses
{
    /// <summary>
    /// Represents a variable attribute
    /// </summary>
    public class CVariableAttribute
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = new List<string>();

        public CVariableAttribute Clone()
        {
            return new CVariableAttribute
            {
                Name = Name,
                Parameters = new List<string>(Parameters ?? new List<string>())
            };
        }
    }
}
