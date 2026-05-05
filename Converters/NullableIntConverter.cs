using System;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace FitClub.Converters
{
    public class NullableIntConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int intValue)
                return intValue.ToString();
            if (value is null)
                return "";
            return value.ToString();
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (string.IsNullOrWhiteSpace(stringValue))
                    return null;
                    
                if (int.TryParse(stringValue, out int result))
                    return result;
            }
            return null;
        }
    }
}