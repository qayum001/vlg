using System;
using System.Collections.Generic;
using Godot;
using SN = System.Numerics;

namespace Vlg.Game;

/// <summary>
/// Code-made placeholder art (modeling is paused). Flat-shaded triangle soup: every triangle has its own
/// 3 vertices, so VERTEX_ID indexes the VAT directly. Model faces +Z, feet at y = 0, ~1.5 m tall.
/// </summary>
public static class PlaceholderMeshes
{
    public const int AnimFrames = 24;
    const int MaxTextureWidth = 16384;

    static readonly Color Skin = new(1f, 1f, 1f, 1f);
    static readonly Color Eye = new(1f, 0.85f, 0.2f, 0f);
    static readonly Color Horn = new(0.15f, 0.12f, 0.1f, 0.5f);
    static readonly Color Leg = new(0.25f, 0.22f, 0.2f, 0.5f);

    enum Part { Body, Head, ArmL, ArmR, LegL, LegR }

    /// <param name="detail">0..2; sphere subdivision = detail + 1 (~280 / ~760 / ~2700 triangles).</param>
    public static EnemyAsset BuildEnemy(int detail)
    {
        int sub = Math.Clamp(detail, 0, 2) + 1;
        var b = new Builder();
        b.Sphere(sub, new(0f, 0.62f, 0f), new(0.38f, 0.42f, 0.32f), Skin, Part.Body);
        b.Sphere(sub, new(0f, 1.12f, 0.04f), new(0.26f), Skin, Part.Head);
        b.Sphere(0, new(-0.1f, 1.16f, 0.27f), new(0.06f), Eye, Part.Head);
        b.Sphere(0, new(0.1f, 1.16f, 0.27f), new(0.06f), Eye, Part.Head);
        b.Cone(new(-0.14f, 1.3f, 0f), new(-0.26f, 1.56f, -0.04f), 0.06f, 8, Horn, Part.Head);
        b.Cone(new(0.14f, 1.3f, 0f), new(0.26f, 1.56f, -0.04f), 0.06f, 8, Horn, Part.Head);
        b.Box(new(-0.46f, 0.66f, 0f), new(0.12f, 0.42f, 0.12f), Skin, Part.ArmL);
        b.Box(new(0.46f, 0.66f, 0f), new(0.12f, 0.42f, 0.12f), Skin, Part.ArmR);
        b.Box(new(-0.16f, 0.18f, 0f), new(0.14f, 0.36f, 0.16f), Leg, Part.LegL);
        b.Box(new(0.16f, 0.18f, 0f), new(0.14f, 0.36f, 0.16f), Leg, Part.LegR);
        return new EnemyAsset(b.ToMesh(), b.BakeWalk(AnimFrames), b.Triangles);
    }

    /// <summary>Diamond stretched along +Z (flight direction).</summary>
    public static ArrayMesh BuildProjectile()
    {
        var b = new Builder();
        SN.Vector3 front = new(0, 0, 0.35f), back = new(0, 0, -0.35f);
        SN.Vector3[] ring = { new(0.08f, 0, 0), new(0, 0.08f, 0), new(-0.08f, 0, 0), new(0, -0.08f, 0) };
        for (int i = 0; i < 4; i++)
        {
            var a = ring[i];
            var c = ring[(i + 1) % 4];
            b.Tri(front, a, c, SN.Vector3.Zero, Skin, Part.Body);
            b.Tri(back, a, c, SN.Vector3.Zero, Skin, Part.Body);
        }
        return b.ToMesh();
    }

    sealed class Builder
    {
        readonly List<SN.Vector3> _pos = new();
        readonly List<SN.Vector3> _nrm = new();
        readonly List<Color> _col = new();
        readonly List<Part> _part = new();

        public int Triangles => _pos.Count / 3;

        /// <summary>
        /// Orients the triangle away from <paramref name="inside"/>. Godot front faces are clockwise,
        /// so vertices are emitted a, c, b relative to the counter-clockwise outward order.
        /// </summary>
        public void Tri(SN.Vector3 a, SN.Vector3 b, SN.Vector3 c, SN.Vector3 inside, Color color, Part part)
        {
            var n = SN.Vector3.Cross(b - a, c - a);
            if (SN.Vector3.Dot(n, (a + b + c) / 3f - inside) < 0f)
            {
                (b, c) = (c, b);
                n = -n;
            }
            n = SN.Vector3.Normalize(n);
            foreach (var v in new[] { a, c, b })
            {
                _pos.Add(v);
                _nrm.Add(n);
                _col.Add(color);
                _part.Add(part);
            }
        }

        public void Sphere(int subdivisions, SN.Vector3 center, SN.Vector3 radii, Color color, Part part)
        {
            foreach (var (a, b, c) in Icosphere(subdivisions))
                Tri(center + a * radii, center + b * radii, center + c * radii, center, color, part);
        }

        public void Box(SN.Vector3 center, SN.Vector3 size, Color color, Part part)
        {
            var h = size / 2f;
            SN.Vector3 P(int x, int y, int z) => center + new SN.Vector3(x * h.X, y * h.Y, z * h.Z);
            int[][] faces =
            {
                new[] { 1, 0, 0 }, new[] { -1, 0, 0 }, new[] { 0, 1, 0 }, new[] { 0, -1, 0 }, new[] { 0, 0, 1 }, new[] { 0, 0, -1 },
            };
            foreach (var f in faces)
            {
                // Two in-plane axes for this face.
                int ax = f[0] != 0 ? 1 : 0, ay = f[2] != 0 ? 1 : 2;
                var corners = new SN.Vector3[4];
                int[][] uv = { new[] { -1, -1 }, new[] { 1, -1 }, new[] { 1, 1 }, new[] { -1, 1 } };
                for (int k = 0; k < 4; k++)
                {
                    var c = new int[3];
                    c[0] = f[0]; c[1] = f[1]; c[2] = f[2];
                    c[ax] = uv[k][0];
                    c[ay] = uv[k][1];
                    corners[k] = P(c[0], c[1], c[2]);
                }
                Tri(corners[0], corners[1], corners[2], center, color, part);
                Tri(corners[0], corners[2], corners[3], center, color, part);
            }
        }

        public void Cone(SN.Vector3 baseCenter, SN.Vector3 apex, float radius, int segments, Color color, Part part)
        {
            var axis = SN.Vector3.Normalize(apex - baseCenter);
            var side = SN.Vector3.Normalize(SN.Vector3.Cross(axis, MathF.Abs(axis.X) < 0.9f ? SN.Vector3.UnitX : SN.Vector3.UnitY));
            var up = SN.Vector3.Cross(axis, side);
            var mid = (baseCenter + apex) / 2f;
            for (int i = 0; i < segments; i++)
            {
                float a0 = MathF.Tau * i / segments, a1 = MathF.Tau * (i + 1) / segments;
                var p0 = baseCenter + (side * MathF.Cos(a0) + up * MathF.Sin(a0)) * radius;
                var p1 = baseCenter + (side * MathF.Cos(a1) + up * MathF.Sin(a1)) * radius;
                // Axis point at the triangle's height is inside even for thin cones.
                var centroid = (p0 + p1 + apex) / 3f;
                var inside = baseCenter + axis * SN.Vector3.Dot(centroid - baseCenter, axis);
                Tri(p0, p1, apex, inside, color, part);
                Tri(p0, p1, baseCenter, mid, color, part);
            }
        }

        public ArrayMesh ToMesh()
        {
            var arrays = new Godot.Collections.Array();
            arrays.Resize((int)Mesh.ArrayType.Max);
            var verts = new Vector3[_pos.Count];
            var norms = new Vector3[_pos.Count];
            for (int i = 0; i < verts.Length; i++)
            {
                verts[i] = ToGodot(_pos[i]);
                norms[i] = ToGodot(_nrm[i]);
            }
            arrays[(int)Mesh.ArrayType.Vertex] = verts;
            arrays[(int)Mesh.ArrayType.Normal] = norms;
            arrays[(int)Mesh.ArrayType.Color] = _col.ToArray();
            var mesh = new ArrayMesh();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            return mesh;
        }

        /// <summary>Walk cycle: legs/arms swing about hips/shoulders, upper body bobs and rolls. RGBA16F.</summary>
        public ImageTexture BakeWalk(int frames)
        {
            int width = _pos.Count;
            if (width > MaxTextureWidth) throw new InvalidOperationException($"VAT width {width} > {MaxTextureWidth}");
            int height = frames * 2;
            var data = new byte[width * height * 8];
            SN.Vector3 hipL = new(-0.16f, 0.36f, 0), hipR = new(0.16f, 0.36f, 0);
            SN.Vector3 shoulderL = new(-0.46f, 0.86f, 0), shoulderR = new(0.46f, 0.86f, 0);
            SN.Vector3 pelvis = new(0, 0.36f, 0);

            for (int f = 0; f < frames; f++)
            {
                float a = MathF.Tau * f / frames;
                float swing = MathF.Sin(a);
                var upper = SN.Matrix4x4.CreateRotationZ(0.06f * swing, pelvis)
                    * SN.Matrix4x4.CreateTranslation(0, 0.04f * MathF.Abs(MathF.Sin(a)), 0);
                var mats = new SN.Matrix4x4[6];
                mats[(int)Part.Body] = upper;
                mats[(int)Part.Head] = upper;
                mats[(int)Part.ArmL] = SN.Matrix4x4.CreateRotationX(-0.6f * swing, shoulderL) * upper;
                mats[(int)Part.ArmR] = SN.Matrix4x4.CreateRotationX(0.6f * swing, shoulderR) * upper;
                mats[(int)Part.LegL] = SN.Matrix4x4.CreateRotationX(0.5f * swing, hipL);
                mats[(int)Part.LegR] = SN.Matrix4x4.CreateRotationX(-0.5f * swing, hipR);

                for (int v = 0; v < width; v++)
                {
                    var m = mats[(int)_part[v]];
                    WriteHalf4(data, (f * width + v) * 8, SN.Vector3.Transform(_pos[v], m));
                    WriteHalf4(data, ((f + frames) * width + v) * 8, SN.Vector3.Normalize(SN.Vector3.TransformNormal(_nrm[v], m)));
                }
            }
            var image = Image.CreateFromData(width, height, false, Image.Format.Rgbah, data);
            return ImageTexture.CreateFromImage(image);
        }

        static void WriteHalf4(byte[] dst, int offset, SN.Vector3 v)
        {
            BitConverter.TryWriteBytes(dst.AsSpan(offset, 2), BitConverter.HalfToUInt16Bits((Half)v.X));
            BitConverter.TryWriteBytes(dst.AsSpan(offset + 2, 2), BitConverter.HalfToUInt16Bits((Half)v.Y));
            BitConverter.TryWriteBytes(dst.AsSpan(offset + 4, 2), BitConverter.HalfToUInt16Bits((Half)v.Z));
            BitConverter.TryWriteBytes(dst.AsSpan(offset + 6, 2), BitConverter.HalfToUInt16Bits((Half)1f));
        }
    }

    static Vector3 ToGodot(SN.Vector3 v) => new(v.X, v.Y, v.Z);

    /// <summary>Unit icosphere triangles.</summary>
    static IEnumerable<(SN.Vector3, SN.Vector3, SN.Vector3)> Icosphere(int subdivisions)
    {
        float t = (1f + MathF.Sqrt(5f)) / 2f;
        SN.Vector3[] v =
        {
            new(-1, t, 0), new(1, t, 0), new(-1, -t, 0), new(1, -t, 0),
            new(0, -1, t), new(0, 1, t), new(0, -1, -t), new(0, 1, -t),
            new(t, 0, -1), new(t, 0, 1), new(-t, 0, -1), new(-t, 0, 1),
        };
        int[] f =
        {
            0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1,
        };
        var tris = new List<(SN.Vector3, SN.Vector3, SN.Vector3)>();
        for (int i = 0; i < f.Length; i += 3)
            tris.Add((SN.Vector3.Normalize(v[f[i]]), SN.Vector3.Normalize(v[f[i + 1]]), SN.Vector3.Normalize(v[f[i + 2]])));
        for (int s = 0; s < subdivisions; s++)
        {
            var next = new List<(SN.Vector3, SN.Vector3, SN.Vector3)>(tris.Count * 4);
            foreach (var (a, b, c) in tris)
            {
                var ab = SN.Vector3.Normalize(a + b);
                var bc = SN.Vector3.Normalize(b + c);
                var ca = SN.Vector3.Normalize(c + a);
                next.Add((a, ab, ca));
                next.Add((b, bc, ab));
                next.Add((c, ca, bc));
                next.Add((ab, bc, ca));
            }
            tris = next;
        }
        return tris;
    }
}
