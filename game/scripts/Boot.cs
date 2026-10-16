using Godot;
using Vlg.Core.Bench;

namespace Vlg.Game;

/// <summary>
/// Main scene: opens the game run, or the stress test for --stress/--bench.
/// Needed because export templates refuse a scene path on the command line.
/// </summary>
public partial class Boot : Node
{
    const string RunScene = "res://scenes/run.tscn";
    const string StressScene = "res://scenes/stress_test.tscn";

    public override void _Ready()
    {
        bool stress = LaunchOptions.Parse(OS.GetCmdlineUserArgs()).WantsStressTest;
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, stress ? StressScene : RunScene);
    }
}
