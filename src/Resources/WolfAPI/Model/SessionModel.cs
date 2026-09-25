using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI;

public class Session
{
    [JsonInclude, JsonPropertyName("gpu")]
    public SessionGpu? Gpu {get;set;}
    [JsonInclude, JsonPropertyName("app_id")]
    public string? AppId {get;set;}
    [JsonInclude, JsonPropertyName("client_id")]
    public string? ClientId {get;set;}
    [JsonInclude, JsonPropertyName("client_ip")]
    public string? ClientIp {get;set;}
    [JsonInclude, JsonPropertyName("video_width")]
    public int VideoWidth {get;set;}
    [JsonInclude, JsonPropertyName("video_height")]
    public int VideoHeight {get;set;}
    [JsonInclude, JsonPropertyName("video_refresh_rate")]
    public int VideoRefreshRate {get;set;}
    [JsonInclude, JsonPropertyName("audio_channel_count")]
    public int AudioChannelCount {get;set;}
    [JsonInclude, JsonPropertyName("client_settings")]
    public ClientSettings? ClientSettings {get;set;}
}

public class SessionsResponse
{
    [JsonInclude, JsonPropertyName("success")]
    public bool Success {get;set;}
    [JsonInclude, JsonPropertyName("sessions")]
    public List<Session>? Sessions {get;set;}
}

// Optional additive contract: older Wolf servers omit it and the status line stays hidden.
[JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
public class SessionGpu
{
    [JsonInclude, JsonPropertyName("id")]
    public string? Id {get;set;}
    [JsonInclude, JsonPropertyName("name")]
    public string? Name {get;set;}
    [JsonInclude, JsonPropertyName("render_node")]
    public string? RenderNode {get;set;}
    [JsonInclude, JsonPropertyName("encoder_node")]
    public string? EncoderNode {get;set;}
    [JsonInclude, JsonPropertyName("vram_bytes")]
    public ulong? VramBytes {get;set;}
    [JsonInclude, JsonPropertyName("encoder_percent")]
    public double? EncoderPercent {get;set;}
    [JsonInclude, JsonPropertyName("gpu_percent")]
    public double? GpuPercent {get;set;}
    [JsonInclude, JsonPropertyName("codec")]
    public string? Codec {get;set;}
    [JsonInclude, JsonPropertyName("projected_encoder_percent")]
    public double? ProjectedEncoderPercent {get;set;}
    [JsonInclude, JsonPropertyName("session_count_on_gpu")]
    public int SessionCountOnGpu {get;set;}

    [JsonInclude, JsonPropertyName("stream_error")]
    public string? StreamError {get;set;}

    public string StatusText()
    {
        if (!string.IsNullOrEmpty(StreamError)) return StreamError;
        var vram = VramBytes.HasValue ? $"{VramBytes.Value / (1024.0 * 1024 * 1024):0.#} GiB VRAM" : "VRAM unknown";
        // Older servers can send driver/PCI fallback text instead of a product name.
        var name = string.IsNullOrWhiteSpace(Name) || Name.Contains("0x") || Name.Contains(":") ? "GPU" : Name;
        var node = string.IsNullOrWhiteSpace(RenderNode) ? "Render node unknown" : Path.GetFileName(RenderNode);
        var usage = GpuPercent.HasValue ? $"GPU usage {GpuPercent.Value:0.#}%" : "GPU usage unknown";
        return $"{name} · {node} · {Codec ?? "Codec unknown"} · {usage} · {vram} · Other users on GPU: {Math.Max(0, SessionCountOnGpu - 1)}";
    }
}
