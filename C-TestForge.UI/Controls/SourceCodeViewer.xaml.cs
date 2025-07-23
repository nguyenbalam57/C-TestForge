using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;

namespace C_TestForge.UI.Controls
{
    /// <summary>
    /// Interaction logic for SourceCodeViewer.xaml
    /// </summary>
    public partial class SourceCodeViewer : UserControl
    {
        public static readonly DependencyProperty SourceCodeProperty =
        DependencyProperty.Register("SourceCode", typeof(string), typeof(SourceCodeViewer),
            new PropertyMetadata(string.Empty, SourceCodePropertyChanged));

        public string SourceCode
        {
            get { return (string)GetValue(SourceCodeProperty); }
            set { SetValue(SourceCodeProperty, value); }
        }
        public SourceCodeViewer()
        {
            InitializeComponent();

            // Thiết lập syntax highlighting cho C
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream("C_TestForge.UI.Resources.C.xshd"))
            {
                using (var reader = new XmlTextReader(stream))
                {
                    textEditor.SyntaxHighlighting = ICSharpCode.AvalonEdit.Highlighting.Xshd.HighlightingLoader.Load(
                        reader, ICSharpCode.AvalonEdit.Highlighting.HighlightingManager.Instance);
                }
            }
        }

        private static void SourceCodePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SourceCodeViewer;
            if (control != null && e.NewValue is string)
            {
                control.textEditor.Text = (string)e.NewValue;
            }
        }

        // Thêm phương thức để highlight dòng cụ thể
        public void HighlightLine(int lineNumber)
        {
            // Triển khai highlighting cho dòng được chọn
        }
    }
}
