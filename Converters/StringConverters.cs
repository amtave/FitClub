using System;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace FitClub.Converters
{
    public class StringConverters
    {
        public static readonly IValueConverter IsNotNullOrEmpty = 
            new FuncValueConverter<string, bool>(value => !string.IsNullOrEmpty(value));
    }
}