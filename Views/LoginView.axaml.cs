using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using NewRGB.ViewModels;

namespace NewRGB.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void OnEnter(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && OfflineAuthBox is { Text.Length: > 0 }) DoOfflineAuth();
    }

    private void OnOfflineAuthClick(object? sender, RoutedEventArgs e)
    {
        if (OfflineAuthBox is { Text.Length: > 0 }) DoOfflineAuth();
    }

    private void DoOfflineAuth()
    {
        MainWindowViewModel.Instance?.OfflineAuth(OfflineAuthBox?.Text ?? "SOMETHING_WENT_WRONG");
    }
}