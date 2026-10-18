using Godot;
using Vlg.Core.Config;
using Vlg.Core.Meta;

namespace Vlg.Game;

/// <summary>
/// Process-wide player state between scenes: meta config and the save file (user://save.json). Loaded on first use;
/// hub and run read and write it, <see cref="Persist"/> writes it to disk.
/// </summary>
public static class GameSession
{
    const string SavePath = "user://save.json";
    static MetaShop? _shop;

    public static MetaShop Shop => _shop ??= Load();
    public static MetaSave Save => Shop.Save;

    public static void Persist()
    {
        using var f = FileAccess.Open(SavePath, FileAccess.ModeFlags.Write)
            ?? throw new System.IO.IOException($"can't write {SavePath}: {FileAccess.GetOpenError()}");
        f.StoreString(Save.ToJson());
    }

    static MetaShop Load()
    {
        var config = MetaConfig.Parse(FileAccess.GetFileAsString("res://data/meta.json"));
        var save = FileAccess.FileExists(SavePath) ? MetaSave.Parse(FileAccess.GetFileAsString(SavePath)) : new MetaSave();
        return new MetaShop(config, save);
    }
}
