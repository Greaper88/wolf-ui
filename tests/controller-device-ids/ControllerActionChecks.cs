using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class ControllerActionChecks : Node
{
    private int _checks;

    private sealed record ActionSnapshot(float Deadzone, string[] Events);

    private void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        GD.Print("PASS: " + description);
        _checks++;
    }

    private static bool IsJoypad(InputEvent input) => input is InputEventJoypadButton or InputEventJoypadMotion;

    private static string EventSignature(InputEvent input, bool normalizeJoypadDevice)
    {
        if (!normalizeJoypadDevice || !IsJoypad(input)) return GD.VarToStr(input);
        using var copy = (InputEvent)input.Duplicate();
        copy.Device = 0;
        return GD.VarToStr(copy);
    }

    private static Dictionary<string, ActionSnapshot> Snapshot(bool normalizeJoypadDevice)
    {
        var result = new Dictionary<string, ActionSnapshot>();
        foreach (var action in InputMap.GetActions())
        {
            result[action.ToString()] = new ActionSnapshot(InputMap.ActionGetDeadzone(action),
                InputMap.ActionGetEvents(action).Select(input => EventSignature(input, normalizeJoypadDevice)).ToArray());
        }
        return result;
    }

    private void CheckUnchanged(Dictionary<string, ActionSnapshot> before,
        Dictionary<string, ActionSnapshot> after, string description)
    {
        Check(before.Keys.OrderBy(name => name).SequenceEqual(after.Keys.OrderBy(name => name)),
            description + ": action names are unchanged");
        foreach (var (action, previous) in before)
        {
            var current = after[action];
            Check(previous.Deadzone == current.Deadzone && previous.Events.SequenceEqual(current.Events),
                description + ": " + action + " keeps deadzone, event order, keyboard/mouse and non-device fields");
        }
    }

    private void CheckButton(int device, JoyButton button, string action)
    {
        using var input = new InputEventJoypadButton { Device = device, ButtonIndex = button, Pressed = true };
        Check(InputMap.EventIsAction(input, action, true) && input.IsActionPressed(action, false, true),
            $"Device {device} button {button} matches {action}");
    }

    private void CheckAxis(int device, JoyAxis axis, float value, string action)
    {
        using var input = new InputEventJoypadMotion { Device = device, Axis = axis, AxisValue = value };
        Check(InputMap.EventIsAction(input, action, true) && input.IsActionPressed(action, false, true),
            $"Device {device} axis {axis} {value} matches {action}");
    }

    private static void SendButton(int device, JoyButton button, bool pressed)
    {
        // Parse real physical-shaped events: InputEventAction would bypass the
        // device bindings this regression is intended to exercise.
        using var input = new InputEventJoypadButton { Device = device, ButtonIndex = button, Pressed = pressed };
        Input.ParseInputEvent(input);
        Input.FlushBufferedEvents();
    }

    public override async void _Ready()
    {
        try
        {
            // This scene does not instantiate Main, so the startup helper has not
            // run yet. Establish that both configured and inherited bindings
            // reproduce the nonzero-ID failure before testing the actual helper.
            using var baselineAccept = new InputEventJoypadButton { Device = 1, ButtonIndex = JoyButton.A, Pressed = true };
            using var baselineDirection = new InputEventJoypadButton { Device = 1, ButtonIndex = JoyButton.DpadDown, Pressed = true };
            Check(!baselineAccept.IsActionPressed("ui_accept") && !baselineDirection.IsActionPressed("ui_down"),
                "Unnormalized configured accept and inherited direction reject device one");
            var before = Snapshot(true);
            InputActions.ConfigureJoypadActions();
            CheckUnchanged(before, Snapshot(true), "Joypad normalization");
            foreach (var action in InputMap.GetActions())
            {
                foreach (var input in InputMap.ActionGetEvents(action))
                {
                    if (IsJoypad(input)) Check(input.Device == -1, action + " joypad binding accepts every device");
                }
            }
            foreach (int device in new[] { 0, 1, 7 })
            {
                CheckButton(device, JoyButton.A, "ui_accept");
                CheckButton(device, (JoyButton)6, "ui_accept");
                CheckButton(device, JoyButton.Y, "ui_select");
                CheckButton(device, JoyButton.B, "ui_cancel");
                CheckButton(device, JoyButton.DpadLeft, "ui_left");
                CheckButton(device, JoyButton.DpadRight, "ui_right");
                CheckButton(device, JoyButton.DpadUp, "ui_up");
                CheckButton(device, JoyButton.DpadDown, "ui_down");
                CheckAxis(device, JoyAxis.LeftX, -0.9f, "ui_left");
                CheckAxis(device, JoyAxis.LeftX, 0.9f, "ui_right");
                CheckAxis(device, JoyAxis.LeftY, -0.9f, "ui_up");
                CheckAxis(device, JoyAxis.LeftY, 0.9f, "ui_down");
                CheckAxis(device, JoyAxis.RightY, -0.9f, "ui_text_scroll_up");
                CheckAxis(device, JoyAxis.RightY, 0.9f, "ui_text_scroll_down");
            }
            var normalized = Snapshot(false);
            InputActions.ConfigureJoypadActions();
            CheckUnchanged(normalized, Snapshot(false), "Repeated normalization");

            var panel = new Control { Name = "ControllerActionPanel", Size = new Vector2(400, 200) };
            var first = new Button { Name = "First", Text = "First", Position = new Vector2(20, 20), Size = new Vector2(160, 50), FocusMode = Control.FocusModeEnum.All };
            var second = new Button { Name = "Second", Text = "Second", Position = new Vector2(20, 100), Size = new Vector2(160, 50), FocusMode = Control.FocusModeEnum.All };
            AddChild(panel);
            panel.AddChild(first);
            panel.AddChild(second);
            first.FocusNeighborBottom = first.GetPathTo(second);
            second.FocusNeighborTop = second.GetPathTo(first);
            int firstPressed = 0, secondPressed = 0;
            first.Pressed += () => firstPressed++;
            second.Pressed += () => secondPressed++;
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            first.GrabFocus();
            Check(GetViewport().GuiGetFocusOwner() == first, "GUI starts focused on the first button");
            SendButton(1, JoyButton.DpadDown, true);
            SendButton(1, JoyButton.DpadDown, false);
            Check(GetViewport().GuiGetFocusOwner() == second, "Device one D-pad moves GUI focus to the second button");
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            SendButton(1, JoyButton.A, true);
            SendButton(1, JoyButton.A, false);
            Check(firstPressed == 0 && secondPressed == 1, "Device one accept activates only the focused second button");
            panel.QueueFree();
            GD.Print($"PASS: {_checks} real controller action checks");
            GetTree().Quit();
        }
        catch (Exception error)
        {
            GD.PushError(error.ToString());
            GetTree().Quit(1);
        }
    }
}
