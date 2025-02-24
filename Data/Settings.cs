using System;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using ProjBobcat.Class.Helper;

namespace NewRGB.Data;

public class Settings
{
    [System.Text.Json.Serialization.JsonIgnore]
    public static readonly uint MaxMemoryValue =
        (uint)RoundToTwoPower(SystemInfoHelper.GetMemoryUsage()?
            .Total / 2.0 ?? 8192.0);

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
            MinMemory = MaxMemoryValue >= 8192 ? 8192u : 4096,
            MaxMemory = MaxMemoryValue >= 8192 ? 8192u : 4096,
        };
    }
}