using System;
using System.Collections.Generic;

namespace MetropolisTerrainLots
{
    public enum SelectionMode
    {
        PolygonPoints = 0,
        BlockFill = 1,
        RectangleBrush = 2,
        CircleBrush = 3
    }

    public enum PlacePreset
    {
        ParkingLot = 0,
        CityPark = 1,
        GardenNature = 2,
        UrbanPlaza = 3,
        ZoneAndOutgrow = 4
    }

    public enum DensityLevel
    {
        Sparse = 0,
        Balanced = 1,
        Lush = 2
    }

    public enum ZoneChoice
    {
        ResidentialLow = 0,
        ResidentialHigh = 1,
        CommercialLow = 2,
        CommercialHigh = 3,
        Office = 4
    }

    public enum PlannedPropKind
    {
        ParkingBay = 0,
        StreetLamp = 1,
        ParkBench = 2,
        GardenTable = 3
    }

    public struct PlannedPathSegment
    {
        public readonly Point2 Start;
        public readonly Point2 End;
        public readonly bool IsPaved;

        public PlannedPathSegment(Point2 start, Point2 end, bool isPaved)
        {
            Start = start;
            End = end;
            IsPaved = isPaved;
        }
    }

    public struct PlannedTree
    {
        public readonly Point2 Position;
        public readonly bool IsBush;
        public readonly bool IsOutgrow;

        public PlannedTree(Point2 position, bool isBush, bool isOutgrow)
        {
            Position = position;
            IsBush = isBush;
            IsOutgrow = isOutgrow;
        }
    }

    public struct PlannedProp
    {
        public readonly Point2 Position;
        public readonly float Angle;
        public readonly PlannedPropKind Kind;

        public PlannedProp(Point2 position, float angle, PlannedPropKind kind)
        {
            Position = position;
            Angle = angle;
            Kind = kind;
        }
    }

    public sealed class PlacemakerConfig
    {
        public SelectionMode Mode = SelectionMode.PolygonPoints;
        public PlacePreset Preset = PlacePreset.ParkingLot;
        public DensityLevel Density = DensityLevel.Balanced;
        public ZoneChoice Zone = ZoneChoice.ResidentialLow;
        public float BrushSize = 32f;
        public bool SnapToCurb = true;
        public bool SnapRightAngles = true;
        public bool OutgrowEnabled = true;
        public bool IncludeRoadsideBuilding = true;
        public int Seed = 1042;
    }

    public sealed class PlacemakerPlan
    {
        public Point2[] Polygon = new Point2[0];
        public float AreaSqm;
        public float PerimeterMeters;
        public SlopeClass Slope = SlopeClass.Flat;

        public Point2 FrontageStart;
        public Point2 FrontageEnd;
        public float FrontageAngle;
        public bool HasRoadFrontage;

        public bool HasRoadsideBuilding;
        public Point2 BuildingPosition;
        public float BuildingAngle;
        public int BuildingMaxWidthCells;
        public int BuildingMaxDepthCells;
        public float BuildingReservedRadius;

        public readonly List<PlannedPathSegment> Paths = new List<PlannedPathSegment>();
        public readonly List<PlannedTree> Trees = new List<PlannedTree>();
        public readonly List<PlannedProp> Props = new List<PlannedProp>();
        public readonly List<Point2> ZoneCells = new List<Point2>();

        public int EstimatedParkingSpaces;
    }

    /// <summary>
    /// Procedural layout planner inspired by Placemaker (163638), adapted to Cities: Skylines 1.
    /// Converts a valid polygon draft into functional parking lots, parks, gardens, plazas, or zoned blocks with interior outgrow.
    /// </summary>
    public static class PlacemakerPlanner
    {
        public static PlacemakerPlan Generate(LotDraft draft, CurbSegment[] nearbyRoads, PlacemakerConfig config, SlopeClass slope)
        {
            if (draft == null || !draft.IsValid)
                throw new InvalidOperationException("Draft must be a valid polygon before generating a plan.");
            if (config == null) config = new PlacemakerConfig();

            Point2[] poly = draft.Vertices();
            PlacemakerPlan plan = new PlacemakerPlan
            {
                Polygon = poly,
                AreaSqm = draft.AreaSquareMeters,
                PerimeterMeters = draft.PerimeterMeters,
                Slope = slope
            };

            Point2 fStart, fEnd;
            bool hasRoad;
            LotGeometry.FindFrontageEdge(poly, nearbyRoads, out fStart, out fEnd, out hasRoad);
            plan.FrontageStart = fStart;
            plan.FrontageEnd = fEnd;
            plan.HasRoadFrontage = hasRoad;

            float fx = fEnd.X - fStart.X;
            float fy = fEnd.Y - fStart.Y;
            float fLen = (float)Math.Sqrt(fx * fx + fy * fy);
            if (fLen < 0.1f) { fx = 1f; fy = 0f; fLen = 1f; }
            float ux = fx / fLen;
            float uy = fy / fLen;
            plan.FrontageAngle = (float)Math.Atan2(uy, ux);

            // Determine inward normal pointing toward polygon centroid
            Point2 centroid = draft.Centroid;
            Point2 fMid = new Point2((fStart.X + fEnd.X) * 0.5f, (fStart.Y + fEnd.Y) * 0.5f);
            float nx = -uy;
            float ny = ux;
            if ((centroid.X - fMid.X) * nx + (centroid.Y - fMid.Y) * ny < 0f)
            {
                nx = -nx;
                ny = -ny;
            }

            // Compute local aligned coordinates (u along frontage from fMid, v inward from fMid)
            float minU = float.MaxValue, maxU = float.MinValue;
            float minV = float.MaxValue, maxV = float.MinValue;
            for (int i = 0; i < poly.Length; i++)
            {
                float du = (poly[i].X - fMid.X) * ux + (poly[i].Y - fMid.Y) * uy;
                float dv = (poly[i].X - fMid.X) * nx + (poly[i].Y - fMid.Y) * ny;
                if (du < minU) minU = du;
                if (du > maxU) maxU = du;
                if (dv < minV) minV = dv;
                if (dv > maxV) maxV = dv;
            }

            Random rng = new Random(config.Seed);

            // Optional roadside building placement along frontage if polygon is large enough
            float usableFrontageWidth = Math.Max(fLen, (maxU - minU) * 0.6f);
            if (config.IncludeRoadsideBuilding && usableFrontageWidth >= 16f && (maxV - minV) >= 16f &&
                (config.Preset == PlacePreset.CityPark || config.Preset == PlacePreset.ParkingLot || config.Preset == PlacePreset.UrbanPlaza))
            {
                int maxW = Math.Min(8, Math.Max(2, (int)(usableFrontageWidth / 8f)));
                int maxD = Math.Min(4, Math.Max(2, (int)((maxV - minV) / 12f)));
                float depthMeters = maxD * 8f;
                Point2 bPos = ToWorld(fMid, ux, uy, nx, ny, 0f, depthMeters * 0.5f + 1.5f);
                if (LotGeometry.Contains(poly, bPos))
                {
                    plan.HasRoadsideBuilding = true;
                    plan.BuildingPosition = bPos;
                    // CS1 Building angle faces outward toward road (-nx, -ny)
                    plan.BuildingAngle = (float)Math.Atan2(-nx, -ny);
                    plan.BuildingMaxWidthCells = maxW;
                    plan.BuildingMaxDepthCells = maxD;
                    plan.BuildingReservedRadius = Math.Max(maxW, maxD) * 3.5f;
                }
            }

            switch (config.Preset)
            {
                case PlacePreset.ParkingLot:
                    BuildParkingLayout(plan, poly, fMid, ux, uy, nx, ny, minU, maxU, minV, maxV, config, rng);
                    break;
                case PlacePreset.CityPark:
                    BuildCityParkLayout(plan, poly, fMid, ux, uy, nx, ny, minU, maxU, minV, maxV, config, rng);
                    break;
                case PlacePreset.GardenNature:
                    BuildGardenNatureLayout(plan, poly, fMid, ux, uy, nx, ny, minU, maxU, minV, maxV, config, rng);
                    break;
                case PlacePreset.UrbanPlaza:
                    BuildUrbanPlazaLayout(plan, poly, fMid, ux, uy, nx, ny, minU, maxU, minV, maxV, config, rng);
                    break;
                case PlacePreset.ZoneAndOutgrow:
                    BuildZoneAndOutgrowLayout(plan, poly, fMid, ux, uy, nx, ny, minU, maxU, minV, maxV, config, rng);
                    break;
            }

            if (config.OutgrowEnabled)
            {
                AddOutgrowPerimeterPlanting(plan, poly, config, rng);
            }

            return plan;
        }

        private static void BuildParkingLayout(
            PlacemakerPlan plan, Point2[] poly, Point2 origin,
            float ux, float uy, float nx, float ny,
            float minU, float maxU, float minV, float maxV,
            PlacemakerConfig config, Random rng)
        {
            float baySpacingU = config.Density == DensityLevel.Lush ? 4.5f : (config.Density == DensityLevel.Balanced ? 5.5f : 7.0f);
            float rowSpacingV = 12.0f; // double row + drive aisle
            float bayAngle = (float)Math.Atan2(ny, nx);

            // Central access walkway / drive spine
            Point2 spineStart = ToWorld(origin, ux, uy, nx, ny, 0f, Math.Max(4f, minV + 4f));
            Point2 spineEnd = ToWorld(origin, ux, uy, nx, ny, 0f, Math.Max(8f, maxV - 4f));
            AddClippedPath(plan, poly, spineStart, spineEnd, true);

            int rowIndex = 0;
            for (float v = Math.Max(6f, minV + 6f); v <= maxV - 4f; v += rowSpacingV)
            {
                rowIndex++;
                Point2 rowLeft = ToWorld(origin, ux, uy, nx, ny, minU + 4f, v);
                Point2 rowRight = ToWorld(origin, ux, uy, nx, ny, maxU - 4f, v);
                if (rowIndex % 2 == 1)
                {
                    AddClippedPath(plan, poly, rowLeft, rowRight, true);
                }

                for (float u = minU + 4f; u <= maxU - 4f; u += baySpacingU)
                {
                    if (Math.Abs(u) < 3.5f) continue; // keep spine clear

                    // Two stalls facing each other across the row line
                    float[] offsets = { -2.8f, 2.8f };
                    for (int s = 0; s < offsets.Length; s++)
                    {
                        Point2 bayPos = ToWorld(origin, ux, uy, nx, ny, u, v + offsets[s]);
                        if (!IsPointUsable(plan, poly, bayPos, 2.2f)) continue;

                        float angle = s == 0 ? bayAngle : bayAngle + (float)Math.PI;
                        plan.Props.Add(new PlannedProp(bayPos, angle, PlannedPropKind.ParkingBay));
                        plan.EstimatedParkingSpaces += 2; // standard CS1 parking prop holds 2-4 spaces
                    }

                    // Streetlamp every 3 bays
                    if (((int)Math.Round((u - minU) / baySpacingU)) % 3 == 0)
                    {
                        Point2 lampPos = ToWorld(origin, ux, uy, nx, ny, u, v);
                        if (IsPointUsable(plan, poly, lampPos, 1.5f))
                        {
                            plan.Props.Add(new PlannedProp(lampPos, bayAngle, PlannedPropKind.StreetLamp));
                        }
                    }
                }
            }

            if (plan.HasRoadsideBuilding)
            {
                plan.EstimatedParkingSpaces += 16;
            }

            // Perimeter shade trees & islands
            AddPerimeterTrees(plan, poly, config.Density == DensityLevel.Lush ? 9f : 13f, false, rng);
        }

        private static void BuildCityParkLayout(
            PlacemakerPlan plan, Point2[] poly, Point2 origin,
            float ux, float uy, float nx, float ny,
            float minU, float maxU, float minV, float maxV,
            PlacemakerConfig config, Random rng)
        {
            float midV = (minV + maxV) * 0.5f;
            float startV = plan.HasRoadsideBuilding ? Math.Min(maxV - 4f, 18f) : Math.Max(3f, minV + 3f);

            // Main axial park promenade
            Point2 axialStart = ToWorld(origin, ux, uy, nx, ny, 0f, startV);
            Point2 axialEnd = ToWorld(origin, ux, uy, nx, ny, 0f, maxV - 4f);
            AddClippedPath(plan, poly, axialStart, axialEnd, true);

            // Cross promenade
            Point2 crossStart = ToWorld(origin, ux, uy, nx, ny, minU + 4f, midV);
            Point2 crossEnd = ToWorld(origin, ux, uy, nx, ny, maxU - 4f, midV);
            AddClippedPath(plan, poly, crossStart, crossEnd, false);

            // Benches and lamps along the main paths
            for (float v = startV + 6f; v <= maxV - 6f; v += 10f)
            {
                Point2 benchL = ToWorld(origin, ux, uy, nx, ny, -3.2f, v);
                Point2 benchR = ToWorld(origin, ux, uy, nx, ny, 3.2f, v);
                if (IsPointUsable(plan, poly, benchL, 2f))
                    plan.Props.Add(new PlannedProp(benchL, plan.FrontageAngle, PlannedPropKind.ParkBench));
                if (IsPointUsable(plan, poly, benchR, 2f))
                    plan.Props.Add(new PlannedProp(benchR, plan.FrontageAngle + (float)Math.PI, PlannedPropKind.StreetLamp));
            }

            // Tree groves in quadrants
            float step = config.Density == DensityLevel.Lush ? 7.5f : (config.Density == DensityLevel.Balanced ? 10f : 14f);
            for (float v = Math.Max(4f, minV + 5f); v <= maxV - 4f; v += step)
            {
                for (float u = minU + 5f; u <= maxU - 5f; u += step)
                {
                    if (Math.Abs(u) < 5.5f || Math.Abs(v - midV) < 5.5f) continue;
                    float jx = ((float)rng.NextDouble() - 0.5f) * step * 0.45f;
                    float jy = ((float)rng.NextDouble() - 0.5f) * step * 0.45f;
                    Point2 tPos = ToWorld(origin, ux, uy, nx, ny, u + jx, v + jy);
                    if (IsPointUsable(plan, poly, tPos, 2.5f))
                    {
                        bool isBush = rng.NextDouble() < 0.35;
                        plan.Trees.Add(new PlannedTree(tPos, isBush, false));
                    }
                }
            }

            AddPerimeterTrees(plan, poly, 10f, false, rng);
        }

        private static void BuildGardenNatureLayout(
            PlacemakerPlan plan, Point2[] poly, Point2 origin,
            float ux, float uy, float nx, float ny,
            float minU, float maxU, float minV, float maxV,
            PlacemakerConfig config, Random rng)
        {
            // Winding gravel trail from frontage to deep end
            int segments = Math.Max(2, Math.Min(6, (int)((maxV - minV) / 14f)));
            float prevU = 0f;
            float prevV = Math.Max(3f, minV + 3f);
            float vSpan = Math.Max(8f, (maxV - 4f) - prevV);
            float amp = Math.Min(12f, (maxU - minU) * 0.22f);

            for (int i = 1; i <= segments; i++)
            {
                float t = (float)i / segments;
                float nextV = prevV + vSpan / segments;
                float nextU = (float)Math.Sin(t * Math.PI * 2.0) * amp;
                Point2 pA = ToWorld(origin, ux, uy, nx, ny, prevU, prevV);
                Point2 pB = ToWorld(origin, ux, uy, nx, ny, nextU, nextV);
                AddClippedPath(plan, poly, pA, pB, false);

                Point2 benchPos = ToWorld(origin, ux, uy, nx, ny, nextU + 3f, nextV);
                if (IsPointUsable(plan, poly, benchPos, 2f))
                {
                    plan.Props.Add(new PlannedProp(benchPos, plan.FrontageAngle, PlannedPropKind.ParkBench));
                }

                prevU = nextU;
                prevV = nextV;
            }

            // Lush organic clusters of trees & bushes
            float step = config.Density == DensityLevel.Lush ? 6.0f : (config.Density == DensityLevel.Balanced ? 8.5f : 12.0f);
            for (float v = minV + 3f; v <= maxV - 3f; v += step)
            {
                for (float u = minU + 3f; u <= maxU - 3f; u += step)
                {
                    float jx = ((float)rng.NextDouble() - 0.5f) * step * 0.6f;
                    float jy = ((float)rng.NextDouble() - 0.5f) * step * 0.6f;
                    Point2 tPos = ToWorld(origin, ux, uy, nx, ny, u + jx, v + jy);
                    if (!IsPointUsable(plan, poly, tPos, 2.0f)) continue;
                    if (DistanceToPlannedPaths(plan, tPos) < 4.2f) continue;

                    bool isBush = rng.NextDouble() < 0.45;
                    plan.Trees.Add(new PlannedTree(tPos, isBush, false));
                }
            }
        }

        private static void BuildUrbanPlazaLayout(
            PlacemakerPlan plan, Point2[] poly, Point2 origin,
            float ux, float uy, float nx, float ny,
            float minU, float maxU, float minV, float maxV,
            PlacemakerConfig config, Random rng)
        {
            float midV = (minV + maxV) * 0.5f;
            float startV = plan.HasRoadsideBuilding ? Math.Min(maxV - 4f, 18f) : Math.Max(3f, minV + 3f);

            // Symmetrical paved cross + diagonal/perimeter walkways
            AddClippedPath(plan, poly,
                ToWorld(origin, ux, uy, nx, ny, 0f, startV),
                ToWorld(origin, ux, uy, nx, ny, 0f, maxV - 4f), true);
            AddClippedPath(plan, poly,
                ToWorld(origin, ux, uy, nx, ny, minU + 4f, midV),
                ToWorld(origin, ux, uy, nx, ny, maxU - 4f, midV), true);

            // Colonnade of trees, benches, and lamps around the central axis
            float step = config.Density == DensityLevel.Lush ? 8f : 11f;
            for (float u = minU + 6f; u <= maxU - 6f; u += step)
            {
                if (Math.Abs(u) < 4f) continue;
                Point2 treeTop = ToWorld(origin, ux, uy, nx, ny, u, midV + 5.5f);
                Point2 treeBot = ToWorld(origin, ux, uy, nx, ny, u, midV - 5.5f);
                if (IsPointUsable(plan, poly, treeTop, 2f))
                    plan.Trees.Add(new PlannedTree(treeTop, false, false));
                if (IsPointUsable(plan, poly, treeBot, 2f))
                    plan.Trees.Add(new PlannedTree(treeBot, false, false));

                Point2 lamp = ToWorld(origin, ux, uy, nx, ny, u, midV + 2.8f);
                if (IsPointUsable(plan, poly, lamp, 1.5f))
                    plan.Props.Add(new PlannedProp(lamp, plan.FrontageAngle, PlannedPropKind.StreetLamp));

                Point2 bench = ToWorld(origin, ux, uy, nx, ny, u, midV - 2.8f);
                if (IsPointUsable(plan, poly, bench, 1.5f))
                    plan.Props.Add(new PlannedProp(bench, plan.FrontageAngle, PlannedPropKind.ParkBench));
            }

            AddPerimeterTrees(plan, poly, 9f, true, rng);
        }

        /// <summary>
        /// Placemaker "Zoning + Outgrow" mode:
        /// Maps 8 m cells within 32 m of the perimeter/frontage for CS1 ZoneBlock zoning,
        /// and fills the deeper interior courtyard (> 28 m from frontage) with green gardens, paths, and courtyard parking!
        /// </summary>
        private static void BuildZoneAndOutgrowLayout(
            PlacemakerPlan plan, Point2[] poly, Point2 origin,
            float ux, float uy, float nx, float ny,
            float minU, float maxU, float minV, float maxV,
            PlacemakerConfig config, Random rng)
        {
            for (float v = Math.Max(4f, minV + 4f); v <= maxV - 4f; v += 8f)
            {
                for (float u = minU + 4f; u <= maxU - 4f; u += 8f)
                {
                    Point2 cellCenter = ToWorld(origin, ux, uy, nx, ny, u, v);
                    if (!LotGeometry.Contains(poly, cellCenter)) continue;

                    float distToBoundary = LotGeometry.DistanceToPolygonBoundary(poly, cellCenter);
                    if (distToBoundary <= 32f)
                    {
                        plan.ZoneCells.Add(cellCenter);
                    }
                    else
                    {
                        // Interior courtyard ("Outgrow" space behind buildings)
                        if (rng.NextDouble() < 0.55)
                        {
                            plan.Trees.Add(new PlannedTree(cellCenter, rng.NextDouble() < 0.4, true));
                        }
                        else
                        {
                            plan.Props.Add(new PlannedProp(cellCenter, plan.FrontageAngle, PlannedPropKind.ParkingBay));
                            plan.EstimatedParkingSpaces += 2;
                        }
                    }
                }
            }

            // Add a rear garden/courtyard connector path if the lot is deep
            if (maxV - minV >= 40f)
            {
                float midV = (minV + maxV) * 0.5f;
                AddClippedPath(plan, poly,
                    ToWorld(origin, ux, uy, nx, ny, minU + 8f, midV),
                    ToWorld(origin, ux, uy, nx, ny, maxU - 8f, midV), false);
            }
        }

        private static void AddPerimeterTrees(PlacemakerPlan plan, Point2[] poly, float spacing, bool bushesOnly, Random rng)
        {
            if (spacing < 4f) spacing = 8f;
            for (int i = 0; i < poly.Length; i++)
            {
                Point2 a = poly[i];
                Point2 b = poly[(i + 1) % poly.Length];
                float len = a.DistanceTo(b);
                if (len < spacing) continue;

                int steps = (int)(len / spacing);
                for (int s = 1; s < steps; s++)
                {
                    float t = (float)s / steps;
                    Point2 edgePt = new Point2(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
                    // Inset slightly toward centroid
                    float cx = 0f, cy = 0f;
                    for (int k = 0; k < poly.Length; k++) { cx += poly[k].X; cy += poly[k].Y; }
                    cx /= poly.Length; cy /= poly.Length;
                    float dx = cx - edgePt.X;
                    float dy = cy - edgePt.Y;
                    float dLen = (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dLen < 2f) continue;

                    Point2 insetPt = new Point2(edgePt.X + (dx / dLen) * 2.2f, edgePt.Y + (dy / dLen) * 2.2f);
                    if (!IsPointUsable(plan, poly, insetPt, 1.5f)) continue;
                    if (DistanceToPlannedPaths(plan, insetPt) < 3.5f) continue;

                    bool bush = bushesOnly || (rng.NextDouble() < 0.35);
                    plan.Trees.Add(new PlannedTree(insetPt, bush, false));
                }
            }
        }

        private static void AddOutgrowPerimeterPlanting(PlacemakerPlan plan, Point2[] poly, PlacemakerConfig config, Random rng)
        {
            float spacing = config.Density == DensityLevel.Lush ? 6.5f : 10.5f;
            for (int i = 0; i < poly.Length; i++)
            {
                Point2 a = poly[i];
                Point2 b = poly[(i + 1) % poly.Length];
                // Skip frontage edge so we don't block road access
                if (a.DistanceTo(plan.FrontageStart) < 1f && b.DistanceTo(plan.FrontageEnd) < 1f) continue;

                float len = a.DistanceTo(b);
                int count = (int)(len / spacing);
                for (int s = 1; s <= count; s++)
                {
                    float t = (float)s / (count + 1);
                    float jx = ((float)rng.NextDouble() - 0.5f) * 2.0f;
                    float jy = ((float)rng.NextDouble() - 0.5f) * 2.0f;
                    Point2 pt = new Point2(a.X + (b.X - a.X) * t + jx, a.Y + (b.Y - a.Y) * t + jy);
                    if (DistanceToPlannedPaths(plan, pt) < 3.5f) continue;
                    plan.Trees.Add(new PlannedTree(pt, true, true));
                }
            }
        }

        private static void AddClippedPath(PlacemakerPlan plan, Point2[] poly, Point2 start, Point2 end, bool isPaved)
        {
            float dist = start.DistanceTo(end);
            if (dist < 6f) return;

            int samples = Math.Max(4, (int)(dist / 2f));
            bool inRun = false;
            Point2 runStart = start;
            Point2 lastInside = start;

            for (int i = 0; i <= samples; i++)
            {
                float t = (float)i / samples;
                Point2 p = new Point2(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
                bool ok = LotGeometry.Contains(poly, p) &&
                          (!plan.HasRoadsideBuilding || p.DistanceTo(plan.BuildingPosition) > plan.BuildingReservedRadius);

                if (ok)
                {
                    if (!inRun)
                    {
                        inRun = true;
                        runStart = p;
                    }
                    lastInside = p;
                }
                else if (inRun)
                {
                    if (runStart.DistanceTo(lastInside) >= 6f)
                    {
                        plan.Paths.Add(new PlannedPathSegment(runStart, lastInside, isPaved));
                    }
                    inRun = false;
                }
            }

            if (inRun && runStart.DistanceTo(lastInside) >= 6f)
            {
                plan.Paths.Add(new PlannedPathSegment(runStart, lastInside, isPaved));
            }
        }

        private static bool IsPointUsable(PlacemakerPlan plan, Point2[] poly, Point2 pt, float marginFromBoundary)
        {
            if (!LotGeometry.Contains(poly, pt)) return false;
            if (marginFromBoundary > 0f && LotGeometry.DistanceToPolygonBoundary(poly, pt) < marginFromBoundary) return false;
            if (plan.HasRoadsideBuilding && pt.DistanceTo(plan.BuildingPosition) <= plan.BuildingReservedRadius + 2f) return false;
            return true;
        }

        private static float DistanceToPlannedPaths(PlacemakerPlan plan, Point2 pt)
        {
            float best = float.MaxValue;
            for (int i = 0; i < plan.Paths.Count; i++)
            {
                float t;
                Point2 proj = LotGeometry.ProjectOntoSegment(pt, plan.Paths[i].Start, plan.Paths[i].End, out t);
                float d = pt.DistanceTo(proj);
                if (d < best) best = d;
            }
            return best;
        }

        private static Point2 ToWorld(Point2 origin, float ux, float uy, float nx, float ny, float u, float v)
        {
            return new Point2(
                origin.X + ux * u + nx * v,
                origin.Y + uy * u + ny * v);
        }
    }
}
