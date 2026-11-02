using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Resources.WolfAPI;

namespace WolfUI;

public readonly record struct DisplayLaunchTarget(bool Proceed, int? DisplayId, StreamDisplay? Display);

// Wolf switches the launcher and app producers between video tracks. This UI only selects
// the negotiated display ID; moving the Godot window cannot move a remote video track.
public static class DualDisplayLauncher
{
    public static async Task<DisplayLaunchTarget> SelectTarget(Session? session, string appName)
    {
        if (session is null)
        {
            await QuestionDialogue.OpenDialogue("Could not read session", "Wolf did not return the current display configuration. Please try again.",
                new Dictionary<string, bool> { { "OK", true } });
            return new DisplayLaunchTarget(false, null, null);
        }
        var config = session?.DualDisplay;
        if (config is null || !config.IsVideoMode)
            return new DisplayLaunchTarget(true, null, null);

        if (!config.IsSupported)
        {
            await QuestionDialogue.OpenDialogue("Two-display video unavailable",
                "Wolf has not enabled two-display video for this session. Select Controls or Off in your client's second-screen settings and reconnect.",
                new Dictionary<string, bool> { { "OK", true } });
            return new DisplayLaunchTarget(false, null, null);
        }

        // An extended desktop is one app with two monitors. It must never enter the
        // independent-app slot flow or substitute the smaller monitor's video size.
        if (config.Mode == "dual_monitor")
            return new DisplayLaunchTarget(true, null, null);

        var free = config.FreeDisplays();
        if (free.Count == 1)
            return new DisplayLaunchTarget(true, free[0].Id, free[0]);

        var lobbies = free.Count == 0 ? await WolfApi.GetLobbiesSnapshot() : null;
        var choices = config.Displays.OrderBy(display => display.Id).ToDictionary(
            display => TargetLabel(config, display, lobbies),
            display => (int?)display.Id);
        choices.Add("Cancel", null);
        var message = free.Count == 2
            ? $"Choose the display for {appName}. After it starts, the app picker will continue on the other display."
            : $"Both displays already have an app. Choose the display to open {appName} on.";
        var selected = await QuestionDialogue.OpenDialogue("Choose display", message, choices,
            new Dictionary<string, System.Func<bool>> { { "Cancel", () => InputActions.IsActionJustPressed("ui_cancel") } });
        return new DisplayLaunchTarget(selected.HasValue, selected, config.GetDisplay(selected));
    }

    private static string? AppName(DualDisplay config, int displayId, IReadOnlyList<Resources.WolfAPI.Lobby>? lobbies)
    {
        var slot = config.Slots.FirstOrDefault(slot => slot.DisplayId == displayId && slot.State == "running");
        return slot?.AppId is { } id ? lobbies?.FirstOrDefault(lobby => lobby.Id == id)?.Name : null;
    }

    internal static string TargetLabel(DualDisplay config, StreamDisplay display, IReadOnlyList<Resources.WolfAPI.Lobby>? lobbies)
    {
        if (!config.IsRunning(display.Id)) return display.Label;
        var name = AppName(config, display.Id, lobbies);
        return display.Label + (string.IsNullOrWhiteSpace(name) ? " · Replace app" : $" · Replace {name}");
    }

    public static string StatusText(DualDisplay? config, IReadOnlyList<Resources.WolfAPI.Lobby>? lobbies = null)
    {
        if (config is null || !config.IsVideoMode) return "";
        if (!config.IsSupported) return "Two-display video unavailable on this server";
        if (config.Mode == "dual_monitor")
            return $"Extended desktop · {(config.Layout == "vertical" ? "Stacked" : "Side by side")} · Display {config.Primary + 1} primary";

        // The session's slot IDs remain authoritative when Moonlight selects an
        // app. Lobby names only decorate those IDs; they never assign a display.
        if (lobbies is not null && config.Displays.Any(display => !string.IsNullOrWhiteSpace(AppName(config, display.Id, lobbies))))
            return string.Join(" · ", config.Displays.OrderBy(display => display.Id).Select(display =>
                $"Display {display.Id + 1}: " + (config.IsRunning(display.Id)
                    ? AppName(config, display.Id, lobbies) ?? "App running" : "Wolf-UI")));

        var free = config.FreeDisplays();
        return free.Count switch
        {
            2 => "Separate apps · Choose a display when starting your first app",
            1 => $"Choose an app for Display {free[0].Id + 1}",
            _ => "Separate apps · Both displays have an app"
        };
    }
}
