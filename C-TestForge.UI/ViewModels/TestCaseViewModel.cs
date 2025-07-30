using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.Solver;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Core.Services;
using C_TestForge.Models.Core;
using C_TestForge.Models.TestCases;
using C_TestForge.Parser;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace C_TestForge.UI.ViewModels
{
    /// <summary>
    /// ViewModel for managing test cases in the C-TestForge application.
    /// Handles import/export, management, comparison, and automatic generation of test cases.
    /// </summary>
    public class TestCaseViewModel : BindableBase
    {
        #region Private Fields

        private readonly ITestCaseService _testCaseService;
        private readonly IClangSharpParserService _parserService;
        private readonly IZ3SolverService _solverService;
        private readonly IEventAggregator _eventAggregator;

        private ObservableCollection<TestCase> _testCases;
        private TestCase _selectedTestCase;
        private string _statusMessage;
        private bool _isBusy;
        private bool _isComparisonMode;
        private TestCase _testCaseForComparison;
        private bool _showOnlyDifferences;
        private string _searchFilter;
        private bool _autoUpdateExpectedOutput;

        #endregion

        #region Properties

        /// <summary>
        /// Collection of test cases displayed in the UI
        /// </summary>
        public ObservableCollection<TestCase> TestCases
        {
            get => _testCases;
            set => SetProperty(ref _testCases, value);
        }

        /// <summary>
        /// Currently selected test case
        /// </summary>
        public TestCase SelectedTestCase
        {
            get => _selectedTestCase;
            set
            {
                if (SetProperty(ref _selectedTestCase, value))
                {
                    // Update commands that depend on selection
                    DeleteTestCaseCommand.RaiseCanExecuteChanged();
                    EditTestCaseCommand.RaiseCanExecuteChanged();
                    DuplicateTestCaseCommand.RaiseCanExecuteChanged();
                    SelectForComparisonCommand.RaiseCanExecuteChanged();
                    FindVariableValuesCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Status message displayed to the user
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Flag indicating whether an operation is in progress
        /// </summary>
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    // Update all commands when busy state changes
                    ImportTestCasesCommand.RaiseCanExecuteChanged();
                    ExportTestCasesCommand.RaiseCanExecuteChanged();
                    AddTestCaseCommand.RaiseCanExecuteChanged();
                    DeleteTestCaseCommand.RaiseCanExecuteChanged();
                    EditTestCaseCommand.RaiseCanExecuteChanged();
                    DuplicateTestCaseCommand.RaiseCanExecuteChanged();
                    GenerateTestCasesCommand.RaiseCanExecuteChanged();
                    SelectForComparisonCommand.RaiseCanExecuteChanged();
                    FindVariableValuesCommand.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Flag indicating whether comparison mode is active
        /// </summary>
        public bool IsComparisonMode
        {
            get => _isComparisonMode;
            set => SetProperty(ref _isComparisonMode, value);
        }

        /// <summary>
        /// Test case used for comparison
        /// </summary>
        public TestCase TestCaseForComparison
        {
            get => _testCaseForComparison;
            set => SetProperty(ref _testCaseForComparison, value);
        }

        /// <summary>
        /// Flag to show only differences when comparing test cases
        /// </summary>
        public bool ShowOnlyDifferences
        {
            get => _showOnlyDifferences;
            set => SetProperty(ref _showOnlyDifferences, value);
        }

        /// <summary>
        /// Search filter for test cases
        /// </summary>
        public string SearchFilter
        {
            get => _searchFilter;
            set
            {
                if (SetProperty(ref _searchFilter, value))
                {
                    FilterTestCases();
                }
            }
        }

        /// <summary>
        /// Flag to automatically update expected output when input values change
        /// </summary>
        public bool AutoUpdateExpectedOutput
        {
            get => _autoUpdateExpectedOutput;
            set => SetProperty(ref _autoUpdateExpectedOutput, value);
        }

        #endregion

        #region Commands

        public DelegateCommand ImportTestCasesCommand { get; }
        public DelegateCommand ExportTestCasesCommand { get; }
        public DelegateCommand AddTestCaseCommand { get; }
        public DelegateCommand DeleteTestCaseCommand { get; }
        public DelegateCommand EditTestCaseCommand { get; }
        public DelegateCommand DuplicateTestCaseCommand { get; }
        public DelegateCommand GenerateTestCasesCommand { get; }
        public DelegateCommand SelectForComparisonCommand { get; }
        public DelegateCommand ExitComparisonModeCommand { get; }
        public DelegateCommand FindVariableValuesCommand { get; }
        public DelegateCommand<string> ExportToSpecificFormatCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor for TestCaseViewModel
        /// </summary>
        /// <param name="testCaseService">Service for managing test cases</param>
        /// <param name="parserService">Service for parsing C code</param>
        /// <param name="solverService">Service for solving constraints using Z3</param>
        /// <param name="eventAggregator">Event aggregator for communication between ViewModels</param>
        public TestCaseViewModel(
            ITestCaseService testCaseService,
            IClangSharpParserService parserService,
            IZ3SolverService solverService,
            IEventAggregator eventAggregator)
        {
            _testCaseService = testCaseService ?? throw new ArgumentNullException(nameof(testCaseService));
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _solverService = solverService ?? throw new ArgumentNullException(nameof(solverService));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));

            // Initialize collections
            _testCases = new ObservableCollection<TestCase>();

            // Initialize commands
            ImportTestCasesCommand = new DelegateCommand(ImportTestCases, CanExecuteCommand);
            ExportTestCasesCommand = new DelegateCommand(ExportTestCases, CanExecuteCommand);
            AddTestCaseCommand = new DelegateCommand(AddTestCase, CanExecuteCommand);
            DeleteTestCaseCommand = new DelegateCommand(DeleteTestCase, CanDeleteTestCase);
            EditTestCaseCommand = new DelegateCommand(EditTestCase, CanEditTestCase);
            DuplicateTestCaseCommand = new DelegateCommand(DuplicateTestCase, CanDuplicateTestCase);
            GenerateTestCasesCommand = new DelegateCommand(GenerateTestCases, CanExecuteCommand);
            SelectForComparisonCommand = new DelegateCommand(SelectForComparison, CanSelectForComparison);
            ExitComparisonModeCommand = new DelegateCommand(ExitComparisonMode);
            FindVariableValuesCommand = new DelegateCommand(FindVariableValues, CanFindVariableValues);
            ExportToSpecificFormatCommand = new DelegateCommand<string>(ExportToSpecificFormat);

            // Set default values
            StatusMessage = "Ready";
            IsBusy = false;
            IsComparisonMode = false;
            ShowOnlyDifferences = false;
            AutoUpdateExpectedOutput = false;

            // Subscribe to events from other ViewModels
            SubscribeToEvents();

            // Load initial test cases
            LoadTestCases();
        }

        #endregion

        #region Command Methods

        /// <summary>
        /// Import test cases from file
        /// </summary>
        private void ImportTestCases()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Importing test cases...";

                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Test Case Files (*.tst;*.csv;*.xlsx)|*.tst;*.csv;*.xlsx|All files (*.*)|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var fileName = openFileDialog.FileName;
                    var extension = Path.GetExtension(fileName).ToLower();

                    List<TestCase> importedTestCases = new List<TestCase>();

                    switch (extension)
                    {
                        //case ".tst":
                        //    importedTestCases = _testCaseService.ImportFromTstFile(fileName);
                        //    break;
                        //case ".csv":
                        //    importedTestCases = _testCaseService.ImportFromCsvFile(fileName);
                        //    break;
                        //case ".xlsx":
                        //    importedTestCases = _testCaseService.ImportFromExcelFile(fileName);
                        //    break;
                        default:
                            MessageBox.Show("Unsupported file format.", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }

                    if (importedTestCases.Any())
                    {
                        // Add imported test cases to collection
                        foreach (var testCase in importedTestCases)
                        {
                            TestCases.Add(testCase);
                        }

                        StatusMessage = $"Successfully imported {importedTestCases.Count} test cases.";

                        // Notify any listeners that test cases have been imported
                        //_eventAggregator.GetEvent<TestCasesImportedEvent>().Publish(importedTestCases);
                    }
                    else
                    {
                        StatusMessage = "No test cases were imported.";
                    }
                }
                else
                {
                    StatusMessage = "Import cancelled.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importing test cases: {ex.Message}";
                MessageBox.Show($"Error importing test cases: {ex.Message}", "Import Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Export test cases to file
        /// </summary>
        private void ExportTestCases()
        {
            try
            {
                if (!TestCases.Any())
                {
                    MessageBox.Show("No test cases to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsBusy = true;
                StatusMessage = "Exporting test cases...";

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Test Case File (*.tst)|*.tst|CSV File (*.csv)|*.csv|Excel File (*.xlsx)|*.xlsx|All files (*.*)|*.*",
                    DefaultExt = ".tst"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var fileName = saveFileDialog.FileName;
                    var extension = Path.GetExtension(fileName).ToLower();

                    switch (extension)
                    {
                        //case ".tst":
                        //    _testCaseService.ExportToTstFile(TestCases.ToList(), fileName);
                        //    break;
                        //case ".csv":
                        //    _testCaseService.ExportToCsvFile(TestCases.ToList(), fileName);
                        //    break;
                        //case ".xlsx":
                        //    _testCaseService.ExportToExcelFile(TestCases.ToList(), fileName);
                        //    break;
                        default:
                            MessageBox.Show("Unsupported file format.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }

                    StatusMessage = $"Successfully exported {TestCases.Count} test cases.";
                }
                else
                {
                    StatusMessage = "Export cancelled.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting test cases: {ex.Message}";
                MessageBox.Show($"Error exporting test cases: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Export test cases to a specific format
        /// </summary>
        /// <param name="format">The export format (tst, csv, xlsx)</param>
        private void ExportToSpecificFormat(string format)
        {
            try
            {
                if (!TestCases.Any())
                {
                    MessageBox.Show("No test cases to export.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsBusy = true;
                StatusMessage = "Exporting test cases...";

                string extension = "." + format.ToLower();
                string filter = format.ToUpper() + " File (*" + extension + ")|*" + extension;

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = filter,
                    DefaultExt = extension
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var fileName = saveFileDialog.FileName;

                    switch (format.ToLower())
                    {
                        //case "tst":
                        //    _testCaseService.ExportToTstFile(TestCases.ToList(), fileName);
                        //    break;
                        //case "csv":
                        //    _testCaseService.ExportToCsvFile(TestCases.ToList(), fileName);
                        //    break;
                        //case "xlsx":
                        //    _testCaseService.ExportToExcelFile(TestCases.ToList(), fileName);
                        //    break;
                        default:
                            MessageBox.Show("Unsupported file format.", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            break;
                    }

                    StatusMessage = $"Successfully exported {TestCases.Count} test cases to {format.ToUpper()} format.";
                }
                else
                {
                    StatusMessage = "Export cancelled.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error exporting test cases: {ex.Message}";
                MessageBox.Show($"Error exporting test cases: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Add a new test case
        /// </summary>
        private void AddTestCase()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Adding new test case...";

                // Create a new test case with default values
                //var newTestCase = new TestCase
                //{
                //    Id = Guid.NewGuid(),
                //    Name = $"TestCase_{TestCases.Count + 1}",
                //    Description = "New test case",
                //    InputParameters = new Dictionary<string, string>(),
                //    ExpectedOutputs = new Dictionary<string, string>(),
                //    ActualOutputs = new Dictionary<string, string>(),
                //    IsEnabled = true,
                //    CreatedDate = DateTime.Now,
                //    ModifiedDate = DateTime.Now
                //};

                //// Show dialog to edit the new test case
                //var result = _eventAggregator.GetEvent<EditTestCaseEvent>().Publish(newTestCase);

                // If the user confirmed the edit, add the test case
                //if (result)
                //{
                //    TestCases.Add(newTestCase);
                //    SelectedTestCase = newTestCase;
                //    StatusMessage = "New test case added.";

                //    // Save the updated test cases
                //    SaveTestCases();
                //}
                //else
                //{
                //    StatusMessage = "Add test case cancelled.";
                //}
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error adding test case: {ex.Message}";
                MessageBox.Show($"Error adding test case: {ex.Message}", "Add Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Delete the selected test case
        /// </summary>
        private void DeleteTestCase()
        {
            try
            {
                if (SelectedTestCase == null)
                    return;

                IsBusy = true;
                StatusMessage = "Deleting test case...";

                var result = MessageBox.Show(
                    $"Are you sure you want to delete the test case '{SelectedTestCase.Name}'?",
                    "Confirm Delete",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    TestCases.Remove(SelectedTestCase);
                    SelectedTestCase = TestCases.FirstOrDefault();
                    StatusMessage = "Test case deleted.";

                    // Save the updated test cases
                    SaveTestCases();
                }
                else
                {
                    StatusMessage = "Delete cancelled.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting test case: {ex.Message}";
                MessageBox.Show($"Error deleting test case: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Edit the selected test case
        /// </summary>
        private void EditTestCase()
        {
            try
            {
                if (SelectedTestCase == null)
                    return;

                IsBusy = true;
                StatusMessage = "Editing test case...";

                // Create a copy of the test case for editing
                var testCaseCopy = SelectedTestCase.Clone();

                // Show dialog to edit the test case
                //var result = _eventAggregator.GetEvent<EditTestCaseEvent>().Publish(testCaseCopy);

                //// If the user confirmed the edit, update the test case
                //if (result)
                //{
                //    // Update the original test case with the edited values
                //    SelectedTestCase.Name = testCaseCopy.Name;
                //    SelectedTestCase.Description = testCaseCopy.Description;
                //    SelectedTestCase.Inputs = new Dictionary<string, string>(testCaseCopy.Inputs);
                //    SelectedTestCase.ExpectedOutputs = new Dictionary<string, string>(testCaseCopy.ExpectedOutputs);
                //    SelectedTestCase.IsEnabled = testCaseCopy.IsEnabled;
                //    SelectedTestCase.ModifiedDate = DateTime.Now;

                //    // Notify property changes
                //    RaisePropertyChanged(nameof(SelectedTestCase));

                //    StatusMessage = "Test case updated.";

                //    // Save the updated test cases
                //    SaveTestCases();
                //}
                //else
                //{
                //    StatusMessage = "Edit cancelled.";
                //}
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error editing test case: {ex.Message}";
                MessageBox.Show($"Error editing test case: {ex.Message}", "Edit Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Duplicate the selected test case
        /// </summary>
        private void DuplicateTestCase()
        {
            try
            {
                if (SelectedTestCase == null)
                    return;

                IsBusy = true;
                StatusMessage = "Duplicating test case...";

                // Create a copy of the test case
                var duplicatedTestCase = SelectedTestCase.Clone();

                // Update the duplicated test case properties
                //duplicatedTestCase.Id = Guid.NewGuid();
                //duplicatedTestCase.Name = $"{SelectedTestCase.Name}_Copy";
                //duplicatedTestCase.CreatedDate = DateTime.Now;
                //duplicatedTestCase.ModifiedDate = DateTime.Now;

                // Add the duplicated test case to the collection
                TestCases.Add(duplicatedTestCase);
                SelectedTestCase = duplicatedTestCase;

                StatusMessage = "Test case duplicated.";

                // Save the updated test cases
                SaveTestCases();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error duplicating test case: {ex.Message}";
                MessageBox.Show($"Error duplicating test case: {ex.Message}", "Duplication Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Generate test cases automatically based on code analysis
        /// </summary>
        private async void GenerateTestCases()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Generating test cases...";

                // Show dialog to select a function for which to generate test cases
                //var selectedFunction = await _eventAggregator.GetEvent<SelectFunctionEvent>().PublishAsync();

                //if (selectedFunction != null)
                //{
                //    // Generate test cases for the selected function
                //    var generatedTestCases = await Task.Run(() =>
                //       // _testCaseService.GenerateTestCasesForFunction(selectedFunction)
                //    );

                //    if (generatedTestCases.Any())
                //    {
                //        // Add generated test cases to the collection
                //        foreach (var testCase in generatedTestCases)
                //        {
                //            TestCases.Add(testCase);
                //        }

                //        SelectedTestCase = generatedTestCases.First();
                //        StatusMessage = $"Successfully generated {generatedTestCases.Count} test cases.";

                //        // Save the updated test cases
                //        SaveTestCases();
                //    }
                //    else
                //    {
                //        StatusMessage = "No test cases were generated.";
                //    }
                //}
                //else
                //{
                //    StatusMessage = "Test case generation cancelled.";
                //}
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating test cases: {ex.Message}";
                MessageBox.Show($"Error generating test cases: {ex.Message}", "Generation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Select the current test case for comparison
        /// </summary>
        private void SelectForComparison()
        {
            try
            {
                if (SelectedTestCase == null)
                    return;

                IsBusy = true;
                StatusMessage = "Setting up comparison mode...";

                // Set the test case for comparison
                TestCaseForComparison = SelectedTestCase.Clone();
                IsComparisonMode = true;

                StatusMessage = $"Comparison mode active. Comparing with '{TestCaseForComparison.Name}'.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error setting up comparison: {ex.Message}";
                MessageBox.Show($"Error setting up comparison: {ex.Message}", "Comparison Error", MessageBoxButton.OK, MessageBoxImage.Error);
                IsComparisonMode = false;
                TestCaseForComparison = null;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Exit comparison mode
        /// </summary>
        private void ExitComparisonMode()
        {
            IsComparisonMode = false;
            TestCaseForComparison = null;
            StatusMessage = "Comparison mode exited.";
        }

        /// <summary>
        /// Find appropriate variable values using Z3 solver
        /// </summary>
        private async void FindVariableValues()
        {
            try
            {
                if (SelectedTestCase == null)
                    return;

                IsBusy = true;
                StatusMessage = "Finding variable values...";

                // Get the constraints for the test case
                //var constraints = await _eventAggregator.GetEvent<GetConstraintsEvent>().PublishAsync(SelectedTestCase);

                //if (constraints != null && constraints.Any())
                //{
                //    // Use Z3 solver to find values that satisfy the constraints
                //    var solverResult = await Task.Run(() =>
                //        _solverService.SolveConstraints(constraints)
                //    );

                //    if (solverResult.IsSuccess)
                //    {
                //        // Update the test case with the found values
                //        foreach (var kvp in solverResult.VariableValues)
                //        {
                //            if (SelectedTestCase.Inputs.ContainsKey(kvp.Key))
                //            {
                //                SelectedTestCase.Inputs[kvp.Key] = kvp.Value;
                //            }
                //            else
                //            {
                //                SelectedTestCase.Inputs.Add(kvp.Key, kvp.Value);
                //            }
                //        }

                //        // Update expected outputs if auto-update is enabled
                //        if (AutoUpdateExpectedOutput)
                //        {
                //            await UpdateExpectedOutputs();
                //        }

                //        // Notify property changes
                //        RaisePropertyChanged(nameof(SelectedTestCase));

                //        StatusMessage = "Variable values found and applied.";

                //        // Save the updated test cases
                //        SaveTestCases();
                //    }
                //    else
                //    {
                //        StatusMessage = $"Could not find variable values: {solverResult.ErrorMessage}";
                //        MessageBox.Show($"Could not find variable values: {solverResult.ErrorMessage}", "Solver Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                //    }
                //}
                //else
                //{
                //    StatusMessage = "No constraints were provided for solving.";
                //}
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error finding variable values: {ex.Message}";
                MessageBox.Show($"Error finding variable values: {ex.Message}", "Solver Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Load test cases from storage
        /// </summary>
        private void LoadTestCases()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "Loading test cases...";

                //var loadedTestCases = _testCaseService.LoadTestCases();

                //TestCases.Clear();
                //foreach (var testCase in loadedTestCases)
                //{
                //    TestCases.Add(testCase);
                //}

                SelectedTestCase = TestCases.FirstOrDefault();

                StatusMessage = $"Loaded {TestCases.Count} test cases.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading test cases: {ex.Message}";
                MessageBox.Show($"Error loading test cases: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>
        /// Save test cases to storage
        /// </summary>
        private void SaveTestCases()
        {
            try
            {
                //_testCaseService.SaveTestCases(TestCases.ToList());
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving test cases: {ex.Message}";
                MessageBox.Show($"Error saving test cases: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Filter test cases based on search text
        /// </summary>
        private void FilterTestCases()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(SearchFilter))
                {
                    // Load all test cases
                    LoadTestCases();
                    return;
                }

                var filter = SearchFilter.ToLower();

                // Apply filter
                //var filteredTestCases = _testCaseService.LoadTestCases().Where(tc =>
                //    tc.Name.ToLower().Contains(filter) ||
                //    tc.Description.ToLower().Contains(filter) ||
                //    tc.Inputs.Any(i => i.Key.ToLower().Contains(filter) || i.Value.ToLower().Contains(filter)) ||
                //    tc.ExpectedOutputs.Any(o => o.Key.ToLower().Contains(filter) || o.Value.ToLower().Contains(filter))
                //).ToList();

                // Update the collection
                //TestCases.Clear();
                //foreach (var testCase in filteredTestCases)
                //{
                //    TestCases.Add(testCase);
                //}

                SelectedTestCase = TestCases.FirstOrDefault();

                StatusMessage = $"Found {TestCases.Count} test cases matching '{SearchFilter}'.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error filtering test cases: {ex.Message}";
            }
        }

        /// <summary>
        /// Update expected outputs based on current inputs
        /// </summary>
        private async Task UpdateExpectedOutputs()
        {
            try
            {
                if (SelectedTestCase == null)
                    return;

                // Get the function associated with the test case
                //var function = await _eventAggregator.GetEvent<GetFunctionForTestCaseEvent>().PublishAsync(SelectedTestCase);

                //if (function != null)
                //{
                //    // Execute the function with the current inputs to get expected outputs
                //    var expectedOutputs = await Task.Run(() =>
                //        _testCaseService.CalculateExpectedOutputs(function, SelectedTestCase.Inputs)
                //    );

                //    if (expectedOutputs != null)
                //    {
                //        // Update the expected outputs
                //        SelectedTestCase.ExpectedOutputs = new Dictionary<string, string>(expectedOutputs);

                //        // Notify property changes
                //        RaisePropertyChanged(nameof(SelectedTestCase));
                //    }
                //}
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error updating expected outputs: {ex.Message}";
            }
        }

        /// <summary>
        /// Subscribe to events from other ViewModels
        /// </summary>
        private void SubscribeToEvents()
        {
            // Subscribe to code analysis completed event
            //_eventAggregator.GetEvent<CodeAnalysisCompletedEvent>().Subscribe(OnCodeAnalysisCompleted);

            // Subscribe to test execution completed event
            _eventAggregator.GetEvent<TestExecutionCompletedEvent>().Subscribe(OnTestExecutionCompleted);
        }

        /// <summary>
        /// Handler for code analysis completed event
        /// </summary>
        /// <param name="analysisResult">The result of the code analysis</param>
        //private void OnCodeAnalysisCompleted(CodeAnalysisResult analysisResult)
        //{
        //    // Update test cases based on code analysis result
        //    // For example, check if variables used in test cases are still valid

        //    StatusMessage = "Code analysis completed. Checking test cases for compatibility...";

        //    // Detect disabled variables
        //    var disabledVariables = analysisResult.DisabledVariables ?? new List<string>();

        //    // Check each test case for disabled variables
        //    foreach (var testCase in TestCases)
        //    {
        //        var usesDisabledVariables = testCase.Inputs.Keys
        //            .Intersect(disabledVariables, StringComparer.OrdinalIgnoreCase)
        //            .Any();

        //        if (usesDisabledVariables)
        //        {
        //            // Mark test case with warning
        //            testCase.HasWarning = true;
        //            testCase.WarningMessage = "Test case uses disabled variables.";
        //        }
        //        else
        //        {
        //            testCase.HasWarning = false;
        //            testCase.WarningMessage = null;
        //        }
        //    }

        //    // Notify UI of changes
        //    RaisePropertyChanged(nameof(TestCases));

        //    StatusMessage = "Test cases updated based on code analysis.";
        //}

        /// <summary>
        /// Handler for test execution completed event
        /// </summary>
        /// <param name="testResults">The results of the test execution</param>
        private void OnTestExecutionCompleted(List<TestResult> testResults)
        {
            if (testResults == null || !testResults.Any())
                return;

            StatusMessage = "Updating test cases with execution results...";

            // Update actual outputs and test status
            //foreach (var testResult in testResults)
            //{
            //    var testCase = TestCases.FirstOrDefault(tc => tc.Id == testResult.TestCaseId);

            //    if (testCase != null)
            //    {
            //        testCase.ActualOutputs = new Dictionary<string, string>(testResult.ActualOutputs);
            //        testCase.ExecutionStatus = testResult.Status;
            //        testCase.ExecutionMessage = testResult.Message;
            //        testCase.LastExecutionDate = testResult.ExecutionTime;
            //    }
            //}

            // Notify UI of changes
            RaisePropertyChanged(nameof(TestCases));

            StatusMessage = $"Updated {testResults.Count} test cases with execution results.";
        }

        #endregion

        #region Command Can Execute Methods

        /// <summary>
        /// Determines whether a command can be executed
        /// </summary>
        /// <returns>True if command can be executed, false otherwise</returns>
        private bool CanExecuteCommand()
        {
            return !IsBusy;
        }

        /// <summary>
        /// Determines whether the delete test case command can be executed
        /// </summary>
        /// <returns>True if a test case is selected and the application is not busy, false otherwise</returns>
        private bool CanDeleteTestCase()
        {
            return SelectedTestCase != null && !IsBusy;
        }

        /// <summary>
        /// Determines whether the edit test case command can be executed
        /// </summary>
        /// <returns>True if a test case is selected and the application is not busy, false otherwise</returns>
        private bool CanEditTestCase()
        {
            return SelectedTestCase != null && !IsBusy;
        }

        /// <summary>
        /// Determines whether the duplicate test case command can be executed
        /// </summary>
        /// <returns>True if a test case is selected and the application is not busy, false otherwise</returns>
        private bool CanDuplicateTestCase()
        {
            return SelectedTestCase != null && !IsBusy;
        }

        /// <summary>
        /// Determines whether the select for comparison command can be executed
        /// </summary>
        /// <returns>True if a test case is selected, the application is not busy, and not already in comparison mode, false otherwise</returns>
        private bool CanSelectForComparison()
        {
            return SelectedTestCase != null && !IsBusy && !IsComparisonMode;
        }

        /// <summary>
        /// Determines whether the find variable values command can be executed
        /// </summary>
        /// <returns>True if a test case is selected and the application is not busy, false otherwise</returns>
        private bool CanFindVariableValues()
        {
            return SelectedTestCase != null && !IsBusy;
        }

        #endregion
    }

    #region Events

    /// <summary>
    /// Event triggered when test cases are imported
    /// </summary>
    public class TestCasesImportedEvent : PubSubEvent<List<TestCase>> { }

    /// <summary>
    /// Event triggered to edit a test case
    /// </summary>
    public class EditTestCaseEvent : PubSubEvent<TestCase> { }

    /// <summary>
    /// Event triggered to select a function
    /// </summary>
    public class SelectFunctionEvent : PubSubEvent<CFunction> { }

    /// <summary>
    /// Event triggered to get constraints for a test case
    /// </summary>
    //public class GetConstraintsEvent : PubSubEvent<TestCase, List<Constraint>> { }

    ///// <summary>
    ///// Event triggered to get the function associated with a test case
    ///// </summary>
    //public class GetFunctionForTestCaseEvent : PubSubEvent<TestCase, CFunction> { }

    /// <summary>
    /// Event triggered when code analysis is completed
    /// </summary>
    //public class CodeAnalysisCompletedEvent : PubSubEvent<CodeAnalysisResult> { }

    /// <summary>
    /// Event triggered when test execution is completed
    /// </summary>
    public class TestExecutionCompletedEvent : PubSubEvent<List<TestResult>> { }

    #endregion
}