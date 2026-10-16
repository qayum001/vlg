using System;
using Godot;

namespace Vlg.Game;

/// <summary>
/// Error in a scene root's _Ready (bad args, data, preset): log and quit with exit code 1. Without this the
/// half-built node throws in _Process every frame and the process never exits (bench.ps1 would hang).
/// </summary>
public static class StartupFailure
{
    public static void Quit(Node node, Exception e)
    {
        GD.PushError($"startup failed: {e}");
        node.SetProcess(false);
        node.SetProcessUnhandledInput(false);
        node.GetTree().Quit(1);
    }
}
