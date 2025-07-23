using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    public class TestCaseInput
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string VariableName { get; set; } = string.Empty;
        public string VariableType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsStub { get; set; }

        public TestCaseInput Clone()
        {
            return new TestCaseInput
            {
                Id = Guid.NewGuid(),
                VariableName = this.VariableName,
                VariableType = this.VariableType,
                Value = this.Value,
                IsStub = this.IsStub
            };
        }
    }
}
