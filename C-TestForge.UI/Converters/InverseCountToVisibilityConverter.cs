using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter to check if a count is zero for visibility (inverse of CountToVisibilityConverter)
    /// </summary>
    public class InverseCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
