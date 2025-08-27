using System;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter to extract file name from full path
    /// </summary>
    public class FileNameFromPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string filePath && !string.IsNullOrEmpty(filePath))
            {
                try
                {
                    return Path.GetFileName(filePath);
                }
                catch
                {
                    return filePath;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to show/hide elements based on collection count (inverse logic)
    /// </summary>
    public class InverseCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to show/hide elements based on collection count
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to enable/disable based on null or empty string
    /// </summary>
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string str)
            {
                return !string.IsNullOrWhiteSpace(str);
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Multi-value converter for enabling commands based on multiple conditions
    /// </summary>
    public class MultiConditionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length == 0)
                return false;

            foreach (var value in values)
            {
                if (value is bool boolValue && !boolValue)
                    return false;

                if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
                    return false;

                if (value is int intValue && intValue <= 0)
                    return false;

                if (value == null)
                    return false;
            }

            return true;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converter to format directory path for display (shortening if too long)
    /// </summary>
    public class DirectoryPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                const int maxLength = 50;
                if (path.Length <= maxLength)
                    return path;

                try
                {
                    var parts = path.Split(Path.DirectorySeparatorChar);
                    if (parts.Length <= 2)
                        return path;

                    var result = $"{parts[0]}{Path.DirectorySeparatorChar}...{Path.DirectorySeparatorChar}{parts[^1]}";
                    if (result.Length <= maxLength)
                        return result;

                    return $"...{Path.DirectorySeparatorChar}{parts[^1]}";
                }
                catch
                {
                    return path.Length > maxLength ? $"...{path.Substring(path.Length - maxLength + 3)}" : path;
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}