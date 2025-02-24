using Avalonia.Controls;
using Avalonia.Interactivity;
using NewRGB.ViewModels;

namespace NewRGB.Views;

public partial class UpdateWindow : Window
{
    public UpdateWindow(string notes)
    {
        InitializeComponent();
        if (DataContext is UpdateWindowViewModel updateWindowViewModel) updateWindowViewModel.Markdown = notes;
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