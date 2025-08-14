using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows.Data;
using System.ComponentModel;
using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using C_TestForge.Models.Core;
using C_TestForge.Models.TestCases;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using C_TestForge.Core.Interfaces.Projects;
using Prism.Regions;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Prism.Ioc;
using System.Text;
using System.Text.Json;

namespace C_TestForge.UI.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private readonly ITestCaseService _testCaseService;
        private readonly IAnalysisService _analysisService;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly ISourceFileService _sourceFileService;
        private readonly IClangSharpParserService _clangSharpParserService;
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IRegionManager _regionManager;
        private readonly SettingsViewModel _settingsViewModel;

        private Project? _currentProject;
        private string _statusMessage = string.Empty;
        private SourceFile? _selectedSourceFile;
        private ObservableCollection<SourceFile> _sourceFiles = new();
        private ObservableCollection<TestCase> _testCases = new();
        private TestCase? _selectedTestCase;
        private AnalysisResult? _analysisResult;
        private ProjectAnalysisResult? _projectAnalysisResult;
        private bool _isAnalyzing;
        private AnalysisOptions _analysisOptions = new();
        private bool _hasUnsavedChanges;
        private bool _isNavigationDrawerExpanded = true;
        private string _selectedMenuItem = "Dashboard";
        private string _searchText = string.Empty;
        private double _sidebarWidth = 250;

        // Collections for analysis results
        private ObservableCollection<CDefinition> _definitions = new();
        private ObservableCollection<CVariable> _variables = new();
        private ObservableCollection<CFunction> _functions = new();
        private ObservableCollection<ConditionalDirective> _conditionalDirectives = new();

        private ObservableCollection<Project> _openProjects = new();
        private ObservableCollection<Project> _availableProjects = new();

        public MainWindowViewModel(
            IProjectService projectService,
            ITestCaseService testCaseService,
            IAnalysisService analysisService,
            ISourceCodeService sourceCodeService,
            ISourceFileService sourceFileService,
            IClangSharpParserService clangSharpParserService,
            ILogger<MainWindowViewModel> logger,
            IRegionManager regionManager,
            SettingsViewModel settingsViewModel)
        {
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _testCaseService = testCaseService ?? throw new ArgumentNullException(nameof(testCaseService));
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
            _sourceFileService = sourceFileService ?? throw new ArgumentNullException(nameof(sourceFileService));
            _clangSharpParserService = clangSharpParserService ?? throw new ArgumentNullException(nameof(clangSharpParserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _settingsViewModel = settingsViewModel;

            // Initialize commands
            NewProjectCommand = new AsyncRelayCommand(NewProjectAsync);
            OpenProjectCommand = new AsyncRelayCommand(OpenProjectAsync);
            SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync, CanSaveProject);
            CloseProjectCommand = new RelayCommand(CloseProject, CanCloseProject);
            ParseSourceFilesCommand = new AsyncRelayCommand(ParseSourceFilesAsync, CanParseSourceFiles);
            ImportTestCasesCommand = new AsyncRelayCommand(ImportTestCasesAsync, CanImportTestCases);
            ExportTestCasesCommand = new AsyncRelayCommand(ExportTestCasesAsync, CanExportTestCases);
            AnalyzeSourceFileCommand = new AsyncRelayCommand<SourceFile>(AnalyzeSourceFileAsync, CanAnalyzeSourceFile);
            AddSourceFileCommand = new AsyncRelayCommand(AddSourceFileAsync, CanAddSourceFile);
            RemoveSourceFileCommand = new AsyncRelayCommand<SourceFile>(RemoveSourceFileAsync, CanRemoveSourceFile);
            GenerateTestCasesCommand = new AsyncRelayCommand(GenerateTestCasesAsync, CanGenerateTestCases);
            ProjectSettingsCommand = new RelayCommand(ShowProjectSettings, CanShowProjectSettings);
            AnalysisOptionsCommand = new RelayCommand(ShowAnalysisOptions);
            AboutCommand = new RelayCommand(ShowAboutDialog);
            ExitCommand = new RelayCommand(Exit);
            SignOutCommand = new RelayCommand(SignOut);
            AnalyzeFolderCommand = new AsyncRelayCommand(AnalyzeFolderDirectlyAsync);
            AnalyzeCompleteProjectCommand = new AsyncRelayCommand<string>(AnalyzeCompleteProjectAsync);
            ShowProjectAnalysisReportCommand = new RelayCommand(ShowProjectAnalysisReport);
            ExportSourceFileCommand = new AsyncRelayCommand<SourceFile>(ExportSourceFileAsync);
            ExportDefinitionsCommand = new AsyncRelayCommand(ExportDefinitionsAsync);
            ExportVariablesCommand = new AsyncRelayCommand(ExportVariablesAsync);
            ExportFunctionsCommand = new AsyncRelayCommand(ExportFunctionsAsync);
            ExportDirectivesCommand = new AsyncRelayCommand(ExportDirectivesAsync);
            ExportDependencyGraphCommand = new AsyncRelayCommand(ExportDependencyGraphAsync);
            
            // Additional commands for filtering and analysis
            RefreshDefinitionsCommand = new RelayCommand(RefreshDefinitions);
            SearchDefinitionsCommand = new RelayCommand(SearchDefinitions);
            ClearDefinitionFiltersCommand = new RelayCommand(ClearDefinitionFilters);
            AnalyzeVariableConstraintsCommand = new AsyncRelayCommand(AnalyzeVariableConstraintsAsync);
            ClearVariableFiltersCommand = new RelayCommand(ClearVariableFilters);
            AnalyzeFunctionRelationshipsCommand = new AsyncRelayCommand(AnalyzeFunctionRelationshipsAsync);
            ShowAdvancedSearchCommand = new RelayCommand(ShowAdvancedSearch);
            ClearFunctionFiltersCommand = new RelayCommand(ClearFunctionFilters);
            VisualizeDependencyGraphCommand = new RelayCommand(VisualizeDependencyGraph);
            AnalyzeDependencyCyclesCommand = new AsyncRelayCommand(AnalyzeDependencyCyclesAsync);
            ViewDependenciesCommand = new RelayCommand<object>(ViewDependencies);
            ViewDependentsCommand = new RelayCommand<object>(ViewDependents);
            AnalyzeFileCommand = new AsyncRelayCommand<object>(AnalyzeFileAsync);
            OpenFileCommand = new RelayCommand<object>(OpenFile);
            AnalyzeSingleFunctionCommand = new AsyncRelayCommand<CFunction>(AnalyzeSingleFunctionAsync);
            GenerateTestCasesForFunctionCommand = new AsyncRelayCommand<CFunction>(GenerateTestCasesForFunctionAsync);
            ShowAnalysisOptionsCommand = new RelayCommand(ShowAnalysisOptions);
            
            // UI commands
            NavigateCommand = new RelayCommand<string>(Navigate);
            ToggleNavigationDrawerCommand = new RelayCommand(ToggleNavigationDrawer);

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

            // Initialize filter options
            InitializeFilterOptions();

            // Set default status
            StatusMessage = "Ready";
            
            // Navigate to Dashboard by default
            Navigate("Dashboard");
        }

        private void InitializeFilterOptions()
        {
            // Initialize scope options
            AvailableScopes = new List<string> { "All", "Global", "Local", "Static", "Extern", "Parameter" };
            
            // Initialize type options  
            AvailableTypes = new List<string> { "All", "int", "char", "float", "double", "void", "bool", "struct", "union", "enum" };
            
            // Initialize return type options
            AvailableReturnTypes = new List<string> { "All", "void", "int", "char", "float", "double", "bool", "struct*", "char*" };
            
            // Initialize parameter count options
            ParameterCountOptions = new List<string> { "All", "0", "1", "2", "3", "4", "5+" };
            
            // Initialize file type options
            AvailableFileTypes = new List<string> { "All", "CHeader", "CppHeader", "CSource", "CppSource" };
        }

        // Properties
        public Project? CurrentProject
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

        public SourceFile? SelectedSourceFile
        {
            get => _selectedSourceFile;
            set
            {
                if (SetProperty(ref _selectedSourceFile, value))
                {
                    // Mark project as having unsaved changes
                    HasUnsavedChanges = true;
                    
                    // Update analysis command can execute state
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

        public TestCase? SelectedTestCase
        {
            get => _selectedTestCase;
            set => SetProperty(ref _selectedTestCase, value);
        }

        public bool IsAnalyzing
        {
            get => _isAnalyzing;
            set => SetProperty(ref _isAnalyzing, value);
        }
        
        // UI Properties
        public bool IsNavigationDrawerExpanded
        {
            get => _isNavigationDrawerExpanded;
            set 
            { 
                if (SetProperty(ref _isNavigationDrawerExpanded, value))
                {
                    AnimateSidebarWidth(value ? 250 : 70);
                }
            }
        }

        private void AnimateSidebarWidth(double targetWidth)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = Application.Current.MainWindow;
                if (mainWindow == null) return;
                var sidebar = mainWindow.FindName("SidebarBorder") as System.Windows.Controls.Border;
                if (sidebar == null) return;
                var animation = new System.Windows.Media.Animation.DoubleAnimation
                {
                    To = targetWidth,
                    Duration = new System.Windows.Duration(TimeSpan.FromMilliseconds(300)),
                    EasingFunction = new System.Windows.Media.Animation.CubicEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseInOut }
                };
                sidebar.BeginAnimation(System.Windows.FrameworkElement.WidthProperty, animation);
            });
        }
        
        public double SidebarWidth
        {
            get => _sidebarWidth;
            set => SetProperty(ref _sidebarWidth, value);
        }
        
        public string SelectedMenuItem
        {
            get => _selectedMenuItem;
            set => SetProperty(ref _selectedMenuItem, value);
        }
        
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
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

        // Project Analysis Result property
        public ProjectAnalysisResult? ProjectAnalysisResult
        {
            get => _projectAnalysisResult;
            set 
            { 
                SetProperty(ref _projectAnalysisResult, value);
                OnPropertyChanged(nameof(HasAnalysisData));
                OnPropertyChanged(nameof(HeaderFilesCount));
                OnPropertyChanged(nameof(SourceFilesCount));
            }
        }

        // Property to check if we have any analysis data to display
        public bool HasAnalysisData => 
            ProjectAnalysisResult != null || 
            SelectedSourceFile != null ||
            Functions.Count > 0 || 
            Variables.Count > 0 || 
            Definitions.Count > 0;

        // Helper properties for binding
        public int HeaderFilesCount => ProjectAnalysisResult?.GetHeaderFiles()?.Count ?? 0;
        public int SourceFilesCount => ProjectAnalysisResult?.GetSourceFiles()?.Count ?? 0;

        // Additional properties for filtering and search
        private string _definitionSearchText = string.Empty;
        private string _variableSearchText = string.Empty;
        private string _functionSearchText = string.Empty;
        private string _dependencySearchText = string.Empty;

        // Filter flags
        private bool _showFunctionLikeMacros = true;
        private bool _showEnabledOnly = false;
        private bool _showConstOnly = false;
        private bool _showArraysOnly = false;
        private bool _showPointersOnly = false;
        private bool _showWithDefaultValues = false;
        private bool _showStaticOnly = false;
        private bool _showInlineOnly = false;
        private bool _showWithParametersOnly = false;
        private bool _showFunctionBody = false;
        private bool _showOnlyWithDependencies = false;

        public string DefinitionSearchText
        {
            get => _definitionSearchText;
            set => SetProperty(ref _definitionSearchText, value);
        }

        public string VariableSearchText
        {
            get => _variableSearchText;
            set => SetProperty(ref _variableSearchText, value);
        }

        public string FunctionSearchText
        {
            get => _functionSearchText;
            set => SetProperty(ref _functionSearchText, value);
        }

        public string DependencySearchText
        {
            get => _dependencySearchText;
            set => SetProperty(ref _dependencySearchText, value);
        }

        // Filter properties
        public bool ShowFunctionLikeMacros
        {
            get => _showFunctionLikeMacros;
            set => SetProperty(ref _showFunctionLikeMacros, value);
        }

        public bool ShowEnabledOnly
        {
            get => _showEnabledOnly;
            set => SetProperty(ref _showEnabledOnly, value);
        }

        public bool ShowConstOnly
        {
            get => _showConstOnly;
            set => SetProperty(ref _showConstOnly, value);
        }

        public bool ShowArraysOnly
        {
            get => _showArraysOnly;
            set => SetProperty(ref _showArraysOnly, value);
        }

        public bool ShowPointersOnly
        {
            get => _showPointersOnly;
            set => SetProperty(ref _showPointersOnly, value);
        }

        public bool ShowWithDefaultValues
        {
            get => _showWithDefaultValues;
            set => SetProperty(ref _showWithDefaultValues, value);
        }

        public bool ShowStaticOnly
        {
            get => _showStaticOnly;
            set => SetProperty(ref _showStaticOnly, value);
        }

        public bool ShowInlineOnly
        {
            get => _showInlineOnly;
            set => SetProperty(ref _showInlineOnly, value);
        }

        public bool ShowWithParametersOnly
        {
            get => _showWithParametersOnly;
            set => SetProperty(ref _showWithParametersOnly, value);
        }

        public bool ShowFunctionBody
        {
            get => _showFunctionBody;
            set => SetProperty(ref _showFunctionBody, value);
        }

        public bool ShowOnlyWithDependencies
        {
            get => _showOnlyWithDependencies;
            set => SetProperty(ref _showOnlyWithDependencies, value);
        }

        // Selected items for detailed views
        private CVariable? _selectedVariable;
        private CFunction? _selectedFunction;
        private object? _selectedDependencyFile;

        public CVariable? SelectedVariable
        {
            get => _selectedVariable;
            set => SetProperty(ref _selectedVariable, value);
        }

        public CFunction? SelectedFunction
        {
            get => _selectedFunction;
            set => SetProperty(ref _selectedFunction, value);
        }

        public object? SelectedDependencyFile
        {
            get => _selectedDependencyFile;
            set => SetProperty(ref _selectedDependencyFile, value);
        }

        // Additional properties needed for UI binding
        private int _selectedLineNumber;
        private List<string> _availableScopes = new();
        private List<string> _availableTypes = new();
        private List<string> _availableReturnTypes = new();
        private List<string> _parameterCountOptions = new();
        private List<string> _availableFileTypes = new();
        private string? _selectedScopeFilter;
        private string? _selectedTypeFilter;
        private string? _selectedReturnTypeFilter;
        private string? _selectedParameterCountFilter;
        private string? _selectedFileTypeFilter;

        public int SelectedLineNumber
        {
            get => _selectedLineNumber;
            set => SetProperty(ref _selectedLineNumber, value);
        }

        public List<string> AvailableScopes
        {
            get => _availableScopes;
            set => SetProperty(ref _availableScopes, value);
        }

        public List<string> AvailableTypes
        {
            get => _availableTypes;
            set => SetProperty(ref _availableTypes, value);
        }

        public List<string> AvailableReturnTypes
        {
            get => _availableReturnTypes;
            set => SetProperty(ref _availableReturnTypes, value);
        }

        public List<string> ParameterCountOptions
        {
            get => _parameterCountOptions;
            set => SetProperty(ref _parameterCountOptions, value);
        }

        public List<string> AvailableFileTypes
        {
            get => _availableFileTypes;
            set => SetProperty(ref _availableFileTypes, value);
        }

        public string? SelectedScopeFilter
        {
            get => _selectedScopeFilter;
            set => SetProperty(ref _selectedScopeFilter, value);
        }

        public string? SelectedTypeFilter
        {
            get => _selectedTypeFilter;
            set => SetProperty(ref _selectedTypeFilter, value);
        }

        public string? SelectedReturnTypeFilter
        {
            get => _selectedReturnTypeFilter;
            set => SetProperty(ref _selectedReturnTypeFilter, value);
        }

        public string? SelectedParameterCountFilter
        {
            get => _selectedParameterCountFilter;
            set => SetProperty(ref _selectedParameterCountFilter, value);
        }

        public string? SelectedFileTypeFilter
        {
            get => _selectedFileTypeFilter;
            set => SetProperty(ref _selectedFileTypeFilter, value);
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
        public ICommand NavigateCommand { get; }
        public ICommand SignOutCommand { get; }
        public ICommand AnalyzeFolderCommand { get; }
        public ICommand AnalyzeCompleteProjectCommand { get; }
        public ICommand ShowProjectAnalysisReportCommand { get; }
        public ICommand ExportSourceFileCommand { get; }
        public ICommand ExportDefinitionsCommand { get; }
        public ICommand ExportVariablesCommand { get; }
        public ICommand ExportFunctionsCommand { get; }
        public ICommand ExportDirectivesCommand { get; }
        public ICommand ExportDependencyGraphCommand { get; }

        // Additional commands for filtering and analysis
        public ICommand RefreshDefinitionsCommand { get; }
        public ICommand SearchDefinitionsCommand { get; }
        public ICommand ClearDefinitionFiltersCommand { get; }
        public ICommand AnalyzeVariableConstraintsCommand { get; }
        public ICommand ClearVariableFiltersCommand { get; }
        public ICommand AnalyzeFunctionRelationshipsCommand { get; }
        public ICommand ShowAdvancedSearchCommand { get; }
        public ICommand ClearFunctionFiltersCommand { get; }
        public ICommand VisualizeDependencyGraphCommand { get; }
        public ICommand AnalyzeDependencyCyclesCommand { get; }
        public ICommand ViewDependenciesCommand { get; }
        public ICommand ViewDependentsCommand { get; }
        public ICommand AnalyzeFileCommand { get; }
        public ICommand OpenFileCommand { get; }
        public ICommand AnalyzeSingleFunctionCommand { get; }
        public ICommand GenerateTestCasesForFunctionCommand { get; }
        public ICommand ShowAnalysisOptionsCommand { get; }
        public ICommand ToggleNavigationDrawerCommand { get; }
        public ICommand ViewProjectCommand => new RelayCommand<Project>(ViewProject);

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
                await Task.CompletedTask; // Temporary to avoid CS1998 warning
            }
        }

        private void UpdateAnalysisResults(AnalysisResult? result)
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

        private void UpdateProjectAnalysisResults(ProjectAnalysisResult? result)
        {
            if (result == null)
            {
                return;
            }

            ProjectAnalysisResult = result;

            // Clear existing results
            ClearAnalysisResults();

            // Update collections with project analysis results
            foreach (var definition in result.Definitions)
            {
                Definitions.Add(definition);
            }

            foreach (var macro in result.Macros)
            {
                if (!Definitions.Any(d => d.Name == macro.Name && d.SourceFile == macro.SourceFile))
                {
                    Definitions.Add(macro);
                }
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

            // Notify UI that analysis data is available
            OnPropertyChanged(nameof(HasAnalysisData));

            // Log analysis summary
            _logger.LogInformation($"Project analysis completed - Files: {result.ProcessedFileCount}/{result.TotalFiles}, " +
                $"Functions: {result.Functions?.Count}, Variables: {result.Variables?.Count}, " +
                $"Macros: {result.Macros?.Count}, Duration: {result.Duration.TotalSeconds:F2}s");
        }

        private void ClearAnalysisResults()
        {
            Definitions.Clear();
            Variables.Clear();
            Functions.Clear();
            ConditionalDirectives.Clear();
            _analysisResult = null;
            OnPropertyChanged(nameof(HasAnalysisData));
        }

        private bool CanAddSourceFile() => CurrentProject != null;

        private bool CanAnalyzeSourceFile(SourceFile? sourceFile) => sourceFile != null;

        private bool CanRemoveSourceFile(SourceFile? sourceFile) => CurrentProject != null && sourceFile != null;

        private bool CanGenerateTestCases() => CurrentProject != null && Functions.Count > 0;

        private bool CanShowProjectSettings() => CurrentProject != null;

        /// <summary>
        /// Lấy danh sách các project trong thư mục DefaultProjectLocation
        /// </summary>
        public List<string> GetProjectFilesFromDefaultLocation()
        {
            var root = _settingsViewModel.DefaultProjectLocation;
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
                return new List<string>();
            return Directory.GetFiles(root, "*.ctproj", SearchOption.TopDirectoryOnly).ToList();
        }

        // Navigation Command implementation
        private void Navigate(string? viewName)
        {
            if (string.IsNullOrEmpty(viewName))
                return;

            if (viewName == "Dashboard")
            {
                // Load project list mỗi khi vào Dashboard
                _ = LoadProjectsFromDefaultLocationAsync();
            }
            try
            {
                // Update the selected menu item
                SelectedMenuItem = viewName;
                // Navigate to the selected view
                _regionManager.RequestNavigate("MainRegion", viewName + "View");
                // Update status message
                StatusMessage = $"Viewing {viewName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error navigating to view: {viewName}");
                StatusMessage = $"Error: Could not navigate to {viewName}";
            }
        }

        private void SignOut()
        {
            MessageBox.Show(
                "Sign out functionality will be implemented in a future version.",
                "Sign Out",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

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

                    if (!_openProjects.Contains(newProject))
                        _openProjects.Add(newProject);

                    CurrentProject = newProject;
                    StatusMessage = $"Created new project: {newProject.Name}";

                    // Refresh UI
                    await RefreshSourceFilesAsync();
                    await RefreshTestCasesAsync();
                    
                    // Navigate to Project Explorer
                    Navigate("ProjectExplorer");
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
                    Filter = "C-TestForge Project Files|*.ctproj|All Files|*.*",
                    Title = "Open Project"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    StatusMessage = $"Opening project: {Path.GetFileNameWithoutExtension(openFileDialog.FileName)}...";

                    var project = await _projectService.LoadProjectAsync(openFileDialog.FileName);
                    if (project != null)
                    {
                        if (!_openProjects.Contains(project))
                            _openProjects.Add(project);

                        CurrentProject = project;
                        StatusMessage = $"Opened project: {project.Name}";

                        // Refresh UI
                        await RefreshSourceFilesAsync();
                        await RefreshTestCasesAsync();
                        
                        // Navigate to Project Explorer
                        Navigate("ProjectExplorer");
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
                if (CurrentProject == null) return;
                
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
            ProjectAnalysisResult = null;
            StatusMessage = "Project closed";
            
            // Navigate to Dashboard
            Navigate("Dashboard");
        }

        private bool CanCloseProject() => CurrentProject != null;

        private async Task ParseSourceFilesAsync()
        {
            try
            {
                if (CurrentProject == null) return;
                
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
                
                // Navigate to Source Analysis
                Navigate("SourceAnalysis");
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
                    
                    // Navigate to Test Cases view
                    Navigate("TestCases");
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

        private bool CanExportTestCases() => CurrentProject != null;

        private async Task AnalyzeSourceFileAsync(SourceFile? sourceFile)
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

                // Check if sourceFile has been processed before
                if(string.IsNullOrEmpty(sourceFile.ProcessedContent))
                {
                     _sourceFileService.ProcessTypeReplacements(sourceFile);
                }    

                // Analyze source file
                _analysisResult = await _analysisService.AnalyzeSourceFileAsync(sourceFile, _analysisOptions);

                // Update collections with analysis results
                UpdateAnalysisResults(_analysisResult);

                IsAnalyzing = false;
                StatusMessage = $"Analysis complete for {sourceFile.FileName}";
                
                // Navigate to Source Analysis view after analysis is complete
                Navigate("SourceAnalysis");
            }
            catch (Exception ex)
            {
                IsAnalyzing = false;
                _logger.LogError(ex, $"Error analyzing source file: {sourceFile.FilePath}");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error analyzing source file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Phân tích toàn bộ dự án sử dụng ClangSharpParserService
        /// </summary>
        /// <param name="projectPath">Đường dẫn đến thư mục dự án</param>
        /// <returns>Task</returns>
        private async Task AnalyzeCompleteProjectAsync(string? projectPath)
        {
            if (string.IsNullOrEmpty(projectPath))
            {
                // Nếu không có path được truyền, sử dụng folder picker
                var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
                {
                    Description = "Select project root directory for complete analysis",
                    UseDescriptionForTitle = true,
                    Multiselect = false
                };

                if (dialog.ShowDialog() != true)
                {
                    return;
                }

                projectPath = dialog.SelectedPath;
            }

            if (!Directory.Exists(projectPath))
            {
                MessageBox.Show(
                    $"Directory does not exist: {projectPath}",
                    "Invalid Directory",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            try
            {
                StatusMessage = $"Starting complete project analysis: {projectPath}...";
                IsAnalyzing = true;

                // Show confirmation dialog with analysis details
                var confirmResult = MessageBox.Show(
                    $"This will perform a complete analysis of the project at:\n{projectPath}\n\n" +
                    "The analysis will:\n" +
                    "• Scan all C/C++ files in the directory\n" +
                    "• Build dependency graph\n" +
                    "• Extract functions, variables, and macros\n" +
                    "• Analyze relationships and constraints\n\n" +
                    "This process may take several minutes for large projects.\n\n" +
                    "Do you want to continue?",
                    "Complete Project Analysis",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                {
                    IsAnalyzing = false;
                    StatusMessage = "Project analysis cancelled";
                    return;
                }

                // Perform complete project analysis
                var analysisResult = await _clangSharpParserService.AnalyzeCompleteProjectAsync(projectPath);

                // Update UI with results
                UpdateProjectAnalysisResults(analysisResult);

                // Show completion summary
                var summaryReport = analysisResult.GenerateSummaryReport();
                
                StatusMessage = $"Complete project analysis finished - {analysisResult.Functions?.Count} functions, " +
                    $"{analysisResult.Variables?.Count} variables, {analysisResult.Macros?.Count} macros found";

                // Navigate to Source Analysis view to show results
                Navigate("SourceAnalysis");

                // Show detailed report
                ShowAnalysisReport(summaryReport);

                _logger.LogInformation($"Complete project analysis completed successfully for {projectPath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during complete project analysis: {ex.Message}");
                StatusMessage = $"Error during project analysis: {ex.Message}";
                MessageBox.Show(
                    $"Error during complete project analysis:\n\n{ex.Message}",
                    "Analysis Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        private async Task RemoveSourceFileAsync(SourceFile? sourceFile)
        {
            if (sourceFile == null || CurrentProject == null)
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
                
                await Task.CompletedTask; // Temporary to avoid CS1998 warning
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test cases");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error generating test cases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
                
                // Navigate to Settings
                Navigate("Settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing project settings");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error showing project settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

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
            Application.Current.MainWindow?.Close();
        }

        private async Task AddSourceFileAsync()
        {
            try
            {
                // Show a menu to choose between adding files, analyzing folder, or complete project analysis
                var choice = MessageBox.Show(
                    "Choose an option:\n\n" +
                    "• Yes: Add individual source files to project\n" +
                    "• No: Analyze entire folder/directory\n" +
                    "• Cancel: Cancel operation",
                    "Add Source Files",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (choice == MessageBoxResult.Cancel)
                    return;

                if (choice == MessageBoxResult.Yes)
                {
                    await AddIndividualFilesAsync();
                }
                else if (choice == MessageBoxResult.No)
                {
                    await AnalyzeFolderAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddSourceFileAsync");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Add individual source files to the project
        /// </summary>
        private async Task AddIndividualFilesAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "C/C++ Source Files|*.c;*.h;*.cpp;*.hpp;*.cc;*.cxx;*.hxx|" +
                            "C Files|*.c|" +
                            "C++ Files|*.cpp;*.cc;*.cxx|" +
                            "Header Files|*.h;*.hpp;*.hxx|" +
                            "All Files|*.*",
                    Title = "Add Source Files to Project",
                    Multiselect = true
                };

                if (dialog.ShowDialog() == true && CurrentProject != null)
                {
                    StatusMessage = "Adding source files to project...";
                    IsAnalyzing = true;

                    foreach (var filePath in dialog.FileNames)
                    {
                        await _projectService.AddSourceFileAsync(CurrentProject, filePath);
                    }

                    // Mark project as having unsaved changes
                    HasUnsavedChanges = true;

                    // Refresh source files
                    await RefreshSourceFilesAsync();

                    StatusMessage = $"Added {dialog.FileNames.Length} source file(s) to project";
                    
                    // Navigate to Project Explorer
                    Navigate("ProjectExplorer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding individual files");
                StatusMessage = $"Error adding files: {ex.Message}";
                MessageBox.Show($"Error adding source files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        /// <summary>
        /// Analyze an entire folder and add all C/C++ files found
        /// </summary>
        private async Task AnalyzeFolderAsync()
        {
            try
            {
                // Use Ookii.Dialogs for better WPF integration
                var dialog = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
                {
                    Description = "Select folder to analyze for C/C++ source files",
                    UseDescriptionForTitle = true,
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    var selectedPath = dialog.SelectedPath;
                    
                    // Show options for folder analysis
                    var analysisChoice = MessageBox.Show(
                        $"Selected folder: {selectedPath}\n\n" +
                        "Choose analysis mode:\n\n" +
                        "• Yes: Quick scan and add files to current project\n" +
                        "• No: Complete project analysis with full dependency mapping\n" +
                        "• Cancel: Cancel operation",
                        "Folder Analysis Options",
                        MessageBoxButton.YesNoCancel,
                        MessageBoxImage.Question);

                    if (analysisChoice == MessageBoxResult.Cancel)
                        return;

                    if (analysisChoice == MessageBoxResult.Yes)
                    {
                        await QuickFolderScanAsync(selectedPath);
                    }
                    else if (analysisChoice == MessageBoxResult.No)
                    {
                        // Use complete project analysis
                        await AnalyzeCompleteProjectAsync(selectedPath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing folder");
                StatusMessage = $"Error analyzing folder: {ex.Message}";
                MessageBox.Show($"Error analyzing folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        /// <summary>
        /// Quick scan of folder to find and add C/C++ files to project
        /// </summary>
        private async Task QuickFolderScanAsync(string folderPath)
        {
            try
            {
                StatusMessage = $"Scanning folder: {folderPath}...";
                IsAnalyzing = true;

                // Ask if user wants recursive scan
                var recursive = MessageBox.Show(
                    "Include subdirectories in scan?",
                    "Recursive Scan",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes;

                // Manually scan for C/C++ files (simplified implementation)
                var foundFiles = await ScanDirectoryForFilesAsync(folderPath, recursive);

                if (foundFiles.Count == 0)
                {
                    StatusMessage = "No C/C++ source files found in selected folder";
                    MessageBox.Show(
                        "No C/C++ source files (.c, .h, .cpp, .hpp) were found in the selected folder.",
                        "No Files Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                // Show preview of found files
                var fileList = string.Join("\n", foundFiles.Take(10));
                if (foundFiles.Count > 10)
                    fileList += $"\n... and {foundFiles.Count - 10} more files";

                var addFiles = MessageBox.Show(
                    $"Found {foundFiles.Count} C/C++ files:\n\n{fileList}\n\nAdd all files to project?",
                    "Files Found",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (addFiles == MessageBoxResult.Yes && CurrentProject != null)
                {
                    StatusMessage = $"Adding {foundFiles.Count} files to project...";

                    // Add files to project
                    foreach (var filePath in foundFiles)
                    {
                        await _projectService.AddSourceFileAsync(CurrentProject, filePath);
                    }

                    // Mark project as having unsaved changes
                    HasUnsavedChanges = true;

                    // Refresh source files
                    await RefreshSourceFilesAsync();

                    StatusMessage = $"Added {foundFiles.Count} source files from folder scan";
                    
                    // Navigate to Project Explorer
                    Navigate("ProjectExplorer");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in quick folder scan: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Simple file scanner for C/C++ files
        /// </summary>
        private async Task<List<string>> ScanDirectoryForFilesAsync(string directoryPath, bool recursive)
        {
            return await Task.Run(() =>
            {
                var files = new List<string>();
                try
                {
                    string[] extensions = { "*.c", "*.h", "*.cpp", "*.hpp", "*.cc", "*.cxx", "*.hxx" };
                    
                    var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    
                    foreach (var extension in extensions)
                    {
                        files.AddRange(Directory.GetFiles(directoryPath, extension, searchOption));
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error scanning directory {directoryPath}");
                }
                
                return files.Distinct().ToList();
            });
        }

        /// <summary>
        /// Show detailed analysis report in a message box or dialog
        /// </summary>
        private void ShowAnalysisReport(string report)
        {
            try
            {
                // For now, show in a scrollable message box
                // In a real application, you might want to create a dedicated dialog
                MessageBox.Show(
                    report,
                    "Detailed Analysis Report",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing analysis report: {ex.Message}");
            }
        }

        /// <summary>
        /// Direct folder analysis command - can be used from menu/toolbar
        /// </summary>
        private async Task AnalyzeFolderDirectlyAsync()
        {
            try
            {
                await AnalyzeFolderAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in direct folder analysis");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error analyzing folder: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region Legacy Support Methods (kept for backward compatibility)

        /// <summary>
        /// Deep analysis of entire folder with dependency mapping and insights (Legacy)
        /// This method is kept for backward compatibility but now uses AnalyzeCompleteProjectAsync
        /// </summary>
        private async Task DeepFolderAnalysisAsync(string folderPath)
        {
            try
            {
                // Use the new complete project analysis instead
                await AnalyzeCompleteProjectAsync(folderPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in deep folder analysis: {ex.Message}");
                MessageBox.Show($"Error in deep analysis: {ex.Message}", "Analysis Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Export and Report Methods

        /// <summary>
        /// Show detailed project analysis report
        /// </summary>
        private void ShowProjectAnalysisReport()
        {
            try
            {
                if (ProjectAnalysisResult == null)
                {
                    MessageBox.Show("No project analysis data available.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var report = ProjectAnalysisResult.GenerateSummaryReport();
                ShowAnalysisReport(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing project analysis report");
                MessageBox.Show($"Error showing report: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export source file content to file
        /// </summary>
        private async Task ExportSourceFileAsync(SourceFile? sourceFile)
        {
            if (sourceFile == null)
                return;

            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Text Files|*.txt|All Files|*.*",
                    Title = "Export Source File",
                    FileName = $"{sourceFile.FileName}_exported.txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllTextAsync(dialog.FileName, sourceFile.Content);
                    StatusMessage = $"Exported source file to {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting source file");
                MessageBox.Show($"Error exporting source file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export definitions to CSV file
        /// </summary>
        private async Task ExportDefinitionsAsync()
        {
            try
            {
                if (Definitions.Count == 0)
                {
                    MessageBox.Show("No definitions to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "CSV Files|*.csv|JSON Files|*.json|All Files|*.*",
                    Title = "Export Definitions",
                    FileName = "definitions.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var extension = Path.GetExtension(dialog.FileName).ToLower();
                    
                    if (extension == ".json")
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(Definitions.ToList(), new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(dialog.FileName, json);
                    }
                    else
                    {
                        var csv = new StringBuilder();
                        csv.AppendLine("Name,Value,Type,LineNumber,IsFunctionLike,IsEnabled,SourceFile");
                        
                        foreach (var def in Definitions)
                        {
                            csv.AppendLine($"\"{def.Name}\",\"{def.Value}\",\"{def.DefinitionType}\",{def.LineNumber},{def.IsFunctionLike},{def.IsEnabled},\"{def.SourceFile}\"");
                        }
                        
                        await File.WriteAllTextAsync(dialog.FileName, csv.ToString());
                    }

                    StatusMessage = $"Exported {Definitions.Count} definitions to {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting definitions");
                MessageBox.Show($"Error exporting definitions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export variables to CSV file
        /// </summary>
        private async Task ExportVariablesAsync()
        {
            try
            {
                if (Variables.Count == 0)
                {
                    MessageBox.Show("No variables to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "CSV Files|*.csv|JSON Files|*.json|All Files|*.*",
                    Title = "Export Variables",
                    FileName = "variables.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var extension = Path.GetExtension(dialog.FileName).ToLower();
                    
                    if (extension == ".json")
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(Variables.ToList(), new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(dialog.FileName, json);
                    }
                    else
                    {
                        var csv = new StringBuilder();
                        csv.AppendLine("Name,TypeName,OriginalTypeName,Scope,DefaultValue,IsStatic,IsConst,IsArray,LineNumber,SourceFile");
                        
                        foreach (var var in Variables)
                        {
                            csv.AppendLine($"\"{var.Name}\",\"{var.TypeName}\",\"{var.OriginalTypeName}\",\"{var.Scope}\",\"{var.DefaultValue}\",{var.IsConst},{var.IsConst},{var.IsArray},{var.LineNumber},\"{var.SourceFile}\"");
                        }
                        
                        await File.WriteAllTextAsync(dialog.FileName, csv.ToString());
                    }

                    StatusMessage = $"Exported {Variables.Count} variables to {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting variables");
                MessageBox.Show($"Error exporting variables: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export functions to CSV file
        /// </summary>
        private async Task ExportFunctionsAsync()
        {
            try
            {
                if (Functions.Count == 0)
                {
                    MessageBox.Show("No functions to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "CSV Files|*.csv|JSON Files|*.json|All Files|*.*",
                    Title = "Export Functions",
                    FileName = "functions.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var extension = Path.GetExtension(dialog.FileName).ToLower();
                    
                    if (extension == ".json")
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(Functions.ToList(), new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(dialog.FileName, json);
                    }
                    else
                    {
                        var csv = new StringBuilder();
                        csv.AppendLine("Name,ReturnType,Signature,ParameterCount,IsStatic,IsInline,StartLineNumber,EndLineNumber,SourceFile");
                        
                        foreach (var func in Functions)
                        {
                            csv.AppendLine($"\"{func.Name}\",\"{func.ReturnType}\",\"{func.Signature}\",{func.Parameters?.Count ?? 0},{func.IsStatic},{func.IsInline},{func.StartLineNumber},{func.EndLineNumber},\"{func.SourceFile}\"");
                        }
                        
                        await File.WriteAllTextAsync(dialog.FileName, csv.ToString());
                    }

                    StatusMessage = $"Exported {Functions.Count} functions to {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting functions");
                MessageBox.Show($"Error exporting functions: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export conditional directives to CSV file
        /// </summary>
        private async Task ExportDirectivesAsync()
        {
            try
            {
                if (ConditionalDirectives.Count == 0)
                {
                    MessageBox.Show("No directives to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "CSV Files|*.csv|JSON Files|*.json|All Files|*.*",
                    Title = "Export Directives",
                    FileName = "directives.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    var extension = Path.GetExtension(dialog.FileName).ToLower();
                    
                    if (extension == ".json")
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(ConditionalDirectives.ToList(), new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(dialog.FileName, json);
                    }
                    else
                    {
                        var csv = new StringBuilder();
                        csv.AppendLine("Type,Condition,LineNumber,EndLineNumber,IsConditionSatisfied,SourceFile");
                        
                        foreach (var directive in ConditionalDirectives)
                        {
                            csv.AppendLine($"\"{directive.Type}\",\"{directive.Condition}\",{directive.LineNumber},{directive.EndLineNumber},{directive.IsConditionSatisfied},\"{directive.SourceFile}\"");
                        }
                        
                        await File.WriteAllTextAsync(dialog.FileName, csv.ToString());
                    }

                    StatusMessage = $"Exported {ConditionalDirectives.Count} directives to {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting directives");
                MessageBox.Show($"Error exporting directives: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Export dependency graph to file
        /// </summary>
        private async Task ExportDependencyGraphAsync()
        {
            try
            {
                if (ProjectAnalysisResult?.DependencyGraph?.SourceFiles == null || ProjectAnalysisResult.DependencyGraph.SourceFiles.Count == 0)
                {
                    MessageBox.Show("No dependency graph to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "JSON Files|*.json|CSV Files|*.csv|All Files|*.*",
                    Title = "Export Dependency Graph",
                    FileName = "dependency_graph.json"
                };

                if (dialog.ShowDialog() == true)
                {
                    var extension = Path.GetExtension(dialog.FileName).ToLower();
                    
                    if (extension == ".json")
                    {
                        var json = System.Text.Json.JsonSerializer.Serialize(ProjectAnalysisResult.DependencyGraph, new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(dialog.FileName, json);
                    }
                    else
                    {
                        var csv = new StringBuilder();
                        csv.AppendLine("FileName,FileType,FilePath,DependencyCount,DependentFileCount");
                        
                        foreach (var file in ProjectAnalysisResult.DependencyGraph.SourceFiles)
                        {
                            csv.AppendLine($"\"{file.FileName}\",\"{file.FileType}\",\"{file.FilePath}\",{file.DirectDependencies?.Count ?? 0},{file.DependentFiles?.Count ?? 0}");
                        }
                        
                        await File.WriteAllTextAsync(dialog.FileName, csv.ToString());
                    }

                    StatusMessage = $"Exported dependency graph to {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting dependency graph");
                MessageBox.Show($"Error exporting dependency graph: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Additional Command Implementations

        private void RefreshDefinitions()
        {
            try
            {
                OnPropertyChanged(nameof(Definitions));
                StatusMessage = "Definitions refreshed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing definitions");
                StatusMessage = $"Error refreshing definitions: {ex.Message}";
            }
        }

        private void SearchDefinitions()
        {
            try
            {
                OnPropertyChanged(nameof(Definitions));
                StatusMessage = $"Search applied: {DefinitionSearchText}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching definitions");
                StatusMessage = $"Error searching definitions: {ex.Message}";
            }
        }

        private void ClearDefinitionFilters()
        {
            try
            {
                DefinitionSearchText = string.Empty;
                ShowFunctionLikeMacros = true;
                ShowEnabledOnly = false;
                OnPropertyChanged(nameof(Definitions));
                StatusMessage = "Definition filters cleared";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing definition filters");
                StatusMessage = $"Error clearing filters: {ex.Message}";
            }
        }

        private async Task AnalyzeVariableConstraintsAsync()
        {
            try
            {
                StatusMessage = "Analyzing variable constraints...";
                // Implement variable constraint analysis
                await Task.Delay(1000); // Placeholder
                StatusMessage = "Variable constraint analysis completed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing variable constraints");
                StatusMessage = $"Error analyzing constraints: {ex.Message}";
            }
        }

        private void ClearVariableFilters()
        {
            try
            {
                VariableSearchText = string.Empty;
                ShowConstOnly = false;
                ShowArraysOnly = false;
                ShowPointersOnly = false;
                ShowWithDefaultValues = false;
                SelectedScopeFilter = null;
                SelectedTypeFilter = null;
                OnPropertyChanged(nameof(Variables));
                StatusMessage = "Variable filters cleared";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing variable filters");
                StatusMessage = $"Error clearing filters: {ex.Message}";
            }
        }

        private async Task AnalyzeFunctionRelationshipsAsync()
        {
            try
            {
                StatusMessage = "Analyzing function relationships...";
                // Implement function relationship analysis
                await Task.Delay(1000); // Placeholder
                StatusMessage = "Function relationship analysis completed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing function relationships");
                StatusMessage = $"Error analyzing relationships: {ex.Message}";
            }
        }

        private void ShowAdvancedSearch()
        {
            try
            {
                MessageBox.Show("Advanced search dialog will be implemented in a future version.", 
                    "Advanced Search", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error showing advanced search");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void ClearFunctionFilters()
        {
            try
            {
                FunctionSearchText = string.Empty;
                ShowStaticOnly = false;
                ShowInlineOnly = false;
                ShowWithParametersOnly = false;
                ShowFunctionBody = false;
                SelectedReturnTypeFilter = null;
                SelectedParameterCountFilter = null;
                OnPropertyChanged(nameof(Functions));
                StatusMessage = "Function filters cleared";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing function filters");
                StatusMessage = $"Error clearing filters: {ex.Message}";
            }
        }

        private void VisualizeDependencyGraph()
        {
            try
            {
                if (ProjectAnalysisResult?.DependencyGraph == null)
                {
                    MessageBox.Show("No dependency graph available to visualize.", 
                        "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                MessageBox.Show("Dependency graph visualization will be implemented in a future version.", 
                    "Visualize Graph", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error visualizing dependency graph");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task AnalyzeDependencyCyclesAsync()
        {
            try
            {
                StatusMessage = "Analyzing dependency cycles...";
                // Implement cycle detection
                await Task.Delay(1000); // Placeholder
                StatusMessage = "Dependency cycle analysis completed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing dependency cycles");
                StatusMessage = $"Error analyzing cycles: {ex.Message}";
            }
        }

        private void ViewDependencies(object? file)
        {
            try
            {
                if (file == null) return;
                MessageBox.Show("Dependency viewer will be implemented in a future version.", 
                    "View Dependencies", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing dependencies");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private void ViewDependents(object? file)
        {
            try
            {
                if (file == null) return;
                MessageBox.Show("Dependent files viewer will be implemented in a future version.", 
                    "View Dependents", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error viewing dependents");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task AnalyzeFileAsync(object? file)
        {
            try
            {
                if (file == null) return;
                StatusMessage = "Analyzing file...";
                // Implement file analysis
                await Task.Delay(1000); // Placeholder
                StatusMessage = "File analysis completed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing file");
                StatusMessage = $"Error analyzing file: {ex.Message}";
            }
        }

        private void OpenFile(object? file)
        {
            try
            {
                if (file == null) return;
                MessageBox.Show("File opener will be implemented in a future version.", 
                    "Open File", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening file");
                StatusMessage = $"Error: {ex.Message}";
            }
        }

        private async Task AnalyzeSingleFunctionAsync(CFunction? function)
        {
            try
            {
                if (function == null) return;
                StatusMessage = $"Analyzing function: {function.Name}...";
                // Implement single function analysis
                await Task.Delay(1000); // Placeholder
                StatusMessage = $"Function analysis completed for {function.Name}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing function: {function?.Name}");
                StatusMessage = $"Error analyzing function: {ex.Message}";
            }
        }

        private async Task GenerateTestCasesForFunctionAsync(CFunction? function)
        {
            try
            {
                if (function == null) return;
                StatusMessage = $"Generating test cases for function: {function.Name}...";
                // Implement test case generation for specific function
                await Task.Delay(1000); // Placeholder
                StatusMessage = $"Test case generation completed for {function.Name}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating test cases for function: {function?.Name}");
                StatusMessage = $"Error generating test cases: {ex.Message}";
            }
        }

        #endregion

        // Add this method for toggling the navigation drawer
        private void ToggleNavigationDrawer()
        {
            IsNavigationDrawerExpanded = !IsNavigationDrawerExpanded;
        }

        private void ViewProject(Project? project)
        {
            if (project == null) return;
            CurrentProject = project;
            StatusMessage = $"Viewing project: {project.Name}";
            Navigate("ProjectExplorer");
        }

        private void CloseProjectFromList(Project? project)
        {
            if (project == null) return;
            if (_openProjects.Contains(project))
            {
                _openProjects.Remove(project);
                if (CurrentProject == project)
                {
                    CurrentProject = _openProjects.FirstOrDefault();
                    if (CurrentProject == null)
                    {
                        SourceFiles.Clear();
                        TestCases.Clear();
                        ClearAnalysisResults();
                        ProjectAnalysisResult = null;
                        StatusMessage = "Project closed";
                        Navigate("Dashboard");
                    }
                }
            }
        }

        public async Task LoadProjectsFromDefaultLocationAsync()
        {
            _availableProjects.Clear();
            var projectFiles = GetProjectFilesFromDefaultLocation();
            foreach (var file in projectFiles)
            {
                try
                {
                    var project = await _projectService.LoadProjectAsync(file);
                    if (project != null)
                        _availableProjects.Add(project);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading project: {file}");
                }
            }
        }
    }
}