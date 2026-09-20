using Godot;
using Resources.WolfAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace WolfUI;

public static class GpuDialogue
{
    public record Choice(bool Cancelled, GpuInfo? Gpu);
    public static string Describe(GpuInfo gpu)
    {
        static string Memory(ulong? bytes) => bytes.HasValue ? $"{bytes.Value / 1073741824.0:0.0} GiB" : "unavailable";
        var utilization = gpu.Device.GpuPercent.HasValue ? $"{gpu.Device.GpuPercent:0}%" : "unavailable";
        return $"{gpu.Device.Name} ({gpu.Device.RenderNode})\n" +
               $"VRAM: {Memory(gpu.Device.VramUsedBytes)} used / {Memory(gpu.Device.VramBytes)} total\n" +
               $"GPU utilization: {utilization}\nConnected viewers: {gpu.Users}    Running apps: {gpu.Apps}";
    }

    public static async Task<Choice> Pick(string app, int codec)
    {
        GpusResponse? response;
        try { response = await WolfApi.GetGpus(); }
        catch (Exception) { response = null; }
        if (response?.Success != true)
        {
            var useDefault = await QuestionDialogue.OpenDialogue("GPU information unavailable",
                "Use the app's configured GPU? Manual selection requires a Wolf server with the GPU API.",
                new Dictionary<string, bool> { { "Use configured GPU", true }, { "Cancel", false } });
            return new Choice(!useDefault, null);
        }
        var result = new TaskCompletionSource<Choice>();
        var previousFocus = Main.Singleton.GetViewport().GuiGetFocusOwner();
        var dialog = new ConfirmationDialog { Exclusive = true, Title = $"Choose GPU for {app}", MinSize = new Vector2I(760, 300) };
        var box = new VBoxContainer { CustomMinimumSize = new Vector2(720, 190) };
        dialog.AddChild(box);
        var picker = new OptionButton { FitToLongestItem = false, ClipText = true, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var details = new Label { AutowrapMode = TextServer.AutowrapMode.Off };
        box.AddChild(new Label { Text = "Select a GPU for this launch." });
        box.AddChild(picker);
        box.AddChild(details);
        picker.AddItem("Configured GPU (app default)");
        foreach (var gpu in response.Gpus)
        {
            picker.AddItem($"{gpu.Device.Name} — {gpu.Device.RenderNode}" + (gpu.Supports(codec) ? "" : " (incompatible codec or unavailable)"));
            picker.SetItemDisabled(picker.ItemCount - 1, !gpu.Supports(codec));
        }
        void UpdateDetails(long index) => details.Text = index == 0
            ? "Keep the app's existing GPU configuration. No automatic load balancing."
            : Describe(response.Gpus[(int)index - 1]);
        picker.ItemSelected += UpdateDetails;
        UpdateDetails(0);
        dialog.GetOkButton().Text = "Launch";
        dialog.Confirmed += () => result.TrySetResult(new Choice(false, picker.Selected == 0 ? null : response.Gpus[picker.Selected - 1]));
        dialog.Canceled += () => result.TrySetResult(new Choice(true, null));
        dialog.CloseRequested += () => result.TrySetResult(new Choice(true, null));
        dialog.TreeExiting += () => result.TrySetResult(new Choice(true, null));
        Main.Singleton.AddChild(dialog);
        dialog.PopupCentered(new Vector2I(760, 300));
        picker.GrabFocus();
        var choice = await result.Task;
        dialog.QueueFree();
        if (GodotObject.IsInstanceValid(previousFocus)) previousFocus.GrabFocus();
        return choice;
    }

    public static void ShowStatus()
    {
        var dialog = new AcceptDialog { Exclusive = true, Title = "GPUs available to Wolf", MinSize = new Vector2I(900, 600) };
        var scroll = new ScrollContainer { CustomMinimumSize = new Vector2(840, 500) };
        dialog.AddChild(scroll);
        var details = new Label { Text = "Loading GPU information…", AutowrapMode = TextServer.AutowrapMode.WordSmart,
                                  SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(details);
        bool loading = false;
        async void Refresh()
        {
            if (loading) return;
            loading = true;
            string text;
            try
            {
                var response = await WolfApi.GetGpus();
                text = response?.Success == true
                    ? (response.Gpus.Count == 0 ? "No GPUs are visible to Wolf." : string.Join("\n\n", response.Gpus.Select(Describe)))
                    : "GPU information unavailable. This requires a Wolf server with the GPU API.";
            }
            catch (Exception) { text = "GPU information unavailable. Check the connection to Wolf."; }
            if (GodotObject.IsInstanceValid(details)) details.Text = text;
            loading = false;
        }
        var timer = new Timer { WaitTime = 5, Autostart = true };
        timer.Timeout += Refresh;
        dialog.AddChild(timer);
        dialog.Confirmed += dialog.QueueFree;
        dialog.CloseRequested += dialog.QueueFree;
        Main.Singleton.AddChild(dialog);
        dialog.PopupCentered(new Vector2I(900, 600));
        Refresh();
    }
}
