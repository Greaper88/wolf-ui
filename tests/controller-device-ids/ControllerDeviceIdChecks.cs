using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using WolfUI;

public partial class ControllerDeviceIdChecks : Node
{
    private int _checks;
    private void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        GD.Print("PASS: " + description);
        _checks++;
    }

    public override void _Ready()
    {
        try
        {
            // Real Godot events carry stable device IDs. Reconnect and app
            // assignment can leave gaps without renumbering the remaining pad.
            Array<int> connected = [0, 1];
            var names = new System.Collections.Generic.Dictionary<int, string>
            {
                [0] = "Wolf X-Box One (virtual) pad",
                [1] = "Wolf DualSense (virtual) pad",
                [2] = "Wolf Nintendo (virtual) pad",
                [7] = "Wolf DualSense (virtual) pad"
            };
            var lookedUp = new List<int>();
            using var map = new ControllerMap(() => connected, id => { lookedUp.Add(id); return names[id]; });
            var iconChanges = 0;
            var controllerChanges = new List<ControllerMap.ControllerType>();
            map.IconSetChanged += () => iconChanges++;
            map.UsedControllerChanged += type => controllerChanges.Add(type);

            using var pad0 = new InputEventJoypadButton { Device = 0, ButtonIndex = JoyButton.A, Pressed = true };
            using var pad1 = new InputEventJoypadButton { Device = 1, ButtonIndex = JoyButton.DpadDown, Pressed = true };
            map.SetController(pad0);
            Check(map.Controller == ControllerMap.ControllerType.XBox && lookedUp[^1] == 0,
                "Initial device zero selects its own controller type");
            map.SetController(pad1);
            Check(map.Controller == ControllerMap.ControllerType.PS && lookedUp[^1] == 1,
                "Initial second device selects its own controller type");

            connected = [1];
            map.JoyConnectionChanged(0, false);
            Check(map.Controller == ControllerMap.ControllerType.PS,
                "Assigning the main app leaves the secondary controller selected");
            map.SetController(pad1);
            Check(map.Controller == ControllerMap.ControllerType.PS && lookedUp[^1] == 1,
                "Rejoined launcher accepts device one when it is the only connected pad");
            using var motion1 = new InputEventJoypadMotion { Device = 1, Axis = JoyAxis.LeftX, AxisValue = 0.8f };
            map.SetController(motion1);
            Check(map.Controller == ControllerMap.ControllerType.PS && lookedUp[^1] == 1,
                "Axis events also use the actual sparse device ID");

            var oldLookups = lookedUp.Count;
            var oldIcons = iconChanges;
            map.SetController(pad0);
            map.JoyConnectionChanged(0, true);
            Check(lookedUp.Count == oldLookups && iconChanges == oldIcons && map.Controller == ControllerMap.ControllerType.PS,
                "Queued events and stale connection notifications for removed pads are ignored");

            connected = [2, 7];
            using var motion7 = new InputEventJoypadMotion { Device = 7, Axis = JoyAxis.LeftY, AxisValue = -0.8f };
            map.SetController(motion7);
            Check(map.Controller == ControllerMap.ControllerType.PS && lookedUp[^1] == 7,
                "Device seven is valid in a two-pad list containing IDs two and seven");
            using var pad2 = new InputEventJoypadButton { Device = 2, ButtonIndex = JoyButton.A, Pressed = true };
            map.SetController(pad2);
            Check(map.Controller == ControllerMap.ControllerType.Switch && lookedUp[^1] == 2,
                "Sparse ID two is not mistaken for a list position");

            connected = [7];
            var oldChanges = controllerChanges.Count;
            map.JoyConnectionChanged(2, false);
            Check(map.Controller == ControllerMap.ControllerType.PS && lookedUp[^1] == 7,
                "Unplugging the active controller selects an actual remaining ID");
            Check(controllerChanges.Count == oldChanges + 1 && controllerChanges[^1] == ControllerMap.ControllerType.PS,
                "Disconnect fallback emits one final controller type without a transient keyboard state");

            using var mouse = new InputEventMouseMotion();
            map.SetController(mouse);
            Check(map.Controller == ControllerMap.ControllerType.None, "Mouse input still selects keyboard/mouse icons");
            connected = [2];
            oldIcons = iconChanges;
            map.JoyConnectionChanged(7, false);
            Check(map.Controller == ControllerMap.ControllerType.None && iconChanges == oldIcons,
                "Unplugging a pad after mouse input does not switch icons back to another pad");
            map.SetController(pad2);
            Check(map.Controller == ControllerMap.ControllerType.Switch, "A fresh remaining-pad event restores controller icons");

            connected = [];
            map.JoyConnectionChanged(2, false);
            Check(map.Controller == ControllerMap.ControllerType.None, "Removing the last active pad selects keyboard/mouse icons");
            oldLookups = lookedUp.Count;
            map.SetController(pad2);
            using var invalid = new InputEventJoypadButton { Device = -1, Pressed = true };
            map.SetController(invalid);
            Check(lookedUp.Count == oldLookups && map.Controller == ControllerMap.ControllerType.None,
                "No connected pads and invalid/stale IDs cause no lookup or exception");
            GD.Print($"PASS: {_checks} controller device-ID checks");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
