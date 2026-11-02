using Godot;
using Resources.WolfAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WolfUI;

public partial class DualDisplayChecks : Node
{
    private readonly string _fixture = System.Environment.GetEnvironmentVariable("WOLF_UI_TEST_FIXTURE")!;
    private Main _main = null!;
    private int _checks;
    private App Tile(string title) => _main.AppList.GetNode<GridContainer>("%AppGrid").GetChildren().OfType<App>().Single(app => app.Title == title);
    private IEnumerable<QuestionDialogue> Dialogues => _main.TopLayer.GetChildren().OfType<QuestionDialogue>().Where(dialogue => !dialogue.IsQueuedForDeletion());

    private object Config(string mode = "dual_app", bool available = true, int? running = null, string layout = "horizontal", int version = 1) => new
    {
        version, mode, available, layout, primary = 1, active_display = running.HasValue ? 1 - running.Value : 0,
        displays = new[] {
            new { id = 0, width = 1920, height = 1080, fps = 60, x = 0, y = 0 },
            new { id = 1, width = 1280, height = 720, fps = 30, x = layout == "vertical" ? 0 : 1920, y = layout == "vertical" ? 1080 : 0 }
        },
        slots = Enumerable.Range(0, 2).Select(id => new { display_id = id, app_id = running == id ? "steam" : null, state = running == id ? "running" : "launcher" })
    };

    private void State(object? config, object[]? lobbies = null)
    {
        var next = _fixture + "/next.json";
        File.WriteAllText(next, JsonSerializer.Serialize(new
        {
            lobbies = lobbies ?? [],
            sessions = new[] { new { client_id = "dual-display-fixture", video_width = 1920, video_height = 1080,
                video_refresh_rate = 60, audio_channel_count = 2, client_settings = new { }, dual_display = config,
                cpu_placement = new { numa_node = 1, cpu_ids = new[] { 1 } },
                gpu = new { id = "fixture-gpu", name = "NUMA fixture GPU", render_node = "/dev/dri/renderD128",
                    numa_node = 1, cpu_ids = new[] { 1 }, memory_placement = new { mode = "preferred", nodes = new[] { 1 } } } } }
        }));
        File.Move(next, _fixture + "/state.json", true);
    }

    private JsonElement[] Requests(string path) => !File.Exists(_fixture + "/requests.jsonl") ? [] :
        File.ReadAllLines(_fixture + "/requests.jsonl").Select(line => JsonSerializer.Deserialize<JsonElement>(line))
            .Where(item => item.GetProperty("path").GetString()!.EndsWith(path)).Select(item => item.GetProperty("body")).ToArray();

    private void ClearRequests() => File.WriteAllText(_fixture + "/requests.jsonl", "");

    private async Task Wait(Func<bool> done, string message)
    {
        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (!done())
        {
            if (DateTime.UtcNow > deadline) throw new Exception(message);
            await ToSignal(GetTree().CreateTimer(0.025), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        GD.Print("PASS: " + description);
        _checks++;
    }

    private async Task Answer(string prefix)
    {
        await Wait(() => Dialogues.Any(), "dialogue appeared");
        var dialogue = Dialogues.Single();
        dialogue.GetNode<Container>("%ButtonContainer").GetChildren().OfType<Button>()
            .Single(button => button.Text.StartsWith(prefix)).EmitSignal(Button.SignalName.Pressed);
        await Wait(() => !GodotObject.IsInstanceValid(dialogue) || dialogue.IsQueuedForDeletion(), "dialogue dismissed");
    }

    private async Task Start(string title)
    {
        var tile = Tile(title);
        tile.MenuButtonStart.EmitSignal(Button.SignalName.Pressed);
        await Wait(() => Requests("/lobbies/join").Length == 1 && !tile.MenuButtonStart.Disabled, title + " joined");
    }

    public override async void _Ready()
    {
        try
        {
            State(Config());
            _main = GD.Load<PackedScene>("res://Scenes/Main/Main.tscn").Instantiate<Main>();
            AddChild(_main);
            await Wait(() => _main.UserList.GetNode<Container>("%UserContainer").GetChildren().OfType<Profile>().Any(p => p.Id == "alice"), "profile loaded");
            _main.UserList.GetNode<Container>("%UserContainer").GetChildren().OfType<Profile>().Single(p => p.Id == "alice").EmitSignal(Button.SignalName.Pressed);
            await Wait(() => ((Control)_main.AppList).Visible && _main.HeaderLabel.Text == "Select Application" &&
                _main.AppList.GetNode<GridContainer>("%AppGrid").GetChildCount() == 5, "app picker loaded");
            await Wait(() => _main.GetNode<Label>("Content/Footer/MarginContainer/HBoxContainer/GpuStatus").Text.Contains("NUMA fixture GPU"),
                "additive NUMA session fields preserve GPU status and app loading");
            Check(true, "additive NUMA session and GPU objects deserialize without losing existing status");

            ClearRequests();
            Tile("Steam").MenuButtonStart.EmitSignal(Button.SignalName.Pressed);
            await Answer("Cancel");
            await Wait(() => !Tile("Steam").MenuButtonStart.Disabled, "cancel releases start button");
            Check(Requests("/lobbies/create").Length == 0 && Requests("/lobbies/join").Length == 0, "cancel never launches or joins an app");

            Tile("Steam").MenuButtonStart.EmitSignal(Button.SignalName.Pressed);
            await Answer("Display 2");
            await Wait(() => Requests("/lobbies/join").Length == 1 && !Tile("Steam").MenuButtonStart.Disabled, "first app joined");
            var create = Requests("/lobbies/create").Single();
            Check(create.GetProperty("display_id").GetInt32() == 1, "first app starts on chosen display 2");
            Check(create.GetProperty("source_session_id").GetString() == "dual-display-fixture", "display target remains owned by current session");
            var video = create.GetProperty("video_settings");
            Check(video.GetProperty("width").GetInt32() == 1280 && video.GetProperty("height").GetInt32() == 720 &&
                video.GetProperty("refresh_rate").GetInt32() == 30, "second display uses independent resolution and frame rate");
            Check(Requests("/lobbies/join").Single().GetProperty("display_id").GetInt32() == 1, "join preserves selected display ID");

            ClearRequests();
            Tile("Kodi").MenuButtonCoop.EmitSignal(Button.SignalName.Pressed);
            await Answer("Display 1");
            await Answer("No");
            await Wait(() => Requests("/lobbies/join").Length == 1 && !Tile("Kodi").MenuButtonCoop.Disabled, "co-op app joined");
            var coop = Requests("/lobbies/create").Single();
            Check(coop.GetProperty("display_id").GetInt32() == 0 && coop.GetProperty("multi_user").GetBoolean(),
                "co-op app creation preserves its chosen display and shared-app setting");
            Check(Requests("/lobbies/join").Single().GetProperty("display_id").GetInt32() == 0,
                "co-op join preserves the chosen display after the PIN decision");

            State(Config(running: 1));
            ClearRequests();
            await Start("Firefox");
            Check(!Dialogues.Any() && Requests("/lobbies/create").Single().GetProperty("display_id").GetInt32() == 0,
                "second app automatically targets the remaining launcher display");
            Check(Requests("/lobbies/create").Single().GetProperty("video_settings").GetProperty("width").GetInt32() == 1920,
                "remaining display retains its own video size");
            _main.UpdateDualDisplayStatus(new Session { DualDisplay = JsonSerializer.Deserialize<DualDisplay>(JsonSerializer.Serialize(Config(running: 1))) });
            Check(_main.GetNode<Label>("Content/Footer/MarginContainer/HBoxContainer/DualDisplayStatus").Text == "Choose an app for Display 1",
                "launcher status identifies the remaining display");

            State(Config("dual_monitor", layout: "vertical"));
            ClearRequests();
            await Start("Desktop");
            Check(!Dialogues.Any() && !Requests("/lobbies/create").Single().TryGetProperty("display_id", out _),
                "extended desktop launches one app without independent display targeting");
            Check(!Requests("/lobbies/join").Single().TryGetProperty("display_id", out _), "extended desktop join retains whole desktop routing");
            _main.UpdateDualDisplayStatus(new Session { DualDisplay = JsonSerializer.Deserialize<DualDisplay>(JsonSerializer.Serialize(Config("dual_monitor", layout: "vertical"))) });
            Check(_main.GetNode<Label>("Content/Footer/MarginContainer/HBoxContainer/DualDisplayStatus").Text.Contains("Stacked · Display 2 primary"),
                "extended desktop status reports layout and independent primary display");

            State(Config("controls", available: false));
            ClearRequests();
            await Start("Kodi");
            Check(!Dialogues.Any() && !Requests("/lobbies/create").Single().TryGetProperty("display_id", out _),
                "controls mode never enters separate video app selection");
            State(null);
            ClearRequests();
            await Start("RetroArch");
            Check(!Requests("/lobbies/join").Single().TryGetProperty("display_id", out _), "old servers preserve legacy request shape");

            foreach (var config in new[] { Config(available: false), Config(version: 2) })
            {
                State(config);
                ClearRequests();
                Tile("Steam").MenuButtonStart.EmitSignal(Button.SignalName.Pressed);
                await Answer("OK");
                await Wait(() => !Tile("Steam").MenuButtonStart.Disabled, "unavailable mode releases button");
                Check(Requests("/lobbies/create").Length == 0 && Requests("/lobbies/join").Length == 0,
                    "unavailable or unknown-version video cannot silently replace a single stream");
            }

            // The shared-lobby picker is reachable before a profile has been selected.
            State(Config(running: 0));
            ClearRequests();
            var activeProfile = WolfApi.ActiveProfile;
            WolfApi.ActiveProfile = null!;
            var lobby = WolfUI.Lobby.New(new Resources.WolfAPI.Lobby { Id = "shared-app", Name = "Shared app", MultiUser = true });
            _main.AddChild(lobby);
            lobby.JoinButton.EmitSignal(Button.SignalName.Pressed);
            await Wait(() => Requests("/lobbies/join").Length == 1 && !lobby.JoinButton.Disabled, "shared app joined");
            var shared = Requests("/lobbies/join").Single();
            Check(shared.GetProperty("display_id").GetInt32() == 1 && shared.GetProperty("profile_id").ValueKind == JsonValueKind.Null,
                "shared lobby forwards the free display without inventing a viewer profile");
            WolfApi.ActiveProfile = activeProfile;
            lobby.QueueFree();

            State(Config(running: 1), [new { id = "persistent-steam", name = "Steam", started_by_profile_id = "alice",
                runner_state_folder = "profile-data/alice/WolfSteam", multi_user = false }]);
            await ((AppList)_main.AppList).RefreshRunningApps();
            Check(Tile("Steam").MenuButtonStart.Text == "Connect", "persistent app restored before reconnect");
            ClearRequests();
            await Start("Steam");
            Check(Requests("/lobbies/create").Length == 0 && Requests("/lobbies/join").Single().GetProperty("lobby_id").GetString() == "persistent-steam" &&
                Requests("/lobbies/join").Single().GetProperty("display_id").GetInt32() == 0,
                "persistent app reconnect routes the existing app to the selected free display");

            // A native Moonlight picker writes the same host slots and creates
            // the same persistent profile app identities as Wolf-UI.
            var nativeAssignments = JsonSerializer.Deserialize<DualDisplay>(JsonSerializer.Serialize(Config()))!;
            nativeAssignments.Slots[0].State = nativeAssignments.Slots[1].State = "running";
            nativeAssignments.Slots[0].AppId = "native-steam";
            nativeAssignments.Slots[1].AppId = "native-kodi";
            var nativeApps = new[]
            {
                new Resources.WolfAPI.Lobby { Id = "native-steam", Name = "Steam", StartedByProfileId = "alice",
                    RunnerStateFolder = "profile-data/alice/WolfSteam" },
                new Resources.WolfAPI.Lobby { Id = "native-kodi", Name = "Kodi", StartedByProfileId = "alice",
                    RunnerStateFolder = "profile-data/alice/WolfKodi" }
            };
            State(nativeAssignments, nativeApps.Cast<object>().ToArray());
            await ((AppList)_main.AppList).RefreshRunningApps();
            Check(Tile("Steam").MenuButtonStart.Text == "Connect" && Tile("Kodi").MenuButtonStart.Text == "Connect",
                "native-started apps are recognized as the existing persistent apps");
            Check(DualDisplayLauncher.TargetLabel(nativeAssignments, nativeAssignments.Displays[0], nativeApps).Contains("Replace Steam") &&
                DualDisplayLauncher.TargetLabel(nativeAssignments, nativeAssignments.Displays[1], nativeApps).Contains("Replace Kodi"),
                "replacement targets describe the apps currently assigned by Moonlight");
            var routeSignals = 0;
            void OnRouteChanged() => ++routeSignals;
            WolfApi.Singleton.DisplayAssignmentsChanged += OnRouteChanged;
            try
            {
                WolfApi.Singleton.EmitSignal(WolfApi.SignalName.ApiEvent,
                    "wolf::core::events::SwitchStreamProducerEvents",
                    JsonSerializer.Serialize(new { session_id = "other-client", interpipe_src_id = "native-steam" }));
                Check(routeSignals == 0, "another client's assignments do not trigger the owned route event");
                WolfApi.Singleton.EmitSignal(WolfApi.SignalName.ApiEvent,
                    "wolf::core::events::SwitchStreamProducerEvents",
                    JsonSerializer.Serialize(new { session_id = "dual-display-fixture", interpipe_src_id = "native-steam" }));
                var status = _main.GetNode<Label>("Content/Footer/MarginContainer/HBoxContainer/DualDisplayStatus");
                await Wait(() => status.Text == "Display 1: Steam · Display 2: Kodi", "native assignments reached the launcher footer");
                Check(routeSignals == 1 && status.Text.Contains("Display 2: Kodi"),
                    "native route events refresh Wolf-UI from authoritative host assignments");
                nativeAssignments.Slots[0].State = "launcher";
                nativeAssignments.Slots[0].AppId = null;
                State(nativeAssignments, nativeApps.Cast<object>().ToArray());
                WolfApi.Singleton.EmitSignal(WolfApi.SignalName.ApiEvent,
                    "wolf::core::events::SwitchStreamProducerEvents",
                    JsonSerializer.Serialize(new { session_id = "dual-display-fixture", interpipe_src_id = "dual-display-fixture" }));
                await Wait(() => status.Text == "Display 1: Wolf-UI · Display 2: Kodi", "cleared native slot reached the launcher footer");
                Check(nativeAssignments.FreeDisplays().Single().Id == 0 && status.Text.Contains("Display 2: Kodi"),
                    "returning one native slot to the launcher preserves the other assignment");
                ClearRequests();
                await Start("Steam");
                Check(Requests("/lobbies/create").Length == 0 && Requests("/lobbies/join").Single().GetProperty("display_id").GetInt32() == 0 &&
                    Requests("/lobbies/join").Single().GetProperty("lobby_id").GetString() == "native-steam",
                    "Wolf-UI reuses the native app when assigning the free display");
            }
            finally { WolfApi.Singleton.DisplayAssignmentsChanged -= OnRouteChanged; }

            var malformed = JsonSerializer.Deserialize<DualDisplay>(JsonSerializer.Serialize(Config()))!;
            malformed.Displays[1].Id = 0;
            Check(!malformed.IsSupported, "duplicate display IDs are rejected");
            malformed = JsonSerializer.Deserialize<DualDisplay>(JsonSerializer.Serialize(Config()))!;
            malformed.Slots.Clear();
            Check(!malformed.IsSupported, "missing slot state cannot assume both displays are free");
            GD.Print($"PASS: {_checks} dual-display checks");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
