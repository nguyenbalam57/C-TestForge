using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace C_TestForge.UI.Helpers
{
    public static class SyntaxHighlightHelper
    {
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.RegisterAttached(
                "Text",
                typeof(string),
                typeof(SyntaxHighlightHelper),
                new PropertyMetadata(null, OnTextChanged));

        public static string GetText(RichTextBox richTextBox)
        {
            return (string)richTextBox.GetValue(TextProperty);
        }

        public static void SetText(RichTextBox richTextBox, string value)
        {
            richTextBox.SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var richTextBox = (RichTextBox)d;
            var text = (string)e.NewValue;

            if (string.IsNullOrEmpty(text))
            {
                richTextBox.Document.Blocks.Clear();
                return;
            }

            var paragraph = new Paragraph();
            richTextBox.Document.Blocks.Clear();
            richTextBox.Document.Blocks.Add(paragraph);

            // Highlight C syntax
            HighlightCSyntax(paragraph, text);
        }

        private static void HighlightCSyntax(Paragraph paragraph, string code)
        {
            // Split the code into lines for better processing
            var lines = code.Split('\n');
            bool isFirstLine = true;

            foreach (var line in lines)
            {
                if (!isFirstLine)
                {
                    paragraph.Inlines.Add(new LineBreak());
                }

                // Process the line
                ProcessLine(paragraph, line);

                isFirstLine = false;
            }
        }

        private static void ProcessLine(Paragraph paragraph, string line)
        {
            // Check for preprocessor directives
            if (line.TrimStart().StartsWith("#"))
            {
                paragraph.Inlines.Add(new Run(line) { Foreground = Brushes.Blue });
                return;
            }

            // Regular expressions for different parts of C syntax
            var patterns = new[]
            {
                // Comments
                new { Pattern = @"(//.*$)|(/\*.*?\*/)", Brush = Brushes.Green },
                // Keywords
                new { Pattern = @"\b(auto|break|case|char|const|continue|default|do|double|else|enum|extern|float|for|goto|if|int|long|register|return|short|signed|sizeof|static|struct|switch|typedef|union|unsigned|void|volatile|while)\b", Brush = Brushes.Blue },
                // Numbers
                new { Pattern = @"\b\d+\b", Brush = Brushes.DarkOrange },
                // Strings
                new { Pattern = @"""[^""\\]*(\\.[^""\\]*)*""", Brush = Brushes.Brown },
                // Characters
                new { Pattern = @"'[^'\\]*(\\.[^'\\]*)*'", Brush = Brushes.Brown },
                // Function calls
                new { Pattern = @"\b([a-zA-Z_]\w*)\s*\(", Brush = Brushes.Purple }
            };

            int currentIndex = 0;
            while (currentIndex < line.Length)
            {
                int bestMatchIndex = -1;
                int bestMatchLength = 0;
                Brush bestMatchBrush = null;

                // Find the next match
                for (int i = 0; i < patterns.Length; i++)
                {
                    var match = Regex.Match(line.Substring(currentIndex), patterns[i].Pattern);
                    if (match.Success && match.Index == 0 && match.Length > bestMatchLength)
                    {
                        bestMatchIndex = i;
                        bestMatchLength = match.Length;
                        bestMatchBrush = patterns[i].Brush;
                    }
                }

                if (bestMatchIndex != -1)
                {
                    // Add the matched text with the appropriate color
                    paragraph.Inlines.Add(new Run(line.Substring(currentIndex, bestMatchLength)) { Foreground = bestMatchBrush });
                    currentIndex += bestMatchLength;
                }
                else
                {
                    // Add non-matched text as is
                    paragraph.Inlines.Add(new Run(line.Substring(currentIndex, 1)));
                    currentIndex++;
                }
            }
        }
    }
}
