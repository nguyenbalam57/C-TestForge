using C_TestForge.Models;
using C_TestForge.Models.TestCases;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace C_TestForge.UI.Controls
{
    /// <summary>
    /// Interaction logic for TestCaseEditor.xaml
    /// </summary>
    public partial class TestCaseEditor : UserControl
    {
        public TestCaseEditor()
        {
            InitializeComponent();
        }

        // DependencyProperty cho TestCase
        public static readonly DependencyProperty TestCaseProperty =
            DependencyProperty.Register("TestCase", typeof(TestCaseCustom), typeof(TestCaseEditor),
                new PropertyMetadata(null, TestCasePropertyChanged));

        public TestCaseCustom TestCase
        {
            get { return (TestCaseCustom)GetValue(TestCaseProperty); }
            set { SetValue(TestCaseProperty, value); }
        }

        private static void TestCasePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as TestCaseEditor;
            if (control != null && control.DataContext is TestCaseEditorViewModel viewModel)
            {
                viewModel.TestCase = e.NewValue as TestCaseCustom;
            }
        }
    }
}
