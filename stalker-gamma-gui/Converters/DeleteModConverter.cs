using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data;
using Avalonia.Data.Converters;
using stalker_gamma.core.ViewModels.Tabs.BackupTab;

namespace stalker_gamma_gui.Converters;

public class DeleteModConverter : IMultiValueConverter
{
    public object? Convert(
        IList<object?> values,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        values is not [string pathToDelete, ModBackupVm modBackupVm]
            ? BindingOperations.DoNothing
            : (pathToDelete, modBackupVm.FileName);
}
