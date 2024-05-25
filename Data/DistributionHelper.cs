using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace NewRGB.Data;

[SupportedOSPlatform(nameof(OSPlatform.Linux))]
public static class DistributionHelper
{
    public enum LinuxDistribution
    {
        Arch,
        Debian,
        RedHat,
        OpenSuse,
        Other
    }

    public static LinuxDistribution GetSystemDistribution()
    {
        const string binPath = "/usr/bin/";

        const string archPm = $"{binPath}pacman";
        const string debianPm = $"{binPath}apt";
        const string redHatPm1 = $"{binPath}yum";
        const string redHatPm2 = $"{binPath}dnf";
        const string openSusePm = $"{binPath}zypper";

        if (File.Exists(archPm)) return LinuxDistribution.Arch;
        if (File.Exists(debianPm)) return LinuxDistribution.Debian;
        if (File.Exists(redHatPm1) || File.Exists(redHatPm2)) return LinuxDistribution.RedHat;
        return File.Exists(openSusePm) ? LinuxDistribution.OpenSuse : LinuxDistribution.Other;
    }
}