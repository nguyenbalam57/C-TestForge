using System;
using System.Globalization;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    public class EnumToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is Enum enumValue)
            {
                // Convert enum value to display name using spaces between camel case words
                string enumString = enumValue.ToString();
                string result = enumString[0].ToString();

                for (int i = 1; i < enumString.Length; i++)
                {
                    if (char.IsUpper(enumString[i]))
                        result += " " + enumString[i];
                    else
                        result += enumString[i];
                }

                return result;
            }

            return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
                return null;

            if (targetType.IsEnum)
            {
                // Remove spaces and try to parse as enum
                string enumString = value.ToString().Replace(" ", "");
                return Enum.Parse(targetType, enumString);
            }

            return value;
        }
    }
}