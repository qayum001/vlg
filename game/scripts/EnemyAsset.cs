using Godot;

namespace Vlg.Game;

/// <param name="AnimTexture">VAT for enemy_vat.gdshader, see PlaceholderMeshes.BakeWalk.</param>
public sealed record EnemyAsset(ArrayMesh Mesh, ImageTexture AnimTexture, int Triangles);
