using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows;

namespace C_TestForge.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for NewProjectDialog.xaml
    /// </summary>
    public partial class NewProjectDialog : Window
    {
        public NewProjectDialog()
        {
            InitializeComponent();
        }

        public string ProjectName => txtProjectName.Text;
        public string ProjectDescription => txtDescription.Text;
        public string SourceDirectory => txtSourceDirectory.Text;

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true,
                Title = "Select Source Directory"
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtSourceDirectory.Text = dialog.FileName;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                MessageBox.Show("Project name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(SourceDirectory))
            {
                MessageBox.Show("Source directory is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
