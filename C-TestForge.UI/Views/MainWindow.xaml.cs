using C_TestForge.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace C_TestForge.UI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            DataContext = _viewModel;
        }
        /// <summary>
        /// Window loaded event handler
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize any window-specific logic here
        }

        /// <summary>
        /// Window closing event handler
        /// </summary>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Check if there are unsaved changes and prompt user if needed
            if (_viewModel.HasUnsavedChanges)
            {
                var result = MessageBox.Show(
                    "There are unsaved changes. Do you want to save before closing?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                switch (result)
                {
                    case MessageBoxResult.Yes:
                        // Save changes and close
                        _viewModel.SaveProjectCommand.Execute(null);
                        break;
                    case MessageBoxResult.Cancel:
                        // Cancel closing
                        e.Cancel = true;
                        break;
                    case MessageBoxResult.No:
                        // Close without saving
                        break;
                }
            }
        }
    }
}
