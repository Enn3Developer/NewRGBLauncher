using System;
using System.Collections.Generic;
using System.Linq;
using NewRGB.Data;
using ProjBobcat.Class.Model.LauncherAccount;
using ProjBobcat.DefaultComponent.Authenticator;
using ProjBobcat.DefaultComponent.Launch;
using ReactiveUI;

namespace NewRGB.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _contentViewModel;
    public ViewModelBase ContentViewModel
    {
        get => _contentViewModel;
        private set => this.RaiseAndSetIfChanged(ref _contentViewModel, value);
    }

    public MainWindowViewModel()
    {
        DataManager.Instance.InitData(DataManager.Instance.DefaultLauncherAccountParser());
        if (!DataManager.Instance.HasAccount())
        {
            Console.WriteLine("no account");
            // var launcherProfileParser = new DefaultLauncherAccountParser(DataManager.Instance.DataPath, Guid.NewGuid());
            // // if (launcherProfileParser.LauncherAccount?.ActiveAccountLocalId == null)
            // // {
            // //     launcherProfileParser.ActivateAccount(launcherProfileParser.LauncherAccount?.Accounts?.Keys.FirstOrDefault());
            // // }
            // Console.WriteLine(launcherProfileParser.LauncherAccount?.ActiveAccountLocalId);
            // Console.WriteLine(string.Join(Environment.NewLine, launcherProfileParser.LauncherAccount?.Accounts ?? new Dictionary<string,AccountModel>()));
            // var offlineAuthenticator = new OfflineAuthenticator
            // {
            //     LauncherAccountParser = launcherProfileParser,
            //     Username = "Enn3DevPlayer"
            // };
            // launcherProfileParser.ActivateAccount(DataManager.Instance.ManageAuth(offlineAuthenticator)?.ToString());
        }
        _contentViewModel = new MainViewModel();
    }
}