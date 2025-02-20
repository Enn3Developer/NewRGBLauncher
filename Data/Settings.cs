using System.Text.Json.Serialization;

namespace NewRGB.Data;

public class Settings
{
    [JsonPropertyName("minMemory")] public uint MinMemory { get; set; } = 4096;
    [JsonPropertyName("maxMemory")] public uint MaxMemory { get; set; } = 4096;
}

[JsonSerializable(typeof(Settings))]
public partial class SettingsSerializerContext : JsonSerializerContext
{
}