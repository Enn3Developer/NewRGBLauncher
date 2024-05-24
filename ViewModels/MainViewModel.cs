using System;
using System.Diagnostics;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using NewRGB.Data;
using ProjBobcat.Class.Helper;
using ReactiveUI;

namespace NewRGB.ViewModels;

public class MainViewModel : ViewModelBase
{
    private float _progressValue = 1.0f;
    private string _progressDesc = "Ready";
    private string _playText = "Play";
    private readonly Technic _technic = new("newrgb");
    private bool _needsUpdate;

    public MainViewModel()
    {
        LogoCommand = ReactiveCommand.Create(() => OpenUrl("https://rgbcraft.com"));
        PlayBtn = ReactiveCommand.CreateFromTask(OnPlayButton);
        if (DataManager.Instance.LauncherAccountParser != null)
            Username = DataManager.Instance.LauncherAccountParser.LauncherAccount.Accounts?[
                               DataManager.Instance.LauncherAccountParser.LauncherAccount.ActiveAccountLocalId ?? ""]
                           .Username ??
                       "SOMETHING_WENT_WRONG_2";
        else
            Username = "SOMETHING_WENT_WRONG_3";
    }

    public ReactiveCommand<Unit, Unit> LogoCommand { get; }
    public string Username { get; }

    public string PlayText
    {
        get => _playText;
        private set => this.RaiseAndSetIfChanged(ref _playText, value);
    }

    public string ProgressText => $"{_progressValue:P1}";
    public int ProgressValue => (int)(_progressValue * 100);

    public string ProgressDesc
    {
        get => _progressDesc;
        private set => this.RaiseAndSetIfChanged(ref _progressDesc, value);
    }

    public ReactiveCommand<Unit, Unit> PlayBtn { get; }

    private async Task OnPlayButton()
    {
        if (_needsUpdate)
        {
            await DownloadUpdate();
            await InstallUpdate();
            await _technic.SaveVersion();
            _needsUpdate = false;
            UpdateProgress(1.0f, "Ready: Update installed");
            PlayText = "Play";
        }
        else
        {
            var javaList = await Task.Run(() => SystemInfoHelper.FindJava());
            var javaEnumerator = javaList.GetAsyncEnumerator();
            Console.WriteLine("all javas found: ");
            while (await javaEnumerator.MoveNextAsync()) Console.WriteLine(javaEnumerator.Current);
        }
    }

    private async Task DownloadUpdate()
    {
        UpdateProgress(0.0f, "Starting update");
        var progress = await _technic.DownloadUpdate();
        if (progress == null)
        {
            UpdateProgress(1.0f, "Failed to start update. Please retry");
            return;
        }

        UpdateProgress(0.0f, "Downloading update");
        var bytes = 0L;
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        while (bytes < progress.Length)
        {
            bytes += await progress.Progress();
            stopWatch.Stop();
            UpdateProgress((float)bytes / progress.Length,
                $"Downloading update: {(float)bytes / 1024 / 1024 / ((float)(stopWatch.ElapsedMilliseconds > 0 ? stopWatch.ElapsedMilliseconds : 1) / 1000):F1}MB/s");
            stopWatch.Start();
        }

        await progress.End();
    }

    private async Task InstallUpdate()
    {
        var progress = await _technic.InstallUpdate();
        UpdateProgress(0.0f, "Installing update");
        for (var i = 0; i < progress.Length; i++)
        {
            await progress.Progress(i);
            UpdateProgress((float)i / progress.Length, "Installing update");
        }

        UpdateProgress(1.0f, "Installing update");
    }

    public async Task AsyncOnLoaded()
    {
        UpdateProgress(0.0f, "Checking for updates");
        await _technic.Init();
        if (await _technic.CheckUpdate())
        {
            _needsUpdate = true;
            PlayText = "Update";
            UpdateProgress(1.0f, "Update available");
        }
        else
        {
            UpdateProgress(1.0f, "Ready");
        }
    }

    private void UpdateProgress(float value, string desc)
    {
        _progressValue = value;
        this.RaisePropertyChanged(nameof(ProgressText));
        this.RaisePropertyChanged(nameof(ProgressValue));
        ProgressDesc = desc;
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            // hack because of this: https://github.com/dotnet/corefx/issues/10361
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
}