using Avalonia.Controls;
using Avalonia.Interactivity;
using NewRGB.ViewModels;

namespace NewRGB.Views;

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
}