using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Infrastructure.ViewModels;
using C_TestForge.Models.TestCases;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace C_TestForge.UI.ViewModels
{
    public class TestCaseComparisonDialogViewModel : BindableBase, IDialogAware, ITestCaseComparisonDialogViewModel
    {
        private readonly ITestCaseService _testCaseService;

        private string _title = "Compare Test Cases";
        private ObservableCollection<TestCase> _testCases;
        private TestCase _testCase1;
        private TestCase _testCase2;
        //private ObservableCollection<Models.TestCases.TestCaseDifference> _differences;
        private bool _isComparing;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public ObservableCollection<TestCase> TestCases
        {
            get => _testCases;
            set => SetProperty(ref _testCases, value);
        }

        public TestCase TestCase1
        {
            get => _testCase1;
            set
            {
                if (SetProperty(ref _testCase1, value))
                {
                    CompareTestCases();
                }
            }
        }

        public TestCase TestCase2
        {
            get => _testCase2;
            set
            {
                if (SetProperty(ref _testCase2, value))
                {
                    CompareTestCases();
                }
            }
        }

        //public ObservableCollection<Models.TestCases.TestCaseDifference> Differences
        //{
        //    get => _differences;
        //    set => SetProperty(ref _differences, value);
        //}

        public bool IsComparing
        {
            get => _isComparing;
            set => SetProperty(ref _isComparing, value);
        }

        public DelegateCommand CloseCommand { get; }

        public TestCaseComparisonDialogViewModel(ITestCaseService testCaseService)
        {
            _testCaseService = testCaseService;

            CloseCommand = new DelegateCommand(ExecuteClose);
            //Differences = new ObservableCollection<Models.TestCases.TestCaseDifference>();
        }

        private void ExecuteClose()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        private async void CompareTestCases()
        {
            if (TestCase1 == null || TestCase2 == null || IsComparing)
                return;

            IsComparing = true;

            //try
            //{
            //    var result = await _testCaseService.CompareTestCasesAsync(TestCase1, TestCase2);
            //    Differences = new ObservableCollection<Models.TestCases.TestCaseDifference>(result.Differences);
            //}
            //catch (Exception)
            //{
            //    Differences.Clear();
            //}
            //finally
            //{
            //    IsComparing = false;
            //}
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
            if (parameters.ContainsKey("TestCases"))
            {
                var testCases = parameters.GetValue<ObservableCollection<TestCase>>("TestCases");
                TestCases = testCases;

                if (testCases.Count >= 2)
                {
                    TestCase1 = testCases[0];
                    TestCase2 = testCases[1];
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