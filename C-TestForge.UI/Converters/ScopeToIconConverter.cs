using System;
using System.Globalization;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter ?? chuy?n scope th?nh icon
    /// </summary>
    public class ScopeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() is string scope)
            {
                return scope.ToLower() switch
                {
                    "global" => "Earth",
                    "local" => "Home",
                    "static" => "LockOutline",
                    "extern" => "Import",
                    "parameter" => "Variable",
                    _ => "Variable"
                };
            }
            
            return "Variable";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}