using C_TestForge.Core.Interfaces.UI;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace C_TestForge.Parser.UI
{
    /// <summary>
    /// Implementation of the dialog service using Material Design and WPF dialogs
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly ILogger<DialogService> _logger;

        public DialogService(ILogger<DialogService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public bool Show(string title, string message, string okText = "OK", string cancelText = "Cancel")
        {
            _logger.LogDebug($"Showing confirmation dialog: {title}");

            try
            {
                var dialogResult = MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OKCancel,
                    MessageBoxImage.Question,
                    MessageBoxResult.Cancel);

                return dialogResult == MessageBoxResult.OK;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing confirmation dialog: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc/>
        public void ShowInformation(string title, string message)
        {
            _logger.LogDebug($"Showing information dialog: {title}");

            try
            {
                MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing information dialog: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void ShowError(string title, string message)
        {
            _logger.LogDebug($"Showing error dialog: {title}");

            try
            {
                MessageBox.Show(
                    message,
                    title,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing error dialog: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public string[] ShowOpenFileDialog(string title, string filter, bool multiSelect = false)
        {
            _logger.LogDebug($"Showing open file dialog: {title}");

            try
            {
                var dialog = new OpenFileDialog
                {
                    Title = title,
                    Filter = filter,
                    Multiselect = multiSelect
                };

                if (dialog.ShowDialog() == true)
                {
                    return multiSelect ? dialog.FileNames : new[] { dialog.FileName };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing open file dialog: {ex.Message}");
            }

            return null;
        }

        /// <inheritdoc/>
        public string ShowSaveFileDialog(string title, string filter, string defaultFileName = null)
        {
            _logger.LogDebug($"Showing save file dialog: {title}");

            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = title,
                    Filter = filter,
                    FileName = defaultFileName
                };

                if (dialog.ShowDialog() == true)
                {
                    return dialog.FileName;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing save file dialog: {ex.Message}");
            }

            return null;
        }
    }
}
