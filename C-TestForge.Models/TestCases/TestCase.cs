using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Models.TestCases
{
    public class TestCase
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public TestCaseType Type { get; set; }
        public List<TestCaseInput> Inputs { get; set; } = new List<TestCaseInput>();
        public List<TestCaseOutput> ExpectedOutputs { get; set; } = new List<TestCaseOutput>();
        public List<TestCaseOutput> ActualOutputs { get; set; } = new List<TestCaseOutput>();
        public TestCaseStatus Status { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public TestCase Clone()
        {
            var clone = new TestCase
            {
                Id = Guid.NewGuid(),
                Name = this.Name + " (Copy)",
                Description = this.Description,
                FunctionName = this.FunctionName,
                Type = this.Type,
                Status = this.Status,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            foreach (var input in this.Inputs)
            {
                clone.Inputs.Add(input.Clone());
            }

            foreach (var output in this.ExpectedOutputs)
            {
                clone.ExpectedOutputs.Add(output.Clone());
            }

            foreach (var output in this.ActualOutputs)
            {
                clone.ActualOutputs.Add(output.Clone());
            }

            return clone;
        }
    }
}
