using System.Windows;
using System.Windows.Controls;
using C_TestForge.Models.TestCases;
using C_TestForge.UI.ViewModels;

namespace C_TestForge.UI.Controls
{
    /// <summary>
    /// Interaction logic for TestCaseEditor.xaml
    /// </summary>
    public partial class TestCaseEditor : UserControl
    {
        public static readonly DependencyProperty TestCaseProperty =
            DependencyProperty.Register("TestCase", typeof(Models.TestCases.TestCase), typeof(TestCaseEditor),
                new PropertyMetadata(null, TestCasePropertyChanged));

        public Models.TestCases.TestCase TestCase
        {
            get { return (Models.TestCases.TestCase)GetValue(TestCaseProperty); }
            set { SetValue(TestCaseProperty, value); }
        }

        public TestCaseEditor()
        {
            InitializeComponent();
        }

        private static void TestCasePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as TestCaseEditor;
            if (control != null && control.DataContext is TestCaseEditorViewModel viewModel)
            {
                viewModel.TestCase = e.NewValue as Models.TestCases.TestCase;
            }
        }
    }
}
