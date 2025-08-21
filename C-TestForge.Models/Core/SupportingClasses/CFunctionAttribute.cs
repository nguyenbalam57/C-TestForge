using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses
{
    /// <summary>
    /// Represents a function attribute
    /// </summary>
    public class CFunctionAttribute
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Parameters { get; set; } = new List<string>();

        public override string ToString()
        {
            if (Parameters.Count > 0)
                return $"__attribute__(({Name}({string.Join(", ", Parameters)})))";
            return $"__attribute__(({Name}))";
        }

        public CFunctionAttribute Clone()
        {
            return new CFunctionAttribute
            {
                Name = Name,
                Parameters = new List<string>(Parameters ?? new List<string>())
            };
        }
    }

}
