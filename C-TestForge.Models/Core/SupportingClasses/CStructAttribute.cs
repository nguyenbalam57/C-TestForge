using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses
{
    /// <summary>
    /// Represents a structure attribute
    /// </summary>
    public class CStructAttribute
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = new List<string>();

        public CStructAttribute Clone()
        {
            return new CStructAttribute
            {
                Name = Name,
                Parameters = new List<string>(Parameters ?? new List<string>())
            };
        }
    }
}
