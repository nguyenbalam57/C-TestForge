using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace C_TestForge.UI.Converters
{
    /// <summary>
    /// Converter that converts a collection to a comma-separated string
    /// </summary>
    public class CollectionToStringConverter : IValueConverter
    {
        /// <summary>
        /// Converts a collection to a comma-separated string
        /// </summary>
        /// <param name="value">Value to convert</param>
        /// <param name="targetType">Target type</param>
        /// <param name="parameter">Converter parameter</param>
        /// <param name="culture">Culture info</param>
        /// <returns>Converted value</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value is IEnumerable collection)
            {
                // Convert the collection to a list of strings
                var items = collection.Cast<object>()
                    .Select(item => item?.ToString() ?? string.Empty)
                    .Where(item => !string.IsNullOrEmpty(item))
                    .ToList();

                if (items.Count == 0)
                    return string.Empty;

                // Join the items with commas
                return string.Join(", ", items);
            }

            return value.ToString();
        }

        /// <summary>
        /// Converts a string back to a collection (not implemented)
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException("Converting from string to collection is not supported");
        }
    }
}