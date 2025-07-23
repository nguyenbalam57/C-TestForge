using C_TestForge.Models;
using System.Windows;

namespace C_TestForge.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for AddVariableDialog.xaml
    /// </summary>
    public partial class AddVariableDialog : Window
    {
        public TestCaseVariable Variable { get; private set; }

        public AddVariableDialog()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Variable name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtType.Text))
                {
                    MessageBox.Show("Variable type is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int arraySize = 0;
                if (chkArray.IsChecked == true)
                {
                    if (!int.TryParse(txtArraySize.Text, out arraySize) || arraySize <= 0)
                    {
                        MessageBox.Show("Array size must be a positive integer.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                Variable = new TestCaseVariable
                {
                    Name = txtName.Text,
                    Type = txtType.Text,
                    Value = txtValue.Text,
                    IsPointer = chkPointer.IsChecked == true,
                    IsArray = chkArray.IsChecked == true,
                    ArraySize = arraySize
                };

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding variable: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ChkArray_Changed(object sender, RoutedEventArgs e)
        {
            pnlArraySize.Visibility = chkArray.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
