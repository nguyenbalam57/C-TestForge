using C_TestForge.Infrastructure.ViewModels;
using C_TestForge.Models;
using C_TestForge.Models.TestCases;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace C_TestForge.UI.ViewModels
{
    public class GenerateTestCaseDialogViewModel : BindableBase, IDialogAware, IGenerateTestCaseDialogViewModel
    {
        private string _title = "Generate Test Case";
        private List<CFunction> _functions;
        private CFunction _selectedFunction;
        private bool _isUnitTest = true;
        private bool _isIntegrationTest;
        private string _testCaseName;
        private List<string> _coverageLevels = new List<string> { "Basic", "Standard", "Comprehensive" };
        private string _selectedCoverageLevel = "Standard";

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
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
                    UpdateTestCaseName();
                }
            }
        }

        public bool IsUnitTest
        {
            get => _isUnitTest;
            set
            {
                if (SetProperty(ref _isUnitTest, value) && value)
                {
                    IsIntegrationTest = !value;
                    UpdateTestCaseName();
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
                    UpdateTestCaseName();
                }
            }
        }

        public string TestCaseName
        {
            get => _testCaseName;
            set => SetProperty(ref _testCaseName, value);
        }

        public List<string> CoverageLevels
        {
            get => _coverageLevels;
            set => SetProperty(ref _coverageLevels, value);
        }

        public string SelectedCoverageLevel
        {
            get => _selectedCoverageLevel;
            set => SetProperty(ref _selectedCoverageLevel, value);
        }

        public DelegateCommand GenerateCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public GenerateTestCaseDialogViewModel()
        {
            GenerateCommand = new DelegateCommand(ExecuteGenerate, CanExecuteGenerate);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        private bool CanExecuteGenerate()
        {
            return SelectedFunction != null;
        }

        private void ExecuteGenerate()
        {
            var parameters = new DialogParameters
            {
                { "Function", SelectedFunction },
                { "Type", IsUnitTest ? TestCaseType.UnitTest : TestCaseType.IntegrationTest },
                { "CoverageLevel", SelectedCoverageLevel },
                { "TestCaseName", TestCaseName }
            };

            RaiseRequestClose(new DialogResult(ButtonResult.OK, parameters));
        }

        private void ExecuteCancel()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        private void UpdateTestCaseName()
        {
            if (SelectedFunction != null)
            {
                TestCaseName = IsUnitTest
                    ? $"Test_{SelectedFunction.Name}"
                    : $"IntegrationTest_{SelectedFunction.Name}";
            }
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            // Clean up resources if needed
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("Functions"))
            {
                Functions = parameters.GetValue<List<CFunction>>("Functions");

                if (Functions.Count > 0)
                {
                    SelectedFunction = Functions.First();
                }
            }
        }

        public event Action<IDialogResult> RequestClose;

        protected virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }
    }
}