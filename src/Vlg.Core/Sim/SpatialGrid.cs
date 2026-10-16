using System;
using System.Numerics;

namespace Vlg.Core.Sim;

/// <summary>
/// Uniform grid on the ground plane, centered on a point each Build (the player).
/// Rebuilt every frame by counting sort. Points outside the grid clamp into edge cells,
/// so they are still found, only slower.
/// </summary>
public sealed class SpatialGrid
{
    readonly int _dim;
    readonly float _cellSize;
    readonly float _invCell;
    readonly int[] _cellStart; // dim*dim + 1, items of cell c are _items[_cellStart[c].._cellStart[c+1])
    readonly int[] _fill;
    readonly int[] _items;
    readonly int[] _cellOfItem;
    Vector2 _origin;

    public SpatialGrid(int dim, float cellSize, int capacity)
    {
        _dim = dim;
        _cellSize = cellSize;
        _invCell = 1f / cellSize;
        _cellStart = new int[dim * dim + 1];
        _fill = new int[dim * dim];
        _items = new int[capacity];
        _cellOfItem = new int[capacity];
    }

    public void Build(Vector2[] positions, int count, Vector2 center)
    {
        _origin = center - new Vector2(_dim * _cellSize * 0.5f);
        Array.Clear(_cellStart);
        for (int i = 0; i < count; i++)
        {
            int c = CellOf(positions[i]);
            _cellOfItem[i] = c;
            _cellStart[c + 1]++;
        }
        for (int c = 1; c < _cellStart.Length; c++)
            _cellStart[c] += _cellStart[c - 1];
        Array.Copy(_cellStart, _fill, _fill.Length);
        for (int i = 0; i < count; i++)
            _items[_fill[_cellOfItem[i]]++] = i;
    }

    /// <summary>
    /// Indices whose position (read from <paramref name="positions"/> now, may differ from Build time)
    /// is within <paramref name="radius"/>. Stops when <paramref name="results"/> is full.
    /// Callers that move points after Build must pad the radius by the max movement.
    /// </summary>
    public int Query(Vector2 p, float radius, Span<int> results, Vector2[] positions)
    {
        int x0 = Coord(p.X - radius - _origin.X), x1 = Coord(p.X + radius - _origin.X);
        int y0 = Coord(p.Y - radius - _origin.Y), y1 = Coord(p.Y + radius - _origin.Y);
        float r2 = radius * radius;
        int n = 0;
        for (int y = y0; y <= y1; y++)
        {
            int row = y * _dim;
            for (int x = x0; x <= x1; x++)
            {
                int c = row + x;
                for (int k = _cellStart[c], end = _cellStart[c + 1]; k < end; k++)
                {
                    int j = _items[k];
                    if (Vector2.DistanceSquared(positions[j], p) > r2) continue;
                    results[n++] = j;
                    if (n == results.Length) return n;
                }
            }
        }
        return n;
    }

    int CellOf(Vector2 p) => Coord(p.Y - _origin.Y) * _dim + Coord(p.X - _origin.X);

    int Coord(float local) => Math.Clamp((int)MathF.Floor(local * _invCell), 0, _dim - 1);
}
