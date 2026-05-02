using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace FitClub.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public static readonly BoolToColorConverter Default = new();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? 
                    new SolidColorBrush(Color.Parse("#E8F6F3")) : // Зеленый для доступных
                    new SolidColorBrush(Color.Parse("#FADBD8"));  // Красный для недоступных
            }
            return new SolidColorBrush(Color.Parse("#F8F9FA")); // Серый по умолчанию
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}