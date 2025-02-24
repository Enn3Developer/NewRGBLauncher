using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using ProjBobcat.Class.Helper;
using ProjBobcat.Class.Model;

namespace NewRGB.Data;

public class Settings
{
    [System.Text.Json.Serialization.JsonIgnore]
    public readonly uint MaxMemoryValue =
        (uint)(RoundToTwoPower((SystemInfoHelper.GetMemoryUsage() ?? new MemoryInfo(8192.0 * 2.0, 0.0, 0.0, 0.0))
            .Total) / 2.0);

    [System.Text.Json.Serialization.JsonIgnore]
    public const uint MinMemoryValue = 4096;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [JsonPropertyName("minMemory")]
    [DefaultValue(4096)]
    public uint MinMemory { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [JsonPropertyName("maxMemory")]
    [DefaultValue(4096)]
    public uint MaxMemory { get; set; }

    private static double RoundToTwoPower(double n)
    {
        var logTwo = Math.Ceiling(Math.Log2(n));
        return Math.Pow(2.0, logTwo);
    }

    public static Settings Default()
    {
        return new Settings
        {
            MinMemory = 4096,
            MaxMemory = 4096
        };
    }
}