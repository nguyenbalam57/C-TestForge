using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Models.Core;
using C_TestForge.Models.TestCases;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace C_TestForge.UI.ViewModels
{
    public class TestCaseManagementViewModel : BindableBase
    {
        private readonly ITestCaseService _testCaseService;
        private readonly IDialogService _dialogService;

        private ObservableCollection<Models.TestCases.TestCase> _testCases;
        private Models.TestCases.TestCase _selectedTestCase;
        private string _statusMessage;
        private bool _isLoading;

        public ObservableCollection<Models.TestCases.TestCase> TestCases
        {
            get => _testCases;
            set => SetProperty(ref _testCases, value);
        }

        public Models.TestCases.TestCase SelectedTestCase
        {
            get => _selectedTestCase;
            set => SetProperty(ref _selectedTestCase, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public DelegateCommand AddTestCaseCommand { get; }
        public DelegateCommand<Models.TestCases.TestCase> EditTestCaseCommand { get; }
        public DelegateCommand<Models.TestCases.TestCase> DuplicateTestCaseCommand { get; }
        public DelegateCommand<Models.TestCases.TestCase> DeleteTestCaseCommand { get; }
        public DelegateCommand ImportTestCasesCommand { get; }
        public DelegateCommand ExportTestCasesCommand { get; }
        public DelegateCommand CompareTestCasesCommand { get; }
        public DelegateCommand RefreshCommand { get; }
        public DelegateCommand GenerateUnitTestCommand { get; }

        public TestCaseManagementViewModel(ITestCaseService testCaseService, IDialogService dialogService)
        {
            _testCaseService = testCaseService;
            _dialogService = dialogService;

            TestCases = new ObservableCollection<Models.TestCases.TestCase>();

            AddTestCaseCommand = new DelegateCommand(ExecuteAddTestCase);
            EditTestCaseCommand = new DelegateCommand<Models.TestCases.TestCase>(ExecuteEditTestCase, CanExecuteTestCaseCommand);
            DuplicateTestCaseCommand = new DelegateCommand<Models.TestCases.TestCase>(ExecuteDuplicateTestCase, CanExecuteTestCaseCommand);
            DeleteTestCaseCommand = new DelegateCommand<Models.TestCases.TestCase>(ExecuteDeleteTestCase, CanExecuteTestCaseCommand);
            ImportTestCasesCommand = new DelegateCommand(ExecuteImportTestCases);
            ExportTestCasesCommand = new DelegateCommand(ExecuteExportTestCases, CanExecuteExportTestCases);
            CompareTestCasesCommand = new DelegateCommand(ExecuteCompareTestCases, CanExecuteCompareTestCases);
            RefreshCommand = new DelegateCommand(ExecuteRefresh);
            GenerateUnitTestCommand = new DelegateCommand(ExecuteGenerateUnitTest);

            LoadTestCases();
        }

        private async void LoadTestCases()
        {
            try
            {
                IsLoading = true;
                StatusMessage = "Loading test cases...";

                var testCases = await _testCaseService.GetAllTestCasesAsync();
                TestCases = new ObservableCollection<TestCase>(testCases);

                //StatusMessage = $"Loaded {testCases.Count} test cases";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading test cases: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ExecuteAddTestCase()
        {
            var parameters = new DialogParameters
            {
                { "Mode", "Add" },
                { "Functions", GetFunctions() }
            };

            _dialogService.ShowDialog("TestCaseEditorDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("TestCase"))
                {
                    var newTestCase = result.Parameters.GetValue<Models.TestCases.TestCase>("TestCase");
                    TestCases.Add(newTestCase);
                    StatusMessage = $"Added test case: {newTestCase.Name}";
                }
            });
        }

        private bool CanExecuteTestCaseCommand(Models.TestCases.TestCase testCase)
        {
            return testCase != null;
        }

        private void ExecuteEditTestCase(Models.TestCases.TestCase testCase)
        {
            var parameters = new DialogParameters
            {
                { "Mode", "Edit" },
                { "TestCase", testCase },
                { "Functions", GetFunctions() }
            };

            _dialogService.ShowDialog("TestCaseEditorDialog", parameters, result =>
            {
                if (result.Result == ButtonResult.OK && result.Parameters.ContainsKey("TestCase"))
                {
                    var updatedTestCase = result.Parameters.GetValue<TestCase>("TestCase");
                    var index = TestCases.IndexOf(testCase);
                    if (index >= 0)
                    {
                        TestCases[index] = updatedTestCase;
                        StatusMessage = $"Updated test case: {updatedTestCase.Name}";
                    }
                }
            });
        }

        private void ExecuteDuplicateTestCase(TestCase testCase)
        {
            var duplicate = testCase.Clone();
            _testCaseService.AddTestCaseAsync(duplicate);
            TestCases.Add(duplicate);
            StatusMessage = $"Duplicated test case: {testCase.Name} -> {duplicate.Name}";
        }

        private async void ExecuteDeleteTestCase(Models.TestCases.TestCase testCase)
        {
            var parameters = new DialogParameters
            {
                { "Title", "Confirm Delete" },
                { "Message", $"Are you sure you want to delete the test case '{testCase.Name}'?" }
            };

            _dialogService.ShowDialog("ConfirmationDialog", parameters, async result =>
            {
                if (result.Result == ButtonResult.OK)
                {
                    try
                    {
                        //var success = await _testCaseService.DeleteTestCaseAsync(testCase.Id);
                        //if (success)
                        //{
                        //    TestCases.Remove(testCase);
                        //    StatusMessage = $"Deleted test case: {testCase.Name}";
                        //}
                        //else
                        //{
                        //    StatusMessage = "Failed to delete test case";
                        //}
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Error deleting test case: {ex.Message}";
                    }
                }
            });
        }

        private void ExecuteImportTestCases()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "All Supported Files|*.tst;*.csv;*.xlsx|Test Case Files (*.tst)|*.tst|CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Title = "Import Test Cases"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    StatusMessage = $"Importing test cases from {Path.GetFileName(dialog.FileName)}...";
                    IsLoading = true;

                    var extension = Path.GetExtension(dialog.FileName).ToLower();
                    List<Models.TestCases.TestCase> importedTestCases = null;

                    switch (extension)
                    {
                        //case ".tst":
                        //    importedTestCases = _testCaseService.ImportFromTstFileAsync(dialog.FileName).Result;
                        //    break;
                        //case ".csv":
                        //    importedTestCases = _testCaseService.ImportFromCsvFileAsync(dialog.FileName).Result;
                        //    break;
                        //case ".xlsx":
                        //    importedTestCases = _testCaseService.ImportFromExcelFileAsync(dialog.FileName).Result;
                        //    break;
                        default:
                            StatusMessage = "Unsupported file format";
                            return;
                    }

                    foreach (var testCase in importedTestCases)
                    {
                        TestCases.Add(testCase);
                    }

                    StatusMessage = $"Imported {importedTestCases.Count} test cases";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error importing test cases: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private bool CanExecuteExportTestCases()
        {
            return TestCases.Count > 0;
        }

        private void ExecuteExportTestCases()
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Test Case Files (*.tst)|*.tst|CSV Files (*.csv)|*.csv|Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                Title = "Export Test Cases"
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    StatusMessage = $"Exporting test cases to {Path.GetFileName(dialog.FileName)}...";
                    IsLoading = true;

                    var extension = Path.GetExtension(dialog.FileName).ToLower();

                    switch (extension)
                    {
                        //case ".tst":
                        //    _testCaseService.ExportToTstFileAsync(TestCases.ToList(), dialog.FileName).Wait();
                        //    break;
                        //case ".csv":
                        //    _testCaseService.ExportToCsvFileAsync(TestCases.ToList(), dialog.FileName).Wait();
                        //    break;
                        //case ".xlsx":
                        //    _testCaseService.ExportToExcelFileAsync(TestCases.ToList(), dialog.FileName).Wait();
                        //    break;
                        default:
                            StatusMessage = "Unsupported file format";
                            return;
                    }

                    StatusMessage = $"Exported {TestCases.Count} test cases";
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error exporting test cases: {ex.Message}";
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        private bool CanExecuteCompareTestCases()
        {
            return TestCases.Count >= 2;
        }

        private void ExecuteCompareTestCases()
        {
            var parameters = new DialogParameters
            {
                { "TestCases", TestCases }
            };

            _dialogService.ShowDialog("TestCaseComparisonDialog", parameters, result =>
            {
                // Handle any result if needed
            });
        }

        private void ExecuteRefresh()
        {
            LoadTestCases();
        }

        private void ExecuteGenerateUnitTest()
        {
            var parameters = new DialogParameters
            {
                { "Functions", GetFunctions() }
            };

            _dialogService.ShowDialog("GenerateTestCaseDialog", parameters, async result =>
            {
                if (result.Result == ButtonResult.OK &&
                    result.Parameters.ContainsKey("Function") &&
                    result.Parameters.ContainsKey("Type"))
                {
                    var function = result.Parameters.GetValue<CFunction>("Function");
                    var type = result.Parameters.GetValue<TestCaseType>("Type");

                    try
                    {
                        IsLoading = true;
                        StatusMessage = $"Generating {type} for function {function.Name}...";

                        Models.TestCases.TestCase generatedTestCase;

                        if (type == TestCaseType.UnitTest)
                        {
                            //generatedTestCase = await _testCaseService.GenerateUnitTestCaseAsync(function);
                        }
                        else
                        {
                            // For integration test, we'd need to get related functions
                            // This is a simplified version
                            var relatedFunctions = GetFunctions()
                                .Where(f => f.Name != function.Name)
                                .Take(3)
                                .ToList();

                            var functions = new List<CFunction> { function };
                            //functions.AddRange(relatedFunctions);

                            //generatedTestCase = await _testCaseService.GenerateIntegrationTestCaseAsync(functions);
                        }

                        // Save the generated test case
                        //var savedTestCase = await _testCaseService.CreateTestCaseAsync(generatedTestCase);
                        //TestCases.Add(savedTestCase);

                        StatusMessage = $"Generated {type} for function {function.Name}";

                        // Open the generated test case for editing
                        //ExecuteEditTestCase(savedTestCase);
                    }
                    catch (Exception ex)
                    {
                        StatusMessage = $"Error generating test case: {ex.Message}";
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                }
            });
        }

        private List<CFunction> GetFunctions()
        {
            // This would normally come from the parser
            // For now, return some sample functions
            return new List<CFunction>
            {
                new CFunction
                {
                    Name = "sum",
                    ReturnType = "int",
                    Parameters = new List<CVariable>
                    {
                        new CVariable { Name = "a", TypeName = "int" },
                        new CVariable { Name = "b", TypeName = "int" }
                    }
                },
                new CFunction
                {
                    Name = "subtract",
                    ReturnType = "int",
                    Parameters = new List<CVariable>
                    {
                        new CVariable { Name = "a", TypeName = "int" },
                        new CVariable { Name = "b", TypeName = "int" }
                    }
                },
                new CFunction
                {
                    Name = "multiply",
                    ReturnType = "int",
                    Parameters = new List<CVariable>
                    {
                        new CVariable { Name = "a", TypeName = "int" },
                        new CVariable { Name = "b", TypeName = "int" }
                    }
                },
                new CFunction
                {
                    Name = "divide",
                    ReturnType = "double",
                    Parameters = new List<CVariable>
                    {
                        new CVariable { Name = "a", TypeName = "int" },
                        new CVariable { Name = "b", TypeName = "int" }
                    }
                },
                new CFunction
                {
                    Name = "printMessage",
                    ReturnType = "void",
                    Parameters = new List<CVariable>
                    {
                        new CVariable { Name = "message", TypeName = "const char*" }
                    }
                }
            };
        }
    }
}