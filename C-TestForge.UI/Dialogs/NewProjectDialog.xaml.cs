using CommunityToolkit.Mvvm.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace C_TestForge.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for NewProjectDialog.xaml
    /// </summary>
    public partial class NewProjectDialog : Window, INotifyPropertyChanged
    {
        #region Properties for binding

        private string _projectName;
        public string ProjectName
        {
            get => _projectName;
            set 
            { 
                _projectName = value; 
                OnPropertyChanged(nameof(ProjectName)); 
            }
        }

        private string _projectDescription;
        public string ProjectDescription
        {
            get => _projectDescription;
            set 
            { 
                _projectDescription = value; 
                OnPropertyChanged(nameof(ProjectDescription)); 
            }
        }

        private string _projectDirectory;
        public string ProjectDirectory
        {
            get => _projectDirectory;
            set 
            { 
                _projectDirectory = value; 
                OnPropertyChanged(nameof(ProjectDirectory)); 
            }
        }

        public ObservableCollection<string> Macros { get; set; } = new();
        public ObservableCollection<string> IncludePaths { get; set; } = new();

        // All available C files from include paths
        public ObservableCollection<string> AllCFiles { get; set; } = new();

        // Selected C files that will be included in the project
        public ObservableCollection<string> SelectedCFiles { get; set; } = new();

        private string _macroName;
        public string MacroName
        {
            get => _macroName;
            set 
            {
                if(_macroName != value)
                {
                    _macroName = value;
                    OnPropertyChanged(nameof(MacroName));
                    ((RelayCommand)AddMacroCommand).NotifyCanExecuteChanged();
                }
            }
        }

        private string _selectedMacro;
        public string SelectedMacro
        {
            get => _selectedMacro;
            set 
            { 
                _selectedMacro = value; 
                OnPropertyChanged(nameof(SelectedMacro)); 
            }
        }

        private string _sourcePath;
        public string SourcePath
        {
            get => _sourcePath;
            set 
            { 
                _sourcePath = value; 
                OnPropertyChanged(nameof(SourcePath)); 
                ((RelayCommand)AddSourcePathCommand).NotifyCanExecuteChanged();
            }
        }

        private string _selectedSourcePath;
        public string SelectedSourcePath
        {
            get => _selectedSourcePath;
            set 
            { 
                _selectedSourcePath = value; 
                OnPropertyChanged(nameof(SelectedSourcePath)); 
            }
        }

        private string _selectedAvailableFile;
        public string SelectedAvailableFile
        {
            get => _selectedAvailableFile;
            set 
            { 
                _selectedAvailableFile = value; 
                OnPropertyChanged(nameof(SelectedAvailableFile)); 
            }
        }

        private string _selectedFile;
        public string SelectedFile
        {
            get => _selectedFile;
            set 
            { 
                _selectedFile = value; 
                OnPropertyChanged(nameof(SelectedFile)); 
            }
        }

        #endregion

        #region Commands

        public ICommand AddMacroCommand { get; }
        public ICommand RemoveMacroCommand { get; }
        public ICommand AddSourcePathCommand { get; }
        public ICommand RemoveSourcePathCommand { get; }
        public ICommand AddFileCommand { get; }
        public ICommand AddAllFilesCommand { get; }
        public ICommand RemoveFileCommand { get; }
        public ICommand RemoveAllFilesCommand { get; }
        public ICommand RemoveSelectedFileCommand { get; }

        #endregion

        public NewProjectDialog()
        {
            InitializeComponent();

            // Initialize commands
            AddMacroCommand = new RelayCommand(AddMacro, CanAddMacro);
            RemoveMacroCommand = new RelayCommand(RemoveMacro, CanRemoveMacro);
            AddSourcePathCommand = new RelayCommand(AddSourcePath, CanAddSourcePath);
            RemoveSourcePathCommand = new RelayCommand(RemoveSourcePath, CanRemoveSourcePath);
            AddFileCommand = new RelayCommand(AddFile, CanAddFile);
            AddAllFilesCommand = new RelayCommand(AddAllFiles, CanAddAllFiles);
            RemoveFileCommand = new RelayCommand(RemoveFile, CanRemoveFile);
            RemoveAllFilesCommand = new RelayCommand(RemoveAllFiles, CanRemoveAllFiles);
            RemoveSelectedFileCommand = new RelayCommand<string>(RemoveSelectedFile);

            // Setup property change notifications for command updates
            PropertyChanged += OnPropertyChanged;
            Macros.CollectionChanged += OnCollectionChanged;
            IncludePaths.CollectionChanged += OnCollectionChanged;
            AllCFiles.CollectionChanged += OnCollectionChanged;
            SelectedCFiles.CollectionChanged += OnCollectionChanged;

            DataContext = this;
            Title = "Tạo mới Project";
        }

        // Constructor cho tạo mới
        public NewProjectDialog(ObservableCollection<string>? macros = null, ObservableCollection<string>? sourcePaths = null)
            : this()
        {
            Title = "Tạo mới Project";

            if (macros != null)
                foreach (var m in macros) Macros.Add(m);
            if (sourcePaths != null)
            {
                foreach (var s in sourcePaths) IncludePaths.Add(s);
                UpdateAllCFiles();
            }
        }

        // Constructor cho chỉnh sửa
        public NewProjectDialog(
            string name, 
            string description, 
            string directory,
            ObservableCollection<string>? macros = null, 
            ObservableCollection<string>? sourcePaths = null,
            ObservableCollection<string>? selectedCFiles = null)
            : this()
        {
            Title = "Chỉnh sửa Project";
            ProjectName = name;
            ProjectDescription = description;
            ProjectDirectory = directory;

            if (macros != null)
                foreach (var m in macros) Macros.Add(m);

            if (sourcePaths != null)
            {
                foreach (var s in sourcePaths) IncludePaths.Add(s);
                UpdateAllCFiles();
            }

            if (selectedCFiles != null)
                foreach (var f in selectedCFiles) SelectedCFiles.Add(f);
        }

        #region Event Handlers

        private void BrowseProjectDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Chọn thư mục dự án"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ProjectDirectory = dialog.FileName;
            }
        }

        private void BrowseSourcePath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Chọn thư mục source code"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtSourcePath.Text = dialog.FileName;
            }
        }

        private void AvailableFile_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedAvailableFile != null && CanAddFile())
            {
                AddFile();
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                MessageBox.Show("Tên dự án không được để trống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(ProjectDirectory))
            {
                MessageBox.Show("Đường dẫn dự án không được để trống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region Command Methods

        // Macro Commands
        private bool CanAddMacro() => !string.IsNullOrWhiteSpace(MacroName);

        private void AddMacro()
        {
            if (!string.IsNullOrWhiteSpace(MacroName))
            {
                var macroText = MacroName.Trim();
                if (!Macros.Contains(macroText))
                {
                    Macros.Add(macroText);
                    MacroName = string.Empty;
                }
            }
        }

        private bool CanRemoveMacro() => SelectedMacro != null;

        private void RemoveMacro()
        {
            if (SelectedMacro != null)
            {
                Macros.Remove(SelectedMacro);
                SelectedMacro = null;
            }
        }

        // Source Path Commands
        private bool CanAddSourcePath() => !string.IsNullOrWhiteSpace(SourcePath);

        private void AddSourcePath()
        {
            if (!string.IsNullOrWhiteSpace(SourcePath))
            {
                var pathText = SourcePath.Trim();
                if (Directory.Exists(pathText) && !IncludePaths.Contains(pathText))
                {
                    IncludePaths.Add(pathText);
                    SourcePath = string.Empty;
                    UpdateAllCFiles();
                }
                else if (!Directory.Exists(pathText))
                {
                    MessageBox.Show("Đường dẫn không tồn tại.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Đường dẫn đã được thêm.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private bool CanRemoveSourcePath() => SelectedSourcePath != null;

        private void RemoveSourcePath()
        {
            if (SelectedSourcePath != null)
            {
                IncludePaths.Remove(SelectedSourcePath);
                SelectedSourcePath = null;
                UpdateAllCFiles();
            }
        }

        // File Commands
        private bool CanAddFile() => SelectedAvailableFile != null && !SelectedCFiles.Contains(SelectedAvailableFile);

        private void AddFile()
        {
            if (SelectedAvailableFile != null && !SelectedCFiles.Contains(SelectedAvailableFile))
            {
                SelectedCFiles.Add(SelectedAvailableFile);
                // Optionally clear selection or move to next item
                var currentIndex = AllCFiles.IndexOf(SelectedAvailableFile);
                if (currentIndex < AllCFiles.Count - 1)
                {
                    SelectedAvailableFile = AllCFiles[currentIndex + 1];
                }
            }
        }

        private bool CanAddAllFiles() => AllCFiles.Any(f => !SelectedCFiles.Contains(f));

        private void AddAllFiles()
        {
            foreach (var file in AllCFiles.ToList())
            {
                if (!SelectedCFiles.Contains(file))
                {
                    SelectedCFiles.Add(file);
                }
            }
        }

        private bool CanRemoveFile() => SelectedFile != null;

        private void RemoveFile()
        {
            if (SelectedFile != null)
            {
                SelectedCFiles.Remove(SelectedFile);
                SelectedFile = null;
            }
        }

        private bool CanRemoveAllFiles() => SelectedCFiles.Any();

        private void RemoveAllFiles()
        {
            SelectedCFiles.Clear();
        }

        private void RemoveSelectedFile(string filePath)
        {
            if (!string.IsNullOrEmpty(filePath) && SelectedCFiles.Contains(filePath))
            {
                SelectedCFiles.Remove(filePath);
            }
        }

        #endregion

        #region Helper Methods

        private void UpdateAllCFiles()
        {
            AllCFiles.Clear();
            foreach (var dir in IncludePaths)
            {
                if (Directory.Exists(dir))
                {
                    try
                    {
                        var files = Directory.GetFiles(dir, "*.c", SearchOption.AllDirectories);
                        foreach (var file in files)
                        {
                            if (!AllCFiles.Contains(file))
                            {
                                AllCFiles.Add(file);
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Handle access denied errors silently or show a message
                        System.Diagnostics.Debug.WriteLine($"Access denied to directory: {dir}");
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Handle directory not found errors
                        System.Diagnostics.Debug.WriteLine($"Directory not found: {dir}");
                    }
                }
            }

            // Remove selected files that are no longer available
            var filesToRemove = SelectedCFiles.Where(f => !AllCFiles.Contains(f)).ToList();
            foreach (var file in filesToRemove)
            {
                SelectedCFiles.Remove(file);
            }
        }

        private void UpdateCommandStates()
        {
            ((RelayCommand)AddMacroCommand).NotifyCanExecuteChanged();
            ((RelayCommand)RemoveMacroCommand).NotifyCanExecuteChanged();
            ((RelayCommand)AddSourcePathCommand).NotifyCanExecuteChanged();
            ((RelayCommand)RemoveSourcePathCommand).NotifyCanExecuteChanged();
            ((RelayCommand)AddFileCommand).NotifyCanExecuteChanged();
            ((RelayCommand)AddAllFilesCommand).NotifyCanExecuteChanged();
            ((RelayCommand)RemoveFileCommand).NotifyCanExecuteChanged();
            ((RelayCommand)RemoveAllFilesCommand).NotifyCanExecuteChanged();
        }

        #endregion

        #region Event Handlers for Command Updates

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateCommandStates();
        }

        private void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCommandStates();
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Public Properties for External Access

        /// <summary>
        /// Gets the list of selected C files for the project
        /// </summary>
        public ObservableCollection<string> CFiles => SelectedCFiles;

        /// <summary>
        /// Gets or sets the window result data
        /// </summary>
        public ProjectDialogResult ProjectResult
        {
            get
            {
                return new ProjectDialogResult
                {
                    ProjectName = this.ProjectName,
                    ProjectDescription = this.ProjectDescription,
                    ProjectDirectory = this.ProjectDirectory,
                    Macros = new ObservableCollection<string>(this.Macros),
                    IncludePaths = new ObservableCollection<string>(this.IncludePaths),
                    SelectedFiles = new ObservableCollection<string>(this.SelectedCFiles)
                };
            }
        }

        #endregion
    }

    /// <summary>
    /// Result data structure for the project dialog
    /// </summary>
    public class ProjectDialogResult
    {
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectDescription { get; set; } = string.Empty;
        public string ProjectDirectory { get; set; } = string.Empty;
        public ObservableCollection<string> Macros { get; set; } = new();
        public ObservableCollection<string> IncludePaths { get; set; } = new();
        public ObservableCollection<string> SelectedFiles { get; set; } = new();
    }
}