using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Resources.WolfAPI;

// Display IDs belong to the negotiated session, rather than Godot's local screen indices.
public sealed class DualDisplay
{
    [JsonInclude, JsonPropertyName("version")]
    public int Version { get; set; }
    [JsonInclude, JsonPropertyName("mode")]
    public string Mode { get; set; } = "off";
    [JsonInclude, JsonPropertyName("layout")]
    public string Layout { get; set; } = "horizontal";
    [JsonInclude, JsonPropertyName("primary")]
    public int Primary { get; set; }
    [JsonInclude, JsonPropertyName("available")]
    public bool Available { get; set; }
    [JsonInclude, JsonPropertyName("active_display")]
    public int ActiveDisplay { get; set; }
    [JsonInclude, JsonPropertyName("displays")]
    public List<StreamDisplay> Displays { get; set; } = [];
    [JsonInclude, JsonPropertyName("slots")]
    public List<DisplaySlot> Slots { get; set; } = [];

    public bool IsVideoMode => Mode is "dual_monitor" or "dual_app";
    public bool IsSupported => Version == 1 && Available && Displays is { Count: 2 } &&
        Displays.Select(display => display.Id).Order().SequenceEqual(new[] { 0, 1 }) &&
        Displays.All(display => display.Width > 0 && display.Height > 0 && display.Fps > 0) &&
        Primary is 0 or 1 && ActiveDisplay is 0 or 1 && Layout is "horizontal" or "vertical" &&
        (Mode != "dual_app" || Slots is { Count: 2 } &&
            Slots.Select(slot => slot.DisplayId).Order().SequenceEqual(new[] { 0, 1 }) &&
            Slots.All(slot => slot.State is "launcher" or "running"));
    public StreamDisplay? GetDisplay(int? id) => Displays.FirstOrDefault(display => display.Id == id);
    public bool IsRunning(int id) => Slots.Any(slot => slot.DisplayId == id && slot.State == "running");
    public List<StreamDisplay> FreeDisplays() => Displays.Where(display => !IsRunning(display.Id)).ToList();
}

public sealed class StreamDisplay
{
    [JsonInclude, JsonPropertyName("id")]
    public int Id { get; set; }
    [JsonInclude, JsonPropertyName("width")]
    public int Width { get; set; }
    [JsonInclude, JsonPropertyName("height")]
    public int Height { get; set; }
    [JsonInclude, JsonPropertyName("fps")]
    public int Fps { get; set; }
    [JsonInclude, JsonPropertyName("x")]
    public int X { get; set; }
    [JsonInclude, JsonPropertyName("y")]
    public int Y { get; set; }

    public string Label => $"Display {Id + 1} ({Width} × {Height})";
}

public sealed class DisplaySlot
{
    [JsonInclude, JsonPropertyName("display_id")]
    public int DisplayId { get; set; }
    [JsonInclude, JsonPropertyName("app_id")]
    public string? AppId { get; set; }
    [JsonInclude, JsonPropertyName("state")]
    public string State { get; set; } = "launcher";
}
