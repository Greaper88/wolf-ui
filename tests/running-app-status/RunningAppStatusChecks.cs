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

    private void SetState(object[] lobbies, bool fail = false, double delay = 0, bool failStop = false, bool failJoin = false)
    {
        var tmp = _fixture + "/next.json";
        File.WriteAllText(tmp, JsonSerializer.Serialize(new
        {
            lobbies, fail_lobbies = fail, delay_lobbies = delay, fail_stop = failStop, fail_join = failJoin
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
            SetState(Array.Empty<object>(), fail: true, failJoin: true);
            Tile.MenuButtonCoop.EmitSignal(Button.SignalName.Pressed);
            await Wait(() => _main.TopLayer.GetChildren().OfType<QuestionDialogue>().Any(), "co-op PIN choice shown");
            _main.TopLayer.GetChildren().OfType<QuestionDialogue>().Single()
                .GetNode<Container>("%ButtonContainer").GetChildren().OfType<Button>().Single(b => b.Text == "No")
                .EmitSignal(Button.SignalName.Pressed);
            await Wait(() => File.Exists(_fixture + "/created.json") &&
                File.ReadAllText(_fixture + "/created.json").Contains("\"multi_user\": true"), "co-op creation sent");
            await Wait(() => _main.TopLayer.GetChildren().OfType<QuestionDialogue>().Any(), "co-op admission error shown");
            Check(Tile.PlayingHint.Visible, "failed co-op join retains the running app");
            using var created = JsonDocument.Parse(File.ReadAllText(_fixture + "/created.json"));
            Check(created.RootElement.GetProperty("multi_user").GetBoolean() &&
                  created.RootElement.GetProperty("source_session_id").GetString() == WolfApi.SessionId,
                "co-op creation identifies the automatic GPU launcher");
            _main.TopLayer.GetChildren().OfType<QuestionDialogue>().Single()
                .GetNode<Container>("%ButtonContainer").GetChild<Button>(0)
                .EmitSignal(Button.SignalName.Pressed);
            await Wait(() => !_main.TopLayer.GetChildren().OfType<QuestionDialogue>().Any(), "co-op failure dismissed");
            Check(Tile.MenuButtonCoop.Disabled, "existing co-op app cannot be launched twice");

            SetState(Array.Empty<object>());
            WolfApi.ActiveProfile = null;
            var joined = await WolfApi.JoinLobby("front-page-lobby", WolfApi.SessionId);
            Check(joined?.Success == true, "front-page guest joins without selecting a protected profile");
            await SelectProfile("alice");
            var lobbyList = _main.FindChildren("*", "", true, false).OfType<LobbiesContainer>().Single();
            var entries = lobbyList.GetNode<Container>("%Lobbies");
            Task RefreshLobbies() => (Task)typeof(LobbiesContainer).GetMethod("RefreshLobbies",
                BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(lobbyList, null)!;
            var shared = new { id = "shared-test", name = "Shared", started_by_profile_id = "alice",
                multi_user = true, connected_sessions = new[] { "a", "b" } };
            SetState(new[] { shared });
            await RefreshLobbies();
            await Wait(() => entries.GetChildCount() == 1, "lobby snapshot arrived");
            Check(entries.GetChildCount() == 1, "lobby snapshot recovers a missed creation event");
            ApiEvent("CreateLobbyEvent", shared);
            Check(entries.GetChildCount() == 1, "duplicate lobby events keep one entry");
            Check(entries.GetChild<WolfUI.Lobby>(0).GetNode<Label>("%PlayerCountLabel").Text == "2",
                "lobby snapshot restores the actual player count");
            ApiEvent("JoinLobbyEvent", new { profile_id = "guest", lobby_id = "shared-test", moonlight_session_id = "c" });
            Check(entries.GetChild<WolfUI.Lobby>(0).GetNode<Label>("%PlayerCountLabel").Text == "3",
                "membership events read the lobby ID regardless of field order");
            ApiEvent("LeaveLobbyEvent", new { lobby_id = "shared-test", moonlight_session_id = "c" });
            Check(entries.GetChild<WolfUI.Lobby>(0).GetNode<Label>("%PlayerCountLabel").Text == "2",
                "leave events update the shared lobby count");
            SetState(Array.Empty<object>(), fail: true);
            await RefreshLobbies();
            Check(entries.GetChildCount() == 1, "failed lobby snapshot preserves the visible lobby");
            SetState(Array.Empty<object>());
            await RefreshLobbies();
            await Wait(() => entries.GetChildCount() == 0, "empty lobby snapshot arrived");
            Check(entries.GetChildCount() == 0, "lobby snapshot clears a missed stop event");
            await ToSignal(GetTree().CreateTimer(0.05), SceneTreeTimer.SignalName.Timeout);
            ApiEvent("JoinLobbyEvent", new { lobby_id = "shared-test" });
            ApiEvent("LeaveLobbyEvent", new { lobby_id = "shared-test" });
            Check(entries.GetChildCount() == 0, "removed lobby widgets unsubscribe from membership events");
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
