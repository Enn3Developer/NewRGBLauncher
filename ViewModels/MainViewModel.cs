using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reactive;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using mcswlib.ServerStatus;
using NewRGB.Data;
using NewRGB.Views;
using ProjBobcat.Class.Helper;
using ProjBobcat.Class.Model;
using ProjBobcat.Class.Model.LauncherProfile;
using ProjBobcat.Class.Model.Mojang;
using ProjBobcat.DefaultComponent;
using ProjBobcat.DefaultComponent.Authenticator;
using ProjBobcat.DefaultComponent.Installer.ForgeInstaller;
using ProjBobcat.DefaultComponent.ResourceInfoResolver;
using ProjBobcat.Interface;
using ReactiveUI;
using Velopack;
using Velopack.Sources;
using FileInfo = System.IO.FileInfo;

namespace NewRGB.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly Technic _technic = new("newrgb");
    private readonly UpdateManager _updateManager;
    private Bitmap? _background;
    private bool _determinateValue = true;
    private Process? _gameProcess;
    private bool _isPlayEnabled;
    private bool _isWayland;
    private Task? _launcherAssetsTask;
    private bool _needsJava;
    private bool _needsUpdate;
    private string _playText = "Play";
    private Bitmap? _profileAvatar;
    private string _progressDesc = "Ready";
    private float _progressValue = 1.0f;
    private string _serverInfo = "0/20";
    private UpdateInfo? _updateInfo;
    private bool _visibleProgress = true;

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
        _updateManager =
            new UpdateManager(new GithubSource("https://github.com/rgbcraft/NewRGBLauncher/", null, false));
    }

    public ReactiveCommand<Unit, Unit> LogoCommand { get; }
    public string Username { get; }

    public string PlayText
    {
        get => _playText;
        private set => this.RaiseAndSetIfChanged(ref _playText, value);
    }

    public string ProgressText => _visibleProgress ? $"{_progressValue:P1}" : "";
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

    public Bitmap? Background
    {
        get => _background;
        private set => this.RaiseAndSetIfChanged(ref _background, value);
    }

    public string ServerInfo
    {
        get => _serverInfo;
        private set => this.RaiseAndSetIfChanged(ref _serverInfo, value);
    }

    public bool IsPlayEnabled
    {
        get => _isPlayEnabled;
        private set => this.RaiseAndSetIfChanged(ref _isPlayEnabled, value);
    }

    public Bitmap? ProfileAvatar
    {
        get => _profileAvatar;
        private set => this.RaiseAndSetIfChanged(ref _profileAvatar, value);
    }

    private async Task<string?> CheckJava()
    {
        UpdateProgress(0.0f, "Checking Java version", false);
        var possibleJavaPath =
            Path.Combine(DataManager.Instance.DataPath, "runtime", "jdk-21.0.3+9-jre", "bin");
        possibleJavaPath = Path.Combine(possibleJavaPath,
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "java.exe" : "java");
        if (File.Exists(possibleJavaPath))
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                new FileInfo(possibleJavaPath).GetAccessControl().AddAccessRule(new FileSystemAccessRule(
                    Environment.UserName, FileSystemRights.ReadAndExecute,
                    AccessControlType.Allow));
            else
                File.SetUnixFileMode(possibleJavaPath,
                    File.GetUnixFileMode(possibleJavaPath) | UnixFileMode.UserExecute);

            if (await DataManager.IsValidJava(possibleJavaPath))
                return possibleJavaPath;
        }

        var javaList = SystemInfoHelper.FindJava();
        var javaEnumerator = javaList.GetAsyncEnumerator();
        var valid = false;
        while (await javaEnumerator.MoveNextAsync())
        {
            if (!await DataManager.IsValidJava(javaEnumerator.Current)) continue;
            valid = true;
            break;
        }

        return valid ? javaEnumerator.Current : null;
    }

    private async Task CheckForge(string javaPath)
    {
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
            var isLegacy =
                ForgeInstallerFactory.IsLegacyForgeInstaller(DataManager.Instance.ForgeInstallerPath, "43.3.13");
            IForgeInstaller forgeInstaller = isLegacy
                ? new LegacyForgeInstaller
                {
                    ForgeExecutablePath = DataManager.Instance.ForgeInstallerPath,
                    RootPath = DataManager.Instance.MinecraftPath,
                    ForgeVersion = "43.3.13",
                    InheritsFrom = "1.19.2"
                }
                : new HighVersionForgeInstaller
                {
                    ForgeExecutablePath = DataManager.Instance.ForgeInstallerPath,
                    JavaExecutablePath = javaPath,
                    RootPath = DataManager.Instance.MinecraftPath,
                    VersionLocator = DataManager.Instance.GameCoreBase?.VersionLocator!,
                    DownloadUrlRoot = "https://bmclapi2.bangbang93.com/",
                    MineCraftVersion = "1.19.2",
                    MineCraftVersionId = "1.19.2",
                    InheritsFrom = "1.19.2"
                };
            ((InstallerBase)forgeInstaller).StageChangedEventDelegate += (_, args) =>
            {
                UpdateProgress((float)args.Progress, "Installing Forge");
            };
            await forgeInstaller.InstallForgeTaskAsync();
            UpdateProgress(1.0f, "Installing Forge");
        }

        UpdateProgress(0.0f, "Checking Forge installation", false);
        await CompleteMinecraft("1.19.2-forge-43.3.13");
    }

    private async Task CheckMinecraft()
    {
        UpdateProgress(0.0f, "Checking Minecraft. This may take a while", false);
        var httpClient = new HttpClient();
        var versionsPath = Path.Combine(DataManager.Instance.MinecraftPath, "versions");
        var versionDir = Path.Combine(versionsPath, "1.19.2");
        var versionFile = Path.Combine(versionDir, "1.19.2.json");
        if (!Directory.Exists(versionsPath)) Directory.CreateDirectory(versionsPath);
        if (!Directory.Exists(versionDir)) Directory.CreateDirectory(versionDir);
        if (!File.Exists(versionFile))
        {
            var responseVersion = await httpClient.GetAsync(
                "https://piston-meta.mojang.com/v1/packages/ed548106acf3ac7e8205a6ee8fd2710facfa164f/1.19.2.json");
            responseVersion.EnsureSuccessStatusCode();
            await File.WriteAllTextAsync(versionFile, await responseVersion.Content.ReadAsStringAsync());
        }

        await CompleteMinecraft("1.19.2");
    }

    private async Task CompleteMinecraft(string gameId)
    {
        var httpClient = new HttpClient();
        var response =
            await httpClient.GetAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json");
        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync();
        var versionManifest =
            JsonSerializer.Deserialize<VersionManifest>(responseJson);
        var versionInfo = DataManager.Instance.GameCoreBase?.VersionLocator.GetGame(gameId);
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
    }

    private async Task Launch(string javaPath)
    {
        var memory = (uint)(SystemInfoHelper.GetMemoryUsage()?.Total > 11000 ? 8192 : 4096);
        var customGlfw = Path.Combine(DataManager.Instance.DataPath, "glfw.so");
        var additionalJvmArguments = new List<string>();
        if (_isWayland && File.Exists(customGlfw) && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            additionalJvmArguments.Add($"-Dorg.lwjgl.glfw.libname={customGlfw}");
            if (await IsNvidia())
                Environment.SetEnvironmentVariable("__GL_THREADED_OPTIMIZATIONS", "0",
                    EnvironmentVariableTarget.Process);
        }

        var launchSettings = new LaunchSettings
        {
            Authenticator = new OfflineAuthenticator
            {
                Username = Username,
                LauncherAccountParser = DataManager.Instance.LauncherAccountParser!
            },
            GameName = "RGBcraft",
            WindowTitle = "RGBcraft",
            FallBackGameArguments =
                new
                    GameArguments // Default game arguments for all games in .minecraft/ as the fallback of specific game launch.
                    {
                        GcType = GcType.G1Gc, // GC type
                        JavaExecutable = javaPath, //The path of Java executable
                        Resolution = new ResolutionModel // Game Window's Resolution
                        {
                            Height = 480, // Height
                            Width = 840 // Width
                        },
                        MinMemory = memory, // Minimal Memory
                        MaxMemory = memory // Maximum Memory
                    },
            GameArguments = new GameArguments
            {
                AdditionalJvmArguments = additionalJvmArguments,
                JavaExecutable = javaPath,
                MaxMemory = memory,
                GcType = GcType.G1Gc
            },
            Version = "1.19.2-forge-43.3.13", // The version ID of the game to launch, such as 1.7.10 or 1.15.2
            VersionInsulation = false, // Version Isolation
            GameResourcePath = DataManager.Instance.MinecraftPath, // Root path of the game resource(.minecraft/)
            GamePath = Path.Combine(DataManager.Instance.MinecraftPath,
                "versions"), // Root path of the game (.minecraft/versions/)
            VersionLocator = DataManager.Instance.GameCoreBase?.VersionLocator! // Game's version locator
        };

        var result = await DataManager.Instance.GameCoreBase?.LaunchTaskAsync(launchSettings)!;
        Console.WriteLine($"Has exited: {result.GameProcess?.HasExited}");
        _gameProcess = result.GameProcess;
        if (_gameProcess == null) return;
        _gameProcess.ErrorDataReceived += (_, args) => { Console.Error.WriteLine(args.Data); };
        _gameProcess.OutputDataReceived += (_, args) => { Console.WriteLine(args.Data); };
    }

    private async Task InstallJava()
    {
        UpdateProgress(0.0f, "Downloading Java");
        DownloadProgress? progress = null;
        string? path = null;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            path = Path.Combine(DataManager.Instance.DataPath, "java.tar");
            progress = await DownloadProgress.Download(
                "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.3%2B9/OpenJDK21U-jre_x64_linux_hotspot_21.0.3_9.tar.gz",
                path);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            path = Path.Combine(DataManager.Instance.DataPath, "java.tar");
            progress = await DownloadProgress.Download(
                "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.3%2B9/OpenJDK21U-jre_aarch64_mac_hotspot_21.0.3_9.tar.gz",
                path);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            path = Path.Combine(DataManager.Instance.DataPath, "java.zip");
            progress = await DownloadProgress.Download(
                "https://github.com/adoptium/temurin21-binaries/releases/download/jdk-21.0.3%2B9/OpenJDK21U-jre_x64_windows_hotspot_21.0.3_9.zip",
                path);
        }

        if (progress == null || path == null)
        {
            UpdateProgress(1.0f, "Can't download java");
            return;
        }

        await DownloadAndProgress(progress, "Java");
        UpdateProgress(0.0f, "Installing Java", false);
        var installProgress =
            await InstallProgress.Install(Path.Combine(DataManager.Instance.DataPath, "runtime"), path);
        await installProgress.Run();
        await installProgress.End();
        UpdateProgress(1.0f, "Java installed");
        _needsJava = false;
        PlayText = "Play";
    }

    private async Task CheckWaylandWorkaround()
    {
        UpdateProgress(0.0f, "Checking Wayland workaround", false);
        var customGlfw = Path.Combine(DataManager.Instance.DataPath, "glfw.so");
        if (File.Exists(customGlfw)) return;
        UpdateProgress(0.0f, "Downloading GLFW");
        var progress = await DownloadProgress.Download("https://files.enn3.ovh/libglfw-wayland.so", customGlfw);
        if (progress == null)
        {
            UpdateProgress(1.0f, "Can't download GLFW");
            await Task.Delay(1000);
            return;
        }

        await DownloadAndProgress(progress, "GLFW");
    }

    [SupportedOSPlatform(nameof(OSPlatform.Linux))]
    private static async Task<bool> IsWayland()
    {
        var process = Process.Start(new ProcessStartInfo("bash", [
            "-c",
            "loginctl show-session $(loginctl | grep $(whoami) | awk '{print $1}') -p Type"
        ])
        {
            RedirectStandardOutput = true
        });
        if (process == null) return false;
        await process.WaitForExitAsync();
        return (await process.StandardOutput.ReadLineAsync())?.Split('=')[1] == "wayland";
    }

    [SupportedOSPlatform(nameof(OSPlatform.Linux))]
    private static async Task<bool> IsNvidia()
    {
        var process = Process.Start(new ProcessStartInfo("bash", [
            "-c",
            "lspci | grep VGA | cut -d \":\" -f3"
        ])
        {
            RedirectStandardOutput = true
        });
        if (process == null) return false;
        await process.WaitForExitAsync();
        return (await process.StandardOutput.ReadLineAsync())?.Contains("NVIDIA") ?? false;
    }

    private async Task PlayButtonRun()
    {
        if (_gameProcess != null)
        {
            if (_gameProcess.HasExited) _gameProcess = null;
            else return;
        }

        if (_updateInfo != null)
        {
            UpdateProgress(0.0f, "Updating launcher");
            await _updateManager.DownloadUpdatesAsync(_updateInfo,
                i => UpdateProgress((float)i / 100, "Updating launcher"));
            UpdateProgress(1.0f, "Restarting launcher", false);
            _updateManager.ApplyUpdatesAndRestart(_updateInfo);
        }
        else if (_needsJava)
        {
            await InstallJava();
        }
        else if (_needsUpdate)
        {
            if (!await DownloadUpdate()) return;
            await InstallUpdate();
            await _technic.SaveVersion();
            _needsUpdate = false;
            UpdateProgress(1.0f, "Ready: Update installed");
            PlayText = "Play";
        }
        else
        {
            var javaPath = await CheckJava();
            if (javaPath == null)
            {
                UpdateProgress(1.0f, "No valid java installation found");
                PlayText = "Install";
                _needsJava = true;
                return;
            }

            await CheckMinecraft();
            await CheckForge(javaPath);
            if (_isWayland) await CheckWaylandWorkaround();
            UpdateProgress(1.0f, "Launching RGBcraft", false);
            await Launch(javaPath);
            await Task.Delay(25000);
            UpdateProgress(1.0f, "Hiding launcher", false);
            await Task.Delay(2000);
            if (MainWindow.Instance != null) MainWindow.Instance.WindowState = WindowState.Minimized;
            UpdateProgress(1.0f, "Ready");
        }
    }

    private async Task OnPlayButton()
    {
        IsPlayEnabled = false;
        await PlayButtonRun();
        if (_launcherAssetsTask != null)
        {
            await _launcherAssetsTask;
            _launcherAssetsTask = null;
        }

        IsPlayEnabled = true;
    }

    private async Task<bool> DownloadUpdate()
    {
        UpdateProgress(0.0f, "Starting update", false);
        var progress = await _technic.DownloadUpdate();
        if (progress == null)
        {
            UpdateProgress(1.0f, "Failed to start update. Please retry");
            return false;
        }

        await DownloadAndProgress(progress, "update");
        return true;
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
        UpdateProgress(0.0f, "Installing update", false);
        await progress.Run();

        await progress.End();

        UpdateProgress(1.0f, "Update installed");
    }

    private async Task DownloadProfileAvatar(string username)
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (X11; Linux x86_64; rv:126.0) Gecko/20100101 Firefox/126.0");
        var path = Path.Combine(DataManager.Instance.DataPath, "avatar.png");
        var response = await httpClient.GetAsync($"http://skins.rgbcraft.com/api/helm/{username}/48");
        if (!response.IsSuccessStatusCode || (await response.Content.ReadAsStringAsync()).Contains("404"))
        {
            if (username != "SteveDoNotDelete")
                await DownloadProfileAvatar("SteveDoNotDelete");
            return;
        }

        await using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 1024, true))
        {
            await response.Content.CopyToAsync(stream);
        }

        response.Dispose();

        await using (var stream = File.OpenRead(path))
        {
            ProfileAvatar = Bitmap.DecodeToWidth(stream, 48);
        }
    }

    private void CheckBackgrounds()
    {
        var backgrounds = Path.Combine(DataManager.Instance.DataPath, "backgrounds");
        if (Directory.Exists(backgrounds) && Directory.GetFiles(backgrounds).Length == 16)
        {
            var rng = new Random();
            var background = rng.Next(16);
            var path = Path.Combine(backgrounds, $"{background}.png");
            if (!File.Exists(path)) return;
            Background = new Bitmap(path);
            return;
        }

        Background = new Bitmap(AssetLoader.Open(new Uri("avares://NewRGB/Assets/0.png")));
        if (!Directory.Exists(backgrounds)) Directory.CreateDirectory(backgrounds);
        _launcherAssetsTask = Task.Run(async () =>
        {
            var httpClient = new HttpClient();
            for (var i = 0; i < 16; i++)
            {
                var response = await httpClient.GetAsync($"https://newrgb.enn3.ovh/background/{i}");
                if (!response.IsSuccessStatusCode) return;
                await using var stream = File.OpenWrite(Path.Combine(backgrounds, $"{i}.png"));
                await response.Content.CopyToAsync(stream);
            }
        });
    }

    private async Task OnLoaded()
    {
        UpdateProgress(0.0f, "Checking for launcher updates", false);
        CheckBackgrounds();
        var profileAvatarPath = Path.Combine(DataManager.Instance.DataPath, "avatar.png");
        if (File.Exists(profileAvatarPath))
        {
            await using var stream = File.OpenRead(profileAvatarPath);
            ProfileAvatar = Bitmap.DecodeToWidth(stream, 48);
        }

        await DownloadProfileAvatar(Username);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && await IsWayland()) _isWayland = true;

        var factory = new ServerStatusFactory();
        var inst = factory.Make("kamino.a-centauri.com", 25565, false, "One");
        var srv = await Task.Run(() => inst.Updater.Ping());
        if (srv != null) ServerInfo = $"{srv.CurrentPlayerCount}/{srv.MaxPlayerCount}";
        try
        {
            _updateInfo = await _updateManager.CheckForUpdatesAsync();
            if (_updateInfo != null)
            {
                UpdateProgress(1.0f, "Found launcher updates");
                PlayText = "Update";
                return;
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        UpdateProgress(0.0f, "Checking for updates", false);
        if (!await _technic.Init()) UpdateProgress(1.0f, "Can't download info from technic");

        _needsUpdate = await _technic.CheckUpdate();
        if (_needsUpdate)
        {
            PlayText = "Update";
            UpdateProgress(1.0f, "Update available");
        }
        else
        {
            UpdateProgress(1.0f, "Ready");
        }
    }

    public async Task AsyncOnLoaded()
    {
        await OnLoaded();
        IsPlayEnabled = true;
    }

    private void UpdateProgress(float value, string desc, bool determinateValue = true)
    {
        _progressValue = value;
        _visibleProgress = determinateValue;
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