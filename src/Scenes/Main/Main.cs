using Godot;
using System.Collections.Generic;
using Resources.WolfAPI;
using Skerga.GodotNodeUtilGenerator;

namespace WolfUI;

[GlobalClass, SceneAutoConfigure(GenerateNewMethod = false)]
public partial class Main : Control
{
	[Export]
	public ControllerMap? controllerMap;
	public static Main Singleton { get; private set; }

	public Main() 
	{
		Singleton ??= this;
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (Engine.IsEditorHint())
			return;

		InputActions.ConfigureJoypadActions();

		SoundEffects? soundEffects = null;
		foreach (var child in GetChildren())
		{
			if (child is SoundEffects effects)
				soundEffects = effects;
		}

		soundEffects?.CallDeferred(SoundEffects.MethodName.ApplySoundEffects, this);

		var time = new Timer
		{
			WaitTime = 0.1,
			OneShot = false,
			Autostart = true
		};
		time.Timeout += () =>
		{
			soundEffects?.ApplySoundEffects(this);
		};

		AddChild(time);

		WolfApi.Init();
		WolfApi.Singleton.DisplayAssignmentsChanged += OnDisplayAssignmentsChanged;

        var dualDisplayStatus = new Label
        {
            Name = "DualDisplayStatus",
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        GetNode("Content/Footer/MarginContainer/HBoxContainer").AddChild(dualDisplayStatus);

        var gpuTimer = new Timer { WaitTime = 5, OneShot = false, Autostart = true };
        gpuTimer.Timeout += RefreshGpuStatus;
        AddChild(gpuTimer);
        RefreshGpuStatus();

		SelfUpdateAsync();

		Logger.LogInformation("This session's id: {0}", WolfApi.SessionId);
	} 
	

    private bool _gpuStatusPending;
    private bool _sessionRefreshQueued;

    private void OnDisplayAssignmentsChanged() => RefreshGpuStatus();

    public override void _ExitTree()
    {
        if (!Engine.IsEditorHint())
            WolfApi.Singleton.DisplayAssignmentsChanged -= OnDisplayAssignmentsChanged;
        base._ExitTree();
    }

    private async void RefreshGpuStatus()
    {
        if (!IsInsideTree()) return;
        if (_gpuStatusPending)
        {
            _sessionRefreshQueued = true;
            return;
        }
        _gpuStatusPending = true;
        try
        {
            var session = await WolfApi.GetSession();
            // Keep the last valid status during a temporary request failure.
            if (session is null || !IsInstanceValid(this) || !IsInsideTree()) return;
            var label = GetNode<Label>("Content/Footer/MarginContainer/HBoxContainer/GpuStatus");
            label.Visible = session?.Gpu is not null;
            label.Text = session?.Gpu?.StatusText() ?? "";
            label.TooltipText = label.Text;
            var lobbies = session.DualDisplay?.Mode == "dual_app" && session.DualDisplay.IsSupported
                ? await WolfApi.GetLobbiesSnapshot() : null;
            if (!IsInstanceValid(this) || !IsInsideTree()) return;
            UpdateDualDisplayStatus(session, lobbies);
        }
        catch (System.Exception e)
        {
            Logger.LogDebug("GPU status refresh failed; keeping previous readings: {0}", e.Message);
        }
        finally
        {
            _gpuStatusPending = false;
            if (_sessionRefreshQueued)
            {
                _sessionRefreshQueued = false;
                RefreshGpuStatus();
            }
        }
    }

    internal void UpdateDualDisplayStatus(Session? session, IReadOnlyList<Resources.WolfAPI.Lobby>? lobbies = null)
    {
        var label = GetNodeOrNull<Label>("Content/Footer/MarginContainer/HBoxContainer/DualDisplayStatus");
        if (label is null) return;
        label.Text = DualDisplayLauncher.StatusText(session?.DualDisplay, lobbies);
        label.Visible = label.Text.Length > 0;
        label.TooltipText = label.Text;
    }

    internal void RefreshSessionStatus() => RefreshGpuStatus();

	/*
	public void LoadTheme(string themeName)
	{
		var user = System.Environment.GetEnvironmentVariable("USER") ?? "retro";
		user = user == "root" ? "retro" : user;
		var filepath = $"/home/{user}/.wolf-ui/{themeName}.tres";

		if (File.Exists(filepath))
		{
			return;
		}

		GetTree().Root.Theme = ResourceLoader.Load<Theme>(filepath);
	}

	public void SaveTheme(string themeName)
	{
		var user = System.Environment.GetEnvironmentVariable("USER") ?? "retro";
		user = user == "root" ? "retro" : user;
		var filepath = $"/home/{user}/.wolf-ui/{themeName}.tres";

		if (File.Exists(filepath))
		{
			return;
		}

		//GetTree().Root.Theme;
	}
	*/
	
	public override void _Input(InputEvent @event)
	{
		controllerMap?.SetController(@event);
	}
}
