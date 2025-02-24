using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NewRGB.Data;
using NewRGB.ViewModels;

namespace NewRGB.Views;

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
[SupportedOSPlatform(nameof(OSPlatform.OSX))]
[SupportedOSPlatform(nameof(OSPlatform.Windows))]
public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }

    private void OnDone(object? sender, RoutedEventArgs e)
    {
        var settings = DataManager.Instance.Settings;
        settings.MinMemory = (uint)MinMemorySlider.Value;
        settings.MaxMemory = (uint)MaxMemorySlider.Value;
        DataManager.Instance.SaveSettings();
        MainWindowViewModel.Instance?.OpenMainViewModel();
    }
}