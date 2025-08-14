using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter for file type to color mapping
    /// </summary>
    public class FileTypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() is string fileType)
            {
                return fileType.ToLower() switch
                {
                    "cheader" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")), // Blue
                    "cppheader" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F51B5")), // Indigo
                    "csource" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")), // Green
                    "cppsource" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8BC34A")), // Light Green
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")) // Grey
                };
            }
            
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}