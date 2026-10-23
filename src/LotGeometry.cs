using System;
using System.Collections.Generic;

namespace MetropolisTerrainLots
{
    public struct Point2
    {
        public readonly float X;
        public readonly float Y;
        public Point2(float x, float y) { X = x; Y = y; }

        public float DistanceTo(Point2 other)
        {
            float dx = X - other.X;
            float dy = Y - other.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }

    public struct CurbSegment
    {
        public readonly Point2 A;
        public readonly Point2 B;
        public readonly float HalfWidth;

        public CurbSegment(Point2 a, Point2 b, float halfWidth)
        {
            A = a;
            B = b;
            HalfWidth = halfWidth > 1f ? halfWidth : 8f;
        }
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

        public static Point2 ProjectOntoSegment(Point2 p, Point2 a, Point2 b, out float t)
        {
            float dx = b.X - a.X;
            float dy = b.Y - a.Y;
            float lenSq = dx * dx + dy * dy;
            if (lenSq < 0.0001f)
            {
                t = 0f;
                return a;
            }
            t = ((p.X - a.X) * dx + (p.Y - a.Y) * dy) / lenSq;
            if (t < 0f) t = 0f;
            else if (t > 1f) t = 1f;
            return new Point2(a.X + dx * t, a.Y + dy * t);
        }

        public static float DistanceToPolygonBoundary(Point2[] polygon, Point2 p)
        {
            if (polygon == null || polygon.Length < 2) return 0f;
            float minDist = float.MaxValue;
            for (int i = 0; i < polygon.Length; i++)
            {
                Point2 a = polygon[i];
                Point2 b = polygon[(i + 1) % polygon.Length];
                float t;
                Point2 proj = ProjectOntoSegment(p, a, b, out t);
                float d = p.DistanceTo(proj);
                if (d < minDist) minDist = d;
            }
            return minDist;
        }

        /// <summary>
        /// Placemaker-style curb snapping: snaps a point onto the nearest road curb line if within snapTolerance.
        /// </summary>
        public static Point2 SnapToRoadCurb(Point2 rawPoint, CurbSegment[] segments, float snapTolerance, out bool snapped, out float roadAngle)
        {
            snapped = false;
            roadAngle = 0f;
            if (segments == null || segments.Length == 0 || snapTolerance <= 0f) return rawPoint;

            float bestDist = snapTolerance;
            Point2 bestPoint = rawPoint;

            for (int i = 0; i < segments.Length; i++)
            {
                CurbSegment seg = segments[i];
                float dx = seg.B.X - seg.A.X;
                float dy = seg.B.Y - seg.A.Y;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                if (len < 0.1f) continue;

                float ux = dx / len;
                float uy = dy / len;
                // Normal pointing to either side
                float nx = -uy;
                float ny = ux;

                float t;
                Point2 centerProj = ProjectOntoSegment(rawPoint, seg.A, seg.B, out t);
                float sideSign = ((rawPoint.X - centerProj.X) * nx + (rawPoint.Y - centerProj.Y) * ny) >= 0f ? 1f : -1f;

                Point2 curbProj = new Point2(
                    centerProj.X + nx * sideSign * seg.HalfWidth,
                    centerProj.Y + ny * sideSign * seg.HalfWidth);

                float distToCurb = rawPoint.DistanceTo(curbProj);
                if (distToCurb <= bestDist)
                {
                    bestDist = distToCurb;
                    bestPoint = curbProj;
                    snapped = true;
                    roadAngle = (float)Math.Atan2(uy, ux);
                }
            }

            return bestPoint;
        }

        /// <summary>
        /// Placemaker-style right-angle (90 deg) snapping relative to the previous segment.
        /// </summary>
        public static Point2 SnapToRightAngle(Point2 prevPrev, Point2 prev, Point2 candidate, float angleToleranceRadians)
        {
            float vx = prev.X - prevPrev.X;
            float vy = prev.Y - prevPrev.Y;
            float vLen = (float)Math.Sqrt(vx * vx + vy * vy);
            if (vLen < 0.5f) return candidate;

            float ux = vx / vLen;
            float uy = vy / vLen;

            float cx = candidate.X - prev.X;
            float cy = candidate.Y - prev.Y;
            float cLen = (float)Math.Sqrt(cx * cx + cy * cy);
            if (cLen < 0.5f) return candidate;

            // Directions: parallel (ux, uy), perpendicular (-uy, ux) and (uy, -ux)
            Point2[] dirs = new Point2[]
            {
                new Point2(ux, uy),
                new Point2(-ux, -uy),
                new Point2(-uy, ux),
                new Point2(uy, -ux)
            };

            for (int i = 0; i < dirs.Length; i++)
            {
                float dot = (cx / cLen) * dirs[i].X + (cy / cLen) * dirs[i].Y;
                if (dot > 1f) dot = 1f;
                if (dot < -1f) dot = -1f;
                float angle = (float)Math.Acos(dot);
                if (angle <= angleToleranceRadians)
                {
                    float projLen = cx * dirs[i].X + cy * dirs[i].Y;
                    return new Point2(prev.X + dirs[i].X * projLen, prev.Y + dirs[i].Y * projLen);
                }
            }

            return candidate;
        }

        public static Point2[] CreateRectanglePolygon(Point2 center, float width, float depth, float angleRadians)
        {
            float hw = Math.Max(4f, width * 0.5f);
            float hd = Math.Max(4f, depth * 0.5f);
            float cos = (float)Math.Cos(angleRadians);
            float sin = (float)Math.Sin(angleRadians);

            Point2[] local = new Point2[]
            {
                new Point2(-hw, -hd),
                new Point2( hw, -hd),
                new Point2( hw,  hd),
                new Point2(-hw,  hd)
            };

            Point2[] world = new Point2[4];
            for (int i = 0; i < 4; i++)
            {
                world[i] = new Point2(
                    center.X + local[i].X * cos - local[i].Y * sin,
                    center.Y + local[i].X * sin + local[i].Y * cos);
            }
            return world;
        }

        public static Point2[] CreateRegularPolygon(Point2 center, float radius, int sides, float startAngleRadians)
        {
            if (sides < 3) sides = 3;
            if (sides > 32) sides = 32;
            float r = Math.Max(4f, radius);
            Point2[] pts = new Point2[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = startAngleRadians + (float)(2.0 * Math.PI * i / sides);
                pts[i] = new Point2(center.X + (float)Math.Cos(a) * r, center.Y + (float)Math.Sin(a) * r);
            }
            return pts;
        }

        /// <summary>
        /// Placemaker-style "Paint Bucket" (Block Fill): casts radial rays from a center point against surrounding road curbs
        /// to extract the enclosed urban block polygon, or falls back to a road-aligned rectangle when open.
        /// </summary>
        public static Point2[] BuildBlockFillPolygon(Point2 center, CurbSegment[] segments, float maxRadius, float fallbackSize)
        {
            if (maxRadius < 16f) maxRadius = 96f;
            if (fallbackSize < 8f) fallbackSize = 32f;

            if (segments == null || segments.Length == 0)
            {
                return CreateRectanglePolygon(center, fallbackSize, fallbackSize, 0f);
            }

            const int rayCount = 16;
            List<Point2> hits = new List<Point2>(rayCount);
            int curbHitCount = 0;
            float dominantAngle = 0f;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < segments.Length; i++)
            {
                float t;
                Point2 p = ProjectOntoSegment(center, segments[i].A, segments[i].B, out t);
                float d = center.DistanceTo(p);
                if (d < nearestDist)
                {
                    nearestDist = d;
                    dominantAngle = (float)Math.Atan2(segments[i].B.Y - segments[i].A.Y, segments[i].B.X - segments[i].A.X);
                }
            }

            for (int r = 0; r < rayCount; r++)
            {
                float angle = dominantAngle + (float)(2.0 * Math.PI * r / rayCount);
                float dx = (float)Math.Cos(angle);
                float dy = (float)Math.Sin(angle);

                float bestT = maxRadius;
                bool hitCurb = false;

                for (int s = 0; s < segments.Length; s++)
                {
                    CurbSegment seg = segments[s];
                    float tRay;
                    if (RaySegmentIntersect(center, dx, dy, seg.A, seg.B, out tRay))
                    {
                        float curbDistance = Math.Max(4f, tRay - seg.HalfWidth - 1.0f);
                        if (curbDistance < bestT)
                        {
                            bestT = curbDistance;
                            hitCurb = true;
                        }
                    }
                }

                if (hitCurb) curbHitCount++;
                else bestT = Math.Min(maxRadius * 0.5f, fallbackSize * 0.65f);

                hits.Add(new Point2(center.X + dx * bestT, center.Y + dy * bestT));
            }

            if (curbHitCount < 6)
            {
                return CreateRectanglePolygon(center, fallbackSize, fallbackSize, dominantAngle);
            }

            // Simplify collinear/close consecutive vertices
            List<Point2> simplified = SimplifyPolygon(hits, 2.5f);
            if (simplified.Count < 3)
            {
                return CreateRectanglePolygon(center, fallbackSize, fallbackSize, dominantAngle);
            }
            return simplified.ToArray();
        }

        private static bool RaySegmentIntersect(Point2 origin, float dx, float dy, Point2 a, Point2 b, out float tRay)
        {
            tRay = 0f;
            float sx = b.X - a.X;
            float sy = b.Y - a.Y;
            float denom = dx * sy - dy * sx;
            if (Math.Abs(denom) < 0.00001f) return false;

            float qx = a.X - origin.X;
            float qy = a.Y - origin.Y;
            tRay = (qx * sy - qy * sx) / denom;
            float u = (qx * dy - qy * dx) / denom;

            return tRay > 1.0f && u >= -0.05f && u <= 1.05f;
        }

        private static List<Point2> SimplifyPolygon(List<Point2> pts, float minDistance)
        {
            List<Point2> distFiltered = new List<Point2>();
            for (int i = 0; i < pts.Count; i++)
            {
                if (distFiltered.Count == 0 || distFiltered[distFiltered.Count - 1].DistanceTo(pts[i]) >= minDistance)
                {
                    distFiltered.Add(pts[i]);
                }
            }
            if (distFiltered.Count > 2 && distFiltered[0].DistanceTo(distFiltered[distFiltered.Count - 1]) < minDistance)
            {
                distFiltered.RemoveAt(distFiltered.Count - 1);
            }
            if (distFiltered.Count <= 4) return distFiltered;

            // Collapse nearly-collinear vertices along straight road curbs
            List<Point2> result = new List<Point2>();
            int n = distFiltered.Count;
            for (int i = 0; i < n; i++)
            {
                Point2 prev = distFiltered[(i + n - 1) % n];
                Point2 curr = distFiltered[i];
                Point2 next = distFiltered[(i + 1) % n];
                float dx1 = curr.X - prev.X, dy1 = curr.Y - prev.Y;
                float dx2 = next.X - curr.X, dy2 = next.Y - curr.Y;
                float l1 = (float)Math.Sqrt(dx1 * dx1 + dy1 * dy1);
                float l2 = (float)Math.Sqrt(dx2 * dx2 + dy2 * dy2);
                if (l1 > 0.1f && l2 > 0.1f)
                {
                    float cross = Math.Abs((dx1 * dy2 - dy1 * dx2) / (l1 * l2));
                    float dot = (dx1 * dx2 + dy1 * dy2) / (l1 * l2);
                    if (dot > 0.9f && cross < 0.12f)
                    {
                        continue; // collinear interior point on straight curb
                    }
                }
                result.Add(curr);
            }
            return result.Count >= 3 ? result : distFiltered;
        }

        /// <summary>
        /// Finds the polygon edge closest to a road segment (or the longest edge if no road segment is nearby).
        /// </summary>
        public static void FindFrontageEdge(Point2[] polygon, CurbSegment[] roads, out Point2 edgeStart, out Point2 edgeEnd, out bool hasRoadNearby)
        {
            edgeStart = new Point2(0f, 0f);
            edgeEnd = new Point2(1f, 0f);
            hasRoadNearby = false;
            if (polygon == null || polygon.Length < 3) return;

            int bestEdge = 0;
            float bestScore = float.MaxValue;
            float maxEdgeLen = -1f;
            int longestEdge = 0;

            for (int i = 0; i < polygon.Length; i++)
            {
                Point2 a = polygon[i];
                Point2 b = polygon[(i + 1) % polygon.Length];
                float len = a.DistanceTo(b);
                if (len > maxEdgeLen)
                {
                    maxEdgeLen = len;
                    longestEdge = i;
                }

                if (roads != null && roads.Length > 0 && len >= 6f)
                {
                    Point2 mid = new Point2((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f);
                    for (int r = 0; r < roads.Length; r++)
                    {
                        float t;
                        Point2 proj = ProjectOntoSegment(mid, roads[r].A, roads[r].B, out t);
                        float distToCurb = Math.Abs(mid.DistanceTo(proj) - roads[r].HalfWidth);
                        if (distToCurb < bestScore && distToCurb <= 20f)
                        {
                            bestScore = distToCurb;
                            bestEdge = i;
                            hasRoadNearby = true;
                        }
                    }
                }
            }

            int chosen = hasRoadNearby ? bestEdge : longestEdge;
            edgeStart = polygon[chosen];
            edgeEnd = polygon[(chosen + 1) % polygon.Length];
        }
    }
}
