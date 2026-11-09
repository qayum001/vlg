using System.Collections.Generic;
using Godot;

namespace Vlg.Game;

/// <summary>
/// Meshes of the agent-made props (res://assets/models/*.glb, built by tools/models/make_props.py) for MultiMesh use.
/// The first MeshInstance3D of each scene; its surface materials come along. Cached per name.
/// </summary>
public static class ModelMeshes
{
    static readonly Dictionary<string, Mesh> Cache = new();

    public static Mesh Get(string name)
    {
        if (Cache.TryGetValue(name, out var mesh)) return mesh;
        var root = GD.Load<PackedScene>($"res://assets/models/{name}.glb").Instantiate();
        mesh = Find(root)?.Mesh ?? throw new System.InvalidOperationException($"model '{name}' has no mesh");
        root.Free();
        Cache[name] = mesh;
        return mesh;
    }

    static MeshInstance3D? Find(Node node)
    {
        if (node is MeshInstance3D mi) return mi;
        foreach (var child in node.GetChildren())
            if (Find(child) is { } found) return found;
        return null;
    }
}
