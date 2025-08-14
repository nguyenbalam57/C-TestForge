using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter ?? chuy?n type th?nh m?u
    /// </summary>
    public class TypeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() is string typeName)
            {
                var lowerType = typeName.ToLower();
                
                if (lowerType.Contains("int"))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")); // Blue
                else if (lowerType.Contains("float") || lowerType.Contains("double"))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")); // Orange
                else if (lowerType.Contains("char"))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")); // Green
                else if (lowerType.Contains("void"))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")); // Grey
                else if (lowerType.Contains("bool"))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E91E63")); // Pink
                else if (lowerType.Contains("struct") || lowerType.Contains("union"))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9C27B0")); // Purple
                else if (lowerType.Contains("enum"))
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#795548")); // Brown
                else
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#607D8B")); // Blue Grey
            }
            
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}