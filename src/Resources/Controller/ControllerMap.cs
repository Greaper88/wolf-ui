using Godot;
using Godot.Collections;
using System;

namespace WolfUI;

[GlobalClass]
[Tool]
public partial class ControllerMap : Resource
{
    public enum ControllerType { Switch, XBox, PS, None };
    public enum ControllerButton { Accept, Cancel, Up, Down, Left, Right, Back };
    private static readonly ILogger<ControllerMap> Logger = WolfUI.Main.GetLogger<ControllerMap>();
    [Export]
    Dictionary<ControllerType, Texture2D> Accept;
    [Export]
    Dictionary<ControllerType, Texture2D> Cancel;
    [Export]
    Dictionary<ControllerType, Texture2D> Up;
    [Export]
    Dictionary<ControllerType, Texture2D> Down;
    [Export]
    Dictionary<ControllerType, Texture2D> Left;
    [Export]
    Dictionary<ControllerType, Texture2D> Right;
    [Export]
    Dictionary<ControllerType, Texture2D> Back;
    private ControllerType _controller = ControllerType.None;
    private int? _activeDevice;
    private readonly Func<Array<int>> _connectedJoypads;
    private readonly Func<int, string> _joyName;
    private ControllerType UsedController
    {
        get => _controller;
        set
        {
            if (value != _controller)
                EmitSignalUsedControllerChanged(value);
            _controller = value;
        }
    }
    public ControllerType Controller => UsedController;

#nullable disable // Export Variables are Godots Job
    public ControllerMap() : this(Input.GetConnectedJoypads, Input.GetJoyName)
    {
        Input.JoyConnectionChanged += JoyConnectionChanged;
    }

    internal ControllerMap(Func<Array<int>> connectedJoypads, Func<int, string> joyName)
    {
        _connectedJoypads = connectedJoypads;
        _joyName = joyName;
    }
#nullable enable

    private void UseJoypad(int deviceId)
    {
        _activeDevice = deviceId;
        UsedController = _joyName(deviceId) switch
        {
            "Xbox 360 Controller" => ControllerType.XBox,
            "Wolf X-Box One (virtual) pad" => ControllerType.XBox,
            "Wolf DualSense (virtual) pad" => ControllerType.PS,
            "Wolf Nintendo (virtual) pad" => ControllerType.Switch,
            "Nintendo Switch Pro Controller" => ControllerType.Switch,
            _ => ControllerType.XBox,
        };
    }

    internal void JoyConnectionChanged(long deviceId, bool connected)
    {
        var oldController = UsedController;
        var connectedControllers = _connectedJoypads();
        if (connected)
        {
            // Device is a stable Godot ID, not an index in GetConnectedJoypads().
            // A queued input/connection event may outlive a device's removal.
            if (deviceId < 0 || deviceId > int.MaxValue || !connectedControllers.Contains((int)deviceId)) return;
            UseJoypad((int)deviceId);
        }
        else if (_activeDevice == deviceId)
        {
            _activeDevice = null;
            foreach (var remaining in connectedControllers)
            {
                if (remaining == deviceId) continue;
                _activeDevice = remaining;
                break;
            }
            if (_activeDevice is { } replacement) UseJoypad(replacement);
            else UsedController = ControllerType.None;
        }

        if (UsedController == oldController) return;
        
        Logger.LogInformation("Controller icons changed to {0}", UsedController);
        EmitSignalIconSetChanged();
    }

    public Texture2D GetIcon(ControllerButton button)
    {
        return button switch
        {
            ControllerButton.Accept => Accept[UsedController],
            ControllerButton.Cancel => Cancel[UsedController],
            ControllerButton.Up => Up[UsedController],
            ControllerButton.Down => Down[UsedController],
            ControllerButton.Left => Left[UsedController],
            ControllerButton.Right => Right[UsedController],
            ControllerButton.Back => Back[UsedController],
            // Should never be callable but VSCode won't be fine without it
            _ => new Texture2D(),
        };
    }

    public void SetController(InputEvent @event)
    {
        var oldController = UsedController;
        switch (@event)
        {
            case InputEventMouseMotion or InputEventMouseButton or InputEventKey:
            {
                _activeDevice = null;
                UsedController = ControllerType.None;
                break;
            }
            case InputEventJoypadButton or InputEventJoypadMotion:
            {
                JoyConnectionChanged(@event.Device, true);
                return;
            }
        }

        if (UsedController != oldController)
            EmitSignalIconSetChanged();
    }

    [Signal]
    public delegate void IconSetChangedEventHandler();
    [Signal]
    public delegate void UsedControllerChangedEventHandler(ControllerType controller);
}
