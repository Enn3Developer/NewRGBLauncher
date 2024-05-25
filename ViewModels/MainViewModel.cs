using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using NewRGB.Data;
using ProjBobcat.Class.Model.Mojang;
using ProjBobcat.DefaultComponent;
using ProjBobcat.DefaultComponent.Installer.ForgeInstaller;
using ProjBobcat.DefaultComponent.ResourceInfoResolver;
using ProjBobcat.Interface;
using ReactiveUI;

namespace NewRGB.ViewModels;

public class MainViewModel : ViewModelBase
{
    private float _progressValue = 1.0f;
    private string _progressDesc = "Ready";
    private string _playText = "Play";
    private bool _determinateValue = true;
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

    public bool DeterminateValue
    {
        get => _determinateValue;
        private set => this.RaiseAndSetIfChanged(ref _determinateValue, !value);
    }

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
            UpdateProgress(0.0f, "Checking Java version", false);
            var javaList = ProjBobcat.Class.Helper.SystemInfoHelper.FindJava();
            var javaEnumerator = javaList.GetAsyncEnumerator();
            var valid = false;
            while (await javaEnumerator.MoveNextAsync())
            {
                if (!await DataManager.IsValidJava(javaEnumerator.Current)) continue;
                valid = true;
                break;
            }

            if (!valid)
            {
                UpdateProgress(1.0f, "No valid java installation found");
                return;
            }

            var javaPath = javaEnumerator.Current;
            UpdateProgress(0.0f, "Checking Forge installation", false);
            if (!DataManager.Instance.IsForgeInstalled())
            {
                UpdateProgress(0.0f, "Starting Forge download", false);
                var progress = await DataManager.Instance.DownloadForge();
                if (progress == null)
                {
                    UpdateProgress(1.0f, "Failed to start Forge download. Please retry");
                    return;
                }

                await DownloadAndProgress(progress, "Forge");
                var forgeInstaller = new HighVersionForgeInstaller
                {
                    ForgeExecutablePath = DataManager.Instance.ForgeInstallerPath,
                    JavaExecutablePath = javaPath,
                    RootPath = DataManager.Instance.MinecraftPath,
                    VersionLocator = DataManager.Instance.GameCoreBase?.VersionLocator!,
                    DownloadUrlRoot = "https://bmclapi2.bangbang93.com/",
                    MineCraftVersion = "1.19.2",
                    MineCraftVersionId = "1.19.2"
                };
                forgeInstaller.StageChangedEventDelegate += (_, args) =>
                {
                    UpdateProgress((float)args.Progress, "Installing Forge");
                };
                await forgeInstaller.InstallForgeTaskAsync();
                UpdateProgress(1.0f, "Installing Forge");
            }

            UpdateProgress(0.0f, "Checking Minecraft. This may take a while", false);
            var httpClient = new HttpClient();
            var versionsPath = Path.Combine(DataManager.Instance.MinecraftPath, "versions");
            var versionDir = Path.Combine(versionsPath, "1.19.2");
            var versionFile = Path.Combine(versionDir, "1.19.2.json");
            if (!Directory.Exists(versionDir)) Directory.CreateDirectory(versionDir);
            if (!File.Exists(versionFile))
            {
                var responseVersion = await httpClient.GetAsync(
                    "https://piston-meta.mojang.com/v1/packages/ed548106acf3ac7e8205a6ee8fd2710facfa164f/1.19.2.json");
                responseVersion.EnsureSuccessStatusCode();
                await File.WriteAllTextAsync(versionFile, await responseVersion.Content.ReadAsStringAsync());
            }

            var response =
                await httpClient.GetAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");
            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync();
            var versionManifest =
                JsonSerializer.Deserialize(responseJson, VersionManifestContext.Default.VersionManifest);
            var versionInfo = DataManager.Instance.GameCoreBase?.VersionLocator.GetGame("1.19.2-forge-43.3.13");
            if (versionInfo == null)
            {
                UpdateProgress(1.0f, "Can't get info about the game version");
                return;
            }

            var completer = new DefaultResourceCompleter
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount * 2,
                ResourceInfoResolvers = new List<IResourceInfoResolver>
                {
                    new AssetInfoResolver
                    {
                        AssetIndexUriRoot = "https://launchermeta.mojang.com/",
                        AssetUriRoot = "https://resources.download.minecraft.net/",
                        BasePath = DataManager.Instance.MinecraftPath,
                        VersionInfo = versionInfo,
                        CheckLocalFiles = true,
                        Versions = versionManifest?.Versions!
                    },
                    new GameLoggingInfoResolver
                    {
                        BasePath = DataManager.Instance.MinecraftPath,
                        VersionInfo = versionInfo,
                        CheckLocalFiles = true
                    },
                    new LibraryInfoResolver
                    {
                        BasePath = DataManager.Instance.MinecraftPath,
                        ForgeUriRoot = "https://files.minecraftforge.net/maven/",
                        ForgeMavenUriRoot = "https://maven.minecraftforge.net/",
                        ForgeMavenOldUriRoot = "https://files.minecraftforge.net/maven/",
                        FabricMavenUriRoot = "https://maven.fabricmc.net/",
                        LibraryUriRoot = "https://libraries.minecraft.net/",
                        VersionInfo = versionInfo,
                        CheckLocalFiles = true
                    },
                    new VersionInfoResolver
                    {
                        BasePath = DataManager.Instance.MinecraftPath,
                        VersionInfo = versionInfo,
                        CheckLocalFiles = true
                    }
                },
                TotalRetry = 3,
                CheckFile = true
            };
            await completer.CheckAndDownloadTaskAsync();
            UpdateProgress(1.0f, "Done");
        }
    }

    private async Task DownloadUpdate()
    {
        UpdateProgress(0.0f, "Starting update", false);
        var progress = await _technic.DownloadUpdate();
        if (progress == null)
        {
            UpdateProgress(1.0f, "Failed to start update. Please retry");
            return;
        }

        await DownloadAndProgress(progress, "update");
    }

    private async Task DownloadAndProgress(DownloadProgress progress, string desc)
    {
        UpdateProgress(0.0f, $"Downloading {desc}");
        var bytes = 0L;
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        while (bytes < progress.Length)
        {
            bytes += await progress.Progress();
            stopWatch.Stop();
            UpdateProgress((float)bytes / progress.Length,
                $"Downloading {desc}: {(float)bytes / 1024 / 1024 / ((float)(stopWatch.ElapsedMilliseconds > 0 ? stopWatch.ElapsedMilliseconds : 1) / 1000):F1}MB/s");
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

        progress.End();

        UpdateProgress(1.0f, "Installing update");
    }

    public async Task AsyncOnLoaded()
    {
        UpdateProgress(0.0f, "Checking for updates", false);
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

    private void UpdateProgress(float value, string desc, bool determinateValue = true)
    {
        _progressValue = value;
        this.RaisePropertyChanged(nameof(ProgressText));
        this.RaisePropertyChanged(nameof(ProgressValue));
        ProgressDesc = desc;
        DeterminateValue = determinateValue;
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