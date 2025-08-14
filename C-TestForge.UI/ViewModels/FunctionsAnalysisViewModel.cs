using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using C_TestForge.Models.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace C_TestForge.UI.ViewModels
{
    public partial class FunctionsAnalysisViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<CFunction> functions = new();

        [ObservableProperty]
        private ObservableCollection<CFunction> filteredFunctions = new();

        [ObservableProperty]
        private CFunction selectedFunction;

        [ObservableProperty]
        private string functionSearchText;
        [ObservableProperty]
        private string selectedReturnTypeFilter;
        [ObservableProperty]
        private int? selectedParameterCountFilter;
        [ObservableProperty]
        private bool showStaticOnly;
        [ObservableProperty]
        private bool showInlineOnly;
        [ObservableProperty]
        private bool showWithParametersOnly;
        [ObservableProperty]
        private bool showFunctionBody;

        public ObservableCollection<string> AvailableReturnTypes { get; } = new();
        public ObservableCollection<int> ParameterCountOptions { get; } = new();

        public IRelayCommand ExportFunctionsCommand { get; }
        public IRelayCommand AnalyzeFunctionRelationshipsCommand { get; }
        public IRelayCommand ShowAdvancedSearchCommand { get; }
        public IRelayCommand ClearFunctionFiltersCommand { get; }
        public IRelayCommand<CFunction?> AnalyzeSingleFunctionCommand { get; }
        public IRelayCommand<CFunction?> GenerateTestCasesForFunctionCommand { get; }

        public FunctionsAnalysisViewModel()
        {
            ExportFunctionsCommand = new RelayCommand(OnExportFunctions);
            AnalyzeFunctionRelationshipsCommand = new RelayCommand(OnAnalyzeFunctionRelationships);
            ShowAdvancedSearchCommand = new RelayCommand(OnShowAdvancedSearch);
            ClearFunctionFiltersCommand = new RelayCommand(OnClearFunctionFilters);
            AnalyzeSingleFunctionCommand = new RelayCommand<CFunction?>(OnAnalyzeSingleFunction);
            GenerateTestCasesForFunctionCommand = new RelayCommand<CFunction?>(OnGenerateTestCasesForFunction);

            if (DesignerProperties.GetIsInDesignMode(new System.Windows.DependencyObject()))
            {
                LoadDemoData();
            }
        }

        partial void OnFunctionSearchTextChanged(string value) => UpdateFilter();
        partial void OnSelectedReturnTypeFilterChanged(string value) => UpdateFilter();
        partial void OnSelectedParameterCountFilterChanged(int? value) => UpdateFilter();
        partial void OnShowStaticOnlyChanged(bool value) => UpdateFilter();
        partial void OnShowInlineOnlyChanged(bool value) => UpdateFilter();
        partial void OnShowWithParametersOnlyChanged(bool value) => UpdateFilter();
        partial void OnFunctionsChanged(ObservableCollection<CFunction> value) => UpdateFilter();

        private void UpdateFilter()
        {
            var query = Functions.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(FunctionSearchText))
            {
                var text = FunctionSearchText.ToLowerInvariant();
                query = query.Where(f => (f.Name?.ToLowerInvariant().Contains(text) ?? false)
                    || (f.ReturnType?.ToLowerInvariant().Contains(text) ?? false)
                    || (f.Signature?.ToLowerInvariant().Contains(text) ?? false));
            }
            if (!string.IsNullOrWhiteSpace(SelectedReturnTypeFilter))
                query = query.Where(f => f.ReturnType == SelectedReturnTypeFilter);
            if (SelectedParameterCountFilter.HasValue)
                query = query.Where(f => f.Parameters?.Count == SelectedParameterCountFilter.Value);
            if (ShowStaticOnly)
                query = query.Where(f => f.IsStatic);
            if (ShowInlineOnly)
                query = query.Where(f => f.IsInline);
            if (ShowWithParametersOnly)
                query = query.Where(f => (f.Parameters?.Count ?? 0) > 0);
            FilteredFunctions = new ObservableCollection<CFunction>(query);
        }

        private void OnExportFunctions() { /* TODO: Export logic */ }
        private void OnAnalyzeFunctionRelationships() { /* TODO: Analyze relationships logic */ }
        private void OnShowAdvancedSearch() { /* TODO: Show advanced search dialog */ }
        private void OnClearFunctionFilters()
        {
            FunctionSearchText = null;
            SelectedReturnTypeFilter = null;
            SelectedParameterCountFilter = null;
            ShowStaticOnly = false;
            ShowInlineOnly = false;
            ShowWithParametersOnly = false;
            UpdateFilter();
        }
        private void OnAnalyzeSingleFunction(CFunction? function) { /* TODO: Analyze single function logic */ }
        private void OnGenerateTestCasesForFunction(CFunction? function) { /* TODO: Generate test cases logic */ }

        private void LoadDemoData()
        {
            Functions = new ObservableCollection<CFunction>
            {
                new CFunction { Name = "main", ReturnType = "int", Parameters = new(), IsStatic = false, IsInline = false, Body = "{ /* ... */ }", SourceFile = "main.c", StartLineNumber = 1, EndLineNumber = 10 },
                new CFunction { Name = "add", ReturnType = "int", Parameters = new(), IsStatic = true, IsInline = false, Body = "{ return a + b; }", SourceFile = "math.c", StartLineNumber = 12, EndLineNumber = 15 },
                new CFunction { Name = "multiply", ReturnType = "int", Parameters = new(), IsStatic = false, IsInline = true, Body = "{ return a * b; }", SourceFile = "math.c", StartLineNumber = 17, EndLineNumber = 19 },
            };
            AvailableReturnTypes.Clear();
            foreach (var type in Functions.Select(f => f.ReturnType).Distinct())
                AvailableReturnTypes.Add(type);
            ParameterCountOptions.Clear();
            foreach (var cnt in Functions.Select(f => f.Parameters?.Count ?? 0).Distinct())
                ParameterCountOptions.Add(cnt);
            UpdateFilter();
        }
    }
}
