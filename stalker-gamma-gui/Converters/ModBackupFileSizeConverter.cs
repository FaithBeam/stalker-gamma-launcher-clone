using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace stalker_gamma_gui.Converters;

public class ModBackupFileSizeConverter : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is not long l ? null : $"{l / 1024 / 1024 / 1024} GB";

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is not string s ? null : long.Parse(s.Replace(" GB", "")) * 1024 * 1024 * 1024;
}
