using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Avalonia.Data.Converters;
using stalker_gamma_gui.ViewModels.Tabs.MainTab.Enums;

namespace stalker_gamma_gui.Converters;

public class EnumMemberConverter : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        targetType.IsAssignableTo(typeof(IEnumerable)) && value is List<InstallType> enums
            ? enums.Select(x => GetEnumMemberValue(x))
            : value switch
            {
                Enum e => GetEnumMemberValue(e),
                _ => null,
            };

    private static string? GetEnumMemberValue(Enum e) =>
#pragma warning disable IL2075
        e.GetType()
            .GetMember(e.ToString())
#pragma warning restore IL2075
            .First()
            .GetCustomAttribute<EnumMemberAttribute>(false)
            ?.Value;

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        targetType
            .GetFields()
            .Where(f => f.CustomAttributes.Any(a => a.AttributeType == typeof(EnumMemberAttribute)))
            .Select(x => (x, x.GetCustomAttribute<EnumMemberAttribute>(false)))
            .FirstOrDefault(x => x.Item2?.Value == (string?)value)
            .x.GetValue(null);
}
