using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace C_TestForge.UI.ViewModels
{
    public partial class ProgressDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title = "Đang xử lý...";

        [ObservableProperty]
        private string message = "";

        [ObservableProperty]
        private double progress = 0;

        [ObservableProperty]
        private bool isIndeterminate = false;

        [ObservableProperty]
        private string progressText = "";

        [ObservableProperty]
        private bool canCancel = true;

        public ICommand CancelCommand { get; }

        public ProgressDialogViewModel(Action? onCancel = null)
        {
            CancelCommand = new RelayCommand(() => onCancel?.Invoke());
        }
    }
}
