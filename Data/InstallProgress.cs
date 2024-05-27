using System.IO;
using System.Threading.Tasks;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace NewRGB.Data;

public class InstallProgress(string dir, FileStream fileStream, IReader reader, string zip)
{
    public static async Task<InstallProgress> Install(string dir, string zip)
    {
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        var fileStream = File.OpenRead(zip);
        var reader = await Task.Run(() => ReaderFactory.Open(fileStream));
        return new InstallProgress(dir, fileStream, reader, zip);
    }

    public async Task Run()
    {
        var opt = new ExtractionOptions
        {
            ExtractFullPath = true,
            Overwrite = true
        };
        while (await Task.Run(reader.MoveToNextEntry))
            if (!reader.Entry.IsDirectory)
                await Task.Run(() => reader.WriteEntryToDirectory(dir, opt));
    }

    public async Task End()
    {
        reader.Dispose();
        await fileStream.DisposeAsync();
        File.Delete(zip);
    }
}