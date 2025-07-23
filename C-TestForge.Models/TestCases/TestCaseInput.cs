using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    public class TestCaseInput
    {
        public Guid Id { get; set; }
        public string VariableName { get; set; } = string.Empty;
        public string VariableType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool IsStub { get; set; } // Đánh dấu nếu đây là giá trị cho stub
    }
}
