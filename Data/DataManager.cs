using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ProjBobcat;
using ProjBobcat.Class.Helper;
using ProjBobcat.DefaultComponent.Launch;
using ProjBobcat.Interface;

namespace NewRGB.Data;

public class DataManager
{
    public static DataManager Instance { get; } = new();

    public string DataPath { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RGBcraft");

    public ILauncherAccountParser? LauncherAccountParser { get; private set; }

    public void InitData(ILauncherAccountParser launcherAccountParser)
    {
        LauncherAccountParser = launcherAccountParser;
        if (!Directory.Exists(DataPath)) Directory.CreateDirectory(DataPath);
    }

    public ILauncherAccountParser DefaultLauncherAccountParser()
    {
        return new DefaultLauncherAccountParser(DataPath, Guid.Empty);
    }

    public bool HasAccount()
    {
        return LauncherAccountParser is { LauncherAccount.ActiveAccountLocalId: not null };
    }

    public static async IAsyncEnumerable<string> FindJava(bool fullSearch = false)
    {
        var result = new HashSet<string>();

        if (fullSearch)
            await foreach (var path in DeepJavaSearcher.DeepSearch())
                result.Add(path);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            foreach (var path in JavaFinderWindows.FindJavaWindows())
                result.Add(path);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            foreach (var path in JavaFinderMacOS.FindJavaMacOS())
                result.Add(path);
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            foreach (var path in JavaFinderLinux.FindJavaLinux())
                result.Add(path);

        foreach (var path in result)
            yield return path;

        var evJava = FindJavaUsingEnvironmentVariable();

        if (!string.IsNullOrEmpty(evJava))
            yield return Path.Combine(evJava, Constants.JavaExecutablePath);
    }

    private static string? FindJavaUsingEnvironmentVariable()
    {
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            var javaHome = configuration["JAVA_HOME"];
            return javaHome;
        }
        catch (Exception)
        {
            return null;
        }
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