using C_TestForge.UI.ViewModels;
using System.Windows;

namespace C_TestForge.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for EditTestCaseDialog.xaml
    /// </summary>
    public partial class EditTestCaseDialog : Window
    {
        public TestCaseViewModel ViewModel { get; private set; }
        public EditTestCaseDialog(TestCaseViewModel viewModel)
        {
            InitializeComponent();

            ViewModel = viewModel;
            DataContext = ViewModel;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ViewModel.Name))
                {
                    MessageBox.Show("Test case name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(ViewModel.TargetFunction))
                {
                    MessageBox.Show("Target function is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ViewModel.SaveCommand.Execute(null);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving test case: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
