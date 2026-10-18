using System.Collections.Generic;
using Godot;

namespace Vlg.Game;

/// <summary>
/// Skill / ultimate / item icons (res://assets/icons/{skills,items}/&lt;id&gt;.png, made by tools/icons/make_icons.py).
/// Null when an id has no icon yet. Cached.
/// </summary>
public static class Icons
{
    static readonly Dictionary<string, Texture2D?> Cache = new();

    public static Texture2D? Skill(string id) => Get($"res://assets/icons/skills/{id}.png");

    public static Texture2D? Item(string id) => Get($"res://assets/icons/items/{id}.png");

    static Texture2D? Get(string path)
    {
        if (Cache.TryGetValue(path, out var t)) return t;
        t = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
        Cache[path] = t;
        return t;
    }
}
