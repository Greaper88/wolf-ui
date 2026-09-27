using Godot;
using Resources.WolfAPI;
using System;
using System.Linq;
using System.Threading.Tasks;
using Skerga.GodotNodeUtilGenerator;
using WolfUI.Misc;

namespace WolfUI;

[Tool, GlobalClass, SceneAutoConfigure(GenerateNewMethod = false)]
public partial class AppList : Control
{
    public event EventHandler<Resources.WolfAPI.Lobby>? LobbyCreatedEvent;
    public event EventHandler<string>? LobbyStoppedEvent;
    private int _rebuildVersion;
    private int _loadedVersion = -1;
    private int _lobbyRevision;
    private bool _refreshPending;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
		{
			ThemeChanged += EditorMockupReady;
			EditorMockupReady();
			return;
		}
		
		if (Main.Singleton.controllerMap is not null)
		{
			Main.Singleton.controllerMap.UsedControllerChanged += OnControllerChanged;
		}

		VisibilityChanged += RebuildAppList;
		ThemeChanged += RebuildAppList;

		WolfApi.Singleton.LobbyCreatedEvent += OnLobbyStarted;
		WolfApi.Singleton.LobbyStoppedEvent += OnLobbyStopped;
		var refreshTimer = new Timer { WaitTime = 5, Autostart = true };
		refreshTimer.Timeout += async () => await RefreshRunningApps();
		AddChild(refreshTimer);
	}

	public override void _ExitTree()
	{
		if (Engine.IsEditorHint()) return;
		++_rebuildVersion;
		WolfApi.Singleton.LobbyCreatedEvent -= OnLobbyStarted;
		WolfApi.Singleton.LobbyStoppedEvent -= OnLobbyStopped;
		if (Main.Singleton.controllerMap is not null)
			Main.Singleton.controllerMap.UsedControllerChanged -= OnControllerChanged;
	}
	
	private void OnControllerChanged(ControllerMap.ControllerType  controllerType)
	{
		if (!Visible) return;
		
		// Ensure at least one element is focused when switching to controller.
		var focus = Main.Singleton.GetViewport().GuiGetFocusOwner();
		if (focus is not null || Main.Singleton.TopLayer.GetChildCount() > 0) return;

		if (AppGrid.GetChildren().Select(n => n as App).FirstOrDefault(n => n is not null) is { } ctrl)
			ctrl.GrabFocus();	
		else
			Main.Singleton.OptionsButton.GrabFocus();

	}
	
	private async void RebuildAppList()
	{
		var version = ++_rebuildVersion;
		if (!Visible)
		{
			Main.Singleton.BackHint.Hide();
			return;
		}

		AppGrid.Columns = AppGrid.GetThemeConstant("columns", "AppListGrid").Between(1, 6);
		Main.Singleton.BackHint.Visible = true;
		var profile = WolfApi.ActiveProfile;
		if (profile is null || !await LoadAppList(profile, version)) return;
		_loadedVersion = version;
		await RefreshRunningApps();
		if (!IsCurrentView(version, profile.Id)) return;

		if (AppGrid.GetChildren().Select(n => n as App).FirstOrDefault(n => n is not null) is { } ctrl)
			ctrl.GrabFocus();	
		else
			Main.Singleton.OptionsButton.GrabFocus();
	}

	private bool IsCurrentView(int version, string? profileId) =>
		IsInstanceValid(this) && IsInsideTree() && IsVisibleInTree() &&
		version == _rebuildVersion && profileId == WolfApi.ActiveProfile?.Id;

	internal void InvalidateLobbySnapshot() => ++_lobbyRevision;

	internal async Task RefreshRunningApps()
	{
		var profileId = WolfApi.ActiveProfile?.Id;
		var version = _rebuildVersion;
		if (_refreshPending || _loadedVersion != version || !IsCurrentView(version, profileId)) return;
		_refreshPending = true;
		var revision = _lobbyRevision;
		try
		{
			var lobbies = await WolfApi.GetLobbiesSnapshot();
			// Failed or outdated snapshots must not erase a newer create/stop event or another profile's view.
			if (lobbies is null || revision != _lobbyRevision || !IsCurrentView(version, profileId)) return;
			foreach (var app in AppGrid.GetChildren().OfType<App>())
				app.RefreshRunningLobby(lobbies);
		}
		finally { _refreshPending = false; }
	}

	private void OnLobbyStopped(object? caller, string lobbyId)
	{
		++_lobbyRevision;
		if (!Visible)
			return;

		LobbyStoppedEvent?.Invoke(this, lobbyId);
	}

	private void OnLobbyStarted(object? sender, Resources.WolfAPI.Lobby? lobby)
	{
		++_lobbyRevision;
		if (!Visible) return;
		if (lobby is null || lobby.OwnerProfileId != WolfApi.ActiveProfile?.Id)
			return;
		LobbyCreatedEvent?.Invoke(this, lobby);
	}

	public override void _Process(double delta)
	{
		if (!Visible) return;
		
		if (Engine.IsEditorHint())
		{
			return;
		}

		if (InputActions.IsActionJustPressed("ui_select") && Main.Singleton.UserList is Control userList)
		{
			userList.Visible = true;
			SoundEffects.PlayAcceptSound();
		}
	}

	private async Task<bool> LoadAppList(Profile profile, int version)
	{
		Main.Singleton.OptionsButton.Visible = true;
		Main.Singleton.HeaderLabel.Text = "Loading...";
		
		
		foreach (var child in AppGrid.GetChildren())
		{
			child.QueueFree();
			AppGrid.RemoveChild(child);
		}
		
		var apps = await WolfApi.GetApps(profile);
		if (!IsCurrentView(version, profile.Id))
		{
			foreach (var app in apps) app.QueueFree();
			return false;
		}
		var enumerator = apps.Select((value, i) => (value, i));
		
		foreach (var vi in enumerator)
		{
			vi.value.Name = $"App {vi.i}";
			AddAppEntry(vi.value);
		}
		
		var firstChildren = AppGrid.GetChildren().Take(AppGrid.Columns).OfType<App>();
		foreach(var child in firstChildren)
		{
			child.AppButton.FocusEntered += () =>
			{
				AppScrollContainer.ScrollVertical = 0;
			};
		};
		
		var remainder = AppGrid.GetChildCount() % AppGrid.Columns;
		var idx = AppGrid.GetChildCount() - (remainder == 0 ? AppGrid.Columns : remainder);
		var lastChildren = AppGrid.GetChildren().Skip(Math.Max(0, idx)).OfType<App>();
		foreach(var child in lastChildren)
		{
			child.AppButton.FocusEntered += () =>
			{
				AppScrollContainer.ScrollVertical = (int)AppScrollContainer.GetChildren().Cast<Control>().First().Size.X;
			};
		};
		
		Main.Singleton.HeaderLabel.Text = "Select Application";
		return true;
	}

	private void EditorMockupReady()
	{
		
		AppGrid.Columns = AppGrid.GetThemeConstant("columns", "AppListGrid").Between(1, 6);
		foreach (var child in AppGrid.GetChildren())
			child.QueueFree();

		var scene = ResourceLoader.Load<PackedScene>("uid://chspw2lt1qcuc");
		for (var i = 0; i < 6; i++)
		{
			for (var j = 0; j < AppGrid.Columns; j++)
			{
				AppGrid.AddChild(scene.Instantiate());
			}
		}
	}

	private static Control BuildSpacer()
	{
		return new Control()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
	}

	private void AddAppEntry(App newApp)
	{
		if (AppGrid is not { } gridContainer) return;
		var appEntryCount = gridContainer.GetChildCount();
		var gridColumns = gridContainer.Columns;

		gridContainer.AddChild(newApp);

		if (appEntryCount < gridColumns) return;
		var aboveApp = gridContainer.GetChild<App>(appEntryCount - gridColumns);
		newApp.FocusNeighborTop = aboveApp.GetPath();

		if (appEntryCount % gridColumns != 0) return;
		var app = gridContainer.GetChild<App>(appEntryCount - 1);
		app.FocusNeighborRight = gridContainer.GetChild<App>(-1).GetPath();
		gridContainer.GetChild<App>(-1).FocusNeighborLeft = app.GetPath();
	}
}
