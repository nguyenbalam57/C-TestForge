using C_TestForge.Infrastructure.Views;
using System.Windows.Controls;
using C_TestForge.UI.ViewModels;

namespace C_TestForge.UI.Views
{
    /// <summary>
    /// Interaction logic for TestCasesView.xaml
    /// </summary>
    public partial class TestCasesView : UserControl, ITestCasesView
    {
        public TestCasesView()
        {
            InitializeComponent();
            DataContext = new TestCaseViewModel(); // Sử dụng constructor mặc định
        }
    }
}