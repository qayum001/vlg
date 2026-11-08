using System;
using System.IO;
using System.Text.Json;
using Godot;
using Vlg.Core.Config;

namespace Vlg.Game;

public static class GameSettings
{
    const string SettingsPath = "user://settings.json";
    static bool _initialized;

    public static UserSettings Current { get; private set; } = new();

    public static void Initialize()
    {
        if (_initialized) return;
        string path = ProjectSettings.GlobalizePath(SettingsPath);
        try
        {
            if (File.Exists(path)) Current = UserSettings.Parse(File.ReadAllText(path));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException or FormatException)
        {
            GD.PushWarning($"Could not load settings; using defaults: {e.Message}");
        }
        ApplyAudio();
        _initialized = true;
    }

    public static bool SetMasterVolume(int percent)
    {
        var settings = Current with { MasterVolumePercent = percent };
        string json = settings.ToJson();
        Current = settings;
        ApplyAudio();
        string path = ProjectSettings.GlobalizePath(SettingsPath);
        try
        {
            File.WriteAllText(path + ".tmp", json);
            File.Move(path + ".tmp", path, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Could not save settings: {e.Message}");
            return false;
        }
    }

    static void ApplyAudio()
    {
        int bus = AudioServer.GetBusIndex("Master");
        AudioServer.SetBusVolumeLinear(bus, Current.MasterVolumePercent / 100f);
        AudioServer.SetBusMute(bus, Current.MasterVolumePercent == 0);
    }
}
