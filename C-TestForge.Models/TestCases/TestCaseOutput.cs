using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    public class TestCaseOutput
    {
        public Guid Id { get; set; }
        public string VariableName { get; set; } = string.Empty;
        public string VariableType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
