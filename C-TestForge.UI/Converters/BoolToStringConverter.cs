using System;
using System.Globalization;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Chuy?n ??i gi? tr? bool sang chu?i t??ng ?ng
    /// V? d?: true -> "Text khi true", false -> "Text khi false"
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // N?u kh?ng c? tham s?, m?c ??nh l? "True|False"
            string paramString = parameter as string ?? "True|False";
            string[] texts = paramString.Split('|');
            
            // N?u kh?ng ?? hai gi? tr?, s? d?ng m?c ??nh
            if (texts.Length < 2)
            {
                texts = new[] { "True", "False" };
            }
            
            // Chuy?n ??i gi? tr? bool
            bool boolValue = value is bool && (bool)value;
            return boolValue ? texts[0] : texts[1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}