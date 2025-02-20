using System.ComponentModel;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace NewRGB.Data;

public class Settings
{
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [JsonPropertyName("minMemory")]
    [DefaultValue(4096)]
    public uint MinMemory { get; set; }

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [JsonPropertyName("maxMemory")]
    [DefaultValue(4096)]
    public uint MaxMemory { get; set; }
}

[JsonSerializable(typeof(Settings))]
public partial class SettingsSerializerContext : JsonSerializerContext
{
}