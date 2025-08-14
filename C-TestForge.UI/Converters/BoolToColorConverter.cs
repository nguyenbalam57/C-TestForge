using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter ?? chuy?n boolean th?nh m?u
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string colorParams)
                {
                    var colors = colorParams.Split(',');
                    if (colors.Length == 2)
                    {
                        var colorStr = boolValue ? colors[0] : colors[1];
                        return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorStr));
                    }
                }
                
                // Default colors
                return boolValue ? 
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2F81F7")) : 
                    new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E"));
            }
            
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}