using C_TestForge.Infrastructure.ViewModels;
using C_TestForge.Models.TestCases;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace C_TestForge.UI.ViewModels
{
    public class EditTestCaseDialogViewModel : BindableBase, ITestCaseEditorDialogViewModel
    {
        private Models.TestCases.TestCase _originalTestCase;

        #region Properties

        private string _name;
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        private string _description;
        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        private string _functionName;
        public string FunctionName
        {
            get => _functionName;
            set => SetProperty(ref _functionName, value);
        }

        private TestCaseType _type;
        public TestCaseType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        private TestCaseStatus _status;
        public TestCaseStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        private ObservableCollection<TestCaseInput> _inputs;
        public ObservableCollection<TestCaseInput> Inputs
        {
            get => _inputs;
            set => SetProperty(ref _inputs, value);
        }

        private TestCaseInput _selectedInput;
        public TestCaseInput SelectedInput
        {
            get => _selectedInput;
            set => SetProperty(ref _selectedInput, value, () => RemoveInputCommand.RaiseCanExecuteChanged());
        }

        private ObservableCollection<TestCaseOutput> _expectedOutputs;
        public ObservableCollection<TestCaseOutput> ExpectedOutputs
        {
            get => _expectedOutputs;
            set => SetProperty(ref _expectedOutputs, value);
        }

        private TestCaseOutput _selectedExpectedOutput;
        public TestCaseOutput SelectedExpectedOutput
        {
            get => _selectedExpectedOutput;
            set => SetProperty(ref _selectedExpectedOutput, value, () => RemoveExpectedOutputCommand.RaiseCanExecuteChanged());
        }

        private ObservableCollection<TestCaseOutput> _actualOutputs;
        public ObservableCollection<TestCaseOutput> ActualOutputs
        {
            get => _actualOutputs;
            set => SetProperty(ref _actualOutputs, value);
        }

        private TestCaseOutput _selectedActualOutput;
        public TestCaseOutput SelectedActualOutput
        {
            get => _selectedActualOutput;
            set => SetProperty(ref _selectedActualOutput, value, () => RemoveActualOutputCommand.RaiseCanExecuteChanged());
        }

        // Enum lists for comboboxes
        public Array TestCaseTypes { get; } = Enum.GetValues(typeof(TestCaseType));
        public Array TestCaseStatuses { get; } = Enum.GetValues(typeof(TestCaseStatus));

        #endregion

        #region Commands

        public DelegateCommand AddInputCommand { get; }
        public DelegateCommand RemoveInputCommand { get; }
        public DelegateCommand AddExpectedOutputCommand { get; }
        public DelegateCommand RemoveExpectedOutputCommand { get; }
        public DelegateCommand AddActualOutputCommand { get; }
        public DelegateCommand RemoveActualOutputCommand { get; }
        public DelegateCommand ResetCommand { get; }

        #endregion

        public EditTestCaseDialogViewModel(Models.TestCases.TestCase testCase = null)
        {
            // Initialize collections
            Inputs = new ObservableCollection<TestCaseInput>();
            ExpectedOutputs = new ObservableCollection<TestCaseOutput>();
            ActualOutputs = new ObservableCollection<TestCaseOutput>();

            // Initialize commands
            AddInputCommand = new DelegateCommand(ExecuteAddInput);
            RemoveInputCommand = new DelegateCommand(ExecuteRemoveInput, CanExecuteRemoveInput);
            AddExpectedOutputCommand = new DelegateCommand(ExecuteAddExpectedOutput);
            RemoveExpectedOutputCommand = new DelegateCommand(ExecuteRemoveExpectedOutput, CanExecuteRemoveExpectedOutput);
            AddActualOutputCommand = new DelegateCommand(ExecuteAddActualOutput);
            RemoveActualOutputCommand = new DelegateCommand(ExecuteRemoveActualOutput, CanExecuteRemoveActualOutput);
            ResetCommand = new DelegateCommand(ExecuteReset);

            // Set default values
            Type = TestCaseType.UnitTest;
            Status = TestCaseStatus.NotExecuted;

            // Load test case if provided
            if (testCase != null)
            {
                _originalTestCase = testCase;
                LoadTestCase(testCase);
            }
        }

        #region Command Implementations

        private void ExecuteAddInput()
        {
            var input = new TestCaseInput
            {
                Id = Guid.NewGuid(),
                VariableName = $"input{Inputs.Count + 1}",
                VariableType = "int",
                Value = "0",
                IsStub = false
            };

            Inputs.Add(input);
        }

        private bool CanExecuteRemoveInput()
        {
            return SelectedInput != null;
        }

        private void ExecuteRemoveInput()
        {
            if (SelectedInput != null)
            {
                Inputs.Remove(SelectedInput);
                SelectedInput = null;
            }
        }

        private void ExecuteAddExpectedOutput()
        {
            var output = new TestCaseOutput
            {
                Id = Guid.NewGuid(),
                VariableName = $"output{ExpectedOutputs.Count + 1}",
                VariableType = "int",
                Value = "0"
            };

            ExpectedOutputs.Add(output);
        }

        private bool CanExecuteRemoveExpectedOutput()
        {
            return SelectedExpectedOutput != null;
        }

        private void ExecuteRemoveExpectedOutput()
        {
            if (SelectedExpectedOutput != null)
            {
                ExpectedOutputs.Remove(SelectedExpectedOutput);
                SelectedExpectedOutput = null;
            }
        }

        private void ExecuteAddActualOutput()
        {
            var output = new TestCaseOutput
            {
                Id = Guid.NewGuid(),
                VariableName = $"output{ActualOutputs.Count + 1}",
                VariableType = "int",
                Value = "0"
            };

            ActualOutputs.Add(output);
        }

        private bool CanExecuteRemoveActualOutput()
        {
            return SelectedActualOutput != null;
        }

        private void ExecuteRemoveActualOutput()
        {
            if (SelectedActualOutput != null)
            {
                ActualOutputs.Remove(SelectedActualOutput);
                SelectedActualOutput = null;
            }
        }

        private void ExecuteReset()
        {
            if (_originalTestCase != null)
            {
                LoadTestCase(_originalTestCase);
            }
            else
            {
                // Reset to defaults
                Name = string.Empty;
                Description = string.Empty;
                FunctionName = string.Empty;
                Type = TestCaseType.UnitTest;
                Status = TestCaseStatus.NotExecuted;
                Inputs.Clear();
                ExpectedOutputs.Clear();
                ActualOutputs.Clear();
            }
        }

        #endregion

        #region Helper Methods

        private void LoadTestCase(Models.TestCases.TestCase testCase)
        {
            // Basic properties
            Name = testCase.Name;
            Description = testCase.Description;
            FunctionName = testCase.FunctionName;
            Type = testCase.Type;
            Status = testCase.Status;

            // Clear and reload collections
            Inputs.Clear();
            foreach (var input in testCase.Inputs)
            {
                Inputs.Add(CloneTestCaseInput(input));
            }

            ExpectedOutputs.Clear();
            foreach (var output in testCase.ExpectedOutputs)
            {
                ExpectedOutputs.Add(CloneTestCaseOutput(output));
            }

            ActualOutputs.Clear();
            foreach (var output in testCase.ActualOutputs)
            {
                ActualOutputs.Add(CloneTestCaseOutput(output));
            }
        }

        private TestCaseInput CloneTestCaseInput(TestCaseInput input)
        {
            return new TestCaseInput
            {
                Id = input.Id,
                VariableName = input.VariableName,
                VariableType = input.VariableType,
                Value = input.Value,
                IsStub = input.IsStub
            };
        }

        private TestCaseOutput CloneTestCaseOutput(TestCaseOutput output)
        {
            return new TestCaseOutput
            {
                Id = output.Id,
                VariableName = output.VariableName,
                VariableType = output.VariableType,
                Value = output.Value
            };
        }

        public Models.TestCases.TestCase GetUpdatedTestCase()
        {
            Models.TestCases.TestCase testCase = _originalTestCase ?? new Models.TestCases.TestCase
            {
                Id = Guid.NewGuid(),
                CreatedDate = DateTime.Now
            };

            // Update basic properties
            testCase.Name = Name;
            testCase.Description = Description;
            testCase.FunctionName = FunctionName;
            testCase.Type = Type;
            testCase.Status = Status;
            testCase.ModifiedDate = DateTime.Now;

            // Update collections
            testCase.Inputs = Inputs.ToList();
            testCase.ExpectedOutputs = ExpectedOutputs.ToList();
            testCase.ActualOutputs = ActualOutputs.ToList();

            return testCase;
        }

        #endregion
    }
}