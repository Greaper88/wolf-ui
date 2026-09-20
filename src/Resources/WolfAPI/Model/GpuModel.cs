using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace Resources.WolfAPI;
public class GpuDevice
{
    [JsonInclude, JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonInclude, JsonPropertyName("render_node")] public string RenderNode { get; set; } = "";
    [JsonInclude, JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonInclude, JsonPropertyName("accessible")] public bool Accessible { get; set; }
    [JsonInclude, JsonPropertyName("vram_bytes"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] public ulong? VramBytes { get; set; }
    [JsonInclude, JsonPropertyName("vram_used_bytes"), JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)] public ulong? VramUsedBytes { get; set; }
    [JsonInclude, JsonPropertyName("gpu_percent")] public double? GpuPercent { get; set; }
}
public class GpuInfo
{
    [JsonInclude, JsonPropertyName("device")] public GpuDevice Device { get; set; } = new();
    [JsonInclude, JsonPropertyName("codecs")] public bool[] Codecs { get; set; } = [];
    [JsonInclude, JsonPropertyName("users")] public uint Users { get; set; }
    [JsonInclude, JsonPropertyName("apps")] public uint Apps { get; set; }
    public bool Supports(int codec) => Device.Accessible && codec >= 0 && codec < Codecs.Length && Codecs[codec];
}
public class GpusResponse
{
    [JsonInclude, JsonPropertyName("success")] public bool Success { get; set; }
    [JsonInclude, JsonPropertyName("gpus")] public List<GpuInfo> Gpus { get; set; } = [];
}
