using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows.Input;

namespace C_TestForge.UI.Models
{
    /// <summary>
    /// Model for MaterialDialogView
    /// </summary>
    public class MaterialDialogModel : ObservableObject
    {
        private string _title;
        private string _message;
        private string _okText = "OK";
        private string _cancelText = "Cancel";
        private bool _isConfirmation;
        private PackIconKind _icon = PackIconKind.Information;
        private ICommand _okCommand;
        private ICommand _cancelCommand;

        /// <summary>
        /// Dialog title
        /// </summary>
        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        /// <summary>
        /// Dialog message
        /// </summary>
        public string Message
        {
            get => _message;
            set => SetProperty(ref _message, value);
        }

        /// <summary>
        /// Text for the OK button
        /// </summary>
        public string OkText
        {
            get => _okText;
            set => SetProperty(ref _okText, value);
        }

        /// <summary>
        /// Text for the Cancel button
        /// </summary>
        public string CancelText
        {
            get => _cancelText;
            set => SetProperty(ref _cancelText, value);
        }

        /// <summary>
        /// Whether this is a confirmation dialog with two buttons
        /// </summary>
        public bool IsConfirmation
        {
            get => _isConfirmation;
            set => SetProperty(ref _isConfirmation, value);
        }

        /// <summary>
        /// Icon to display in the dialog
        /// </summary>
        public PackIconKind Icon
        {
            get => _icon;
            set
            {
                if (SetProperty(ref _icon, value))
                {
                    OnPropertyChanged(nameof(HasIcon));
                }
            }
        }

        /// <summary>
        /// Whether the dialog has an icon
        /// </summary>
        public bool HasIcon => Icon != PackIconKind.None;

        /// <summary>
        /// Command for the OK button
        /// </summary>
        public ICommand OkCommand
        {
            get => _okCommand;
            set => SetProperty(ref _okCommand, value);
        }

        /// <summary>
        /// Command for the Cancel button
        /// </summary>
        public ICommand CancelCommand
        {
            get => _cancelCommand;
            set => SetProperty(ref _cancelCommand, value);
        }

        /// <summary>
        /// Create a new dialog model
        /// </summary>
        public MaterialDialogModel()
        {
        }

        /// <summary>
        /// Create a new dialog model with basic properties
        /// </summary>
        public MaterialDialogModel(string title, string message, string okText = "OK")
        {
            Title = title;
            Message = message;
            OkText = okText;
            IsConfirmation = false;
        }

        /// <summary>
        /// Create a new confirmation dialog model
        /// </summary>
        public MaterialDialogModel(string title, string message, string okText, string cancelText)
        {
            Title = title;
            Message = message;
            OkText = okText;
            CancelText = cancelText;
            IsConfirmation = true;
        }
    }
}