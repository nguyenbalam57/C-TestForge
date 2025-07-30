using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using C_TestForge.Models.Core;
using C_TestForge.Models.TestCases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace C_TestForge.UI.ViewModels
{
    public class MainWindowViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private readonly ITestCaseService _testCaseService;
        private readonly IAnalysisService _analysisService;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly ILogger<MainWindowViewModel> _logger;

        private Project _currentProject;
        private string _statusMessage;
        private SourceFile _selectedSourceFile;
        private ObservableCollection<SourceFile> _sourceFiles;
        private ObservableCollection<TestCase> _testCases;
        private TestCase _selectedTestCase;
        private AnalysisResult _analysisResult;
        private bool _isAnalyzing;
        private AnalysisOptions _analysisOptions;
        private bool _hasUnsavedChanges;

        // Collections for analysis results
        private ObservableCollection<CDefinition> _definitions;
        private ObservableCollection<CVariable> _variables;
        private ObservableCollection<CFunction> _functions;
        private ObservableCollection<ConditionalDirective> _conditionalDirectives;

        public MainWindowViewModel(
            IProjectService projectService,
            ITestCaseService testCaseService,
            IAnalysisService analysisService,
            ISourceCodeService sourceCodeService,
            ILogger<MainWindowViewModel> logger)
        {
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _testCaseService = testCaseService ?? throw new ArgumentNullException(nameof(testCaseService));
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Initialize commands
            NewProjectCommand = new AsyncRelayCommand(NewProjectAsync);
            OpenProjectCommand = new AsyncRelayCommand(OpenProjectAsync);
            SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync, CanSaveProject);
            CloseProjectCommand = new RelayCommand(CloseProject, CanCloseProject);
            ParseSourceFilesCommand = new AsyncRelayCommand(ParseSourceFilesAsync, CanParseSourceFiles);
            ImportTestCasesCommand = new AsyncRelayCommand(ImportTestCasesAsync, CanImportTestCases);
            //ExportTestCasesCommand = new AsyncRelayCommand(ExportTestCasesAsync, CanExportTestCases);
            AnalyzeSourceFileCommand = new AsyncRelayCommand<SourceFile>(AnalyzeSourceFileAsync, CanAnalyzeSourceFile);
            AddSourceFileCommand = new AsyncRelayCommand(AddSourceFileAsync, CanAddSourceFile);
            RemoveSourceFileCommand = new AsyncRelayCommand<SourceFile>(RemoveSourceFileAsync, CanRemoveSourceFile);
            GenerateTestCasesCommand = new AsyncRelayCommand(GenerateTestCasesAsync, CanGenerateTestCases);
            ProjectSettingsCommand = new RelayCommand(ShowProjectSettings, CanShowProjectSettings);
            AnalysisOptionsCommand = new RelayCommand(ShowAnalysisOptions);
            AboutCommand = new RelayCommand(ShowAboutDialog);
            ExitCommand = new RelayCommand(Exit);

            // Initialize collections
            SourceFiles = new ObservableCollection<SourceFile>();
            TestCases = new ObservableCollection<TestCase>();
            Definitions = new ObservableCollection<CDefinition>();
            Variables = new ObservableCollection<CVariable>();
            Functions = new ObservableCollection<CFunction>();
            ConditionalDirectives = new ObservableCollection<ConditionalDirective>();

            // Initialize analysis options
            _analysisOptions = new AnalysisOptions
            {
                AnalyzePreprocessorDefinitions = true,
                AnalyzeVariables = true,
                AnalyzeFunctions = true,
                AnalyzeFunctionRelationships = true,
                AnalyzeVariableConstraints = true,
                DetailLevel = AnalysisLevel.Detailed
            };

            // Set default status
            StatusMessage = "Ready";
        }

        // Properties
        public Project CurrentProject
        {
            get => _currentProject;
            set
            {
                SetProperty(ref _currentProject, value);
                OnPropertyChanged(nameof(ProjectName));
                OnPropertyChanged(nameof(HasProject));

                // Update command can execute state
                ((AsyncRelayCommand)SaveProjectCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)ParseSourceFilesCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)ImportTestCasesCommand).NotifyCanExecuteChanged();
                ((AsyncRelayCommand)ExportTestCasesCommand).NotifyCanExecuteChanged();
                ((RelayCommand)CloseProjectCommand).NotifyCanExecuteChanged();
            }
        }

        public string ProjectName => CurrentProject?.Name ?? "No Project Loaded";

        public bool HasProject => CurrentProject != null;

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public SourceFile SelectedSourceFile
        {
            get => _selectedSourceFile;
            set
            {
                if (SetProperty(ref _selectedSourceFile, value))
                {

                    // Mark project as having unsaved changes
                    HasUnsavedChanges = true;// Update analysis command can execute state
                    ((AsyncRelayCommand<SourceFile>)AnalyzeSourceFileCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<SourceFile> SourceFiles
        {
            get => _sourceFiles;
            set => SetProperty(ref _sourceFiles, value);
        }

        public ObservableCollection<TestCase> TestCases
        {
            get => _testCases;
            set => SetProperty(ref _testCases, value);
        }

        public TestCase SelectedTestCase
        {
            get => _selectedTestCase;
            set => SetProperty(ref _selectedTestCase, value);
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set => SetProperty(ref _isAnalyzing, value);
        }

        // Analysis result collections
        public ObservableCollection<CDefinition> Definitions
        {
            get => _definitions;
            set => SetProperty(ref _definitions, value);
        }

        public ObservableCollection<CVariable> Variables
        {
            get => _variables;
            set => SetProperty(ref _variables, value);
        }

        public ObservableCollection<CFunction> Functions
        {
            get => _functions;
            set => SetProperty(ref _functions, value);
        }

        public ObservableCollection<ConditionalDirective> ConditionalDirectives
        {
            get => _conditionalDirectives;
            set => SetProperty(ref _conditionalDirectives, value);
        }

        // Commands
        public ICommand NewProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand CloseProjectCommand { get; }
        public ICommand ParseSourceFilesCommand { get; }
        public ICommand ImportTestCasesCommand { get; }
        public ICommand ExportTestCasesCommand { get; }
        public ICommand AnalyzeSourceFileCommand { get; }
        public ICommand AddSourceFileCommand { get; }
        public ICommand RemoveSourceFileCommand { get; }
        public ICommand GenerateTestCasesCommand { get; }
        public ICommand ProjectSettingsCommand { get; }
        public ICommand AnalysisOptionsCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand ExitCommand { get; }

        // Command implementations
        private async Task NewProjectAsync()
        {
            try
            {
                var dialog = new Dialogs.NewProjectDialog();
                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = $"Creating new project: {dialog.ProjectName}...";

                    var newProject = await _projectService.CreateProjectAsync(
                        dialog.ProjectName,
                        dialog.SourceDirectory);

                    CurrentProject = newProject;
                    StatusMessage = $"Created new project: {newProject.Name}";

                    // Refresh UI
                    await RefreshSourceFilesAsync();
                    await RefreshTestCasesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new project");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error creating project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OpenProjectAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "C-TestForge Project Files|*.ctf|All Files|*.*",
                    Title = "Open Project"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    StatusMessage = $"Opening project: {Path.GetFileNameWithoutExtension(openFileDialog.FileName)}...";

                    var project = await _projectService.LoadProjectAsync(openFileDialog.FileName);
                    if (project != null)
                    {
                        CurrentProject = project;
                        StatusMessage = $"Opened project: {project.Name}";

                        // Refresh UI
                        await RefreshSourceFilesAsync();
                        await RefreshTestCasesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening project");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error opening project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SaveProjectAsync()
        {
            try
            {
                StatusMessage = $"Saving project: {CurrentProject.Name}...";

                bool result = await _projectService.SaveProjectAsync(CurrentProject);
                if (result)
                {
                    StatusMessage = $"Saved project: {CurrentProject.Name}";

                    // Reset unsaved changes flag
                    HasUnsavedChanges = false;
                }
                else
                {
                    StatusMessage = $"Failed to save project: {CurrentProject.Name}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving project");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error saving project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanSaveProject() => CurrentProject != null;

        private void CloseProject()
        {
            // Clear all data
            CurrentProject = null;
            SourceFiles.Clear();
            TestCases.Clear();
            ClearAnalysisResults();
            StatusMessage = "Project closed";
        }

        private bool CanCloseProject() => CurrentProject != null;

        private async Task ParseSourceFilesAsync()
        {
            try
            {
                StatusMessage = "Parsing source files...";
                IsAnalyzing = true;

                // Clear existing source files
                SourceFiles.Clear();

                // Get source files from project
                var sourceFiles = await _projectService.GetSourceFilesAsync(CurrentProject);

                // Add source files to collection
                foreach (var file in sourceFiles)
                {
                    SourceFiles.Add(file);
                }

                IsAnalyzing = false;
                StatusMessage = $"Parsed {sourceFiles.Count} source files";
            }
            catch (Exception ex)
            {
                IsAnalyzing = false;
                _logger.LogError(ex, "Error parsing source files");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error parsing source files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanParseSourceFiles() => CurrentProject != null && CurrentProject.SourceFiles.Count > 0;

        private async Task ImportTestCasesAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Test Case Files|*.tst;*.csv;*.xlsx;*.json|TST Files|*.tst|CSV Files|*.csv|Excel Files|*.xlsx|JSON Files|*.json|All Files|*.*",
                    Title = "Import Test Cases"
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = $"Importing test cases from {Path.GetFileName(dialog.FileName)}...";

                    // Import test cases
                    // Uncomment when test case service is implemented
                    // var testCases = await _testCaseService.ImportTestCasesFromFileAsync(dialog.FileName);

                    // Refresh test cases
                    await RefreshTestCasesAsync();

                    StatusMessage = $"Imported test cases from {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing test cases");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error importing test cases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanImportTestCases() => CurrentProject != null;

        private async Task ExportTestCasesAsync()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "TST Files|*.tst|CSV Files|*.csv|Excel Files|*.xlsx|JSON Files|*.json|All Files|*.*",
                    Title = "Export Test Cases",
                    DefaultExt = ".tst"
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = $"Exporting test cases to {Path.GetFileName(dialog.FileName)}...";

                    // Export test cases
                    // Uncomment when test case service is implemented
                    // await _testCaseService.ExportTestCasesToFileAsync(TestCases.ToList(), dialog.FileName);

                    StatusMessage = $"Exported test cases to {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting test cases");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error exporting test cases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddSourceFileAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "C Source Files|*.c;*.h|C Files|*.c|Header Files|*.h|All Files|*.*",
                    Title = "Add Source File",
                    Multiselect = true
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = "Adding source files...";

                    foreach (var filePath in dialog.FileNames)
                    {
                        await _projectService.AddSourceFileAsync(CurrentProject, filePath);
                    }

                    // Mark project as having unsaved changes
                    HasUnsavedChanges = true;

                    // Refresh source files
                    await RefreshSourceFilesAsync();

                    StatusMessage = $"Added {dialog.FileNames.Length} source file(s)";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding source files");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error adding source files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanAddSourceFile() => CurrentProject != null;

        private async Task RemoveSourceFileAsync(SourceFile sourceFile)
        {
            if (sourceFile == null)
            {
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to remove the source file '{sourceFile.FileName}' from the project?",
                    "Remove Source File",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    StatusMessage = $"Removing source file: {sourceFile.FileName}...";

                    bool success = await _projectService.RemoveSourceFileAsync(CurrentProject, sourceFile.FilePath);

                    if (success)
                    {
                        // Mark project as having unsaved changes
                        HasUnsavedChanges = true;

                        // Refresh source files
                        await RefreshSourceFilesAsync();

                        StatusMessage = $"Removed source file: {sourceFile.FileName}";
                    }
                    else
                    {
                        StatusMessage = $"Failed to remove source file: {sourceFile.FileName}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error removing source file: {sourceFile.FilePath}");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error removing source file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanRemoveSourceFile(SourceFile sourceFile) => CurrentProject != null && sourceFile != null;

        private async Task GenerateTestCasesAsync()
        {
            try
            {
                // This would typically show a dialog to select functions to generate test cases for
                // For now, we'll just show a placeholder message
                MessageBox.Show(
                    "Test case generation functionality will be implemented in a future version.",
                    "Generate Test Cases",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test cases");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error generating test cases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanGenerateTestCases() => CurrentProject != null && Functions.Count > 0;

        private void ShowProjectSettings()
        {
            try
            {
                // This would typically show a dialog to edit project settings
                // For now, we'll just show a placeholder message
                MessageBox.Show(
                    "Project settings dialog will be implemented in a future version.",
                    "Project Settings",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing project settings");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error showing project settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanShowProjectSettings() => CurrentProject != null;

        private void ShowAnalysisOptions()
        {
            try
            {
                // This would typically show a dialog to edit analysis options
                // For now, we'll just show a placeholder message
                MessageBox.Show(
                    "Analysis options dialog will be implemented in a future version.",
                    "Analysis Options",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing analysis options");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error showing analysis options: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowAboutDialog()
        {
            MessageBox.Show(
                "C-TestForge v1.0\n\nA comprehensive tool for analyzing, managing, and automatically generating test cases for C code.\n\nDeveloped using ClangSharp and Z3 Theorem Prover.",
                "About C-TestForge",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void Exit()
        {
            // This will trigger the Window_Closing event in MainWindow.xaml.cs
            // which will check for unsaved changes
            Application.Current.MainWindow.Close();
        }

        private async Task AnalyzeSourceFileAsync(SourceFile sourceFile)
        {
            if (sourceFile == null)
            {
                return;
            }

            try
            {
                StatusMessage = $"Analyzing source file: {sourceFile.FileName}...";
                IsAnalyzing = true;

                // Clear previous analysis results
                ClearAnalysisResults();

                // Analyze source file
                _analysisResult = await _analysisService.AnalyzeSourceFileAsync(sourceFile, _analysisOptions);

                // Update collections with analysis results
                UpdateAnalysisResults(_analysisResult);

                IsAnalyzing = false;
                StatusMessage = $"Analysis complete for {sourceFile.FileName}";
            }
            catch (Exception ex)
            {
                IsAnalyzing = false;
                _logger.LogError(ex, $"Error analyzing source file: {sourceFile.FilePath}");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error analyzing source file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanAnalyzeSourceFile(SourceFile sourceFile) => sourceFile != null;

        // Helper methods
        private async Task RefreshSourceFilesAsync()
        {
            SourceFiles.Clear();
            ClearAnalysisResults();

            if (CurrentProject != null)
            {
                var sourceFiles = await _projectService.GetSourceFilesAsync(CurrentProject);
                foreach (var file in sourceFiles)
                {
                    SourceFiles.Add(file);
                }
            }
        }

        private async Task RefreshTestCasesAsync()
        {
            TestCases.Clear();
            if (CurrentProject != null)
            {
                // Fetch test cases for the current project
                // Uncomment when test case service is implemented
                // var testCases = await _testCaseService.GetTestCasesForProjectAsync(CurrentProject.Id);
                // foreach (var testCase in testCases)
                // {
                //     TestCases.Add(testCase);
                // }
            }
        }

        private void ClearAnalysisResults()
        {
            Definitions.Clear();
            Variables.Clear();
            Functions.Clear();
            ConditionalDirectives.Clear();
            _analysisResult = null;
        }

        private void UpdateAnalysisResults(AnalysisResult result)
        {
            if (result == null)
            {
                return;
            }

            // Update collections
            foreach (var definition in result.Definitions)
            {
                Definitions.Add(definition);
            }

            foreach (var variable in result.Variables)
            {
                Variables.Add(variable);
            }

            foreach (var function in result.Functions)
            {
                Functions.Add(function);
            }

            foreach (var directive in result.ConditionalDirectives)
            {
                ConditionalDirectives.Add(directive);
            }
        }
    }
}