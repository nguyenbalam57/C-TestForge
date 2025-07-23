using System;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;

namespace C_TestForge.UI.ViewModels
{
    public class ConfirmationDialogViewModel : BindableBase, IDialogAware
    {
        private string _title = "Confirm";
        private string _message;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        public DelegateCommand YesCommand { get; }
        public DelegateCommand NoCommand { get; }

        public ConfirmationDialogViewModel()
        {
            YesCommand = new DelegateCommand(ExecuteYes);
            NoCommand = new DelegateCommand(ExecuteNo);
        }

        private void ExecuteYes()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.OK));
        }

        private void ExecuteNo()
        {
            RaiseRequestClose(new DialogResult(ButtonResult.Cancel));
        }

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            // Clean up resources if needed
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            if (parameters.ContainsKey("Title"))
            {
                Title = parameters.GetValue<string>("Title");
            }

            if (parameters.ContainsKey("Message"))
            {
                Message = parameters.GetValue<string>("Message");
            }
        }

        public event Action<IDialogResult> RequestClose;

        protected virtual void RaiseRequestClose(IDialogResult dialogResult)
        {
            RequestClose?.Invoke(dialogResult);
        }
    }
}