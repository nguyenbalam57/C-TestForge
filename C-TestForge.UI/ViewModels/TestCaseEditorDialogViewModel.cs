using C_TestForge.Models;
using C_TestForge.TestCase.Services;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;

namespace C_TestForge.UI.ViewModels
{
    public class TestCaseEditorDialogViewModel : BindableBase, IDialogAware
    {
        private readonly ITestCaseService _testCaseService;

        private string _title;
        private Models.TestCases.TestCase _testCase;
        private List<CFunction> _functions;
        private string _mode;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public Models.TestCases.TestCase TestCase
        {
            get => _testCase;
            set => SetProperty(ref _testCase, value);
        }

        public List<CFunction> Functions
        {
            get => _functions;
            set => SetProperty(ref _functions, value);
        }

        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelCommand { get; }

        public TestCaseEditorDialogViewModel(ITestCaseService testCaseService)
        {
            _testCaseService = testCaseService;

            SaveCommand = new DelegateCommand(ExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        private async void ExecuteSave()
        {
            try
            {
                // Get the editor view model from the editor control and apply changes
                if (TestCase != null)
                {
                    if (_mode == "Add")
                    {
                        await _testCaseService.CreateTestCaseAsync(TestCase);
                    }
                    else if (_mode == "Edit")
                    {
                        await _testCaseService.UpdateTestCaseAsync(TestCase);
                    }

                    var parameters = new DialogParameters
                    {
                        { "TestCase", TestCase }
                    };

                    RaiseRequestClose(new DialogResult(ButtonResult.OK, parameters));
                }
            }
            catch (Exception ex)
            {
                // Show error message
                var parameters = new DialogParameters
                {
                    { "Title", "Error" },
                    { "Message", $"Failed to save test case: {ex.Message}" }
                };

                RaiseRequestClose(new DialogResult(ButtonResult.Cancel, parameters));
            }
        }

        private void ExecuteCancel()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
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
            if (parameters.ContainsKey("Mode"))
            {
                _mode = parameters.GetValue<string>("Mode");
                Title = _mode == "Add" ? "Add Test Case" : "Edit Test Case";
            }

            if (parameters.ContainsKey("TestCase"))
            {
                TestCase = parameters.GetValue<Models.TestCases.TestCase>("TestCase").Clone();
            }
            else
            {
                TestCase = new Models.TestCases.TestCase();
            }

            if (parameters.ContainsKey("Functions"))
            {
                Functions = parameters.GetValue<List<CFunction>>("Functions");
            }
        }

        public event Action<IDialogResult> RequestClose;

        protected virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }
    }
}