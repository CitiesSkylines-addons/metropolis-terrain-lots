using System;
using System.Collections.Generic;

namespace MetropolisTerrainLots
{
    // A planning sketch that supports freeform polygons, block fills, and brush shapes.
    public sealed class LotDraft
    {
        private readonly List<Point2> _vertices = new List<Point2>();
        public int Count { get { return _vertices.Count; } }

        public void Add(Point2 point)
        {
            if (_vertices.Count == 64) throw new InvalidOperationException("Lot vertex limit reached");
            if (float.IsNaN(point.X) || float.IsInfinity(point.X) ||
                float.IsNaN(point.Y) || float.IsInfinity(point.Y))
                throw new ArgumentException("Invalid coordinate", "point");
            _vertices.Add(point);
        }

        public void SetPolygon(Point2[] points)
        {
            _vertices.Clear();
            if (points == null) return;
            int limit = Math.Min(points.Length, 64);
            for (int i = 0; i < limit; i++)
            {
                Add(points[i]);
            }
        }

        public void Undo() { if (_vertices.Count > 0) _vertices.RemoveAt(_vertices.Count - 1); }
        public void Clear() { _vertices.Clear(); }
        public Point2[] Vertices() { return _vertices.ToArray(); }

        public float AreaSquareMeters
        {
            get
            {
                if (_vertices.Count < 3) return 0;
                double twice = 0;
                for (int i = 0; i < _vertices.Count; i++)
                {
                    Point2 a = _vertices[i], b = _vertices[(i + 1) % _vertices.Count];
                    twice += (double)a.X * b.Y - (double)b.X * a.Y;
                }
                return (float)(Math.Abs(twice) / 2);
            }
        }

        public float PerimeterMeters
        {
            get
            {
                if (_vertices.Count < 2) return 0f;
                float sum = 0f;
                for (int i = 0; i < _vertices.Count; i++)
                {
                    Point2 a = _vertices[i];
                    Point2 b = _vertices[(i + 1) % _vertices.Count];
                    sum += a.DistanceTo(b);
                }
                return sum;
            }
        }

        public Point2 Centroid
        {
            get
            {
                if (_vertices.Count == 0) return new Point2(0f, 0f);
                float sx = 0f, sy = 0f;
                for (int i = 0; i < _vertices.Count; i++)
                {
                    sx += _vertices[i].X;
                    sy += _vertices[i].Y;
                }
                return new Point2(sx / _vertices.Count, sy / _vertices.Count);
            }
        }

        public void GetBounds(out float minX, out float minY, out float maxX, out float maxY)
        {
            if (_vertices.Count == 0)
            {
                minX = minY = maxX = maxY = 0f;
                return;
            }
            minX = maxX = _vertices[0].X;
            minY = maxY = _vertices[0].Y;
            for (int i = 1; i < _vertices.Count; i++)
            {
                Point2 v = _vertices[i];
                if (v.X < minX) minX = v.X;
                if (v.X > maxX) maxX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Y > maxY) maxY = v.Y;
            }
        }

        public bool IsValid
        {
            get
            {
                if (_vertices.Count < 3 || AreaSquareMeters < 1) return false;
                for (int i = 0; i < _vertices.Count; i++)
                {
                    Point2 a = _vertices[i], b = _vertices[(i + 1) % _vertices.Count];
                    if (DistanceSquared(a, b) < 1) return false;
                    for (int j = i + 1; j < _vertices.Count; j++)
                    {
                        if (j == i + 1 || (i == 0 && j == _vertices.Count - 1)) continue;
                        Point2 c = _vertices[j], d = _vertices[(j + 1) % _vertices.Count];
                        if (Intersects(a, b, c, d)) return false;
                    }
                }
                return true;
            }
        }

        public bool[] Rasterize(float originX, float originY, int columns, int rows, float cellSize)
        {
            if (!IsValid) throw new InvalidOperationException("Complete a simple polygon before preview");
            return LotGeometry.Rasterize(Vertices(), originX, originY, columns, rows, cellSize);
        }

        private static double DistanceSquared(Point2 a, Point2 b)
        {
            double x = (double)a.X - b.X, y = (double)a.Y - b.Y;
            return x * x + y * y;
        }

        private static double Cross(Point2 a, Point2 b, Point2 c)
        {
            return ((double)b.X - a.X) * ((double)c.Y - a.Y) -
                ((double)b.Y - a.Y) * ((double)c.X - a.X);
        }

        private static bool OnSegment(Point2 a, Point2 b, Point2 p)
        {
            return Math.Abs(Cross(a, b, p)) < 0.0001 &&
                p.X >= Math.Min(a.X, b.X) && p.X <= Math.Max(a.X, b.X) &&
                p.Y >= Math.Min(a.Y, b.Y) && p.Y <= Math.Max(a.Y, b.Y);
        }

        private static bool Intersects(Point2 a, Point2 b, Point2 c, Point2 d)
        {
            double abC = Cross(a, b, c), abD = Cross(a, b, d);
            double cdA = Cross(c, d, a), cdB = Cross(c, d, b);
            if ((abC > 0 && abD < 0 || abC < 0 && abD > 0) &&
                (cdA > 0 && cdB < 0 || cdA < 0 && cdB > 0)) return true;
            return OnSegment(a, b, c) || OnSegment(a, b, d) ||
                OnSegment(c, d, a) || OnSegment(c, d, b);
        }
    }
}
