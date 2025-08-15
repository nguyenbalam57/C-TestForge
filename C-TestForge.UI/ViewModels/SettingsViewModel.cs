using Prism.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Collections.ObjectModel;
using C_TestForge.Models.Projects;
using System.Collections.Generic;
using System.Linq;

namespace C_TestForge.UI.ViewModels
{
    /// <summary>
    /// ViewModel for SettingsView
    /// </summary>
    public class SettingsModel
    {
        public bool AutoSave { get; set; }
        public int FontSize { get; set; }
        public string Theme { get; set; }
        public string DefaultProjectLocation { get; set; }
        public string LastSelectedProjectId { get; set; } // Lưu Id dự án gần nhất
    }

    public class SettingsViewModel : ObservableObject
    {
        private bool _autoSave;
        public bool AutoSave
        {
            get => _autoSave;
            set => SetProperty(ref _autoSave, value);
        }

        private int _fontSize = 14;
        public int FontSize
        {
            get => _fontSize;
            set => SetProperty(ref _fontSize, value);
        }

        private string _theme = "Light";
        public string Theme
        {
            get => _theme;
            set => SetProperty(ref _theme, value);
        }

        private string _defaultProjectLocation;

        public string DefaultProjectLocation
        {
            get => _defaultProjectLocation;
            set => SetProperty(ref _defaultProjectLocation, value);
        }

        private ObservableCollection<Project> _projects = new();
        public ObservableCollection<Project> Projects
        {
            get => _projects;
            set => SetProperty(ref _projects, value);
        }

        private Project _selectedProject = null;
        public Project SelectedProject
        {
            get => _selectedProject;
            set
            {
                if (SetProperty(ref _selectedProject, value))
                {
                    OnSelectedProjectChanged();
                    SaveLastSelectedProjectId();
                }
            }
        }

        private string _lastSelectedProjectId;
        public string LastSelectedProjectId
        {
            get => _lastSelectedProjectId;
            set => SetProperty(ref _lastSelectedProjectId, value);
        }

        public IRelayCommand BrowseFolderCommand { get; }
        public IRelayCommand SaveSettingsCommand { get; }
        public IRelayCommand ResetToDefaultCommand { get; }

        public SettingsViewModel()
        {
            // Initialize default values
            DefaultProjectLocation = "C:\\Projects"; // Default value

            // Initialize commands
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            SaveSettingsCommand = new RelayCommand(SaveSettings);
            ResetToDefaultCommand = new RelayCommand(ResetToDefault);

            // Load settings on initialization
            LoadSettings();
            LoadProjects();
        }

        private void LoadProjects()
        {
            Projects.Clear();
            if (!string.IsNullOrEmpty(DefaultProjectLocation) && Directory.Exists(DefaultProjectLocation))
            {
                var files = Directory.GetFiles(DefaultProjectLocation, "*.ctproj", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var project = System.Text.Json.JsonSerializer.Deserialize<Project>(json);
                        if (project != null)
                        {
                            project.ProjectFilePath = file;
                            Projects.Add(project);
                        }
                    }
                    catch { }
                }
            }
            // Nếu có project thì chọn project gần nhất hoặc đầu tiên làm mặc định
            if (Projects.Count > 0)
            {
                if (!string.IsNullOrEmpty(LastSelectedProjectId))
                {
                    var last = Projects.FirstOrDefault(p => p.Id == LastSelectedProjectId);
                    if (last != null)
                        SelectedProject = last;
                    else
                        SelectedProject = Projects[0];
                }
                else if (SelectedProject == null)
                {
                    SelectedProject = Projects[0];
                }
            }
        }

        private void OnSelectedProjectChanged()
        {
            // Đảm bảo PropertyChanged được raise
            OnPropertyChanged(nameof(SelectedProject));
            // TODO: Gọi reload dữ liệu toàn bộ ứng dụng khi đổi dự án
            // Có thể raise event hoặc gọi callback tới MainWindowViewModel
        }

        private void SaveLastSelectedProjectId()
        {
            if (SelectedProject != null)
            {
                LastSelectedProjectId = SelectedProject.Id;
                // Lưu vào file settings
                SaveSettings();
            }
        }

        private string GetSettingsFilePath()
        {
            string appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "C-TestForge");

            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            return Path.Combine(appDataPath, "settings.json");
        }

        private void BrowseFolder()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select Default Project Location",
                SelectedPath = DefaultProjectLocation
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DefaultProjectLocation = dialog.SelectedPath;
                LoadProjects();
            }
        }

        private void SaveSettings()
        {
            try
            {
                string filePath = GetSettingsFilePath();
                // Save all settings to a JSON file
                var settings = new SettingsModel
                {
                    AutoSave = this.AutoSave,
                    FontSize = this.FontSize,
                    Theme = this.Theme,
                    DefaultProjectLocation = this.DefaultProjectLocation,
                    LastSelectedProjectId = this.LastSelectedProjectId
                };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filePath, json);

                // Không cần hiện thông báo khi chỉ lưu Id dự án
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ResetToDefault()
        {
            DefaultProjectLocation = "C:\\Projects"; // Reset to default value
            LoadProjects();
        }

        public void LoadSettings()
        {
            try
            {
                string filePath = GetSettingsFilePath();
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    var settings = JsonSerializer.Deserialize<SettingsModel>(json);
                    if (settings != null)
                    {
                        AutoSave = settings.AutoSave;
                        FontSize = settings.FontSize;
                        Theme = settings.Theme;
                        DefaultProjectLocation = settings.DefaultProjectLocation ?? "C:\\Projects";
                        LastSelectedProjectId = settings.LastSelectedProjectId;
                    }
                }
            }
            catch
            {
                DefaultProjectLocation = "C:\\Projects"; // Fallback to default
            }
        }
    }
}