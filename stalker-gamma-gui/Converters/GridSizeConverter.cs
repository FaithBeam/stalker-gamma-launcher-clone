using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace stalker_gamma_gui.Converters;

public class GridSizeConverter : IValueConverter
{
    public static GridSizeConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new GridLength((double)value!, GridUnitType.Pixel);
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        return ((GridLength)value!).Value;
    }
}
