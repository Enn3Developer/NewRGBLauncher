using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace NewRGB.Data;

public class Technic(string modpackName)
{
    private readonly HttpClient _httpClient = new();
    private const int BuildVersion = 69420;
    private string _version = "";
    private string _downloadUrl = "";

    public async Task Init()
    {
        var response =
            await _httpClient.GetAsync($"https://api.technicpack.net/modpack/{modpackName}?build={BuildVersion}");
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync();
        var json = JsonNode.Parse(responseBody) ?? throw new Exception("can't parse json");
        _version = json["version"]!.GetValue<string>();
        _downloadUrl = json["url"]!.GetValue<string>();
    }

    public async Task<bool> CheckUpdate()
    {
        var path = Path.Combine(DataManager.Instance.DataPath, "version");
        if (!File.Exists(path)) return true;
        var version = await File.ReadAllTextAsync(path);
        return version != _version;
    }

    public async Task SaveVersion()
    {
        var path = Path.Combine(DataManager.Instance.DataPath, "version");
        await File.WriteAllTextAsync(path, _version);
    }

    public Task<DownloadProgress?> DownloadUpdate()
    {
        var path = Path.Combine(DataManager.Instance.DataPath, "modpack.zip");
        return DownloadProgress.Download(_downloadUrl, path, _httpClient);
    }

    public async Task<InstallProgress> InstallUpdate()
    {
        var zippedFile = Path.Combine(DataManager.Instance.DataPath, "modpack.zip");
        var mcDir = Path.Combine(DataManager.Instance.DataPath, ".minecraft");
        var modDir = Path.Combine(mcDir, "mods");
        if (!Directory.Exists(mcDir)) Directory.CreateDirectory(mcDir);
        if (Directory.Exists(modDir)) Directory.Delete(modDir, true);
        var archive = await Task.Run(() => ZipFile.OpenRead(zippedFile));
        var fileList = new List<ZipArchiveEntry>(256);
        fileList.AddRange(archive.Entries);
        return new InstallProgress(fileList, mcDir);
    }
}

public class InstallProgress(List<ZipArchiveEntry> fileList, string mcDir)
{
    public int Length => fileList.Count;

    public async Task Progress(int i)
    {
        if (i >= Length) return;
        var file = fileList[i];
        var path = Path.Combine(mcDir, file.FullName.Replace('/', Path.DirectorySeparatorChar));
        if (path.EndsWith(Path.DirectorySeparatorChar)) Directory.CreateDirectory(path);
        else await Task.Run(() => file.ExtractToFile(path));
    }

    public void End()
    {
        var zipPath = Path.Combine(DataManager.Instance.DataPath, "modpack.zip");
        fileList.Clear();
        File.Delete(zipPath);
    }
}