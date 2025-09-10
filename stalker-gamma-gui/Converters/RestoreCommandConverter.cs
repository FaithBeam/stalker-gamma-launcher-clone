using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;
using stalker_gamma.core.ViewModels.Tabs.BackupTab.Commands;

namespace stalker_gamma_gui.Converters;

public class RestoreCommandConverter : IMultiValueConverter
{
    public object? Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        if (values.Count != 3 || values.Any(x => x is not string))
        {
            return BindingOperations.DoNothing;
        }

        return new RestoreBackup.Command(
            Path.Join((string)values[0]!, (string)values[1]!),
            (string)values[2]!
        );
    }
}
