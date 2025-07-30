using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Models.Core;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using C_TestForge.Parser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace C_TestForge.UI.ViewModels
{
    /// <summary>
    /// ViewModel for source code analysis
    /// </summary>
    public class SourceAnalysisViewModel : ObservableObject
    {
        private readonly ILogger<SourceAnalysisViewModel> _logger;
        private readonly IAnalysisService _analysisService;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly IParserService _parserService;
        private readonly IFileService _fileService;

        private SourceFile _currentSourceFile;
        private AnalysisResult _analysisResult;
        private string _filePath;
        private string _sourceCode;
        private bool _isAnalyzing;
        private string _statusMessage;
        private AnalysisOptions _analysisOptions;

        private ObservableCollection<CDefinition> _definitions;
        private ObservableCollection<CVariable> _variables;
        private ObservableCollection<CFunction> _functions;
        private ObservableCollection<ConditionalDirective> _conditionalDirectives;
        private ObservableCollection<FunctionRelationship> _functionRelationships;
        private ObservableCollection<VariableConstraint> _variableConstraints;

        private CDefinition _selectedDefinition;
        private CVariable _selectedVariable;
        private CFunction _selectedFunction;

        /// <summary>
        /// Constructor for SourceAnalysisViewModel
        /// </summary>
        public SourceAnalysisViewModel(
            ILogger<SourceAnalysisViewModel> logger,
            IAnalysisService analysisService,
            ISourceCodeService sourceCodeService,
            IParserService parserService,
            IFileService fileService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
            _parserService = parserService ?? throw new ArgumentNullException(nameof(parserService));
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));

            // Initialize collections
            Definitions = new ObservableCollection<CDefinition>();
            Variables = new ObservableCollection<CVariable>();
            Functions = new ObservableCollection<CFunction>();
            ConditionalDirectives = new ObservableCollection<ConditionalDirective>();
            FunctionRelationships = new ObservableCollection<FunctionRelationship>();
            VariableConstraints = new ObservableCollection<VariableConstraint>();

            // Initialize commands
            OpenFileCommand = new AsyncRelayCommand(OpenFileAsync);
            AnalyzeCommand = new AsyncRelayCommand(AnalyzeAsync, CanAnalyze);
            SaveAnalysisResultCommand = new AsyncRelayCommand(SaveAnalysisResultAsync, CanSaveAnalysisResult);

            // Initialize options
            _analysisOptions = new AnalysisOptions
            {
                AnalyzePreprocessorDefinitions = true,
                AnalyzeVariables = true,
                AnalyzeFunctions = true,
                AnalyzeFunctionRelationships = true,
                AnalyzeVariableConstraints = true,
                DetailLevel = AnalysisLevel.Detailed
            };
        }

        #region Properties

        /// <summary>
        /// Path to the current source file
        /// </summary>
        public string FilePath
        {
            get => _filePath;
            set
            {
                if (SetProperty(ref _filePath, value))
                {
                    OnPropertyChanged(nameof(FileName));
                    OnPropertyChanged(nameof(IsFileLoaded));
                    ((AsyncRelayCommand)AnalyzeCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)SaveAnalysisResultCommand).NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Name of the current source file
        /// </summary>
        public string FileName => !string.IsNullOrEmpty(FilePath) ? Path.GetFileName(FilePath) : null;

        /// <summary>
        /// Source code of the current file
        /// </summary>
        public string SourceCode
        {
            get => _sourceCode;
            set => SetProperty(ref _sourceCode, value);
        }

        /// <summary>
        /// Whether the analysis is in progress
        /// </summary>
        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set
            {
                if (SetProperty(ref _isAnalyzing, value))
                {
                    OnPropertyChanged(nameof(IsNotAnalyzing));
                    ((AsyncRelayCommand)AnalyzeCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)SaveAnalysisResultCommand).NotifyCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Whether the analysis is not in progress
        /// </summary>
        public bool IsNotAnalyzing => !IsAnalyzing;

        /// <summary>
        /// Status message
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Whether a file is loaded
        /// </summary>
        public bool IsFileLoaded => !string.IsNullOrEmpty(FilePath);

        /// <summary>
        /// Whether analysis results are available
        /// </summary>
        public bool HasAnalysisResults => _analysisResult != null;

        /// <summary>
        /// Collection of preprocessor definitions
        /// </summary>
        public ObservableCollection<CDefinition> Definitions
        {
            get => _definitions;
            set => SetProperty(ref _definitions, value);
        }

        /// <summary>
        /// Collection of variables
        /// </summary>
        public ObservableCollection<CVariable> Variables
        {
            get => _variables;
            set => SetProperty(ref _variables, value);
        }

        /// <summary>
        /// Collection of functions
        /// </summary>
        public ObservableCollection<CFunction> Functions
        {
            get => _functions;
            set => SetProperty(ref _functions, value);
        }

        /// <summary>
        /// Collection of conditional directives
        /// </summary>
        public ObservableCollection<ConditionalDirective> ConditionalDirectives
        {
            get => _conditionalDirectives;
            set => SetProperty(ref _conditionalDirectives, value);
        }

        /// <summary>
        /// Collection of function relationships
        /// </summary>
        public ObservableCollection<FunctionRelationship> FunctionRelationships
        {
            get => _functionRelationships;
            set => SetProperty(ref _functionRelationships, value);
        }

        /// <summary>
        /// Collection of variable constraints
        /// </summary>
        public ObservableCollection<VariableConstraint> VariableConstraints
        {
            get => _variableConstraints;
            set => SetProperty(ref _variableConstraints, value);
        }

        /// <summary>
        /// Selected preprocessor definition
        /// </summary>
        public CDefinition SelectedDefinition
        {
            get => _selectedDefinition;
            set => SetProperty(ref _selectedDefinition, value);
        }

        /// <summary>
        /// Selected variable
        /// </summary>
        public CVariable SelectedVariable
        {
            get => _selectedVariable;
            set
            {
                if (SetProperty(ref _selectedVariable, value))
                {
                    UpdateVariableConstraints();
                }
            }
        }

        /// <summary>
        /// Selected function
        /// </summary>
        public CFunction SelectedFunction
        {
            get => _selectedFunction;
            set
            {
                if (SetProperty(ref _selectedFunction, value))
                {
                    UpdateFunctionRelationships();
                }
            }
        }

        /// <summary>
        /// Analysis options
        /// </summary>
        public AnalysisOptions AnalysisOptions
        {
            get => _analysisOptions;
            set => SetProperty(ref _analysisOptions, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// Command to open a source file
        /// </summary>
        public ICommand OpenFileCommand { get; }

        /// <summary>
        /// Command to analyze the current source file
        /// </summary>
        public ICommand AnalyzeCommand { get; }

        /// <summary>
        /// Command to save the analysis results
        /// </summary>
        public ICommand SaveAnalysisResultCommand { get; }

        #endregion

        #region Command Methods

        /// <summary>
        /// Opens a source file
        /// </summary>
        private async Task OpenFileAsync()
        {
            try
            {
                // In a real implementation, this would use a file dialog
                // For now, let's just use a fixed file path
                string filePath = @"C:\Temp\sample.c";

                // Check if the file exists
                if (!_fileService.FileExists(filePath))
                {
                    StatusMessage = $"File not found: {filePath}";
                    return;
                }

                StatusMessage = $"Loading file: {filePath}";

                // Load the source file
                _currentSourceFile = await _sourceCodeService.LoadSourceFileAsync(filePath);
                FilePath = _currentSourceFile.FilePath;
                SourceCode = _currentSourceFile.Content;

                StatusMessage = $"File loaded: {FileName}";

                // Clear previous analysis results
                ClearAnalysisResults();

                // Auto-analyze the file
                await AnalyzeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error opening file: {ex.Message}");
                StatusMessage = $"Error opening file: {ex.Message}";
            }
        }

        /// <summary>
        /// Analyzes the current source file
        /// </summary>
        private async Task AnalyzeAsync()
        {
            if (_currentSourceFile == null)
            {
                StatusMessage = "No file loaded";
                return;
            }

            try
            {
                IsAnalyzing = true;
                StatusMessage = $"Analyzing {FileName}...";

                // Clear previous analysis results
                ClearAnalysisResults();

                // Analyze the source file
                _analysisResult = await _analysisService.AnalyzeSourceFileAsync(_currentSourceFile, _analysisOptions);

                // Update collections
                UpdateCollectionsFromAnalysisResult();

                StatusMessage = $"Analysis complete: {FileName}";
                OnPropertyChanged(nameof(HasAnalysisResults));
                ((AsyncRelayCommand)SaveAnalysisResultCommand).NotifyCanExecuteChanged();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing file: {ex.Message}");
                StatusMessage = $"Error analyzing file: {ex.Message}";
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        /// <summary>
        /// Saves the analysis results
        /// </summary>
        private async Task SaveAnalysisResultAsync()
        {
            if (_analysisResult == null)
            {
                StatusMessage = "No analysis results to save";
                return;
            }

            try
            {
                StatusMessage = "Saving analysis results...";

                // In a real implementation, this would use a file dialog
                // For now, let's just use a fixed file path
                string filePath = @"C:\Temp\analysis_result.json";

                // Serialize the analysis result
                string json = System.Text.Json.JsonSerializer.Serialize(_analysisResult, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
                });

                // Save the file
                await _fileService.WriteFileAsync(filePath, json);

                StatusMessage = $"Analysis results saved to: {filePath}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving analysis results: {ex.Message}");
                StatusMessage = $"Error saving analysis results: {ex.Message}";
            }
        }

        /// <summary>
        /// Checks if the analyze command can be executed
        /// </summary>
        private bool CanAnalyze()
        {
            return IsFileLoaded && !IsAnalyzing;
        }

        /// <summary>
        /// Checks if the save analysis result command can be executed
        /// </summary>
        private bool CanSaveAnalysisResult()
        {
            return HasAnalysisResults && !IsAnalyzing;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Clears the analysis results
        /// </summary>
        private void ClearAnalysisResults()
        {
            Definitions.Clear();
            Variables.Clear();
            Functions.Clear();
            ConditionalDirectives.Clear();
            FunctionRelationships.Clear();
            VariableConstraints.Clear();

            SelectedDefinition = null;
            SelectedVariable = null;
            SelectedFunction = null;

            _analysisResult = null;
            OnPropertyChanged(nameof(HasAnalysisResults));
            ((AsyncRelayCommand)SaveAnalysisResultCommand).NotifyCanExecuteChanged();
        }

        /// <summary>
        /// Updates the collections from the analysis result
        /// </summary>
        private void UpdateCollectionsFromAnalysisResult()
        {
            if (_analysisResult == null)
            {
                return;
            }

            // Add definitions
            foreach (var definition in _analysisResult.Definitions)
            {
                Definitions.Add(definition);
            }

            // Add variables
            foreach (var variable in _analysisResult.Variables)
            {
                Variables.Add(variable);
            }

            // Add functions
            foreach (var function in _analysisResult.Functions)
            {
                Functions.Add(function);
            }

            // Add conditional directives
            foreach (var directive in _analysisResult.ConditionalDirectives)
            {
                ConditionalDirectives.Add(directive);
            }

            // Add function relationships
            foreach (var relationship in _analysisResult.FunctionRelationships)
            {
                FunctionRelationships.Add(relationship);
            }

            // Add variable constraints
            foreach (var constraint in _analysisResult.VariableConstraints)
            {
                VariableConstraints.Add(constraint);
            }
        }

        /// <summary>
        /// Updates the variable constraints when a variable is selected
        /// </summary>
        private void UpdateVariableConstraints()
        {
            if (_selectedVariable == null || _analysisResult == null)
            {
                return;
            }

            // Filter the variable constraints for the selected variable
            var constraints = _analysisResult.VariableConstraints
                .Where(c => c.VariableName == _selectedVariable.Name)
                .ToList();

            // Clear and update the collection
            VariableConstraints.Clear();
            foreach (var constraint in constraints)
            {
                VariableConstraints.Add(constraint);
            }
        }

        /// <summary>
        /// Updates the function relationships when a function is selected
        /// </summary>
        private void UpdateFunctionRelationships()
        {
            if (_selectedFunction == null || _analysisResult == null)
            {
                return;
            }

            // Filter the function relationships for the selected function
            var relationships = _analysisResult.FunctionRelationships
                .Where(r => r.CallerName == _selectedFunction.Name || r.CalleeName == _selectedFunction.Name)
                .ToList();

            // Clear and update the collection
            FunctionRelationships.Clear();
            foreach (var relationship in relationships)
            {
                FunctionRelationships.Add(relationship);
            }
        }

        #endregion
    }
}