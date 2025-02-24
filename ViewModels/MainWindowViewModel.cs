using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using NewRGB.Data;
using ProjBobcat.Class.Model;
using ProjBobcat.DefaultComponent.Authenticator;
using ReactiveUI;

namespace NewRGB.ViewModels;

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
[SupportedOSPlatform(nameof(OSPlatform.OSX))]
[SupportedOSPlatform(nameof(OSPlatform.Windows))]
public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _contentViewModel;

    private static readonly string Version =
        Assembly.GetExecutingAssembly().GetName().Version?.ToString()[..^2] ?? "NO_VERSION";

    public MainWindowViewModel()
    {
        Instance = this;
        DataManager.Instance.InitData(DataManager.Instance.DefaultLauncherAccountParser(),
            DataManager.Instance.DefaultLauncherProfileParser());
        var filestream = new FileStream(DataManager.Instance.LogPath, FileMode.Create);
        var streamWriter = new StreamWriter(filestream);
        streamWriter.AutoFlush = true;
        Console.SetOut(streamWriter);
        Console.SetError(streamWriter);
        Console.WriteLine(
            $"Launched Launcher v{Version}");
        if (!DataManager.Instance.HasAccount())
            _contentViewModel = new LoginViewModel();
        else
            _contentViewModel = new MainViewModel();
    }

    public static MainWindowViewModel? Instance { get; private set; }

    public ViewModelBase ContentViewModel
    {
        get => _contentViewModel;
        private set => this.RaiseAndSetIfChanged(ref _contentViewModel, value);
    }

    public string Title => $"NewRGB v{Version}";

    public void OfflineAuth(string name)
    {
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
        OpenMainViewModel();
    }

    public void OpenMainViewModel()
    {
        ContentViewModel = new MainViewModel();
    }

    public void OpenSettings()
    {
        ContentViewModel = new SettingsViewModel();
    }
}