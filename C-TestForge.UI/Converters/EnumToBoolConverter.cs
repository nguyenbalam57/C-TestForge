using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace C_TestForge.UI.Converters
{
    public class EnumToBoolConverter : MarkupExtension, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string parameterString = parameter.ToString();
            if (Enum.IsDefined(value.GetType(), value))
            {
                string valueString = value.ToString();
                return valueString.Equals(parameterString, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return Binding.DoNothing;

            bool boolValue = (bool)value;
            if (boolValue)
            {
                string parameterString = parameter.ToString();
                return Enum.Parse(targetType, parameterString);
            }

            return Binding.DoNothing;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}
