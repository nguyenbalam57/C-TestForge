using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    public class TestCaseCustom
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public TestCaseType Type { get; set; } // Unit Test hoặc Integration Test
        public List<TestCaseInput> Inputs { get; set; } = new List<TestCaseInput>();
        public List<TestCaseOutput> ExpectedOutputs { get; set; } = new List<TestCaseOutput>();
        public List<TestCaseOutput> ActualOutputs { get; set; } = new List<TestCaseOutput>();
        public TestCaseStatus Status { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}
