using System;

namespace MetropolisTerrainLots
{
    public struct Point2
    {
        public readonly float X;
        public readonly float Y;
        public Point2(float x, float y) { X = x; Y = y; }
    }

    public enum SlopeClass { Flat, Gentle, Steep }

    public static class LotGeometry
    {
        // A cell belongs to a lot when its centre is inside the polygon.
        // Boundary cells are handled as inside to avoid gaps on straight edges.
        public static bool Contains(Point2[] polygon, Point2 point)
        {
            if (polygon == null || polygon.Length < 3) throw new ArgumentException("At least three vertices are required", "polygon");
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                Point2 a = polygon[j], b = polygon[i];
                float cross = (point.X - a.X) * (b.Y - a.Y) - (point.Y - a.Y) * (b.X - a.X);
                if (Math.Abs(cross) < 0.0001f && point.X >= Math.Min(a.X, b.X) &&
                    point.X <= Math.Max(a.X, b.X) && point.Y >= Math.Min(a.Y, b.Y) &&
                    point.Y <= Math.Max(a.Y, b.Y)) return true;
                if ((a.Y > point.Y) != (b.Y > point.Y) &&
                    point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
            }
            return inside;
        }

        public static bool[] Rasterize(Point2[] polygon, float originX, float originY,
            int columns, int rows, float cellSize)
        {
            if (columns < 1 || rows < 1 || cellSize <= 0 ||
                (long)columns * rows > 1000000) throw new ArgumentOutOfRangeException("columns");
            bool[] cells = new bool[columns * rows];
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                    cells[y * columns + x] = Contains(polygon,
                        new Point2(originX + (x + 0.5f) * cellSize, originY + (y + 0.5f) * cellSize));
            return cells;
        }

        public static SlopeClass Classify(float[] cornerHeights)
        {
            if (cornerHeights == null || cornerHeights.Length != 4) throw new ArgumentException("Four corner heights are required", "cornerHeights");
            float low = cornerHeights[0], high = low;
            foreach (float height in cornerHeights)
            {
                if (float.IsNaN(height) || float.IsInfinity(height)) throw new ArgumentException("Invalid height", "cornerHeights");
                low = Math.Min(low, height);
                high = Math.Max(high, height);
            }
            float range = high - low;
            return range <= 1f ? SlopeClass.Flat : range <= 4f ? SlopeClass.Gentle : SlopeClass.Steep;
        }
    }
}
