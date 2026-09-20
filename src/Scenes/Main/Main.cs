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
        var gpuStatus = new Button { Text = "GPUs: …", TooltipText = "GPU names, memory, utilization and connected viewers" };
        var header = GetNode<HBoxContainer>("Content/Header/MarginContainer/HBoxContainer");
        header.AddChild(gpuStatus);
        header.MoveChild(gpuStatus, 2);
        var options = header.GetNode<Button>("OptionsButton");
        var exit = header.GetNode<Button>("ExitButton");
        options.FocusNeighborRight = gpuStatus.GetPath();
        gpuStatus.FocusNeighborLeft = options.GetPath();
        gpuStatus.FocusNeighborRight = exit.GetPath();
        exit.FocusNeighborLeft = gpuStatus.GetPath();
        gpuStatus.Pressed += GpuDialogue.ShowStatus;
        var gpuTimer = new Timer { WaitTime = 5, Autostart = true };
        bool refreshingGpus = false;
        async void RefreshGpus()
        {
            if (refreshingGpus) return;
            refreshingGpus = true;
            string label;
            try
            {
                var response = await WolfApi.GetGpus();
                label = response?.Success == true ? $"GPUs: {response.Gpus.Count}" : "GPUs: unavailable";
            }
            catch (System.Exception) { label = "GPUs: unavailable"; }
            if (IsInstanceValid(gpuStatus)) gpuStatus.Text = label;
            refreshingGpus = false;
        }
        gpuTimer.Timeout += RefreshGpus;
        AddChild(gpuTimer);
        RefreshGpus();

		SelfUpdateAsync();

		Logger.LogInformation("This session's id: {0}", WolfApi.SessionId);
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
