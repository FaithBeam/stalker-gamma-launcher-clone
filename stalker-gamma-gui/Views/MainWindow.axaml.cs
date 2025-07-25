using System;
using System.IO;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using ReactiveUI;
using stalker_gamma.core.ViewModels.MainWindow;

namespace stalker_gamma_gui.Views;

public partial class MainWindow : ReactiveWindow<MainWindowVm>
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
