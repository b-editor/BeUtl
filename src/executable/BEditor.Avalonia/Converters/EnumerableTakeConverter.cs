﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Avalonia.Data.Converters;

namespace BEditor.Converters
{
    // IEnumerableからparameterの数だけTakeするConverter
    public sealed class EnumerableTakeConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<object> f && int.TryParse(parameter.ToString(), out var count))
            {
                return f.Take(count);
            }

            return value;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}