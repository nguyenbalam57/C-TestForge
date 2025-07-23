using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace C_TestForge.UI.Converters
{
    public class BoolToVisibilityConverter : MarkupExtension, IValueConverter
    {
        public bool Inverse { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool bValue = value is bool && (bool)value;

            if (Inverse)
                bValue = !bValue;

            return bValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Visibility))
                return DependencyProperty.UnsetValue;

            bool result = (Visibility)value == Visibility.Visible;

            if (Inverse)
                result = !result;

            return result;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
