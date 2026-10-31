using Godot;
using System;
using System.Collections.Generic;
using WolfUI;

public class ControlCounter
{
    public required int counter;
    public required Control control;
}

public partial class InputActions : HBoxContainer
{
    [Export] private Control _cancelContainer = null!;
    [Export] private Control _backContainer = null!;
    
    private static Dictionary<string, ControlCounter> _checkCounter =  new Dictionary<string, ControlCounter>();
    private static InputActions _singleton = null!;

    internal static void ConfigureJoypadActions()
    {
        // Godot defaults and project bindings can target device zero. A launcher
        // left on the second display may retain any stable controller device ID.
        foreach (var action in InputMap.GetActions())
        {
            var events = InputMap.ActionGetEvents(action);
            var needsUpdate = false;
            foreach (var binding in events)
            {
                if ((binding is InputEventJoypadButton or InputEventJoypadMotion) && binding.Device != -1)
                {
                    needsUpdate = true;
                    break;
                }
            }
            if (!needsUpdate) continue;

            // Keep event order, keyboard/mouse bindings and the action deadzone.
            // Duplicate joypad resources so project/default resources stay intact.
            InputMap.ActionEraseEvents(action);
            foreach (var binding in events)
            {
                if (binding is InputEventJoypadButton or InputEventJoypadMotion)
                {
                    var anyController = (InputEvent)binding.Duplicate();
                    anyController.Device = -1;
                    InputMap.ActionAddEvent(action, anyController);
                }
                else InputMap.ActionAddEvent(action, binding);
            }
        }
    }
    
    public override void _Ready()
    {
        _singleton ??= this;
        _checkCounter["ui_select"] = new ControlCounter()
        {
            counter = 0,
            control = _backContainer,
        };
        _checkCounter["ui_cancel"] = new ControlCounter()
        {
            counter = 0,
            control = _cancelContainer,
        };
    }

    public override void _Process(double delta)
    {
        foreach (var kv in _checkCounter)
        {
            kv.Value.counter--;
            if (kv.Value.counter == 0)
            {
                kv.Value.control.Hide();
            }
        }
    }   

    public static bool IsActionJustPressed(string action)
    {
        _checkCounter[action].counter = 2;
        _checkCounter[action].control.Visible = true;
        return Input.IsActionJustPressed(action);
    }
}
