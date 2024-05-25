using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NewRGB.Data;

public class DownloadProgress(byte[] buffer, FileStream fileStream, long length, Stream stream)
{
    public const int BufferSize = 4096;

    public long Length { get; } = length;

    public static async Task<DownloadProgress?> Download(string url, string path, HttpClient? httpClient = null)
    {
        httpClient ??= new HttpClient();
        var fileStream = File.OpenWrite(path);
        try
        {
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            if (response.Content.Headers.ContentLength == null) throw new Exception("content length is null");
            var length = response.Content.Headers.ContentLength.Value;
            var stream = await response.Content.ReadAsStreamAsync();
            var buffer = new byte[BufferSize];
            return new DownloadProgress(buffer, fileStream, length, stream);
        }
        catch (HttpRequestException requestException)
        {
            return null;
        }
    }

    public async Task<long> Progress()
    {
        var bytesRead = (long)await stream.ReadAsync(buffer);
        if (bytesRead == 0) return 0;
        await fileStream.WriteAsync(buffer);
        return bytesRead;
    }

    public async Task End()
    {
        await fileStream.FlushAsync();
        fileStream.Close();
    }
}