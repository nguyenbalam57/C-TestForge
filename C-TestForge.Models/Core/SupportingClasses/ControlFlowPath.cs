using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.Core.SupportingClasses
{
    /// <summary>
    /// Control flow path in a function
    /// </summary>
    public class ControlFlowPath
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public int StartLine { get; set; }
        public int EndLine { get; set; }
        public string Condition { get; set; } = string.Empty;
        public bool IsCovered { get; set; }
        public List<string> Statements { get; set; } = new List<string>();

        public ControlFlowPath Clone()
        {
            return new ControlFlowPath
            {
                Id = Id,
                StartLine = StartLine,
                EndLine = EndLine,
                Condition = Condition,
                IsCovered = IsCovered,
                Statements = new List<string>(Statements ?? new List<string>())
            };
        }
    }
}
