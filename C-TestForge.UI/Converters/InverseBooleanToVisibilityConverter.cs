using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Chuy?n ??i gi? tr? boolean th?nh Visibility theo c?ch ??o ng??c:
    /// true -> Collapsed v? false -> Visible
    /// </summary>
    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            
            return false;
        }
    }
}