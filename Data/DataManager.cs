using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using ProjBobcat.Class.Model.LauncherProfile;
using ProjBobcat.DefaultComponent.Launch;
using ProjBobcat.DefaultComponent.Launch.GameCore;
using ProjBobcat.DefaultComponent.Logging;
using ProjBobcat.Exceptions;
using ProjBobcat.Interface;

namespace NewRGB.Data;

public class DataManager
{
    public static DataManager Instance { get; } = new();

    public string DataPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RGBcraft");

    public string MinecraftPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RGBcraft",
            ".minecraft");

    public string ForgeInstallerPath { get; private set; } = "";
    public ILauncherAccountParser? LauncherAccountParser { get; private set; }
    public ILauncherProfileParser? LauncherProfileParser { get; private set; }

    public GameCoreBase? GameCoreBase { get; private set; }

    public void InitData(ILauncherAccountParser launcherAccountParser, ILauncherProfileParser launcherProfileParser)
    {
        LauncherAccountParser = launcherAccountParser;
        LauncherProfileParser = launcherProfileParser;
        try
        {
            LauncherProfileParser.GetGameProfile("RGBcraft");
        }
        catch (UnknownGameNameException)
        {
            LauncherProfileParser.AddNewGameProfile(new GameProfileModel
            {
                Name = "RGBcraft",
                Resolution = new ResolutionModel
                {
                    Height = 600,
                    Width = 800
                }
            });
        }

        ForgeInstallerPath = Path.Combine(DataPath, "forge_installer.jar");
        if (!Directory.Exists(DataPath)) Directory.CreateDirectory(DataPath);
        if (!Directory.Exists(MinecraftPath)) Directory.CreateDirectory(MinecraftPath);
        var versionsDir = Path.Combine(MinecraftPath, "versions");
        var forgeDir = Path.Combine(versionsDir, "1.19.2-forge-43.3.13");
        if (!Directory.Exists(versionsDir)) Directory.CreateDirectory(versionsDir);
        if (!Directory.Exists(forgeDir)) Directory.CreateDirectory(forgeDir);
        var clientToken = Guid.Empty;
        GameCoreBase = new DefaultGameCore
        {
            ClientToken = clientToken,
            RootPath = MinecraftPath,
            VersionLocator = new DefaultVersionLocator(MinecraftPath, clientToken)
            {
                LauncherAccountParser = LauncherAccountParser,
                LauncherProfileParser = LauncherProfileParser
            },
            GameLogResolver = new DefaultGameLogResolver()
        };
    }

    public bool IsForgeInstalled()
    {
        return File.Exists(ForgeInstallerPath);
    }

    public Task<DownloadProgress?> DownloadForge()
    {
        return DownloadProgress.Download("https://files.enn3.ovh/forge-1.19.2-43.3.13-installer.jar",
            ForgeInstallerPath);
    }

    public ILauncherAccountParser DefaultLauncherAccountParser()
    {
        return new DefaultLauncherAccountParser(MinecraftPath, Guid.Empty);
    }

    public ILauncherProfileParser DefaultLauncherProfileParser()
    {
        return new DefaultLauncherProfileParser(MinecraftPath, Guid.Empty);
    }

    public bool HasAccount()
    {
        return LauncherAccountParser is { LauncherAccount.ActiveAccountLocalId: not null };
    }

    public static async Task<bool> IsValidJava(string javaPath)
    {
        var process = Process.Start(new ProcessStartInfo(javaPath, "-version")
            { RedirectStandardOutput = true, RedirectStandardError = true });
        if (process == null)
        {
            Console.WriteLine("NO PROCESS FOUND");
            return false;
        }

        var firstLine = await process.StandardError.ReadLineAsync();
        if (firstLine == null) return false;
        var firstIndex = firstLine.IndexOf('"') + 1;
        var versionString = firstLine.Substring(firstIndex, firstLine.LastIndexOf('"') - firstIndex);
        var versionNumbers = versionString.Split('.');
        await process.WaitForExitAsync();
        return int.Parse(versionNumbers[0]) >= 17;
    }
}