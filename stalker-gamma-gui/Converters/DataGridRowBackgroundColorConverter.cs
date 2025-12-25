using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using stalker_gamma.core.Models;
using stalker_gamma.core.ViewModels.Tabs.MainTab;

namespace stalker_gamma_gui.Converters;

public class DataGridRowBackgroundColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not ModDownloadExtractProgressVm vm)
        {
            return null;
        }

        if (vm.ModListRecord is GitRecord or ModpackSpecific)
        {
            return Brush.Parse("#0F110C");
        }

        return Brush.Parse("Black");
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
