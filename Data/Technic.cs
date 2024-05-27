using System;
using System.IO;
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
    private readonly string _modpackZipPath = Path.Combine(DataManager.Instance.DataPath, "modpack.zip");

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
        return DownloadProgress.Download(_downloadUrl, _modpackZipPath, _httpClient);
    }

    public async Task<InstallProgress> InstallUpdate()
    {
        var mcDir = DataManager.Instance.MinecraftPath;
        var modDir = Path.Combine(mcDir, "mods");
        if (!Directory.Exists(mcDir)) Directory.CreateDirectory(mcDir);
        if (Directory.Exists(modDir)) Directory.Delete(modDir, true);
        return await InstallProgress.Install(mcDir, _modpackZipPath);
    }
}