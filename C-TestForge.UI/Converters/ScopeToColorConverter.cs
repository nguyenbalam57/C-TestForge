using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converts a variable scope string to a corresponding color
    /// </summary>
    public class ScopeToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string scope)
            {
                return scope.ToLower() switch
                {
                    "global" => new SolidColorBrush(Color.FromRgb(233, 30, 99)),    // Pink
                    "static" => new SolidColorBrush(Color.FromRgb(156, 39, 176)),   // Purple
                    "local" => new SolidColorBrush(Color.FromRgb(63, 81, 181)),     // Indigo
                    "extern" => new SolidColorBrush(Color.FromRgb(0, 150, 136)),    // Teal
                    "parameter" => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                    "auto" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),      // Green
                    "register" => new SolidColorBrush(Color.FromRgb(121, 85, 72)),  // Brown
                    "rom" => new SolidColorBrush(Color.FromRgb(96, 125, 139)),      // Blue Grey
                    _ => new SolidColorBrush(Color.FromRgb(97, 97, 97))             // Grey (default)
                };
            }

            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Converting back from color to scope is not typically needed
            throw new NotImplementedException("Converting from color to scope is not supported.");
        }
    }
}
