using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace NewRGB.Data;

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
public static class JavaFinderLinux
{
    public static IEnumerable<string> FindJavaLinux()
    {
        var distribution = DistributionHelper.GetSystemDistribution();
        var jvmPath = string.Empty switch
        {
            _ when Directory.Exists("/usr/lib/jvm") => "/usr/lib/jvm",
            _ when Directory.Exists("/usr/lib64/jvm") => "/usr/lib64/jvm",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(jvmPath)) return Enumerable.Empty<string>();

        var subJvmDirectories = Directory.GetDirectories(jvmPath);

        return distribution switch
        {
            DistributionHelper.LinuxDistribution.Arch => FindJavaArchLinux(subJvmDirectories),
            DistributionHelper.LinuxDistribution.Debian => FindJavaDebianLinux(subJvmDirectories),
            DistributionHelper.LinuxDistribution.RedHat => FindJavaRedHatLinux(subJvmDirectories),
            DistributionHelper.LinuxDistribution.OpenSuse => FindJavaSuseLinux(subJvmDirectories),
            _ => FindJavaOtherLinux()
        };
    }

    private static IEnumerable<string> FindJavaArchLinux(string[] paths)
    {
        return
            from path in paths
            where !path.Contains("default", StringComparison.OrdinalIgnoreCase)
            select $"{path}/bin/java"
            into result
            where File.Exists(result)
            select result;
    }

    private static IEnumerable<string> FindJavaDebianLinux(string[] paths)
    {
        return
            from path in paths
            select $"{path}/bin/java"
            into result
            where File.Exists(result)
            select result;
    }

    private static IEnumerable<string> FindJavaRedHatLinux(string[] paths)
    {
        return
            from path in paths
            where !path.Contains("jre", StringComparison.OrdinalIgnoreCase)
            select $"{path}/bin/java"
            into result
            where File.Exists(result)
            select result;
    }

    private static IEnumerable<string> FindJavaSuseLinux(string[] paths)
    {
        return
            from path in paths
            where !path.Contains("jre", StringComparison.OrdinalIgnoreCase)
            select $"{path}/bin/java"
            into result
            where File.Exists(result)
            select result;
    }

    private static IEnumerable<string> FindJavaOtherLinux()
    {
        var jvmPath = string.Empty switch
        {
            _ when Directory.Exists("/usr/lib/jvm") => "/usr/lib/jvm",
            _ when Directory.Exists("/usr/lib32/jvm") => "/usr/lib32/jvm",
            _ => "/usr/lib64/jvm"
        };

        foreach (var path in Directory.GetDirectories(jvmPath))
        {
            var result = $"{path}/bin/java";

            if (File.Exists(result)) yield return result;
        }
    }
}