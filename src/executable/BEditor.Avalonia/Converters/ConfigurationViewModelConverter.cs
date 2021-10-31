﻿using System;
using System.Globalization;

using Avalonia.Data.Converters;

using BEditor.Data;
using BEditor.ViewModels;

namespace BEditor.Converters
{
    // プロジェクトからプロジェクトのコンフィグのViewModelに変換
    public sealed class ConfigurationViewModelConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Project f) return new ConfigurationViewModel(f);

            return null;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}