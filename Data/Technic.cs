using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Path = System.IO.Path;

namespace NewRGB.Data;

public class Technic(string modpackName)
{
    private const int BufferSize = 4096;
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
        dynamic json = JObject.Parse(responseBody);
        _version = json.version;
        _downloadUrl = json.url;
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

    public async Task<DownloadProgress?> DownloadUpdate()
    {
        var path = Path.Combine(DataManager.Instance.DataPath, "modpack.zip");
        var fileStream = File.OpenWrite(path);
        try
        {
            var response = await _httpClient.GetAsync(_downloadUrl);


            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength == null) throw new Exception("content length is null");
            var length = response.Content.Headers.ContentLength.Value;
            Console.WriteLine($"length: {length}");
            var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[BufferSize];
            return new DownloadProgress(buffer, fileStream, length, stream);
        }
        catch (HttpRequestException requestException)
        {
            return null;
        }
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

public class DownloadProgress(byte[] buffer, FileStream fileStream, long length, Stream stream)
{
    public long Length { get; } = length;

    public async Task<long> Progress()
    {
        var bytesRead = (long)await stream.ReadAsync(buffer);
        if (bytesRead == 0) return 0;
        Console.WriteLine($"downloaded: {bytesRead}");
        await fileStream.WriteAsync(buffer);
        return bytesRead;
    }

    public async Task End()
    {
        await fileStream.FlushAsync();
        fileStream.Close();
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
}