using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace NewRGB.Data;

[SupportedOSPlatform(nameof(OSPlatform.Windows))]
public class JavaFinderWindows
{
    public static IEnumerable<string> FindJavaWindows()
    {
        try
        {
            using var rootReg = Registry.LocalMachine.OpenSubKey("SOFTWARE");

            if (rootReg == null) return [];

            using var wow64Reg = rootReg.OpenSubKey("Wow6432Node");

            var javas = FindJavaInternal(rootReg)
                .Union(FindJavaInternal(wow64Reg))
                .ToHashSet();

            return javas;
        }
        catch
        {
            return [];
        }
    }

    public static IEnumerable<string> FindJavaInternal(RegistryKey? registry)
    {
        if (registry == null) return [];

        try
        {
            using var regKey = registry.OpenSubKey("JavaSoft");
            using var javaRuntimeReg = regKey?.OpenSubKey("Java Runtime Environment");

            if (javaRuntimeReg == null)
                return [];

            var result = new List<string>();
            foreach (var ver in javaRuntimeReg.GetSubKeyNames())
            {
                var versions = javaRuntimeReg.OpenSubKey(ver);
                var javaHomes = versions?.GetValue("JavaHome");

                if (javaHomes == null) continue;

                var str = javaHomes?.ToString();

                if (string.IsNullOrWhiteSpace(str)) continue;

                result.Add($@"{str}\bin\javaw.exe");
            }

            return result;
        }
        catch
        {
            return Enumerable.Empty<string>();
        }
    }
}