using Avalonia.Controls;
using NewRGB.ViewModels;

namespace NewRGB.Views;

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