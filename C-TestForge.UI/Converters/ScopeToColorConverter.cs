using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter để chuyển scope thành màu
    /// </summary>
    public class ScopeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() is string scope)
            {
                return scope.ToLower() switch
                {
                    "global" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")), // Blue
                    "local" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")), // Green
                    "static" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")), // Orange
                    "extern" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9C27B0")), // Purple
                    "parameter" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E91E63")), // Pink
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
