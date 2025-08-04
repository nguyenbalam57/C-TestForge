using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converts a null or non-null value to Visibility
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value == null;

            // Check if we need to invert the logic
            if (parameter is string param && param.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                isNull = !isNull;
            }

            return isNull ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // This converter doesn't support converting back
            throw new NotImplementedException("Converting from Visibility to null is not supported.");
        }
    }
}
