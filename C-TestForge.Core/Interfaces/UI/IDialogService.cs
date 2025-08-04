using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace C_TestForge.Core.Interfaces.UI
{
    /// <summary>
    /// Interface for dialog service to handle UI interactions
    /// </summary>
    public interface IDialogService
    {
        /// <summary>
        /// Shows a confirmation dialog with OK and Cancel buttons
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Dialog message</param>
        /// <param name="okText">Text for OK button</param>
        /// <param name="cancelText">Text for Cancel button</param>
        /// <returns>True if user clicks OK, false otherwise</returns>
        bool Show(string title, string message, string okText = "OK", string cancelText = "Cancel");

        /// <summary>
        /// Shows an information dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Dialog message</param>
        void ShowInformation(string title, string message);

        /// <summary>
        /// Shows an error dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="message">Error message</param>
        void ShowError(string title, string message);

        /// <summary>
        /// Shows an open file dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="filter">File filter</param>
        /// <param name="multiSelect">Whether multiple files can be selected</param>
        /// <returns>Selected file paths or null if dialog was canceled</returns>
        string[] ShowOpenFileDialog(string title, string filter, bool multiSelect = false);

        /// <summary>
        /// Shows a save file dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="filter">File filter</param>
        /// <param name="defaultFileName">Default file name</param>
        /// <returns>Selected file path or null if dialog was canceled</returns>
        string ShowSaveFileDialog(string title, string filter, string defaultFileName = null);
    }
}
