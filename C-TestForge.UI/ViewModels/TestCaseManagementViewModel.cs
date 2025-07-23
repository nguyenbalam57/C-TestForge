using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using C_TestForge.Models;
using C_TestForge.TestCase.Services;
using C_TestForge.Models.TestCases;

namespace C_TestForge.UI.ViewModels
{
    public class TestCaseManagementViewModel : BindableBase
    {
        private readonly ITestCaseService _testCaseService;
        private readonly IDialogService _dialogService;

        private ObservableCollection<TestCaseCustom> _testCases;
        private TestCaseCustom _selectedTestCase;

        public ObservableCollection<TestCaseCustom> TestCases
        {
            get => _testCases;
            set => SetProperty(ref _testCases, value);
        }

        public TestCaseCustom SelectedTestCase
        {
            get => _selectedTestCase;
            set => SetProperty(ref _selectedTestCase, value);
        }

        public DelegateCommand AddTestCaseCommand { get; }
        public DelegateCommand<TestCaseCustom> EditTestCaseCommand { get; }
        public DelegateCommand<TestCaseCustom> DeleteTestCaseCommand { get; }
        public DelegateCommand ImportTestCasesCommand { get; }
        public DelegateCommand ExportTestCasesCommand { get; }
        public DelegateCommand CompareTestCasesCommand { get; }

        public TestCaseManagementViewModel(ITestCaseService testCaseService, IDialogService dialogService)
        {
            _testCaseService = testCaseService;
            _dialogService = dialogService;

            AddTestCaseCommand = new DelegateCommand(ExecuteAddTestCase);
            EditTestCaseCommand = new DelegateCommand<TestCaseCustom>(ExecuteEditTestCase, CanEditTestCase);
            DeleteTestCaseCommand = new DelegateCommand<TestCaseCustom>(ExecuteDeleteTestCase, CanDeleteTestCase);
            ImportTestCasesCommand = new DelegateCommand(ExecuteImportTestCases);
            ExportTestCasesCommand = new DelegateCommand(ExecuteExportTestCases);
            CompareTestCasesCommand = new DelegateCommand(ExecuteCompareTestCases);

            LoadTestCases();
        }

        private async void LoadTestCases()
        {
            var testCases = await _testCaseService.GetAllTestCasesAsync();
            TestCases = new ObservableCollection<TestCaseCustom>(testCases);
        }

        private void ExecuteAddTestCase()
        {
            var parameters = new NavigationParameters
        {
            { "Mode", "Add" }
        };

            _dialogService.ShowDialog("TestCaseEditorDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("TestCase"))
                {
                    var newTestCase = result.Parameters.GetValue<TestCaseCustom>("TestCase");
                    TestCases.Add(newTestCase);
                }
            });
        }

        private bool CanEditTestCase(TestCaseCustom testCase)
        {
            return testCase != null;
        }

        private void ExecuteEditTestCase(TestCaseCustom testCase)
        {
            var parameters = new NavigationParameters
        {
            { "Mode", "Edit" },
            { "TestCase", testCase }
        };

            _dialogService.ShowDialog("TestCaseEditorDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("TestCase"))
                {
                    var updatedTestCase = result.Parameters.GetValue<TestCaseCustom>("TestCase");
                    var index = TestCases.IndexOf(testCase);
                    if (index >= 0)
                    {
                        TestCases[index] = updatedTestCase;
                    }
                }
            });
        }

        // Triển khai các phương thức còn lại...
    }
}
