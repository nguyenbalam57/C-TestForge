using C_TestForge.Models;
using C_TestForge.Models.Core;
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
        private TestCase _testCase;
        private List<CFunction> _functions;
        private CFunction _selectedFunction;
        private ObservableCollection<TestCaseVariableInput> _inputs;
        private ObservableCollection<TestCaseVariableOutput> _expectedOutputs;
        private bool _isUnitTest;
        private bool _isIntegrationTest;

        public TestCase TestCase
        {
            get => _testCase;
            set
            {
                if (SetProperty(ref _testCase, value))
                {
                    Inputs = new ObservableCollection<TestCaseVariableInput>(value?.InputVariables ?? new List<TestCaseVariableInput>());
                    ExpectedOutputs = new ObservableCollection<TestCaseVariableOutput>(value?.OutputVariables ?? new List<TestCaseVariableOutput>());

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

        public ObservableCollection<TestCaseVariableInput> Inputs
        {
            get => _inputs;
            set => SetProperty(ref _inputs, value);
        }

        public ObservableCollection<TestCaseVariableOutput> ExpectedOutputs
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

        public DelegateCommand<TestCaseVariableInput> RemoveInputCommand { get; }
        public DelegateCommand<TestCaseVariableOutput> RemoveOutputCommand { get; }

        public TestCaseEditorViewModel()
        {
            TestCase = new TestCase();
            Inputs = new ObservableCollection<TestCaseVariableInput>();
            ExpectedOutputs = new ObservableCollection<TestCaseVariableOutput>();
            IsUnitTest = true;

            RemoveInputCommand = new DelegateCommand<TestCaseVariableInput>(ExecuteRemoveInput);
            RemoveOutputCommand = new DelegateCommand<TestCaseVariableOutput>(ExecuteRemoveOutput);
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
                    Inputs.Add(new TestCaseVariableInput
                    {
                        Name = param.Name,
                        Type = param.TypeName,
                        Value = GenerateDefaultValueForType(param.TypeName),
                        IsStubParameter = false
                    });
                }
            }

            // If we don't already have expected outputs, generate them from the function return type
            if (ExpectedOutputs.Count == 0 && SelectedFunction.ReturnType != "void")
            {
                ExpectedOutputs.Add(new TestCaseVariableOutput
                {
                    Name = "return",
                    Type = SelectedFunction.ReturnType,
                    ActualValue = GenerateDefaultValueForType(SelectedFunction.ReturnType)
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

        private void ExecuteRemoveInput(TestCaseVariableInput input)
        {
            if (input != null)
            {
                Inputs.Remove(input);
            }
        }

        private void ExecuteRemoveOutput(TestCaseVariableOutput output)
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
                TestCase.InputVariables = new List<TestCaseVariableInput>(Inputs);
                TestCase.OutputVariables = new List<TestCaseVariableOutput>(ExpectedOutputs);
            }
        }
    }
}
