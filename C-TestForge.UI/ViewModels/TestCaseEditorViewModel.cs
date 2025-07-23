using C_TestForge.Models;
using C_TestForge.Models.TestCases;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.UI.ViewModels
{
    public class TestCaseEditorViewModel : BindableBase
    {
        private Models.TestCases.TestCase _testCase;
        private List<CFunction> _functions;
        private CFunction _selectedFunction;
        private ObservableCollection<TestCaseInput> _inputs;
        private ObservableCollection<TestCaseOutput> _expectedOutputs;
        private bool _isUnitTest;
        private bool _isIntegrationTest;

        public Models.TestCases.TestCase TestCase
        {
            get => _testCase;
            set
            {
                if (SetProperty(ref _testCase, value))
                {
                    Inputs = new ObservableCollection<TestCaseInput>(value?.Inputs ?? new List<TestCaseInput>());
                    ExpectedOutputs = new ObservableCollection<TestCaseOutput>(value?.ExpectedOutputs ?? new List<TestCaseOutput>());

                    if (value != null)
                    {
                        IsUnitTest = value.Type == TestCaseType.UnitTest;
                        IsIntegrationTest = value.Type == TestCaseType.IntegrationTest;

                        if (!string.IsNullOrEmpty(value.FunctionName) && Functions != null)
                        {
                            SelectedFunction = Functions.FirstOrDefault(f => f.Name == value.FunctionName);
                        }
                    }
                }
            }
        }

        public List<CFunction> Functions
        {
            get => _functions;
            set => SetProperty(ref _functions, value);
        }

        public CFunction SelectedFunction
        {
            get => _selectedFunction;
            set
            {
                if (SetProperty(ref _selectedFunction, value))
                {
                    if (value != null && TestCase != null)
                    {
                        TestCase.FunctionName = value.Name;
                        UpdateInputsAndOutputs();
                    }
                }
            }
        }

        public ObservableCollection<TestCaseInput> Inputs
        {
            get => _inputs;
            set => SetProperty(ref _inputs, value);
        }

        public ObservableCollection<TestCaseOutput> ExpectedOutputs
        {
            get => _expectedOutputs;
            set => SetProperty(ref _expectedOutputs, value);
        }

        public bool IsUnitTest
        {
            get => _isUnitTest;
            set
            {
                if (SetProperty(ref _isUnitTest, value) && value)
                {
                    IsIntegrationTest = !value;

                    if (TestCase != null)
                    {
                        TestCase.Type = TestCaseType.UnitTest;
                    }
                }
            }
        }

        public bool IsIntegrationTest
        {
            get => _isIntegrationTest;
            set
            {
                if (SetProperty(ref _isIntegrationTest, value) && value)
                {
                    IsUnitTest = !value;

                    if (TestCase != null)
                    {
                        TestCase.Type = TestCaseType.IntegrationTest;
                    }
                }
            }
        }

        public DelegateCommand<TestCaseInput> RemoveInputCommand { get; }
        public DelegateCommand<TestCaseOutput> RemoveOutputCommand { get; }

        public TestCaseEditorViewModel()
        {
            TestCase = new Models.TestCases.TestCase();
            Inputs = new ObservableCollection<TestCaseInput>();
            ExpectedOutputs = new ObservableCollection<TestCaseOutput>();
            IsUnitTest = true;

            RemoveInputCommand = new DelegateCommand<TestCaseInput>(ExecuteRemoveInput);
            RemoveOutputCommand = new DelegateCommand<TestCaseOutput>(ExecuteRemoveOutput);
        }

        private void UpdateInputsAndOutputs()
        {
            if (SelectedFunction == null || TestCase == null)
                return;

            // If we don't already have inputs, generate them from the function parameters
            if (Inputs.Count == 0)
            {
                foreach (var param in SelectedFunction.Parameters)
                {
                    Inputs.Add(new TestCaseInput
                    {
                        VariableName = param.Name,
                        VariableType = param.Type,
                        Value = GenerateDefaultValueForType(param.Type),
                        IsStub = false
                    });
                }
            }

            // If we don't already have expected outputs, generate them from the function return type
            if (ExpectedOutputs.Count == 0 && SelectedFunction.ReturnType != "void")
            {
                ExpectedOutputs.Add(new TestCaseOutput
                {
                    VariableName = "return",
                    VariableType = SelectedFunction.ReturnType,
                    Value = GenerateDefaultValueForType(SelectedFunction.ReturnType)
                });
            }
        }

        private string GenerateDefaultValueForType(string type)
        {
            // Generate a sensible default value for the given C type
            switch (type.Trim())
            {
                case "int":
                case "long":
                case "short":
                    return "0";
                case "unsigned int":
                case "unsigned long":
                case "unsigned short":
                case "size_t":
                    return "0";
                case "float":
                case "double":
                    return "0.0";
                case "char":
                    return "'a'";
                case "bool":
                    return "false";
                case "char*":
                case "const char*":
                    return "\"test\"";
                default:
                    if (type.Contains("*"))
                        return "NULL";
                    return "0";
            }
        }

        private void ExecuteRemoveInput(TestCaseInput input)
        {
            if (input != null)
            {
                Inputs.Remove(input);
            }
        }

        private void ExecuteRemoveOutput(TestCaseOutput output)
        {
            if (output != null)
            {
                ExpectedOutputs.Remove(output);
            }
        }

        public void ApplyChanges()
        {
            if (TestCase != null)
            {
                TestCase.Inputs = new List<TestCaseInput>(Inputs);
                TestCase.ExpectedOutputs = new List<TestCaseOutput>(ExpectedOutputs);
            }
        }
    }
}
