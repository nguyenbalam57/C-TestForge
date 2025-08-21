using CommunityToolkit.Mvvm.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace C_TestForge.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for NewProjectDialog.xaml
    /// </summary>
    public partial class NewProjectDialog : Window
    {
        // Thuộc tính cho binding
        private string _projectName;
        public string ProjectName
        {
            get => _projectName;
            set { _projectName = value; OnPropertyChanged(nameof(ProjectName)); }
        }

        private string _projectDescription;
        public string ProjectDescription
        {
            get => _projectDescription;
            set { _projectDescription = value; OnPropertyChanged(nameof(ProjectDescription)); }
        }

        private string _projectDirectory;
        public string ProjectDirectory
        {
            get => _projectDirectory;
            set { _projectDirectory = value; OnPropertyChanged(nameof(ProjectDirectory)); }
        }

        public ObservableCollection<string> Macros { get; set; } = new();
        public ObservableCollection<string> IncludePaths { get; set; } = new();
        public ObservableCollection<string> CFiles { get; set; } = new();

        private string _selectedMacro;
        public string SelectedMacro
        {
            get => _selectedMacro;
            set { _selectedMacro = value; OnPropertyChanged(nameof(SelectedMacro)); }
        }

        private string _selectedSourcePath;
        public string SelectedSourcePath
        {
            get => _selectedSourcePath;
            set { _selectedSourcePath = value; OnPropertyChanged(nameof(SelectedSourcePath)); }
        }

        public ICommand AddMacroCommand { get; }
        public ICommand RemoveMacroCommand { get; }
        public ICommand AddSourcePathCommand { get; }
        public ICommand RemoveSourcePathCommand { get; }


        public NewProjectDialog()
        {
            InitializeComponent();

            AddMacroCommand = new RelayCommand(AddMacro);
            RemoveMacroCommand = new RelayCommand(RemoveMacro);
            AddSourcePathCommand = new RelayCommand(AddSourcePath);
            RemoveSourcePathCommand = new RelayCommand(RemoveSourcePath);

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
                foreach (var s in sourcePaths) IncludePaths.Add(s);
        }

        // Constructor cho chỉnh sửa
        public NewProjectDialog(string name, string description, string directory,
            ObservableCollection<string>? macros = null, ObservableCollection<string>? sourcePaths = null)
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
                UpdateCFiles();
            }    
        }

        // Sự kiện chọn thư mục project
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
                txtProjectDirectory.Text = dialog.FileName;
            }
        }

        // Sự kiện chọn thư mục source
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

        // Thêm macro
        public void AddMacro()
        {
            if (!string.IsNullOrWhiteSpace(txtMacroName.Text))
            {
                Macros.Add(txtMacroName.Text.Trim());
                txtMacroName.Clear();
            }
        }

        // Xóa macro
        public void RemoveMacro()
        {
            if (SelectedMacro != null)
                Macros.Remove(SelectedMacro);
        }

        // Thêm source path
        public void AddSourcePath()
        {
            if (!string.IsNullOrWhiteSpace(txtSourcePath.Text))
            {
                IncludePaths.Add(txtSourcePath.Text.Trim());
                txtSourcePath.Clear();
                UpdateCFiles();
            }
        }

        // Xóa source path
        public void RemoveSourcePath()
        {
            if (SelectedSourcePath != null)
            {
                IncludePaths.Remove(SelectedSourcePath);
                UpdateCFiles();
            }    
        }

        // Gọi hàm này sau khi thêm đường dẫn source
        private void UpdateCFiles()
        {
            CFiles.Clear();
            foreach (var dir in IncludePaths)
            {
                if (Directory.Exists(dir))
                {
                    var files = Directory.GetFiles(dir, "*.c", SearchOption.TopDirectoryOnly);
                    foreach (var file in files)
                        CFiles.Add(file);
                }
            }
        }

        // Nút OK
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                MessageBox.Show("Tên dự án không được để trống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(ProjectDirectory))
            {
                MessageBox.Show("Đường dẫn dự án không được để trống.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        // Nút Cancel
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
