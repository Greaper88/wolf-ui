using Godot;
using Resources.WolfAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using WolfUI;

public partial class RunningAppStatusChecks : Node
{
    private readonly string _fixture = System.Environment.GetEnvironmentVariable("WOLF_UI_TEST_FIXTURE")!;
    private Main _main = null!;
    private AppList List => (AppList)_main.AppList;
    private App Tile => List.GetNode<GridContainer>("%AppGrid").GetChildren().OfType<App>().Single(a => a.Title == "Steam");
    private int _checks;

    private static object Lobby(string id, string owner = "alice", string gpu = "renderD130") => new
    {
        id, name = "Steam", started_by_profile_id = owner,
        runner_state_folder = $"profile-data/{owner}/WolfSteam", multi_user = false,
        connected_sessions = Array.Empty<string>(), gpu = new { render_node = gpu }
    };

    private void SetState(object[] lobbies, bool fail = false, double delay = 0, bool failStop = false)
    {
        var tmp = _fixture + "/next.json";
        File.WriteAllText(tmp, JsonSerializer.Serialize(new
        {
            lobbies, fail_lobbies = fail, delay_lobbies = delay, fail_stop = failStop
        }));
        File.Move(tmp, _fixture + "/state.json", true);
    }

    private async Task Wait(Func<bool> done, string description, double seconds = 8)
    {
        var deadline = DateTime.UtcNow.AddSeconds(seconds);
        while (!done())
        {
            if (DateTime.UtcNow > deadline) throw new Exception(description);
            await ToSignal(GetTree().CreateTimer(0.025), SceneTreeTimer.SignalName.Timeout);
        }
    }

    private void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        _checks++;
        GD.Print("PASS: " + description);
    }

    private static void ApiEvent(string type, object data) =>
        WolfApi.Singleton.EmitSignal(WolfApi.SignalName.ApiEvent,
            "wolf::core::events::" + type, JsonSerializer.Serialize(data));

    private Task Refresh() => (Task)typeof(AppList).GetMethod("RefreshRunningApps",
        BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(List, null)!;

    private async Task SelectProfile(string id)
    {
        ((Control)_main.UserList).Visible = true;
        await Wait(() => _main.UserList.GetNode<Container>("%UserContainer").GetChildren()
            .OfType<Profile>().Any(p => !p.IsQueuedForDeletion() && p.Id == id), "profile loaded");
        var profile = _main.UserList.GetNode<Container>("%UserContainer").GetChildren()
            .OfType<Profile>().First(p => !p.IsQueuedForDeletion() && p.Id == id);
        profile.EmitSignal(Button.SignalName.Pressed);
        await Wait(() => List.Visible && _main.HeaderLabel.Text == "Select Application" &&
            List.GetNode<GridContainer>("%AppGrid").GetChildCount() == 5, "app list loaded");
    }

    public override async void _Ready()
    {
        try
        {
            _main = GD.Load<PackedScene>("res://Scenes/Main/Main.tscn").Instantiate<Main>();
            AddChild(_main);
            await SelectProfile("alice");
            Check(!Tile.MenuButtonStop.Visible, "idle app starts without Stop");

            SetState(new[] { Lobby("event-app") });
            ApiEvent("CreateLobbyEvent", new
            {
                id = "event-app", name = "Steam", profile_id = "alice", multi_user = false,
                runner_state_folder = "profile-data/alice/WolfSteam"
            });
            // Assert immediately: the periodic snapshot must not mask a broken event parser.
            Check(Tile.MenuButtonStop.Visible && Tile.MenuButtonStart.Text == "Connect" && Tile.PlayingHint.Visible,
                "live profile_id event restores Connect, running badge and Stop");

            foreach (var gpu in new[] { "renderD130", "renderD129" })
            {
                SetState(new[] { Lobby("persistent-app", gpu: gpu) });
                await SelectProfile("alice");
                await Wait(() => Tile.MenuButtonStop.Visible, "reconnect status restored");
                Check(Tile.MenuButtonStart.Text == "Connect", "reconnect snapshot on " + gpu);
            }

            SetState(new[] { Lobby("missed-event") });
            ApiEvent("StopLobbyEvent", new { lobby_id = "persistent-app" });
            Check(!Tile.MenuButtonStop.Visible, "stop event clears the old app");
            await Wait(() => Tile.MenuButtonStop.Visible, "periodic refresh recovers missing create event");
            Check(Tile.PlayingHint.Visible, "missed event recovered without reopening app list");

            SetState(Array.Empty<object>(), fail: true);
            await Refresh();
            Check(Tile.MenuButtonStop.Visible, "failed snapshot preserves Stop");

            SetState(Array.Empty<object>(), delay: 0.4);
            var pending = Refresh();
            await Wait(() => File.Exists(_fixture + "/snapshot-started"), "delayed snapshot began");
            SetState(new[] { Lobby("newer-event") });
            ApiEvent("CreateLobbyEvent", new { id = "newer-event", name = "Steam", profile_id = "alice",
                runner_state_folder = "profile-data/alice/WolfSteam", multi_user = false });
            await pending;
            Check(Tile.MenuButtonStop.Visible, "older snapshot cannot erase a newer create event");

            await SelectProfile("bob");
            await Refresh();
            Check(!Tile.MenuButtonStop.Visible, "another profile cannot stop Alice's app");
            await SelectProfile("alice");
            await Wait(() => Tile.MenuButtonStop.Visible, "owner's state restored");

            SetState(Array.Empty<object>());
            await Refresh();
            Check(!Tile.MenuButtonStop.Visible, "successful empty snapshot clears stopped apps");

            SetState(Array.Empty<object>(), fail: true);
            Tile.MenuButtonStart.EmitSignal(Button.SignalName.Pressed);
            await Wait(() => Tile.MenuButtonStop.Visible && !Tile.MenuButtonStart.Disabled,
                "deduplicated create response restored running state");
            Check(Tile.MenuButtonStart.Text == "Connect", "existing app ID retained without a create event");
            // A failed stop must retain the ID and controls so the owner can retry.
            SetState(Array.Empty<object>(), fail: true, failStop: true);
            Tile.MenuButtonStop.EmitSignal(Button.SignalName.Pressed);
            await Wait(() => _main.TopLayer.GetChildren().OfType<QuestionDialogue>().Any(), "failed stop dialog shown");
            Check(Tile.MenuButtonStop.Visible, "failed stop keeps running app available");
            _main.TopLayer.GetChildren().OfType<QuestionDialogue>().Single()
                .GetNode<Container>("%ButtonContainer").GetChild<Button>(0)
                .EmitSignal(Button.SignalName.Pressed);
            await Wait(() => !_main.TopLayer.GetChildren().OfType<QuestionDialogue>().Any(), "stop error dismissed");

            SetState(Array.Empty<object>(), fail: true);
            Tile.MenuButtonStop.EmitSignal(Button.SignalName.Pressed);
            await Wait(() => !Tile.MenuButtonStop.Visible, "successful stop clears state without SSE");
            using var stopped = JsonDocument.Parse(File.ReadAllText(_fixture + "/stopped.json"));
            Check(stopped.RootElement.GetProperty("lobby_id").GetString() == "deduplicated-app",
                "Stop targets the retained persistent app ID");
            Check(Tile.MenuButtonStart.Text == "Start", "stopped app returns to Start");
            GD.Print($"PASS: {_checks} running-app status checks");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
