using System;
using System.Globalization;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter ?? chuy?n boolean th?nh icon
    /// </summary>
    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string iconParams)
            {
                var icons = iconParams.Split(',');
                if (icons.Length == 2)
                {
                    return boolValue ? icons[0] : icons[1];
                }
            }
            
            return "Help"; // Default icon
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}