using System;
using Godot;
using Vlg.Core.Bench;

namespace Vlg.Game;

/// <summary>
/// Main scene: opens the hub, the level run (--run or any run dev arg), or the stress test (--stress/--bench).
/// Needed because export templates refuse a scene path on the command line.
/// </summary>
public partial class Boot : Node
{
    const string HubScene = "res://scenes/hub.tscn";
    const string RunScene = "res://scenes/run.tscn";
    const string StressScene = "res://scenes/stress_test.tscn";

    public override void _Ready()
    {
        try
        {
            var o = LaunchOptions.Parse(OS.GetCmdlineUserArgs());
            string scene = o.WantsStressTest ? StressScene : o.WantsRun ? RunScene : HubScene;
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, scene);
        }
        catch (Exception e)
        {
            StartupFailure.Quit(this, e);
        }
    }
}
