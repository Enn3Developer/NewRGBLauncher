using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace NewRGB.Data;

public class DownloadProgress(byte[] buffer, FileStream fileStream, long length, Stream stream)
{
    private const int BufferSize = 4096;

    private static readonly Lazy<HttpClient> HttpClient = new(() =>
    {
        var httpClient = new HttpClient();

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (RGBCraft Launcher)");

        return httpClient;
    });

    public long Length { get; } = length;

    public static async Task<DownloadProgress?> Download(string url, string path, HttpClient? httpClient = null)
    {
        httpClient ??= HttpClient.Value;
        var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, BufferSize, true);
        try
        {
            var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength == null) throw new Exception("content length is null");
            var length = response.Content.Headers.ContentLength.Value;
            var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[BufferSize];
            return new DownloadProgress(buffer, fileStream, length, stream);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<long> Progress()
    {
        var bytesRead = await stream.ReadAsync(buffer);
        if (bytesRead == 0) return 0;
        await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
        return bytesRead;
    }

    public async Task End()
    {
        await fileStream.FlushAsync();
        fileStream.Close();
        await stream.DisposeAsync();
        await fileStream.DisposeAsync();
    }
}