using Prism.Mvvm;
using Prism.Commands;
using Microsoft.Win32;
using System.IO;

namespace C_TestForge.UI.ViewModels
{
    /// <summary>
    /// ViewModel for SourceCodeView
    /// </summary>
    public class SourceCodeViewModel : BindableBase
    {
        private string _sourceCode;
        public string SourceCode
        {
            get => _sourceCode;
            set => SetProperty(ref _sourceCode, value);
        }

        public DelegateCommand LoadFileCommand { get; }

        public SourceCodeViewModel()
        {
            LoadFileCommand = new DelegateCommand(LoadFile);
        }

        private void LoadFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "C/C++ Files (*.c;*.cpp;*.h;*.hpp)|*.c;*.cpp;*.h;*.hpp|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                SourceCode = File.ReadAllText(dialog.FileName);
            }
        }
    }
}