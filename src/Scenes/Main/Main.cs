using Godot;
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

        var gpuTimer = new Timer { WaitTime = 5, OneShot = false, Autostart = true };
        gpuTimer.Timeout += RefreshGpuStatus;
        AddChild(gpuTimer);
        RefreshGpuStatus();

		SelfUpdateAsync();

		Logger.LogInformation("This session's id: {0}", WolfApi.SessionId);
	} 
	

    private bool _gpuStatusPending;

    private async void RefreshGpuStatus()
    {
        if (_gpuStatusPending || !IsInsideTree()) return;
        _gpuStatusPending = true;
        try
        {
            var session = await WolfApi.GetSession();
            if (!IsInstanceValid(this) || !IsInsideTree()) return;
            var label = GetNode<Label>("Content/GpuStatus");
            label.Visible = session?.Gpu is not null;
            label.Text = session?.Gpu?.StatusText() ?? "";
            label.TooltipText = label.Text;
        }
        catch (System.Exception e)
        {
            if (IsInstanceValid(this) && IsInsideTree())
                GetNode<Label>("Content/GpuStatus").Visible = false;
            Logger.LogDebug("GPU status unavailable: {0}", e.Message);
        }
        finally { _gpuStatusPending = false; }
    }

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
