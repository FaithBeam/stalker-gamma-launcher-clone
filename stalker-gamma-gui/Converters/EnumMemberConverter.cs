using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Avalonia.Data.Converters;

namespace stalker_gamma_gui.Converters;

public class EnumMemberConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (targetType == typeof(IEnumerable))
        {
            var enums = value as IEnumerable<Enum>;
            return enums?.Select(GetEnumMemberValue);
        }

        if (value is Enum e)
        {
            return GetEnumMemberValue(e);
        }

        return null;
    }

    private static string? GetEnumMemberValue(Enum e) =>
        e.GetType()
            .GetMember(e.ToString())
            .First()
            .GetCustomAttribute<EnumMemberAttribute>(false)
            ?.Value;

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
