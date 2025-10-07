using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace stalker_gamma_gui.Converters;

public class ToolTipBorderConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? Brushes.Black : Brushes.Transparent;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        throw new NotImplementedException();
    }
}
