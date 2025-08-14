using System;
using System.Globalization;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter ?? chuy?n boolean th?nh row height cho DataGrid
    /// </summary>
    public class BoolToRowHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool showExpanded)
            {
                return showExpanded ? 80.0 : 44.0; // Expanded height vs normal height
            }
            
            return 44.0; // Default height
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}