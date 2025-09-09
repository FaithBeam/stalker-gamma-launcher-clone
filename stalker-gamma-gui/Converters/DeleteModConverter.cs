using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace stalker_gamma_gui.Converters;

public class DeleteModConverter : IMultiValueConverter
{
    public object? Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (values.Count != 2 || values.Any(x => x is not string))
        {
            return BindingOperations.DoNothing;
        }

        return ((string)values[0]!, (string)values[1]!);
    }
}
