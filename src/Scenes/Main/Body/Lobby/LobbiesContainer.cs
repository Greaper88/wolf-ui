using Godot;
using Resources.WolfAPI;
using Skerga.GodotNodeUtilGenerator;
using System.Linq;
using System.Threading.Tasks;

namespace WolfUI;

[Tool][SceneAutoConfigure]
public partial class LobbiesContainer : VBoxContainer
{
    private static readonly ILogger<LobbiesContainer> Logger = Main.GetLogger<LobbiesContainer>();
    private int _revision;
    private bool _refreshPending;
    public override async void _Ready()
    {
        if (Engine.IsEditorHint())
        {
            EditorMockupReady();
            return;
        }

        Hide();

        WolfApi.Singleton.LobbyCreatedEvent += AddLobby;
        WolfApi.Singleton.LobbyStoppedEvent += OnLobbyStopped;
        WolfApi.Singleton.LobbyJoinEvent += OnMembershipChanged;
        WolfApi.Singleton.LobbyLeaveEvent += OnMembershipChanged;
        Lobbies.ChildEnteredTree += (_) => SetDeferred(CanvasItem.PropertyName.Visible, true);
        Lobbies.ChildExitingTree += (_) => CallDeferred(MethodName.OnChildExitingTree);

        var timer = new Timer { WaitTime = 5, Autostart = true };
        timer.Timeout += async () => await RefreshLobbies();
        AddChild(timer);
        await RefreshLobbies();
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint()) return;
        ++_revision;
        WolfApi.Singleton.LobbyCreatedEvent -= AddLobby;
        WolfApi.Singleton.LobbyStoppedEvent -= OnLobbyStopped;
        WolfApi.Singleton.LobbyJoinEvent -= OnMembershipChanged;
        WolfApi.Singleton.LobbyLeaveEvent -= OnMembershipChanged;
    }

    private void OnMembershipChanged(string lobbyId) => ++_revision;

    internal async Task RefreshLobbies()
    {
        if (_refreshPending || !IsInsideTree()) return;
        _refreshPending = true;
        var revision = _revision;
        try
        {
            var snapshot = await WolfApi.GetLobbiesSnapshot();
            if (snapshot is null || !IsInstanceValid(this) || !IsInsideTree() || revision != _revision) return;
            var shared = snapshot.Where(l => l.MultiUser && !string.IsNullOrEmpty(l.Id)).ToList();
            foreach (var node in Lobbies.GetChildren().OfType<Lobby>())
            {
                if (shared.All(l => l.Id != node.Name.ToString()))
                {
                    Lobbies.RemoveChild(node);
                    node.QueueFree();
                }
            }
            foreach (var lobby in shared) UpsertLobby(lobby);
            Visible = Lobbies.GetChildCount() > 0;
        }
        finally { _refreshPending = false; }
    }

    private void EditorMockupReady()
    {
        for(var i = 0; i < 10; ++i)
        {
            var node = Lobby.Create();
            node.Name = $"{i}";
            Lobbies.AddChild(node);
        }
    }

    private void OnChildExitingTree()
    {
        //Logger.LogInformation("{Msg}", lobbies.GetChildCount());
        //GD.Print(lobbies.GetChildCount());
        if(Lobbies.GetChildCount() == 0)
        {
            Hide();
        }
    }

    private void AddLobby(object? caller, Resources.WolfAPI.Lobby lobby)
    {
        ++_revision;
        UpsertLobby(lobby);
    }

    private void UpsertLobby(Resources.WolfAPI.Lobby lobby)
    {
        if (!lobby.MultiUser || string.IsNullOrEmpty(lobby.Id))
            return;
        var existing = Lobbies.GetChildren().OfType<Lobby>().FirstOrDefault(l => l.Name == lobby.Id);
        if (existing is not null) existing.RefreshLobby(lobby);
        else Lobbies.AddChild(Lobby.New(lobby));
    }
    
    private void OnLobbyStopped(object? caller, string lobbyId)
    {
        ++_revision;
        Logger.LogInformation("Lobby stopped {0}", lobbyId);

        foreach (var node in Lobbies.GetChildren())
        {
            if (node.Name == lobbyId)
            {
                Lobbies.RemoveChild(node);
                node.QueueFree();
            }
        }
    }
}
