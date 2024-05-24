using System;
using NewRGB.Data;
using ProjBobcat.Class.Model;
using ProjBobcat.DefaultComponent.Authenticator;
using ReactiveUI;

namespace NewRGB.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _contentViewModel;

    public MainWindowViewModel()
    {
        Instance = this;
        DataManager.Instance.InitData(DataManager.Instance.DefaultLauncherAccountParser());
        if (!DataManager.Instance.HasAccount())
        {
            Console.WriteLine("no account");
            _contentViewModel = new LoginViewModel();
        }
        else
        {
            _contentViewModel = new MainViewModel();
        }
    }

    public static MainWindowViewModel? Instance { get; private set; }

    public ViewModelBase ContentViewModel
    {
        get => _contentViewModel;
        private set => this.RaiseAndSetIfChanged(ref _contentViewModel, value);
    }

    public void OfflineAuth(string name)
    {
        Console.WriteLine("Offline auth: {0}", name);
        if (DataManager.Instance.LauncherAccountParser == null) return;
        var offlineAuthenticator = new OfflineAuthenticator
        {
            Username = name,
            LauncherAccountParser = DataManager.Instance.LauncherAccountParser
        };
        var result = offlineAuthenticator.Auth();
        if (result.AuthStatus is AuthStatus.Failed or AuthStatus.Unknown || result.SelectedProfile == null) return;
        DataManager.Instance.LauncherAccountParser.ActivateAccount(DataManager.Instance.LauncherAccountParser
            .Find(result.SelectedProfile.UUID.ToGuid())?.Key ?? "");
        ContentViewModel = new MainViewModel();
    }
}