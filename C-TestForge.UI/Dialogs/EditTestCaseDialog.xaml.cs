using C_TestForge.Models.TestCases;
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

        public TestCaseUser TestCase { get; private set; }

        public EditTestCaseDialog(TestCaseUser testCase = null)
        {
            InitializeComponent();

            // Tạo và gán ViewModel
            var viewModel = new EditTestCaseDialogViewModel(testCase);
            DataContext = viewModel;

            // Lưu testCase gốc
            TestCase = testCase ?? new TestCaseUser();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Lấy ViewModel
            var viewModel = DataContext as EditTestCaseDialogViewModel;

            // Cập nhật TestCase từ ViewModel
            TestCase.Name = viewModel.Name;
            TestCase.Description = viewModel.Description;
            TestCase.FunctionName = viewModel.FunctionName;
            TestCase.Type = viewModel.Type;
            TestCase.Status = viewModel.Status;

            // Cập nhật inputs và outputs
            TestCase.Inputs.Clear();
            foreach (var input in viewModel.Inputs)
            {
                TestCase.Inputs.Add(input);
            }

            TestCase.ExpectedOutputs.Clear();
            foreach (var output in viewModel.ExpectedOutputs)
            {
                TestCase.ExpectedOutputs.Add(output);
            }

            TestCase.ActualOutputs.Clear();
            foreach (var output in viewModel.ActualOutputs)
            {
                TestCase.ActualOutputs.Add(output);
            }

            // Đặt DialogResult và đóng dialog
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Hủy và đóng dialog
            DialogResult = false;
            Close();
        }
    }
}
