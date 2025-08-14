using Prism.Mvvm;
using C_TestForge.Models.TestCases;

namespace C_TestForge.UI.ViewModels
{
    /// <summary>
    /// ViewModel for TestCaseView
    /// </summary>
    public class TestCaseViewModel : BindableBase
    {
        private TestCase _testCase;
        public TestCase TestCase
        {
            get => _testCase;
            set => SetProperty(ref _testCase, value);
        }

        public TestCaseViewModel()
        {
            // Dữ liệu mẫu
            TestCase = new TestCase
            {
                Name = "Sample Test Case",
                Description = "This is a sample test case.",
                FunctionName = "AddNumbers",
                Type = TestCaseType.UnitTest,
                Status = TestCaseStatus.NotRun
            };
        }
    }
}