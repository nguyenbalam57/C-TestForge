using System;
using System.Globalization;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter for file type to icon mapping
    /// </summary>
    public class FileTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value?.ToString() is string fileType)
            {
                return fileType.ToLower() switch
                {
                    "cheader" => "FileCodeOutline",
                    "cppheader" => "FileCodeOutline", 
                    "csource" => "FileCode",
                    "cppsource" => "FileCode",
                    _ => "File"
                };
            }
            
            return "File";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}