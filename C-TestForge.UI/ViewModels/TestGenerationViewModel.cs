using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using C_TestForge.Core.Interfaces;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Models;
using C_TestForge.Models.Core;
using C_TestForge.Models.TestCases;
using Prism.Commands;
using Prism.Mvvm;

namespace C_TestForge.UI.ViewModels
{
    public class TestGenerationViewModel : BindableBase
    {
        private readonly IParserService _parserService;
        private readonly IUnitTestGeneratorService _unitTestGenerator;
        private readonly IIntegrationTestGeneratorService _integrationTestGenerator;
        private readonly ITestCaseService _testCaseService;

        private ObservableCollection<CFunctionViewModel> _availableFunctions;
        private ObservableCollection<TestCase> _generatedTestCases;
        private TestCase _selectedTestCase;
        private bool _isGenerating;
        private string _generatedCode;
        private string _statusMessage;
        private bool _showUnitTestOptions;
        private bool _showIntegrationTestOptions;
        private TestCaseType _selectedTestType;
        private ObservableCollection<CFunctionViewModel> _selectedFunctions;

        public TestGenerationViewModel(
            IParserService parserService,
            IUnitTestGeneratorService unitTestGenerator,
            IIntegrationTestGeneratorService integrationTestGenerator,
            ITestCaseService testCaseService)
        {
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _unitTestGenerator = unitTestGenerator ?? throw new ArgumentNullException(nameof(unitTestGenerator));
            _integrationTestGenerator = integrationTestGenerator ?? throw new ArgumentNullException(nameof(integrationTestGenerator));
            _testCaseService = testCaseService ?? throw new ArgumentNullException(nameof(testCaseService));

            _availableFunctions = new ObservableCollection<CFunctionViewModel>();
            _generatedTestCases = new ObservableCollection<TestCase>();
            _selectedFunctions = new ObservableCollection<CFunctionViewModel>();

            GenerateTestsCommand = new DelegateCommand(GenerateTestsAsync, CanGenerateTests);
            SaveTestCasesCommand = new DelegateCommand(SaveTestCases, CanSaveTestCases);
            CopyCodeCommand = new DelegateCommand(CopyCode, () => !string.IsNullOrEmpty(GeneratedCode));
            SelectAllFunctionsCommand = new DelegateCommand(SelectAllFunctions);
            UnselectAllFunctionsCommand = new DelegateCommand(UnselectAllFunctions);

            // Default to Unit Test
            SelectedTestType = TestCaseType.UnitTest;
            ShowUnitTestOptions = true;
            ShowIntegrationTestOptions = false;

            LoadFunctions();
        }

        public ObservableCollection<CFunctionViewModel> AvailableFunctions
        {
            get => _availableFunctions;
            set => SetProperty(ref _availableFunctions, value);
        }

        public ObservableCollection<CFunctionViewModel> SelectedFunctions
        {
            get => _selectedFunctions;
            set => SetProperty(ref _selectedFunctions, value);
        }

        public ObservableCollection<TestCase> GeneratedTestCases
        {
            get => _generatedTestCases;
            set => SetProperty(ref _generatedTestCases, value);
        }

        public TestCase SelectedTestCase
        {
            get => _selectedTestCase;
            set
            {
                if (SetProperty(ref _selectedTestCase, value))
                {
                    GenerateCodeForSelectedTestCase();
                }
            }
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                SetProperty(ref _isGenerating, value);
                RaisePropertyChanged(nameof(CanGenerate));
            }
        }

        public string GeneratedCode
        {
            get => _generatedCode;
            set => SetProperty(ref _generatedCode, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool ShowUnitTestOptions
        {
            get => _showUnitTestOptions;
            set => SetProperty(ref _showUnitTestOptions, value);
        }

        public bool ShowIntegrationTestOptions
        {
            get => _showIntegrationTestOptions;
            set => SetProperty(ref _showIntegrationTestOptions, value);
        }

        public TestCaseType SelectedTestType
        {
            get => _selectedTestType;
            set
            {
                if (SetProperty(ref _selectedTestType, value))
                {
                    ShowUnitTestOptions = value == TestCaseType.UnitTest;
                    ShowIntegrationTestOptions = value == TestCaseType.IntegrationTest;
                }
            }
        }

        public bool CanGenerate => !IsGenerating && SelectedFunctions.Any();

        public ICommand GenerateTestsCommand { get; }
        public ICommand SaveTestCasesCommand { get; }
        public ICommand CopyCodeCommand { get; }
        public ICommand SelectAllFunctionsCommand { get; }
        public ICommand UnselectAllFunctionsCommand { get; }

        private void LoadFunctions()
        {
            try
            {
                //var functions = _parserService.GetFunctions();

                //AvailableFunctions.Clear();
                //foreach (var function in functions)
                //{
                //    AvailableFunctions.Add(new CFunctionViewModel(function));
                //}

                //StatusMessage = $"Loaded {functions.Count} functions";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading functions: {ex.Message}";
            }
        }

        private async void GenerateTestsAsync()
        {
            if (!CanGenerateTests())
                return;

            try
            {
                IsGenerating = true;
                StatusMessage = "Generating test cases...";
                GeneratedTestCases.Clear();
                GeneratedCode = string.Empty;

                var selectedFunctionModels = SelectedFunctions
                    .Select(f => f.Function)
                    .ToList();

                List<TestCase> newTestCases = new List<TestCase>();

                await Task.Run(() =>
                {
                    //var availableVariables = _parserService.GetVariables();

                    if (SelectedTestType == TestCaseType.UnitTest)
                    {
                        // Generate unit tests for each selected function
                        foreach (var function in selectedFunctionModels)
                        {
                            //var functionTestCases = _unitTestGenerator.GenerateTestCasesForFunction(function, availableVariables);
                            //newTestCases.AddRange(functionTestCases);
                        }
                    }
                    else if (SelectedTestType == TestCaseType.IntegrationTest)
                    {
                        // Generate integration tests for the selected functions
                        var functionNames = selectedFunctionModels.Select(f => f.Name).ToList();
                        //var integrationTests = _integrationTestGenerator.GenerateIntegrationTests(functionNames, availableVariables);
                       // newTestCases.AddRange(integrationTests);
                    }
                });

                // Add to observable collection on UI thread
                foreach (var testCase in newTestCases)
                {
                    GeneratedTestCases.Add(testCase);
                }

                if (GeneratedTestCases.Any())
                {
                    SelectedTestCase = GeneratedTestCases.First();
                    StatusMessage = $"Generated {GeneratedTestCases.Count} test cases";
                }
                else
                {
                    StatusMessage = "No test cases were generated";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating tests: {ex.Message}";
            }
            finally
            {
                IsGenerating = false;
            }
        }

        private bool CanGenerateTests()
        {
            return !IsGenerating && SelectedFunctions.Any();
        }

        private void GenerateCodeForSelectedTestCase()
        {
            if (SelectedTestCase == null)
            {
                GeneratedCode = string.Empty;
                return;
            }

            try
            {
                StatusMessage = $"Generating code for {SelectedTestCase.Name}...";

                if (SelectedTestCase.Type == TestCaseType.UnitTest)
                {
                    //GeneratedCode = _unitTestGenerator.GenerateTestCode(SelectedTestCase);
                }
                else if (SelectedTestCase.Type == TestCaseType.IntegrationTest)
                {
                    //GeneratedCode = _integrationTestGenerator.GenerateIntegrationTestCode(SelectedTestCase);
                }

                StatusMessage = "Code generated successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error generating code: {ex.Message}";
                GeneratedCode = $"// Error generating code: {ex.Message}";
            }
        }

        private void SaveTestCases()
        {
            if (!CanSaveTestCases())
                return;

            try
            {
                StatusMessage = "Saving test cases...";

                foreach (var testCase in GeneratedTestCases)
                {
                    //_testCaseService.SaveTestCase(testCase);
                }

                StatusMessage = $"Saved {GeneratedTestCases.Count} test cases";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving test cases: {ex.Message}";
            }
        }

        private bool CanSaveTestCases()
        {
            return GeneratedTestCases.Any();
        }

        private void CopyCode()
        {
            if (string.IsNullOrEmpty(GeneratedCode))
                return;

            try
            {
                System.Windows.Clipboard.SetText(GeneratedCode);
                StatusMessage = "Code copied to clipboard";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error copying code: {ex.Message}";
            }
        }

        private void SelectAllFunctions()
        {
            SelectedFunctions.Clear();
            foreach (var function in AvailableFunctions)
            {
                SelectedFunctions.Add(function);
            }
        }

        private void UnselectAllFunctions()
        {
            SelectedFunctions.Clear();
        }
    }

    public class CFunctionViewModel : BindableBase
    {
        private readonly CFunction _function;
        private bool _isSelected;

        public CFunctionViewModel(CFunction function)
        {
            _function = function ?? throw new ArgumentNullException(nameof(function));
        }

        public CFunction Function => _function;

        public string Name => _function.Name;

        public string ReturnType => _function.ReturnType;

        public string Signature
        {
            get
            {
                string parameters = string.Join(", ", _function.Parameters.Select(p => $"{p.TypeName} {p.Name}"));
                return $"{ReturnType} {Name}({parameters})";
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }
}