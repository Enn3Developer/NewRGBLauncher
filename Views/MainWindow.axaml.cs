using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using NewRGB.ViewModels;

namespace NewRGB.Views;

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
[SupportedOSPlatform(nameof(OSPlatform.OSX))]
[SupportedOSPlatform(nameof(OSPlatform.Windows))]
public partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;
        Closing += (sender, args) =>
        {
            if (DataContext is MainWindowViewModel mainWindowViewModel) mainWindowViewModel.ContentViewModel.OnClose();
        };
    }
}