using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter ?? chuy?n dependency depth th?nh m?u
    /// </summary>
    public class DepthToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int depth)
            {
                return depth switch
                {
                    0 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")), // Green - Root
                    1 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")), // Blue - Level 1
                    2 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")), // Orange - Level 2
                    3 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E91E63")), // Pink - Level 3
                    4 => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9C27B0")), // Purple - Level 4
                    _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")) // Red - Deep levels
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