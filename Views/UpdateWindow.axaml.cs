using Avalonia.Controls;
using Avalonia.Interactivity;
using NewRGB.ViewModels;

namespace NewRGB.Views;

public partial class UpdateWindow : Window
{
    public UpdateWindow()
    {
        InitializeComponent();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void OnUpdate(object? sender, RoutedEventArgs e)
    {
        Close(true);
    }
}