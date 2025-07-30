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
        private TestCase _originalTestCase;

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

        private ObservableCollection<TestCaseVariableInput> _inputs;
        public ObservableCollection<TestCaseVariableInput> Inputs
        {
            get => _inputs;
            set => SetProperty(ref _inputs, value);
        }

        private TestCaseVariableInput _selectedInput;
        public TestCaseVariableInput SelectedInput
        {
            get => _selectedInput;
            set => SetProperty(ref _selectedInput, value, () => RemoveInputCommand.RaiseCanExecuteChanged());
        }

        private ObservableCollection<TestCaseVariableOutput> _expectedOutputs;
        public ObservableCollection<TestCaseVariableOutput> ExpectedOutputs
        {
            get => _expectedOutputs;
            set => SetProperty(ref _expectedOutputs, value);
        }

        private TestCaseVariableOutput _selectedExpectedOutput;
        public TestCaseVariableOutput SelectedExpectedOutput
        {
            get => _selectedExpectedOutput;
            set => SetProperty(ref _selectedExpectedOutput, value, () => RemoveExpectedOutputCommand.RaiseCanExecuteChanged());
        }

        private ObservableCollection<TestCaseVariableOutput> _actualOutputs;
        public ObservableCollection<TestCaseVariableOutput> ActualOutputs
        {
            get => _actualOutputs;
            set => SetProperty(ref _actualOutputs, value);
        }

        private TestCaseVariableOutput _selectedActualOutput;
        public TestCaseVariableOutput SelectedActualOutput
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

        public EditTestCaseDialogViewModel(TestCase testCase = null)
        {
            // Initialize collections
            Inputs = new ObservableCollection<TestCaseVariableInput>();
            ExpectedOutputs = new ObservableCollection<TestCaseVariableOutput>();
            ActualOutputs = new ObservableCollection<TestCaseVariableOutput>();

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
            Status = TestCaseStatus.NotRun;

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
            var input = new TestCaseVariableInput
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"input{Inputs.Count + 1}",
                Type = "int",
                Value = "0",
                IsStubParameter = false
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
            var output = new TestCaseVariableOutput
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"output{ExpectedOutputs.Count + 1}",
                Type = "int",
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
            var output = new TestCaseVariableOutput
            {
                Id = Guid.NewGuid().ToString(),
                Name = $"output{ActualOutputs.Count + 1}",
                Type = "int",
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
                Status = TestCaseStatus.NotRun;
                Inputs.Clear();
                ExpectedOutputs.Clear();
                ActualOutputs.Clear();
            }
        }

        #endregion

        #region Helper Methods

        private void LoadTestCase(TestCase testCase)
        {
            // Basic properties
            Name = testCase.Name;
            Description = testCase.Description;
            FunctionName = testCase.FunctionName;
            Type = testCase.Type;
            Status = testCase.Status;

            // Clear and reload collections
            Inputs.Clear();
            foreach (var input in testCase.InputVariables)
            {
                Inputs.Add(CloneTestCaseInput(input));
            }

            ExpectedOutputs.Clear();
            foreach (var output in testCase.OutputVariables)
            {
                ExpectedOutputs.Add(CloneTestCaseOutput(output));
            }

            ActualOutputs.Clear();
            foreach (var output in testCase.ActualOutputs)
            {
                ActualOutputs.Add(CloneTestCaseOutput(output));
            }
        }

        private TestCaseVariableInput CloneTestCaseInput(TestCaseVariableInput input)
        {
            return new TestCaseVariableInput
            {
                Id = input.Id,
                Name = input.Name,
                Type = input.Type,
                Value = input.Value,
                IsStubParameter = input.IsStubParameter
            };
        }

        private TestCaseVariableOutput CloneTestCaseOutput(TestCaseVariableOutput output)
        {
            return new TestCaseVariableOutput
            {
                Id = output.Id,
                Name = output.Name,
                Type = output.Type,
                Value = output.Value
            };
        }

        public TestCase GetUpdatedTestCase()
        {
            TestCase testCase = _originalTestCase ?? new TestCase
            {
                Id = Guid.NewGuid().ToString(),
                CreationDate = DateTime.Now
            };

            // Update basic properties
            testCase.Name = Name;
            testCase.Description = Description;
            testCase.FunctionName = FunctionName;
            testCase.Type = Type;
            testCase.Status = Status;
            testCase.LastModifiedDate = DateTime.Now;

            // Update collections
            testCase.InputVariables = Inputs.ToList();
            testCase.OutputVariables = ExpectedOutputs.ToList();
            testCase.ActualOutputs = ActualOutputs.ToList();

            return testCase;
        }

        #endregion
    }
}