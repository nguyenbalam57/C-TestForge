using C_TestForge.Models.TestCases;

namespace C_TestForge.Models.TestCases
{
    /// <summary>
    /// Represents a test case created or managed by a user
    /// </summary>
    public class TestCaseUser
    {
        /// <summary>
        /// Gets or sets the unique identifier for the test case
        /// </summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Gets or sets the name of the test case
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the description of the test case
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the name of the function being tested
        /// </summary>
        public string FunctionUnderTest { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of the test case
        /// </summary>
        public TestCaseType Type { get; set; }

        /// <summary>
        /// Gets or sets the list of input parameters for the test case
        /// </summary>
        public List<CVariable> InputParameters { get; set; } = new List<CVariable>();

        /// <summary>
        /// Gets or sets the list of expected outputs for the test case
        /// </summary>
        public List<CVariable> ExpectedOutputs { get; set; } = new List<CVariable>();

        /// <summary>
        /// Gets or sets the list of stub functions required for the test case
        /// </summary>
        public List<StubFunction> RequiredStubs { get; set; } = new List<StubFunction>();

        /// <summary>
        /// Gets or sets the status of the test case
        /// </summary>
        public TestCaseStatus Status { get; set; } = TestCaseStatus.NotRun;

        /// <summary>
        /// Gets or sets the list of actual outputs from the test execution
        /// </summary>
        public List<CVariable> ActualOutputs { get; set; } = new List<CVariable>();

        /// <summary>
        /// Gets or sets the creation date of the test case
        /// </summary>
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the last modified date of the test case
        /// </summary>
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets or sets the user who created the test case
        /// </summary>
        public string CreatedBy { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the tags associated with the test case
        /// </summary>
        public string Tags { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the category of the test case
        /// </summary>
        public string Category { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the priority of the test case
        /// </summary>
        public int Priority { get; set; } = 1;

        /// <summary>
        /// Gets or sets comments associated with the test case
        /// </summary>
        public string Comments { get; set; } = string.Empty;

        /// <summary>
        /// Creates a deep copy of this test case
        /// </summary>
        /// <returns>A new TestCaseUser instance with the same properties</returns>
        public TestCaseUser Clone()
        {
            var clone = new TestCaseUser
            {
                Id = Guid.NewGuid(),
                Name = this.Name + " (Copy)",
                Description = this.Description,
                FunctionUnderTest = this.FunctionUnderTest,
                Type = this.Type,
                Status = this.Status,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now,
                CreatedBy = this.CreatedBy,
                Tags = this.Tags,
                Category = this.Category,
                Priority = this.Priority,
                Comments = this.Comments
            };

            // Clone input parameters
            foreach (var input in this.InputParameters)
            {
                clone.InputParameters.Add(input.Clone());
            }

            // Clone expected outputs
            foreach (var output in this.ExpectedOutputs)
            {
                clone.ExpectedOutputs.Add(output.Clone());
            }

            // Clone actual outputs
            foreach (var output in this.ActualOutputs)
            {
                clone.ActualOutputs.Add(output.Clone());
            }

            // Clone required stubs
            foreach (var stub in this.RequiredStubs)
            {
                clone.RequiredStubs.Add(stub.Clone());
            }

            return clone;
        }

    }
}
