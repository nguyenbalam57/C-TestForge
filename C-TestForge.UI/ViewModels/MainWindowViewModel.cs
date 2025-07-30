using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using C_TestForge.Core;
using C_TestForge.Core.Interfaces.ProjectManagement;
using C_TestForge.Core.Interfaces.TestCaseManagement;
using C_TestForge.Core.Services;
using C_TestForge.Models;
using C_TestForge.Models.Projects;
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
        private readonly ILogger<MainWindowViewModel> _logger;

        private Project _currentProject;
        private string _statusMessage;
        private SourceFile _selectedSourceFile;
        private ObservableCollection<SourceFile> _sourceFiles;
        private ObservableCollection<TestCase> _testCases;
        private TestCase _selectedTestCase;

        public MainWindowViewModel(IProjectService projectService, ITestCaseService testCaseService, ILogger<MainWindowViewModel> logger)
        {
            _projectService = projectService;
            _testCaseService = testCaseService;
            _logger = logger;

            // Initialize commands
            NewProjectCommand = new RelayCommand(NewProject);
            OpenProjectCommand = new RelayCommand(OpenProject);
            SaveProjectCommand = new RelayCommand(SaveProject, CanSaveProject);
            CloseProjectCommand = new RelayCommand(CloseProject, CanCloseProject);
            ParseSourceFilesCommand = new RelayCommand(ParseSourceFiles, CanParseSourceFiles);
            ImportTestCasesCommand = new RelayCommand(ImportTestCases, CanImportTestCases);
            //ExportTestCasesCommand = new RelayCommand(ExportTestCases, CanExportTestCases);

            // Initialize collections
            SourceFiles = new ObservableCollection<SourceFile>();
            TestCases = new ObservableCollection<TestCase>();

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
            }
        }

        public string ProjectName => CurrentProject?.Name ?? "No Project Loaded";

        public bool HasProject => CurrentProject != null;

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public SourceFile SelectedSourceFile
        {
            get => _selectedSourceFile;
            set => SetProperty(ref _selectedSourceFile, value);
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

        // Commands
        public ICommand NewProjectCommand { get; }
        public ICommand OpenProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand CloseProjectCommand { get; }
        public ICommand ParseSourceFilesCommand { get; }
        public ICommand ImportTestCasesCommand { get; }
        public ICommand ExportTestCasesCommand { get; }

        // Command implementations
        private void NewProject()
        {
            try
            {
                var dialog = new Dialogs.NewProjectDialog();
                if (dialog.ShowDialog() == true)
                {
                    //var newProject = _projectService.CreateProjectAsync(
                    //    dialog.ProjectName,
                    //    dialog.ProjectDescription,
                    //    dialog.SourceDirectory);

                    //CurrentProject = newProject;
                    //StatusMessage = $"Created new project: {newProject.Name}";

                    // Refresh UI
                    RefreshSourceFiles();
                    RefreshTestCases();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new project");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error creating project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenProject()
        {
            try
            {
                //var dialog = new Dialogs.OpenProjectDialog(_projectService.GetAllProjects());
                //if (dialog.ShowDialog() == true)
                //{
                //    var project = _projectService.LoadProject(dialog.SelectedProjectId);
                //    if (project != null)
                //    {
                //        CurrentProject = project;
                //        StatusMessage = $"Opened project: {project.Name}";

                //        // Refresh UI
                //        RefreshSourceFiles();
                //        RefreshTestCases();
                //    }
                //}
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening project");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error opening project: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveProject()
        {
            try
            {
                //_projectService.SaveProject(CurrentProject);
                //StatusMessage = $"Saved project: {CurrentProject.Name}";
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
            CurrentProject = null;
            SourceFiles.Clear();
            TestCases.Clear();
            StatusMessage = "Project closed";
        }

        private bool CanCloseProject() => CurrentProject != null;

        private void ParseSourceFiles()
        {
            try
            {
                StatusMessage = "Parsing source files...";

                //var parsedFiles = _projectService.ParseProjectFiles(CurrentProject);
                //SourceFiles.Clear();

                //foreach (var file in parsedFiles.Values)
                //{
                //    SourceFiles.Add(file);
                //}

                //StatusMessage = $"Parsed {parsedFiles.Count} source files";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing source files");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error parsing source files: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool CanParseSourceFiles() => CurrentProject != null && CurrentProject.SourceFiles.Count > 0;

        private void ImportTestCases()
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
                    //_testCaseService.ImportTestCasesFromFile(dialog.FileName);
                    //RefreshTestCases();
                    //StatusMessage = $"Imported test cases from {Path.GetFileName(dialog.FileName)}";
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

        private void ExportTestCases()
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
                    //_testCaseService.ExportTestCasesToFile(CurrentProject.TestCases, dialog.FileName);
                    //StatusMessage = $"Exported test cases to {Path.GetFileName(dialog.FileName)}";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting test cases");
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show($"Error exporting test cases: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        //private bool CanExportTestCases() => CurrentProject != null && CurrentProject.TestCases.Count > 0;

        // Helper methods
        private void RefreshSourceFiles()
        {
            SourceFiles.Clear();
            if (CurrentProject != null)
            {
                foreach (var filePath in CurrentProject.SourceFiles)
                {
                    if (File.Exists(filePath))
                    {
                        //SourceFiles.Add(new CSourceFile
                        //{
                        //    FilePath = filePath,
                        //    Content = File.ReadAllText(filePath)
                        //});
                    }
                }
            }
        }

        private void RefreshTestCases()
        {
            TestCases.Clear();
            if (CurrentProject != null)
            {
                //foreach (var testCase in CurrentProject.TestCases)
                //{
                //    TestCases.Add(testCase);
                //}
            }
        }
    }
}
