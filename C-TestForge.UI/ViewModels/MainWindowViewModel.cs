using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.Parser;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Models.Core;
using C_TestForge.Models.Parse;
using C_TestForge.Models.Projects;
using C_TestForge.Models.TestCases;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Prism.Regions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace C_TestForge.UI.ViewModels
{
    /// <summary>
    /// ViewModel chính cho MainWindow, quản lý trạng thái và logic của toàn bộ ứng dụng
    /// </summary>
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IProjectService _projectService;
        private readonly ITestCaseService _testCaseService;
        private readonly IAnalysisService _analysisService;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly IClangSharpParserService _clangSharpParserService;
        private readonly ILogger<MainWindowViewModel> _logger;
        private readonly IRegionManager _regionManager;
        private readonly SettingsViewModel _settingsViewModel;

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

        public MainWindowViewModel(
            IProjectService projectService,
            ITestCaseService testCaseService,
            IAnalysisService analysisService,
            ISourceCodeService sourceCodeService,
            IClangSharpParserService clangSharpParserService,
            ILogger<MainWindowViewModel> logger,
            IRegionManager regionManager,
            SettingsViewModel settingsViewModel)
        {
            _projectService = projectService ?? throw new ArgumentNullException(nameof(projectService));
            _testCaseService = testCaseService ?? throw new ArgumentNullException(nameof(testCaseService));
            _analysisService = analysisService ?? throw new ArgumentNullException(nameof(analysisService));
            _sourceCodeService = sourceCodeService ?? throw new ArgumentNullException(nameof(sourceCodeService));
            _clangSharpParserService = clangSharpParserService ?? throw new ArgumentNullException(nameof(clangSharpParserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _settingsViewModel = settingsViewModel;
            _sourceFiles.CollectionChanged += SourceFiles_CollectionChanged;

            // Listen for project change from settings
            // _settingsViewModel.PropertyChanged += SettingsViewModel_PropertyChanged;
            // Always use project from settings
            OnPropertyChanged(nameof(CurrentProject));

            // Initialize commands
            NewProjectCommand = new AsyncRelayCommand(NewProjectAsync);
            EditProjectCommand = new AsyncRelayCommand(EditProjectAsync);
            OpenProjectCommand = new AsyncRelayCommand(OpenProjectAsync);
            SaveProjectCommand = new AsyncRelayCommand(SaveProjectAsync, CanSaveProject);
            CloseProjectCommand = new RelayCommand(CloseProject, CanCloseProject);
            ParseSourceFilesCommand = new AsyncRelayCommand(ParseSourceFilesAsync, CanParseSourceFiles);
            ImportTestCasesCommand = new AsyncRelayCommand(ImportTestCasesAsync, CanImportTestCases);
            ExportTestCasesCommand = new AsyncRelayCommand(ExportTestCasesAsync, CanExportTestCases);
            RemoveSourceFileCommand = new AsyncRelayCommand<SourceFile>(RemoveSourceFileAsync, CanRemoveSourceFile);
            GenerateTestCasesCommand = new AsyncRelayCommand(GenerateTestCasesAsync, CanGenerateTestCases);
            ProjectSettingsCommand = new RelayCommand(ShowProjectSettings, CanShowProjectSettings);
            AnalysisOptionsCommand = new RelayCommand(ShowAnalysisOptions);
            AboutCommand = new RelayCommand(ShowAboutDialog);
            ExitCommand = new RelayCommand(Exit);
            SignOutCommand = new RelayCommand(SignOut);
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
            ReloadDataCommand = new RelayCommand(ReloadData);
            
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
            StatusMessage = "Sẵn sàng";
            
            // Navigate to Dashboard by default
            Navigate("Dashboard");
        }

        private void InitializeFilterOptions()
        {
            // Initialize scope options
            AvailableScopes = new List<string> { "Tất cả", "Toàn cục", "Địa phương", "Tĩnh", "Ngoại", "Tham số" };
            
            // Initialize type options  
            AvailableTypes = new List<string> { "Tất cả", "int", "char", "float", "double", "void", "bool", "struct", "union", "enum" };
            
            // Initialize return type options
            AvailableReturnTypes = new List<string> { "Tất cả", "void", "int", "char", "float", "double", "bool", "struct*", "char*" };
            
            // Initialize parameter count options
            ParameterCountOptions = new List<string> { "Tất cả", "0", "1", "2", "3", "4", "5+" };
            
            // Initialize file type options
            AvailableFileTypes = new List<string> { "Tất cả", "CHeader", "CppHeader", "CSource", "CppSource" };
        }

        // Properties
        public Project? CurrentProject
        {
            get => _settingsViewModel.SelectedProject;
            set
            {
                if (_settingsViewModel.SelectedProject != value)
                {
                    _settingsViewModel.SelectedProject = value;
                    // Không raise OnPropertyChanged ở đây, vì SettingsViewModel sẽ tự raise PropertyChanged
                    // MainWindowViewModel đã lắng nghe sự kiện này và sẽ cập nhật UI khi cần thiết
                    // Chỉ cần cập nhật trạng thái lệnh nếu cần
                    ((AsyncRelayCommand)SaveProjectCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)ParseSourceFilesCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)ImportTestCasesCommand).NotifyCanExecuteChanged();
                    ((AsyncRelayCommand)ExportTestCasesCommand).NotifyCanExecuteChanged();
                    ((RelayCommand)CloseProjectCommand).NotifyCanExecuteChanged();
                }
            }
        }

        public string ProjectName => CurrentProject?.Name ?? "Chưa có dự án nào được tải";

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
                }
            }
        }

        public ObservableCollection<SourceFile> SourceFiles
        {
            get => _sourceFiles;
            set
            {
                if (SetProperty(ref _sourceFiles, value))
                {
                    // Đăng ký lại sự kiện khi collection được set mới
                    if (_sourceFiles != null)
                        _sourceFiles.CollectionChanged += SourceFiles_CollectionChanged;
                    OnPropertyChanged(nameof(HasSourceFiles));
                }
            }
        }

        public bool HasSourceFiles => SourceFiles != null && SourceFiles.Count > 0;

        private void SourceFiles_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(HasSourceFiles));
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
        public ICommand EditProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand CloseProjectCommand { get; }
        public ICommand ParseSourceFilesCommand { get; }
        public ICommand ImportTestCasesCommand { get; }
        public ICommand ExportTestCasesCommand { get; }
        public ICommand AnalyzeSourceFileCommand { get; }
        public ICommand RemoveSourceFileCommand { get; }
        public ICommand GenerateTestCasesCommand { get; }
        public ICommand ProjectSettingsCommand { get; }
        public ICommand AnalysisOptionsCommand { get; }
        public ICommand AboutCommand { get; }
        public ICommand ExitCommand { get; }
        public ICommand NavigateCommand { get; }
        public ICommand SignOutCommand { get; }
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
        public ICommand ReloadDataCommand { get; }

        // Helper methods
        /// <summary>
        /// Làm mới danh sách tệp nguồn của dự án hiện tại
        /// </summary>
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

        /// <summary>
        /// Làm mới danh sách test case của dự án hiện tại
        /// </summary>
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

        /// <summary>
        /// Cập nhật kết quả phân tích toàn dự án vào các collection Observable
        /// </summary>
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

        /// <summary>
        /// Xóa toàn bộ kết quả phân tích hiện tại
        /// </summary>
        private void ClearAnalysisResults()
        {
            Definitions.Clear();
            Variables.Clear();
            Functions.Clear();
            ConditionalDirectives.Clear();
            _analysisResult = null;
            OnPropertyChanged(nameof(HasAnalysisData));
        }

        private bool CanRemoveSourceFile(SourceFile? sourceFile) => CurrentProject != null && sourceFile != null;

        private bool CanGenerateTestCases() => CurrentProject != null && Functions.Count > 0;

        private bool CanShowProjectSettings() => CurrentProject != null;

        // Navigation Command implementation
        /// <summary>
        /// Chuyển hướng sang view tương ứng
        /// </summary>
        private void Navigate(string? viewName)
        {
            if (string.IsNullOrEmpty(viewName))
                return;
            try
            {
                SelectedMenuItem = viewName;
                _regionManager.RequestNavigate("MainRegion", viewName + "View");
                StatusMessage = $"Đang xem {viewName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi chuyển hướng tới view: {viewName}");
                StatusMessage = $"Lỗi: Không thể chuyển hướng tới {viewName}";
            }
        }

        /// <summary>
        /// Đăng xuất khỏi ứng dụng (chưa triển khai)
        /// </summary>
        private void SignOut()
        {
            MessageBox.Show(
                "Chức năng đăng xuất sẽ được bổ sung trong phiên bản sau.",
                "Đăng xuất",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // Command implementations
        /// <summary>
        /// Tạo mới một dự án
        /// </summary>
        private async Task NewProjectAsync()
        {
            try
            {
                var dialog = new Dialogs.NewProjectDialog();
                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = $"Đang tạo dự án mới: {dialog.ProjectName}...";

                    var newProject = await _projectService.CreateProjectAsync(
                        dialog.ProjectName,
                        dialog.ProjectDescription,
                        dialog.ProjectDirectory,
                        dialog.Macros.ToList(),
                        dialog.IncludePaths.ToList(),
                        dialog.SelectedCFiles.ToList());

                    SetCurrentProject(newProject);
                    StatusMessage = $"Đã tạo dự án mới: {newProject.Name}";
                    
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo dự án mới");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi tạo dự án: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        /// <summary>
        /// Chỉnh sửa dự án hiện tại
        /// </summary>
        /// <returns></returns>
        private async Task EditProjectAsync()
        {
            if (CurrentProject == null) return;
            try
            {
                var cof = CurrentProject.Configurations.FirstOrDefault(c => c.Name == CurrentProject.ActiveConfigurationName);
                var dialog = new Dialogs.NewProjectDialog(
                    CurrentProject.Name,
                    CurrentProject.Description,
                    Path.GetDirectoryName(CurrentProject.ProjectFilePath),
                    new ObservableCollection<string>(cof.MacroDefinitions),
                    new ObservableCollection<string>(cof.IncludePaths),
                    new ObservableCollection<string>(CurrentProject.SourceFiles));

                if (dialog.ShowDialog() == true)
                {
                    // So sánh các trường
                    var changes = new List<string>();

                    if (dialog.ProjectName != CurrentProject.Name)
                        changes.Add($"- Tên dự án: \"{CurrentProject.Name}\" → \"{dialog.ProjectName}\"");
                    if (dialog.ProjectDescription != CurrentProject.Description)
                        changes.Add($"- Mô tả dự án: \"{CurrentProject.Description}\" → \"{dialog.ProjectDescription}\"");
                    if (dialog.ProjectDirectory != Path.GetDirectoryName(CurrentProject.ProjectFilePath))
                        changes.Add($"- Thư mục dự án: \"{System.IO.Path.GetDirectoryName(CurrentProject.ProjectFilePath)}\" → \"{dialog.ProjectDirectory}\"");

                    // So sánh macro
                    var oldMacros = new ObservableCollection<string>(cof.MacroDefinitions);
                    var newMacros = dialog.Macros;
                    if (!oldMacros.SequenceEqual(newMacros))
                        changes.Add("- Macro định nghĩa");

                    // So sánh include paths
                    var oldPaths = new ObservableCollection<string>(cof.IncludePaths ?? new List<string>());
                    var newPaths = dialog.IncludePaths;
                    if (!oldPaths.SequenceEqual(newPaths))
                        changes.Add("- Đường dẫn include");

                    // So sánh cFiles
                    var oldCFiles = new ObservableCollection<string>(CurrentProject.SourceFiles ?? new List<string>());
                    var newCFiles = dialog.SelectedCFiles;
                    if (!oldCFiles.SequenceEqual(newCFiles))
                        changes.Add("- Danh sách file .c");

                    if (changes.Count == 0)
                    {
                        MessageBox.Show("Không có thay đổi nào được thực hiện.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    var message = "Các thay đổi sau sẽ được áp dụng:\n\n" + string.Join("\n", changes) + "\n\nBạn có muốn lưu các thay đổi này không?";
                    var result = MessageBox.Show(message, "Xác nhận chỉnh sửa dự án", MessageBoxButton.YesNo, MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        StatusMessage = "Đã hủy chỉnh sửa dự án.";
                        return;
                    }

                    StatusMessage = $"Đang chỉnh sửa dự án: {dialog.ProjectName}...";

                    var newProject = await _projectService.EditProjectAsync(
                        CurrentProject,
                        dialog.ProjectName,
                        dialog.ProjectDescription,
                        dialog.ProjectDirectory,
                        dialog.Macros.ToList(),
                        dialog.IncludePaths.ToList(),
                        dialog.SelectedCFiles.ToList());

                    SetCurrentProject(newProject);
                    StatusMessage = $"Đã chỉnh sửa dự án: {newProject.Name}";

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi chỉnh sửa dự án");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi chỉnh sửa dự án: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Mở một dự án từ file
        /// </summary>
        private async Task OpenProjectAsync()
        {
            try
            {
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "Tệp dự án C-TestForge|*.ctproj|Tất cả tệp|*.*",
                    Title = "Mở dự án"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    StatusMessage = $"Đang mở dự án: {Path.GetFileNameWithoutExtension(openFileDialog.FileName)}...";

                    var project = await _projectService.LoadProjectAsync(openFileDialog.FileName);
                    if (project != null)
                    {
                        SetCurrentProject(project);
                        StatusMessage = $"Đã mở dự án: {project.Name}";

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
                _logger.LogError(ex, "Lỗi khi mở dự án");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi mở dự án: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Lưu dự án hiện tại
        /// </summary>
        private async Task SaveProjectAsync()
        {
            try
            {
                if (CurrentProject == null) return;
                
                StatusMessage = $"Đang lưu dự án: {CurrentProject.Name}...";

                bool result = await _projectService.SaveProjectAsync(CurrentProject);
                if (result)
                {
                    StatusMessage = $"Đã lưu dự án: {CurrentProject.Name}";

                    // Reset unsaved changes flag
                    HasUnsavedChanges = false;
                }
                else
                {
                    StatusMessage = $"Lưu dự án thất bại: {CurrentProject.Name}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu dự án");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi lưu dự án: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Đóng dự án hiện tại
        /// </summary>
        private void CloseProject()
        {
            SetCurrentProject(null);
            StatusMessage = "Đã đóng dự án";
            Navigate("Dashboard");
        }

        /// <summary>
        /// Phân tích các tệp nguồn của dự án
        /// </summary>
        private async Task ParseSourceFilesAsync()
        {
            try
            {
                if (CurrentProject == null) return;
                
                StatusMessage = "Đang phân tích tệp nguồn...";
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

                await AnalyzeCompleteProjectAsync(CurrentProject, sourceFiles);

                IsAnalyzing = false;
                StatusMessage = $"Đã phân tích {sourceFiles.Count} tệp nguồn";
                
            }
            catch (Exception ex)
            {
                IsAnalyzing = false;
                _logger.LogError(ex, "Lỗi khi phân tích tệp nguồn");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi phân tích tệp nguồn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Nhập test case từ file
        /// </summary>
        private async Task ImportTestCasesAsync()
        {
            try
            {
                var dialog = new OpenFileDialog
                {
                    Filter = "Tệp test case|*.tst;*.csv;*.xlsx;*.json|Tất cả tệp|*.*",
                    Title = "Nhập test case"
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = $"Đang nhập test case từ {Path.GetFileName(dialog.FileName)}...";

                    // Import test cases
                    // Uncomment when test case service is implemented
                    // var testCases = await _testCaseService.ImportTestCasesFromFileAsync(dialog.FileName);

                    // Refresh test cases
                    await RefreshTestCasesAsync();

                    StatusMessage = $"Đã nhập test case từ {Path.GetFileName(dialog.FileName)}";
                    
                    // Navigate to Test Cases view
                    Navigate("TestCases");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi nhập test case");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi nhập test case: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Xuất test case ra file
        /// </summary>
        private async Task ExportTestCasesAsync()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Tệp TST|*.tst|Tệp CSV|*.csv|Tệp Excel|*.xlsx|Tệp JSON|*.json|Tất cả tệp|*.*",
                    Title = "Xuất test case",
                    DefaultExt = ".tst"
                };

                if (dialog.ShowDialog() == true)
                {
                    StatusMessage = $"Đang xuất test case ra {Path.GetFileName(dialog.FileName)}...";

                    // Export test cases
                    // Uncomment when test case service is implemented
                    // await _testCaseService.ExportTestCasesToFileAsync(TestCases.ToList(), dialog.FileName);

                    StatusMessage = $"Đã xuất test case ra {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất test case");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi xuất test case: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Phân tích toàn bộ dự án sử dụng ClangSharpParserService
        /// </summary>
        /// <param name="projectPath">Đường dẫn đến thư mục dự án</param>
        /// <returns>Task</returns>
        private async Task AnalyzeCompleteProjectAsync(Project project, List<SourceFile> sourceFiles)
        {
            if (sourceFiles == null || sourceFiles.Count < 1)
            {
                MessageBox.Show("Không có file nào được phân tích!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            StatusMessage = $"Bắt đầu phân tích toàn bộ dự án: {sourceFiles.Count} File...";
            IsAnalyzing = true;

            // Tạo CancellationTokenSource
            var cts = new System.Threading.CancellationTokenSource();

            // Tạo ViewModel cho dialog tiến độ
            var progressVm = new ProgressDialogViewModel(() => cts.Cancel())
            {
                Title = "Đang phân tích toàn bộ dự án",
                Message = $"Đang phân tích {sourceFiles.Count} tệp nguồn...\nQuá trình này có thể mất vài phút đối với các dự án lớn.",
                Progress = 0,
                IsIndeterminate = false,
                CanCancel = true,
                ProgressText = "0%"
            };

            // Tạo và hiển thị dialog tiến độ
            var progressDialog = new C_TestForge.UI.Dialogs.ProgressDialog
            {
                DataContext = progressVm
            };
            var window = new Window
            {
                Content = progressDialog,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                SizeToContent = SizeToContent.WidthAndHeight,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Owner = Application.Current.MainWindow,
                Title = "Tiến độ phân tích dự án"
            };
            window.Show();

            ProjectAnalysisResult? analysisResult = null;
            Exception? analysisException = null;

            try
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        analysisResult = await _clangSharpParserService.AnalyzeCompleteProjectAsync(
                            project,
                            sourceFiles,
                            progress: new Progress<double>(percent =>
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    progressVm.Progress = percent;
                                    progressVm.ProgressText = $"{percent:0}%";
                                });
                            }),
                            cancellationToken: cts.Token
                        );
                    }
                    catch (Exception ex)
                    {
                        analysisException = ex;
                    }
                });

                window.Close();

                if (cts.IsCancellationRequested)
                {
                    StatusMessage = "Đã hủy phân tích dự án";
                    return;
                }

                if (analysisException != null)
                    throw analysisException;

                if (analysisResult != null)
                {
                    // Ghi xuống file build
                    //bool saveResult = await _projectService.SaveBuildFilesAsync(project, sourceFiles);

                    UpdateProjectAnalysisResults(analysisResult);
                    var summaryReport = analysisResult.GenerateSummaryReport();
                    StatusMessage = $"Hoàn thành phân tích dự án - {analysisResult.Functions?.Count} hàm, {analysisResult.Variables?.Count} biến, {analysisResult.Macros?.Count} macro đã tìm thấy";
                    ShowAnalysisReport(summaryReport);
                    _logger.LogInformation($"Complete project analysis completed successfully for {project.Name}");
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Đã hủy phân tích dự án";
            }
            catch (Exception ex)
            {
                window.Close();
                _logger.LogError(ex, $"Lỗi trong quá trình phân tích toàn bộ dự án: {ex.Message}");
                StatusMessage = $"Lỗi trong quá trình phân tích dự án: {ex.Message}";
                MessageBox.Show($"Lỗi trong quá trình phân tích toàn bộ dự án:\n\n{ex.Message}", "Lỗi phân tích", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsAnalyzing = false;
            }
        }

        /// <summary>
        /// Xóa một tệp nguồn khỏi dự án
        /// </summary>
        private async Task RemoveSourceFileAsync(SourceFile? sourceFile)
        {
            if (sourceFile == null || CurrentProject == null)
            {
                return;
            }

            try
            {
                var result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa tệp nguồn '{sourceFile.FileName}' khỏi dự án không?",
                    "Xóa tệp nguồn",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    StatusMessage = $"Đang xóa tệp nguồn: {sourceFile.FileName}...";

                    bool success = await _projectService.RemoveSourceFileAsync(CurrentProject, sourceFile.FilePath);

                    if (success)
                    {
                        // Mark project as having unsaved changes
                        HasUnsavedChanges = true;

                        // Refresh source files
                        await RefreshSourceFilesAsync();

                        StatusMessage = $"Đã xóa tệp nguồn: {sourceFile.FileName}";
                    }
                    else
                    {
                        StatusMessage = $"Không thể xóa tệp nguồn: {sourceFile.FileName}";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi xóa tệp nguồn: {sourceFile.FilePath}");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi xóa tệp nguồn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Sinh test case tự động (chưa triển khai)
        /// </summary>
        private async Task GenerateTestCasesAsync()
        {
            try
            {
                MessageBox.Show(
                    "Chức năng sinh test case sẽ được bổ sung trong phiên bản sau.",
                    "Sinh test case",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                await Task.CompletedTask; // Temporary to avoid CS1998 warning
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi sinh test case");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi sinh test case: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Hiển thị hộp thoại cài đặt dự án (chưa triển khai)
        /// </summary>
        private void ShowProjectSettings()
        {
            try
            {
                MessageBox.Show(
                    "Hộp thoại cài đặt dự án sẽ được bổ sung trong phiên bản sau.",
                    "Cài đặt dự án",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                
                // Navigate to Settings
                Navigate("Settings");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hiển thị cài đặt dự án");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi hiển thị cài đặt dự án: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Hiển thị hộp thoại tuỳ chọn phân tích (chưa triển khai)
        /// </summary>
        private void ShowAnalysisOptions()
        {
            try
            {
                MessageBox.Show(
                    "Hộp thoại tuỳ chọn phân tích sẽ được bổ sung trong phiên bản sau.",
                    "Tuỳ chọn phân tích",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hiển thị tuỳ chọn phân tích");
                StatusMessage = $"Lỗi: {ex.Message}";
                MessageBox.Show($"Lỗi khi hiển thị tuỳ chọn phân tích: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Hiển thị hộp thoại giới thiệu phần mềm
        /// </summary>
        private void ShowAboutDialog()
        {
            MessageBox.Show(
                "C-TestForge v1.0\n\nCông cụ phân tích, quản lý và sinh test case tự động cho mã nguồn C.\n\nPhát triển dựa trên ClangSharp và Z3 Theorem Prover.",
                "Giới thiệu C-TestForge",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        /// <summary>
        /// Thoát ứng dụng
        /// </summary>
        private void Exit()
        {
            Application.Current.MainWindow?.Close();
        }

        /// <summary>
        /// Hiển thị báo cáo phân tích chi tiết trong hộp thoại
        /// </summary>
        private void ShowAnalysisReport(string report)
        {
            try
            {
                // For now, show in a scrollable message box
                // In a real application, you might want to create a dedicated dialog
                MessageBox.Show(
                    report,
                    "Báo cáo phân tích chi tiết",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi hiển thị báo cáo phân tích: {ex.Message}");
            }
        }

        #region Export and Report Methods

        /// <summary>
        /// Hiển thị báo cáo phân tích dự án chi tiết
        /// </summary>
        private void ShowProjectAnalysisReport()
        {
            try
            {
                if (ProjectAnalysisResult == null)
                {
                    MessageBox.Show("Không có dữ liệu phân tích dự án.", "Không có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var report = ProjectAnalysisResult.GenerateSummaryReport();
                ShowAnalysisReport(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hiển thị báo cáo phân tích dự án");
                MessageBox.Show($"Lỗi khi hiển thị báo cáo: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Xuất nội dung tệp nguồn ra file
        /// </summary>
        private async Task ExportSourceFileAsync(SourceFile? sourceFile)
        {
            if (sourceFile == null)
                return;

            try
            {
                var dialog = new SaveFileDialog
                {
                    Filter = "Tệp văn bản|*.txt|Tất cả tệp|*.*",
                    Title = "Xuất tệp nguồn",
                    FileName = $"{sourceFile.FileName}_exported.txt"
                };

                if (dialog.ShowDialog() == true)
                {
                    await File.WriteAllTextAsync(dialog.FileName, sourceFile.Content);
                    StatusMessage = $"Đã xuất tệp nguồn đến {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất tệp nguồn");
                MessageBox.Show($"Lỗi khi xuất tệp nguồn: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Xuất các định nghĩa (macro) ra file CSV hoặc JSON
        /// </summary>
        private async Task ExportDefinitionsAsync()
        {
            try
            {
                if (Definitions.Count == 0)
                {
                    MessageBox.Show("Không có định nghĩa nào để xuất.", "Không có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Tệp CSV|*.csv|Tệp JSON|*.json|Tất cả tệp|*.*",
                    Title = "Xuất Định Nghĩa",
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

                    StatusMessage = $"Đã xuất {Definitions.Count} định nghĩa đến {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất định nghĩa");
                MessageBox.Show($"Lỗi khi xuất định nghĩa: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Xuất biến ra file CSV hoặc JSON
        /// </summary>
        private async Task ExportVariablesAsync()
        {
            try
            {
                if (Variables.Count == 0)
                {
                    MessageBox.Show("Không có biến nào để xuất.", "Không có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Tệp CSV|*.csv|Tệp JSON|*.json|Tất cả tệp|*.*",
                    Title = "Xuất Biến",
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

                    StatusMessage = $"Đã xuất {Variables.Count} biến đến {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất biến");
                MessageBox.Show($"Lỗi khi xuất biến: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Xuất hàm ra file CSV hoặc JSON
        /// </summary>
        private async Task ExportFunctionsAsync()
        {
            try
            {
                if (Functions.Count == 0)
                {
                    MessageBox.Show("Không có hàm nào để xuất.", "Không có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Tệp CSV|*.csv|Tệp JSON|*.json|Tất cả tệp|*.*",
                    Title = "Xuất Hàm",
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

                    StatusMessage = $"Đã xuất {Functions.Count} hàm đến {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất hàm");
                MessageBox.Show($"Lỗi khi xuất hàm: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Xuất các chỉ thị điều kiện ra file CSV hoặc JSON
        /// </summary>
        private async Task ExportDirectivesAsync()
        {
            try
            {
                if (ConditionalDirectives.Count == 0)
                {
                    MessageBox.Show("Không có chỉ thị nào để xuất.", "Không có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Tệp CSV|*.csv|Tệp JSON|*.json|Tất cả tệp|*.*",
                    Title = "Xuất Chỉ Thị",
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

                    StatusMessage = $"Đã xuất {ConditionalDirectives.Count} chỉ thị đến {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất chỉ thị");
                MessageBox.Show($"Lỗi khi xuất chỉ thị: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Xuất đồ thị phụ thuộc ra file
        /// </summary>
        private async Task ExportDependencyGraphAsync()
        {
            try
            {
                if (ProjectAnalysisResult?.DependencyGraph?.SourceFiles == null || ProjectAnalysisResult.DependencyGraph.SourceFiles.Count == 0)
                {
                    MessageBox.Show("Không có đồ thị phụ thuộc nào để xuất.", "Không có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new SaveFileDialog
                {
                    Filter = "Tệp JSON|*.json|Tệp CSV|*.csv|Tất cả tệp|*.*",
                    Title = "Xuất Đồ Thị Phụ Thuộc",
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

                    StatusMessage = $"Đã xuất đồ thị phụ thuộc đến {dialog.FileName}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xuất đồ thị phụ thuộc");
                MessageBox.Show($"Lỗi khi xuất đồ thị phụ thuộc: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Các lệnh bổ sung cho phân tích và lọc

        /// <summary>
        /// Làm mới danh sách macro/định nghĩa
        /// </summary>
        private void RefreshDefinitions()
        {
            try
            {
                OnPropertyChanged(nameof(Definitions));
                StatusMessage = "Đã làm mới danh sách định nghĩa";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi làm mới định nghĩa");
                StatusMessage = $"Lỗi làm mới định nghĩa: {ex.Message}";
            }
        }

        /// <summary>
        /// Tìm kiếm macro/định nghĩa
        /// </summary>
        private void SearchDefinitions()
        {
            try
            {
                OnPropertyChanged(nameof(Definitions));
                StatusMessage = $"Đã áp dụng tìm kiếm: {DefinitionSearchText}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tìm kiếm định nghĩa");
                StatusMessage = $"Lỗi tìm kiếm định nghĩa: {ex.Message}";
            }
        }

        /// <summary>
        /// Xóa bộ lọc macro/định nghĩa
        /// </summary>
        private void ClearDefinitionFilters()
        {
            try
            {
                DefinitionSearchText = string.Empty;
                ShowFunctionLikeMacros = true;
                ShowEnabledOnly = false;
                OnPropertyChanged(nameof(Definitions));
                StatusMessage = "Đã xóa bộ lọc định nghĩa";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa bộ lọc định nghĩa");
                StatusMessage = $"Lỗi xóa bộ lọc: {ex.Message}";
            }
        }

        /// <summary>
        /// Phân tích ràng buộc biến (chưa triển khai)
        /// </summary>
        private async Task AnalyzeVariableConstraintsAsync()
        {
            try
            {
                StatusMessage = "Đang phân tích ràng buộc biến...";
                // Implement variable constraint analysis
                await Task.Delay(1000); // Placeholder
                StatusMessage = "Phân tích ràng buộc biến hoàn tất";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân tích ràng buộc biến");
                StatusMessage = $"Lỗi phân tích ràng buộc: {ex.Message}";
            }
        }

        /// <summary>
        /// Xóa bộ lọc biến
        /// </summary>
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
                StatusMessage = "Đã xóa bộ lọc biến";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa bộ lọc biến");
                StatusMessage = $"Lỗi xóa bộ lọc: {ex.Message}";
            }
        }

        /// <summary>
        /// Phân tích quan hệ giữa các hàm (chưa triển khai)
        /// </summary>
        private async Task AnalyzeFunctionRelationshipsAsync()
        {
            try
            {
                StatusMessage = "Đang phân tích quan hệ giữa các hàm...";
                // Implement function relationship analysis
                await Task.Delay(1000); // Placeholder
                StatusMessage = "Phân tích quan hệ hàm hoàn tất";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân tích quan hệ hàm");
                StatusMessage = $"Lỗi phân tích quan hệ: {ex.Message}";
            }
        }

        /// <summary>
        /// Hiển thị tìm kiếm nâng cao (chưa triển khai)
        /// </summary>
        private void ShowAdvancedSearch()
        {
            try
            {
                MessageBox.Show("Hộp thoại tìm kiếm nâng cao sẽ được bổ sung trong phiên bản sau.", 
                    "Tìm kiếm nâng cao", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hiển thị tìm kiếm nâng cao");
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        /// <summary>
        /// Xóa bộ lọc hàm
        /// </summary>
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
                StatusMessage = "Đã xóa bộ lọc hàm";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa bộ lọc hàm");
                StatusMessage = $"Lỗi xóa bộ lọc: {ex.Message}";
            }
        }

        /// <summary>
        /// Hiển thị đồ thị phụ thuộc (chưa triển khai)
        /// </summary>
        private void VisualizeDependencyGraph()
        {
            try
            {
                if (ProjectAnalysisResult?.DependencyGraph == null)
                {
                    MessageBox.Show("Không có đồ thị phụ thuộc nào để hiển thị.", 
                        "Không có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                MessageBox.Show("Chức năng hiển thị đồ thị phụ thuộc sẽ được bổ sung trong phiên bản sau.", 
                    "Hiển thị đồ thị phụ thuộc", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi hiển thị đồ thị phụ thuộc");
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        /// <summary>
        /// Phân tích chu trình phụ thuộc (chưa triển khai)
        /// </summary>
        private async Task AnalyzeDependencyCyclesAsync()
        {
            try
            {
                StatusMessage = "Đang phân tích chu trình phụ thuộc...";
                // Implement cycle detection
                await Task.Delay(1000); // Placeholder
                StatusMessage = "Phân tích chu trình phụ thuộc hoàn tất";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân tích chu trình phụ thuộc");
                StatusMessage = $"Lỗi phân tích chu trình: {ex.Message}";
            }
        }

        /// <summary>
        /// Xem các tệp phụ thuộc của một tệp (chưa triển khai)
        /// </summary>
        private void ViewDependencies(object? file)
        {
            try
            {
                if (file == null) return;
                MessageBox.Show("Chức năng xem các tệp phụ thuộc sẽ được bổ sung trong phiên bản sau.", 
                    "Xem các tệp phụ thuộc", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xem các tệp phụ thuộc");
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        /// <summary>
        /// Xem các tệp phụ thuộc ngược của một tệp (chưa triển khai)
        /// </summary>
        private void ViewDependents(object? file)
        {
            try
            {
                if (file == null) return;
                MessageBox.Show("Chức năng xem các tệp phụ thuộc ngược sẽ được bổ sung trong phiên bản sau.", 
                    "Xem các tệp phụ thuộc ngược", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xem các tệp phụ thuộc ngược");
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        /// <summary>
        /// Phân tích một tệp bất kỳ (chưa triển khai)
        /// </summary>
        private async Task AnalyzeFileAsync(object? file)
        {
            try
            {
                if (file == null) return;
                StatusMessage = "Đang phân tích tệp...";
                // Implement file analysis
                await Task.Delay(1000); // Placeholder
                StatusMessage = "Phân tích tệp hoàn tất";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi phân tích tệp");
                StatusMessage = $"Lỗi phân tích tệp: {ex.Message}";
            }
        }

        /// <summary>
        /// Mở tệp bất kỳ (chưa triển khai)
        /// </summary>
        private void OpenFile(object? file)
        {
            try
            {
                if (file == null) return;
                MessageBox.Show("Chức năng mở tệp sẽ được bổ sung trong phiên bản sau.", 
                    "Mở tệp", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi mở tệp");
                StatusMessage = $"Lỗi: {ex.Message}";
            }
        }

        /// <summary>
        /// Phân tích một hàm cụ thể (chưa triển khai)
        /// </summary>
        private async Task AnalyzeSingleFunctionAsync(CFunction? function)
        {
            try
            {
                if (function == null) return;
                StatusMessage = $"Đang phân tích hàm: {function.Name}...";
                // Implement single function analysis
                await Task.Delay(1000); // Placeholder
                StatusMessage = $"Phân tích hàm hoàn tất cho {function.Name}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi phân tích hàm: {function?.Name}");
                StatusMessage = $"Lỗi phân tích hàm: {ex.Message}";
            }
        }

        /// <summary>
        /// Sinh test case cho một hàm cụ thể (chưa triển khai)
        /// </summary>
        private async Task GenerateTestCasesForFunctionAsync(CFunction? function)
        {
            try
            {
                if (function == null) return;
                StatusMessage = $"Đang sinh test case cho hàm: {function.Name}...";
                // Implement test case generation for specific function
                await Task.Delay(1000); // Placeholder
                StatusMessage = $"Sinh test case hoàn tất cho {function.Name}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Lỗi khi sinh test case cho hàm: {function?.Name}");
                StatusMessage = $"Lỗi sinh test case: {ex.Message}";
            }
        }

        #endregion

        /// <summary>
        /// Đảo trạng thái mở rộng/thu gọn menu điều hướng
        /// </summary>
        private void ToggleNavigationDrawer()
        {
            IsNavigationDrawerExpanded = !IsNavigationDrawerExpanded;
        }

        /// <summary>
        /// Xem chi tiết một dự án
        /// </summary>
        private void ViewProject(Project? project)
        {
            if (project == null) return;
            CurrentProject = project;
            StatusMessage = $"Đang xem dự án: {project.Name}";
            Navigate("ProjectExplorer");
        }

        /// <summary>
        /// Đặt dự án hiện tại
        /// </summary>
        private async Task SetCurrentProject(Project? project)
        {
            // So sánh 2 dự án để tránh reload không cần thiết
            bool HasChanged = Project.HasChanged(project, CurrentProject);

            CurrentProject = project;
            if (project != null)
            {
                StatusMessage = $"Đã chọn dự án: {project.Name}";

                // Kiểm tra và yêu cầu build lại dự án khi có thay đổi.
                //if(HasChanged)
                {
                    StatusMessage = "Dự án đã thay đổi, đang tiến hành build lại để phân tích chính xác.";
                    await ParseSourceFilesAsync();
                }

                // Refresh UI
                _ = RefreshSourceFilesAsync();
                _ = RefreshTestCasesAsync();
                ClearAnalysisResults();
                ProjectAnalysisResult = null;
                Navigate("ProjectExplorer");
            }
            else
            {
                StatusMessage = "Đã đóng dự án";
                SourceFiles.Clear();
                TestCases.Clear();
                ClearAnalysisResults();
                ProjectAnalysisResult = null;
                Navigate("Dashboard");
            }
        }

        /// <summary>
        /// Tải lại dữ liệu dự án hiện tại
        /// </summary>
        public async void ReloadData()
        {
            OnPropertyChanged(nameof(CurrentProject));
            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(HasProject));
            _ = RefreshSourceFilesAsync();
            _ = RefreshTestCasesAsync();
            ClearAnalysisResults();
            ProjectAnalysisResult = null;
            StatusMessage = "Đã tải lại dữ liệu dự án.";

            if (SelectedMenuItem == "Dashboard")
            {
                OnPropertyChanged(nameof(CurrentProject));
                OnPropertyChanged(nameof(ProjectName));
                OnPropertyChanged(nameof(HasProject));
                OnPropertyChanged(nameof(SourceFiles));
                OnPropertyChanged(nameof(TestCases));
                OnPropertyChanged(nameof(ProjectAnalysisResult));
            }
        }

        // Các phương thức kiểm tra điều kiện cho lệnh (command)
        private bool CanSaveProject() => CurrentProject != null;
        private bool CanCloseProject() => CurrentProject != null;
        private bool CanParseSourceFiles() => CurrentProject != null && CurrentProject.SourceFiles.Count > 0;
        private bool CanImportTestCases() => CurrentProject != null;
        private bool CanExportTestCases() => CurrentProject != null;
    }
}