using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ProjBobcat;

namespace NewRGB.Data;

[SupportedOSPlatform(nameof(OSPlatform.OSX))]
public class JavaFinderMacOS
{
    public static IEnumerable<string> FindJavaMacOS()
    {
        const string rootPath = "/Library/Java/JavaVirtualMachines";

        foreach (var dir in Directory.EnumerateDirectories(rootPath))
        {
            var filePath = $"{dir}/{Constants.JavaExecutablePath}";
            if (!File.Exists(filePath)) continue;
            var flag = File.GetUnixFileMode(filePath);

            if (flag.HasFlag(UnixFileMode.GroupExecute) && flag.HasFlag(UnixFileMode.UserExecute))
                yield return filePath;
        }
    }
}