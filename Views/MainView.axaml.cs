using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Interactivity;
using NewRGB.ViewModels;

namespace NewRGB.Views;

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
[SupportedOSPlatform(nameof(OSPlatform.OSX))]
[SupportedOSPlatform(nameof(OSPlatform.Windows))]
public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel) await viewModel.AsyncOnLoaded();
    }

    private void OnSettings(object? sender, RoutedEventArgs e)
    {
        if (MainWindowViewModel.Instance != null) MainWindowViewModel.Instance.OpenSettings();
    }
}