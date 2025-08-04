using C_TestForge.Core.Interfaces.UI;
using C_TestForge.UI.Dialogs;
using C_TestForge.UI.Models;
using CommunityToolkit.Mvvm.Input;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace C_TestForge.UI.Services
{
    /// <summary>
    /// Implementation of the dialog service using Material Design dialogs
    /// </summary>
    public class MaterialDialogService : IDialogService
    {
        private readonly ILogger<MaterialDialogService> _logger;
        private readonly Core.Interfaces.UI.ISnackbarMessageQueue _snackbarMessageQueue;
        private readonly string _dialogIdentifier = "RootDialog";

        public MaterialDialogService(
            ILogger<MaterialDialogService> logger,
            Core.Interfaces.UI.ISnackbarMessageQueue snackbarMessageQueue = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _snackbarMessageQueue = snackbarMessageQueue;
        }

        /// <inheritdoc/>
        // Sửa lại phương thức Show trong MaterialDialogService
        public bool Show(string title, string message, string okText = "OK", string cancelText = "Cancel")
        {
            // Call the async version and wait for result

            try
            {
                // Use Task.Run to avoid deadlocks when calling from UI thread
                return Task.Run(() => ShowAsync(title, message, okText, cancelText)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing confirmation dialog: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shows a confirmation dialog with OK and Cancel buttons asynchronously
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Dialog message</param>
        /// <param name="okText">Text for OK button</param>
        /// <param name="cancelText">Text for Cancel button</param>
        /// <returns>Task resolving to true if user clicks OK, false otherwise</returns>
        public async Task<bool> ShowAsync(string title, string message, string okText = "OK", string cancelText = "Cancel")
        {
            _logger.LogDebug($"Showing confirmation dialog: {title}");

            try
            {
                // Create a task completion source to await dialog result
                var taskCompletionSource = new TaskCompletionSource<bool>();

                // Create dialog model
                var model = new MaterialDialogModel(title, message, okText, cancelText);

                // Set commands
                model.OkCommand = new RelayCommand(() =>
                {
                    DialogHost.Close(_dialogIdentifier);
                    taskCompletionSource.SetResult(true);
                });

                model.CancelCommand = new RelayCommand(() =>
                {
                    DialogHost.Close(_dialogIdentifier);
                    taskCompletionSource.SetResult(false);
                });

                // Create view
                var view = new MaterialDialogView(model);

                // Define what happens when dialog is closed
                DialogClosingEventHandler closingHandler = (sender, args) =>
                {
                    // If task isn't completed yet, dialog was likely closed by clicking outside
                    // or pressing Escape - treat as Cancel
                    if (!taskCompletionSource.Task.IsCompleted)
                    {
                        taskCompletionSource.SetResult(false);
                    }
                };

                // Show dialog
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await DialogHost.Show(view, _dialogIdentifier, closingHandler);
                });

                // Wait for result
                return await taskCompletionSource.Task;
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
            // Call the async version and wait

            try
            {
                Task.Run(() => ShowInformationAsync(title, message)).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing information dialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows an information dialog asynchronously
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Dialog message</param>
        /// <returns>Task completing when dialog is closed</returns>
        public async Task ShowInformationAsync(string title, string message)
        {
            _logger.LogDebug($"Showing information dialog: {title}");

            try
            {
                // Try to use snackbar for simple informational messages if available
                if (_snackbarMessageQueue != null && message.Length < 100)
                {
                    _snackbarMessageQueue.Enqueue(message);
                    return;
                }

                // Create a task completion source to await dialog closure
                var taskCompletionSource = new TaskCompletionSource<bool>();

                // Create dialog model
                var model = new MaterialDialogModel(title, message)
                {
                    Icon = PackIconKind.Information
                };

                // Set OK command
                model.OkCommand = new RelayCommand(() =>
                {
                    DialogHost.Close(_dialogIdentifier);
                    taskCompletionSource.SetResult(true);
                });

                // Create view
                var view = new MaterialDialogView(model);

                // Define what happens when dialog is closed
                DialogClosingEventHandler closingHandler = (sender, args) =>
                {
                    if (!taskCompletionSource.Task.IsCompleted)
                    {
                        taskCompletionSource.SetResult(true);
                    }
                };

                // Show dialog
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await DialogHost.Show(view, _dialogIdentifier, closingHandler);
                });

                // Wait for completion
                await taskCompletionSource.Task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing information dialog: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void ShowError(string title, string message)
        {
            // Call the async version and wait

            try
            {
                Task.Run(() => ShowErrorAsync(title, message)).Wait();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing error dialog: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows an error dialog asynchronously
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Error message</param>
        /// <returns>Task completing when dialog is closed</returns>
        public async Task ShowErrorAsync(string title, string message)
        {
            _logger.LogDebug($"Showing error dialog: {title}");

            try
            {
                // Create a task completion source to await dialog closure
                var taskCompletionSource = new TaskCompletionSource<bool>();

                // Create dialog model
                var model = new MaterialDialogModel(title, message)
                {
                    Icon = PackIconKind.Error
                };

                // Set OK command
                model.OkCommand = new RelayCommand(() =>
                {
                    DialogHost.Close(_dialogIdentifier);
                    taskCompletionSource.SetResult(true);
                });

                // Create view
                var view = new MaterialDialogView(model);

                // Define what happens when dialog is closed
                DialogClosingEventHandler closingHandler = (sender, args) =>
                {
                    if (!taskCompletionSource.Task.IsCompleted)
                    {
                        taskCompletionSource.SetResult(true);
                    }
                };

                // Show dialog
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await DialogHost.Show(view, _dialogIdentifier, closingHandler);
                });

                // Wait for completion
                await taskCompletionSource.Task;
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
                // We need to use the UI thread for file dialogs
                return Application.Current.Dispatcher.Invoke(() =>
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

                    return null;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing save file dialog: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Shows a file dialog asynchronously
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="filter">File filter</param>
        /// <param name="multiSelect">Whether multiple files can be selected</param>
        /// <returns>Task resolving to selected file paths or null if dialog was canceled</returns>
        public async Task<string[]> ShowOpenFileDialogAsync(string title, string filter, bool multiSelect = false)
        {
            _logger.LogDebug($"Showing open file dialog asynchronously: {title}");

            try
            {
                // Execute file dialog on UI thread
                return await Application.Current.Dispatcher.InvokeAsync(() =>
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

                    return null;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing open file dialog asynchronously: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Shows a save file dialog asynchronously
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="filter">File filter</param>
        /// <param name="defaultFileName">Default file name</param>
        /// <returns>Task resolving to selected file path or null if dialog was canceled</returns>
        public async Task<string> ShowSaveFileDialogAsync(string title, string filter, string defaultFileName = null)
        {
            _logger.LogDebug($"Showing save file dialog asynchronously: {title}");

            try
            {
                // Execute file dialog on UI thread
                return await Application.Current.Dispatcher.InvokeAsync(() =>
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

                    return null;
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error showing save file dialog asynchronously: {ex.Message}");
                return null;
            }
        }
    }
}