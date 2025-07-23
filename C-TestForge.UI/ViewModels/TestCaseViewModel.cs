using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using C_TestForge.Core.Services;
using C_TestForge.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace C_TestForge.UI.ViewModels
{
    public class TestCaseViewModel : ObservableObject
    {
        private readonly ITestCaseService _testCaseService;
        private readonly ILogger<TestCaseViewModel> _logger;

        private TestCase _testCase;
        private string _name;
        private string _description;
        private string _targetFunction;
        private TestCaseType _type;
        private TestCaseStatus _status;
        private ObservableCollection<TestCaseVariable> _inputVariables;
        private ObservableCollection<TestCaseVariable> _outputVariables;
        private ObservableCollection<TestCaseStub> _stubs;
        private TestCaseVariable _selectedInputVariable;
        private TestCaseVariable _selectedOutputVariable;
        private TestCaseStub _selectedStub;
        private bool _isModified;

        public TestCaseViewModel(ITestCaseService testCaseService, ILogger<TestCaseViewModel> logger)
        {
            _testCaseService = testCaseService;
            _logger = logger;

            // Initialize collections
            InputVariables = new ObservableCollection<TestCaseVariable>();
            OutputVariables = new ObservableCollection<TestCaseVariable>();
            Stubs = new ObservableCollection<TestCaseStub>();

            // Initialize commands
            SaveCommand = new RelayCommand(Save, CanSave);
            AddInputVariableCommand = new RelayCommand(AddInputVariable);
            RemoveInputVariableCommand = new RelayCommand(RemoveInputVariable, CanRemoveInputVariable);
            AddOutputVariableCommand = new RelayCommand(AddOutputVariable);
            RemoveOutputVariableCommand = new RelayCommand(RemoveOutputVariable, CanRemoveOutputVariable);
            AddStubCommand = new RelayCommand(AddStub);
            RemoveStubCommand = new RelayCommand(RemoveStub, CanRemoveStub);
            ResetCommand = new RelayCommand(Reset, CanReset);
        }

        // Properties
        public TestCase TestCase
        {
            get => _testCase;
            set
            {
                if (SetProperty(ref _testCase, value))
                {
                    // Update all properties from the test case
                    if (value != null)
                    {
                        Name = value.Name;
                        Description = value.Description;
                        TargetFunction = value.TargetFunction;
                        Type = value.Type;
                        Status = value.Status;

                        // Update collections
                        InputVariables.Clear();
                        foreach (var variable in value.InputVariables)
                        {
                            InputVariables.Add(variable);
                        }

                        OutputVariables.Clear();
                        foreach (var variable in value.OutputVariables)
                        {
                            OutputVariables.Add(variable);
                        }

                        Stubs.Clear();
                        foreach (var stub in value.Stubs)
                        {
                            Stubs.Add(stub);
                        }

                        IsModified = false;
                    }
                    else
                    {
                        Name = null;
                        Description = null;
                        TargetFunction = null;
                        Type = TestCaseType.UnitTest;
                        Status = TestCaseStatus.NotRun;
                        InputVariables.Clear();
                        OutputVariables.Clear();
                        Stubs.Clear();
                        IsModified = false;
                    }
                }
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    IsModified = true;
                }
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
                {
                    IsModified = true;
                }
            }
        }

        public string TargetFunction
        {
            get => _targetFunction;
            set
            {
                if (SetProperty(ref _targetFunction, value))
                {
                    IsModified = true;
                }
            }
        }

        public TestCaseType Type
        {
            get => _type;
            set
            {
                if (SetProperty(ref _type, value))
                {
                    IsModified = true;
                }
            }
        }

        public TestCaseStatus Status
        {
            get => _status;
            set
            {
                if (SetProperty(ref _status, value))
                {
                    IsModified = true;
                }
            }
        }

        public ObservableCollection<TestCaseVariable> InputVariables
        {
            get => _inputVariables;
            set => SetProperty(ref _inputVariables, value);
        }

        public ObservableCollection<TestCaseVariable> OutputVariables
        {
            get => _outputVariables;
            set => SetProperty(ref _outputVariables, value);
        }

        public ObservableCollection<TestCaseStub> Stubs
        {
            get => _stubs;
            set => SetProperty(ref _stubs, value);
        }

        public TestCaseVariable SelectedInputVariable
        {
            get => _selectedInputVariable;
            set => SetProperty(ref _selectedInputVariable, value);
        }

        public TestCaseVariable SelectedOutputVariable
        {
            get => _selectedOutputVariable;
            set => SetProperty(ref _selectedOutputVariable, value);
        }

        public TestCaseStub SelectedStub
        {
            get => _selectedStub;
            set => SetProperty(ref _selectedStub, value);
        }

        public bool IsModified
        {
            get => _isModified;
            set => SetProperty(ref _isModified, value);
        }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand AddInputVariableCommand { get; }
        public ICommand RemoveInputVariableCommand { get; }
        public ICommand AddOutputVariableCommand { get; }
        public ICommand RemoveOutputVariableCommand { get; }
        public ICommand AddStubCommand { get; }
        public ICommand RemoveStubCommand { get; }
        public ICommand ResetCommand { get; }

        // Command methods
        private void Save()
        {
            try
            {
                // Update test case from properties
                if (_testCase == null)
                {
                    _testCase = new TestCase
                    {
                        Id = Guid.NewGuid().ToString(),
                        CreatedDate = DateTime.Now
                    };
                }

                _testCase.Name = Name;
                _testCase.Description = Description;
                _testCase.TargetFunction = TargetFunction;
                _testCase.Type = Type;
                _testCase.Status = Status;
                _testCase.ModifiedDate = DateTime.Now;

                // Update collections
                _testCase.InputVariables.Clear();
                foreach (var variable in InputVariables)
                {
                    _testCase.InputVariables.Add(variable);
                }

                _testCase.OutputVariables.Clear();
                foreach (var variable in OutputVariables)
                {
                    _testCase.OutputVariables.Add(variable);
                }

                _testCase.Stubs.Clear();
                foreach (var stub in Stubs)
                {
                    _testCase.Stubs.Add(stub);
                }

                // Save to the service
                if (_testCaseService.GetTestCase(_testCase.Id) == null)
                {
                    _testCaseService.AddTestCaseToProject(_testCase);
                }
                else
                {
                    _testCaseService.UpdateTestCase(_testCase);
                }

                IsModified = false;

                _logger.LogInformation($"Successfully saved test case: {_testCase.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving test case: {Name}");
                throw;
            }
        }

        private bool CanSave() => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(TargetFunction) && IsModified;

        private void AddInputVariable()
        {
            var variable = new TestCaseVariable
            {
                Name = $"input{InputVariables.Count + 1}",
                Type = "int",
                Value = "0"
            };

            InputVariables.Add(variable);
            IsModified = true;
        }

        private void RemoveInputVariable()
        {
            if (SelectedInputVariable != null)
            {
                InputVariables.Remove(SelectedInputVariable);
                IsModified = true;
            }
        }

        private bool CanRemoveInputVariable() => SelectedInputVariable != null;

        private void AddOutputVariable()
        {
            var variable = new TestCaseVariable
            {
                Name = $"output{OutputVariables.Count + 1}",
                Type = "int",
                Value = "0"
            };

            OutputVariables.Add(variable);
            IsModified = true;
        }

        private void RemoveOutputVariable()
        {
            if (SelectedOutputVariable != null)
            {
                OutputVariables.Remove(SelectedOutputVariable);
                IsModified = true;
            }
        }

        private bool CanRemoveOutputVariable() => SelectedOutputVariable != null;

        private void AddStub()
        {
            var stub = new TestCaseStub
            {
                FunctionName = "stubFunction",
                ReturnValue = "0",
                Parameters = new System.Collections.Generic.List<TestCaseVariable>()
            };

            Stubs.Add(stub);
            IsModified = true;
        }

        private void RemoveStub()
        {
            if (SelectedStub != null)
            {
                Stubs.Remove(SelectedStub);
                IsModified = true;
            }
        }

        private bool CanRemoveStub() => SelectedStub != null;

        private void Reset()
        {
            if (_testCase != null)
            {
                // Reset to original state
                TestCase = _testCase;
            }
            else
            {
                // Clear all fields
                Name = null;
                Description = null;
                TargetFunction = null;
                Type = TestCaseType.UnitTest;
                Status = TestCaseStatus.NotRun;
                InputVariables.Clear();
                OutputVariables.Clear();
                Stubs.Clear();
                IsModified = false;
            }
        }

        private bool CanReset() => IsModified;

        // Methods
        public void CreateNew()
        {
            TestCase = null;
            Name = "New Test Case";
            Type = TestCaseType.UnitTest;
            Status = TestCaseStatus.NotRun;
            IsModified = true;
        }
    }
}
