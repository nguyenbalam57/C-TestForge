using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using C_TestForge.Models;
using C_TestForge.Parser;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace C_TestForge.UI.ViewModels
{
    public class SourceFileViewModel : ObservableObject
    {
        private readonly IParser _parser;
        private readonly ILogger<SourceFileViewModel> _logger;

        private CSourceFile _sourceFile;
        private string _filePath;
        private string _content;
        private ObservableCollection<CDefinition> _definitions;
        private ObservableCollection<CVariable> _variables;
        private ObservableCollection<CFunction> _functions;
        private ObservableCollection<CPreprocessorDirective> _preprocessorDirectives;
        private CFunction _selectedFunction;
        private CDefinition _selectedDefinition;
        private CVariable _selectedVariable;
        private CPreprocessorDirective _selectedPreprocessor;

        public SourceFileViewModel(IParser parser, ILogger<SourceFileViewModel> logger)
        {
            _parser = parser;
            _logger = logger;

            // Initialize collections
            Definitions = new ObservableCollection<CDefinition>();
            Variables = new ObservableCollection<CVariable>();
            Functions = new ObservableCollection<CFunction>();
            PreprocessorDirectives = new ObservableCollection<CPreprocessorDirective>();

            // Initialize commands
            ParseCommand = new RelayCommand(Parse, CanParse);
            ReloadCommand = new RelayCommand(Reload, CanReload);
        }

        // Properties
        public CSourceFile SourceFile
        {
            get => _sourceFile;
            set
            {
                if (SetProperty(ref _sourceFile, value))
                {
                    // Update all properties from the source file
                    if (value != null)
                    {
                        FilePath = value.FilePath;
                        Content = value.Content;

                        // Update collections
                        Definitions.Clear();
                        foreach (var definition in value.Definitions)
                        {
                            Definitions.Add(definition);
                        }

                        Variables.Clear();
                        foreach (var variable in value.Variables)
                        {
                            Variables.Add(variable);
                        }

                        Functions.Clear();
                        foreach (var function in value.Functions)
                        {
                            Functions.Add(function);
                        }

                        PreprocessorDirectives.Clear();
                        foreach (var directive in value.PreprocessorDirectives)
                        {
                            PreprocessorDirectives.Add(directive);
                        }
                    }
                    else
                    {
                        FilePath = null;
                        Content = null;
                        Definitions.Clear();
                        Variables.Clear();
                        Functions.Clear();
                        PreprocessorDirectives.Clear();
                    }
                }
            }
        }

        public string FilePath
        {
            get => _filePath;
            set => SetProperty(ref _filePath, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

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

        public ObservableCollection<CPreprocessorDirective> PreprocessorDirectives
        {
            get => _preprocessorDirectives;
            set => SetProperty(ref _preprocessorDirectives, value);
        }

        public CFunction SelectedFunction
        {
            get => _selectedFunction;
            set => SetProperty(ref _selectedFunction, value);
        }

        public CDefinition SelectedDefinition
        {
            get => _selectedDefinition;
            set => SetProperty(ref _selectedDefinition, value);
        }

        public CVariable SelectedVariable
        {
            get => _selectedVariable;
            set => SetProperty(ref _selectedVariable, value);
        }

        public CPreprocessorDirective SelectedPreprocessor
        {
            get => _selectedPreprocessor;
            set => SetProperty(ref _selectedPreprocessor, value);
        }

        // Commands
        public ICommand ParseCommand { get; }
        public ICommand ReloadCommand { get; }

        // Command methods
        private void Parse()
        {
            try
            {
                if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
                {
                    _logger.LogWarning($"File does not exist: {FilePath}");
                    return;
                }

                var parsedFile = _parser.ParseFile(FilePath);
                SourceFile = parsedFile;

                _logger.LogInformation($"Successfully parsed file: {FilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error parsing file: {FilePath}");
            }
        }

        private bool CanParse() => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

        private void Reload()
        {
            try
            {
                if (string.IsNullOrEmpty(FilePath) || !File.Exists(FilePath))
                {
                    _logger.LogWarning($"File does not exist: {FilePath}");
                    return;
                }

                // Load file content
                Content = File.ReadAllText(FilePath);

                _logger.LogInformation($"Successfully reloaded file: {FilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reloading file: {FilePath}");
            }
        }

        private bool CanReload() => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

        // Methods
        public void LoadFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                _logger.LogWarning($"File does not exist: {filePath}");
                return;
            }

            FilePath = filePath;
            Reload();
        }
    }
}
