using C_TestForge.Core.Interfaces.Analysis;
using C_TestForge.Core.Interfaces.UI;
using C_TestForge.Parser.UI;
using C_TestForge.Models.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.IO;

namespace C_TestForge.UI.ViewModels
{
    public class TypeMappingManagerViewModel : ObservableObject
    {
        private readonly ITypeManager _typeManager;
        private readonly ILogger<TypeMappingManagerViewModel> _logger;
        private readonly Core.Interfaces.UI.IDialogService _dialogService;

        private ObservableCollection<TypedefMappingViewModel> _typeMappings;
        private TypedefMappingViewModel _selectedMapping;
        private string _filterText;
        private bool _showPredefined = true;
        private bool _showDetected = true;
        private bool _showLearned = true;
        private bool _hasChanges;

        public ObservableCollection<TypedefMappingViewModel> TypeMappings
        {
            get => _typeMappings;
            set => SetProperty(ref _typeMappings, value);
        }

        public TypedefMappingViewModel SelectedMapping
        {
            get => _selectedMapping;
            set => SetProperty(ref _selectedMapping, value);
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool ShowPredefined
        {
            get => _showPredefined;
            set
            {
                if (SetProperty(ref _showPredefined, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool ShowDetected
        {
            get => _showDetected;
            set
            {
                if (SetProperty(ref _showDetected, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool ShowLearned
        {
            get => _showLearned;
            set
            {
                if (SetProperty(ref _showLearned, value))
                {
                    ApplyFilter();
                }
            }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set => SetProperty(ref _hasChanges, value);
        }

        // Commands
        public ICommand AddMappingCommand { get; }
        public ICommand RemoveMappingCommand { get; }
        public ICommand SaveMappingsCommand { get; }
        public ICommand ImportFromHeaderCommand { get; }
        public ICommand ExportToHeaderCommand { get; }
        public ICommand DeriveConstraintsCommand { get; }

        // Constructor
        public TypeMappingManagerViewModel(
            ITypeManager typeManager,
            ILogger<TypeMappingManagerViewModel> logger,
            Core.Interfaces.UI.IDialogService dialogService)
        {
            _typeManager = typeManager ?? throw new ArgumentNullException(nameof(typeManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Khởi tạo collection
            TypeMappings = new ObservableCollection<TypedefMappingViewModel>();
            LoadMappings();

            // Khởi tạo commands
            AddMappingCommand = new RelayCommand(AddMapping);
            RemoveMappingCommand = new RelayCommand(RemoveMapping, CanRemoveMapping);
            SaveMappingsCommand = new RelayCommand(SaveMappings, () => HasChanges);
            ImportFromHeaderCommand = new AsyncRelayCommand(ImportFromHeaderAsync);
            ExportToHeaderCommand = new AsyncRelayCommand(ExportToHeaderAsync, () => TypeMappings.Any());
            DeriveConstraintsCommand = new RelayCommand(DeriveConstraints, () => SelectedMapping != null);

            // Hook PropertyChanged events
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectedMapping))
                {
                    CommandManager.InvalidateRequerySuggested();
                }
            };
        }

        // Load all mappings from TypeManager
        private void LoadMappings()
        {
            var mappings = _typeManager.GetAllTypeMappings();

            TypeMappings.Clear();
            foreach (var mapping in mappings.Values)
            {
                TypeMappings.Add(new TypedefMappingViewModel(mapping));
            }

            // Hook CollectionChanged event
            TypeMappings.CollectionChanged += (s, e) =>
            {
                HasChanges = true;
                ApplyFilter();
            };

            // Hook PropertyChanged events for each item
            foreach (var item in TypeMappings)
            {
                item.PropertyChanged += (s, e) => HasChanges = true;
            }

            ApplyFilter();
            HasChanges = false;
        }

        // Apply filtering based on current filter settings
        private void ApplyFilter()
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(TypeMappings);
            if (view != null)
            {
                view.Filter = item =>
                {
                    var mapping = item as TypedefMappingViewModel;
                    if (mapping == null) return false;

                    // Filter by source
                    bool showBySource =
                        (ShowPredefined && mapping.Source.Equals("Predefined", StringComparison.OrdinalIgnoreCase)) ||
                        (ShowDetected && mapping.Source.Contains("Header")) ||
                        (ShowLearned && (mapping.Source.Contains("Source") ||
                                          mapping.Source.Contains("Usage") ||
                                          mapping.Source.Contains("User") ||
                                          mapping.Source.Contains("Inferred")));

                    // Filter by text
                    bool showByText = string.IsNullOrEmpty(FilterText) ||
                                     mapping.UserType.Contains(FilterText, StringComparison.OrdinalIgnoreCase) ||
                                     mapping.BaseType.Contains(FilterText, StringComparison.OrdinalIgnoreCase);

                    return showBySource && showByText;
                };
            }
        }

        // Add a new mapping
        private void AddMapping()
        {
            var newMapping = new TypedefMappingViewModel(new TypedefMapping
            {
                UserType = "NEW_TYPE",
                BaseType = "int",
                Source = "User",
                Size = 4,
                MinValue = "-2147483648",
                MaxValue = "2147483647"
            });

            TypeMappings.Add(newMapping);
            SelectedMapping = newMapping;
            HasChanges = true;
        }

        // Check if current selection can be removed
        private bool CanRemoveMapping()
        {
            return SelectedMapping != null &&
                   !SelectedMapping.Source.Equals("Predefined", StringComparison.OrdinalIgnoreCase);
        }

        // Remove the selected mapping
        private void RemoveMapping()
        {
            if (SelectedMapping != null && CanRemoveMapping())
            {
                string message = $"Are you sure you want to remove the mapping for '{SelectedMapping.UserType}'?";
                if (_dialogService.Show("Confirm Removal", message, "Remove", "Cancel"))
                {
                    TypeMappings.Remove(SelectedMapping);
                    HasChanges = true;
                }
            }
        }

        // Save all mappings
        private async void SaveMappings()
        {
            try
            {
                // Chuyển từ ViewModel sang Model và cập nhật vào TypeManager
                foreach (var vm in TypeMappings)
                {
                    _typeManager.AddTypedef(vm.UserType, vm.BaseType, vm.Source);
                }

                // Lưu cấu hình
                await _typeManager.SaveTypedefConfigAsync();
                _logger.LogInformation("Type mappings saved successfully");

                // Báo thành công và reset trạng thái thay đổi
                _dialogService.ShowInformation("Success", "Type mappings saved successfully.");
                HasChanges = false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving type mappings");
                _dialogService.ShowError("Error", $"Failed to save type mappings: {ex.Message}");
            }
        }

        // Import type definitions from header files
        private async Task ImportFromHeaderAsync()
        {
            try
            {
                // Show file picker to select header files
                var files = _dialogService.ShowOpenFileDialog(
                    "Select Header Files",
                    "Header Files (*.h)|*.h|All Files (*.*)|*.*",
                    true);

                if (files != null && files.Length > 0)
                {
                    // Analyze the selected header files
                    await _typeManager.AnalyzeHeaderFilesAsync(files);

                    // Reload mappings to reflect changes
                    LoadMappings();

                    _dialogService.ShowInformation(
                        "Import Complete",
                        $"Successfully analyzed {files.Length} header files.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing from header files");
                _dialogService.ShowError("Import Error", $"Failed to import from header files: {ex.Message}");
            }
        }

        // Export type definitions to a header file
        private async Task ExportToHeaderAsync()
        {
            try
            {
                // Show file picker to select destination file
                string file = _dialogService.ShowSaveFileDialog(
                    "Export Type Definitions",
                    "Header Files (*.h)|*.h|All Files (*.*)|*.*",
                    "typedefs.h");

                if (!string.IsNullOrEmpty(file))
                {
                    // Generate header file content
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("/* Auto-generated type definitions */");
                    sb.AppendLine("#ifndef TYPEDEFS_H");
                    sb.AppendLine("#define TYPEDEFS_H");
                    sb.AppendLine();

                    // Group by source
                    var grouped = TypeMappings
                        .GroupBy(m => m.Source)
                        .OrderBy(g => g.Key);

                    foreach (var group in grouped)
                    {
                        sb.AppendLine($"/* {group.Key} types */");
                        foreach (var mapping in group.OrderBy(m => m.UserType))
                        {
                            sb.AppendLine($"typedef {mapping.BaseType} {mapping.UserType};");
                        }
                        sb.AppendLine();
                    }

                    sb.AppendLine("#endif /* TYPEDEFS_H */");

                    // Write to file
                    await File.WriteAllTextAsync(file, sb.ToString());

                    _dialogService.ShowInformation(
                        "Export Complete",
                        $"Successfully exported {TypeMappings.Count} type definitions to {file}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting to header file");
                _dialogService.ShowError("Export Error", $"Failed to export to header file: {ex.Message}");
            }
        }

        // Derive constraints for the selected mapping based on its base type
        private void DeriveConstraints()
        {
            if (SelectedMapping != null)
            {
                try
                {
                    // Create temporary mapping for constraint derivation
                    var tempMapping = new TypedefMapping
                    {
                        UserType = SelectedMapping.UserType,
                        BaseType = SelectedMapping.BaseType,
                        Source = SelectedMapping.Source
                    };

                    // Use TypeManager's method to derive constraints
                    _typeManager.DeriveConstraintsFromBaseType(tempMapping);

                    // Update selected mapping with derived constraints
                    SelectedMapping.MinValue = tempMapping.MinValue;
                    SelectedMapping.MaxValue = tempMapping.MaxValue;
                    SelectedMapping.Size = tempMapping.Size;

                    HasChanges = true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deriving constraints");
                    _dialogService.ShowError("Error", $"Failed to derive constraints: {ex.Message}");
                }
            }
        }
    }
}
