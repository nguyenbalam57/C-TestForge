using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Rendering;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;

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

        public static readonly DependencyProperty HighlightedLineProperty =
            DependencyProperty.Register("HighlightedLine", typeof(int), typeof(SourceCodeViewer),
                new PropertyMetadata(-1, HighlightedLinePropertyChanged));

        public int HighlightedLine
        {
            get { return (int)GetValue(HighlightedLineProperty); }
            set { SetValue(HighlightedLineProperty, value); }
        }
        public SourceCodeViewer()
        {
            InitializeComponent();

            // Load C syntax highlighting
            LoadSyntaxHighlighting();
        }

        private static void SourceCodePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SourceCodeViewer;
            if (control != null && e.NewValue is string)
            {
                control.textEditor.Text = (string)e.NewValue;
            }
        }

        private static void HighlightedLinePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = d as SourceCodeViewer;
            if (control != null && e.NewValue is int lineNumber)
            {
                control.HighlightLine(lineNumber);
            }
        }

        private void LoadSyntaxHighlighting()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream("C_TestForge.UI.Resources.C.xshd"))
                {
                    if (stream != null)
                    {
                        using (var reader = new XmlTextReader(stream))
                        {
                            textEditor.SyntaxHighlighting = HighlightingLoader.Load(
                                reader, HighlightingManager.Instance);
                        }
                    }
                    else
                    {
                        // Fallback to built-in C# highlighting
                        textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C");
                    }
                }
            }
            catch
            {
                // If something goes wrong, use default highlighting
                textEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C");
            }
        }

        public void HighlightLine(int lineNumber)
        {
            // Clear existing highlighting
            textEditor.TextArea.TextView.LineTransformers.Clear();

            if (lineNumber > 0)
            {
                // Add a line transformer to highlight the specified line
                textEditor.TextArea.TextView.BackgroundRenderers.Add(
                    new LineBackgroundRenderer(textEditor, lineNumber));
            }
        }

        public class LineBackgroundRenderer : IBackgroundRenderer
        {
            private readonly TextEditor _editor;
            private readonly int _targetLine;

            public LineBackgroundRenderer(TextEditor editor, int line)
            {
                _editor = editor;
                _targetLine = line;
            }

            public KnownLayer Layer => KnownLayer.Selection;

            public void Draw(TextView textView, DrawingContext drawingContext)
            {
                if (!(_editor.Document?.Text?.Length > 0)) return;
                textView.EnsureVisualLines();

                var line = _editor.Document.GetLineByNumber(_targetLine);
                foreach (var rect in BackgroundGeometryBuilder.GetRectsForSegment(textView, line))
                {
                    drawingContext.DrawRectangle(
                        Brushes.LightYellow, null,
                        new Rect(rect.Location, new Size(rect.Width, rect.Height)));
                }
            }
        }
    }
}
