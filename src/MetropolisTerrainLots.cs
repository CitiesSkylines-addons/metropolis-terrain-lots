using System;
using ICities;
using UnityEngine;
using ColossalFramework.Math;

namespace MetropolisTerrainLots
{
    public sealed class Mod : IUserMod
    {
        public string Name { get { return "Metropolis Terrain Lots · Placemaker"; } }
        public string Description { get { return "Placemaker-inspired park, parking lot, plaza & polygonal lot builder with live map overlay and undo (Alt+P)."; } }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Metropolis Terrain Lots · Placemaker (Alt+P)");
            group.AddButton("Open Placemaker Panel / Apri Pannello Placemaker (Alt+P)", PlacemakerPanel.Toggle);
            group.AddButton("Sample 32 m lot at screen centre / Campiona lotto", LotScanner.ScanCenter);
            group.AddButton("Add vertex at screen centre / Aggiungi vertice", LotScanner.AddCenterVertex);
            group.AddButton("Undo last vertex / Annulla vertice", LotScanner.UndoVertex);
            group.AddButton("Preview lot cells in log / Anteprima celle nel log", LotScanner.Preview);
            group.AddButton("Clear sketch / Cancella bozza", LotScanner.Clear);
        }
    }

    public static class LotScanner
    {
        private static readonly LotDraft Draft = new LotDraft();
        public static readonly PlacemakerConfig Config = new PlacemakerConfig();

        public static PlacemakerPlan CurrentPlan { get; private set; }
        public static bool MapPickActive { get; set; }
        public static bool HasHoverPoint { get; set; }
        public static Point2 HoverPoint { get; private set; }
        public static bool HoverSnappedToCurb { get; private set; }
        public static float HoverRoadAngle { get; private set; }

        public static int VertexCount { get { return Draft.Count; } }

        public static bool TryCenterHit(out Vector3 point)
        {
            return TryScreenRayHit(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f), out point);
        }

        public static bool TryMouseHit(out Vector3 point)
        {
            return TryScreenRayHit(Input.mousePosition, out point);
        }

        private static bool TryScreenRayHit(Vector3 screenPos, out Vector3 point)
        {
            point = Vector3.zero;
            Camera camera = Camera.main;
            TerrainManager terrain = TerrainManager.instance;
            if (camera == null || terrain == null)
            {
                return false;
            }
            Ray ray = camera.ScreenPointToRay(screenPos);
            Segment3 segment = new Segment3(ray.origin, ray.GetPoint(camera.farClipPlane));
            return terrain.RayCast(segment, out point);
        }

        public static void UpdateHoverPoint(Vector3 rawWorldHit)
        {
            HasHoverPoint = true;
            Point2 candidate = new Point2(rawWorldHit.x, rawWorldHit.z);
            bool snapped = false;
            float roadAngle = 0f;

            if (Config.SnapToCurb)
            {
                CurbSegment[] roads = PlacemakerExecutor.ScanNearbyRoadCurbs(rawWorldHit, 36f);
                candidate = LotGeometry.SnapToRoadCurb(candidate, roads, 9.5f, out snapped, out roadAngle);
            }

            if (Config.SnapRightAngles && Draft.Count >= 2 && Config.Mode == SelectionMode.PolygonPoints)
            {
                Point2[] pts = Draft.Vertices();
                candidate = LotGeometry.SnapToRightAngle(pts[pts.Length - 2], pts[pts.Length - 1], candidate, 0.22f);
            }

            HoverPoint = candidate;
            HoverSnappedToCurb = snapped;
            HoverRoadAngle = roadAngle;
        }

        public static void HandleMapLeftClick()
        {
            if (!HasHoverPoint) return;
            ApplyPointByMode(HoverPoint, HoverRoadAngle);
        }

        public static void StampOrVertexAtScreenCenter()
        {
            Vector3 hit;
            if (!TryCenterHit(out hit))
            {
                Debug.LogWarning("[MetropolisTerrainLots] Load a city and aim at terrain / Carica una città e punta il terreno.");
                return;
            }
            UpdateHoverPoint(hit);
            ApplyPointByMode(HoverPoint, HoverRoadAngle);
        }

        private static void ApplyPointByMode(Point2 target, float roadAngle)
        {
            try
            {
                switch (Config.Mode)
                {
                    case SelectionMode.PolygonPoints:
                        // If clicking very close to the first vertex with >= 3 vertices, keep polygon closed
                        if (Draft.Count >= 3 && Draft.Vertices()[0].DistanceTo(target) < 4.0f)
                        {
                            RebuildPreviewPlan();
                            return;
                        }
                        Draft.Add(target);
                        break;

                    case SelectionMode.BlockFill:
                        CurbSegment[] blockRoads = PlacemakerExecutor.ScanNearbyRoadCurbs(new Vector3(target.X, 0f, target.Y), 140f);
                        Point2[] blockPoly = LotGeometry.BuildBlockFillPolygon(target, blockRoads, 110f, Config.BrushSize);
                        Draft.SetPolygon(blockPoly);
                        break;

                    case SelectionMode.RectangleBrush:
                        Point2[] rectPoly = LotGeometry.CreateRectanglePolygon(target, Config.BrushSize, Config.BrushSize, roadAngle);
                        Draft.SetPolygon(rectPoly);
                        break;

                    case SelectionMode.CircleBrush:
                        Point2[] circlePoly = LotGeometry.CreateRegularPolygon(target, Config.BrushSize * 0.5f, 12, roadAngle);
                        Draft.SetPolygon(circlePoly);
                        break;
                }

                RebuildPreviewPlan();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MetropolisTerrainLots] " + ex.Message);
            }
        }

        public static void RebuildPreviewPlan()
        {
            if (!Draft.IsValid)
            {
                CurrentPlan = null;
                return;
            }

            try
            {
                Point2 c = Draft.Centroid;
                Vector3 center3 = new Vector3(c.X, 0f, c.Y);
                CurbSegment[] nearbyRoads = PlacemakerExecutor.ScanNearbyRoadCurbs(center3, 120f);
                SlopeClass slope = SampleSlopeAt(c);
                CurrentPlan = PlacemakerPlanner.Generate(Draft, nearbyRoads, Config, slope);
            }
            catch (Exception ex)
            {
                CurrentPlan = null;
                Debug.LogWarning("[MetropolisTerrainLots] Preview generation warning: " + ex.Message);
            }
        }

        private static SlopeClass SampleSlopeAt(Point2 center)
        {
            TerrainManager terrain = TerrainManager.instance;
            if (terrain == null) return SlopeClass.Flat;
            float[] heights = new float[4];
            heights[0] = terrain.SampleRawHeightSmooth(center.X - 16f, center.Y - 16f);
            heights[1] = terrain.SampleRawHeightSmooth(center.X + 16f, center.Y - 16f);
            heights[2] = terrain.SampleRawHeightSmooth(center.X + 16f, center.Y + 16f);
            heights[3] = terrain.SampleRawHeightSmooth(center.X - 16f, center.Y + 16f);
            return LotGeometry.Classify(heights);
        }

        public static void ScanCenter()
        {
            Vector3 point;
            if (!TryCenterHit(out point)) return;
            SlopeClass slope = SampleSlopeAt(new Point2(point.x, point.z));
            Debug.Log("[MetropolisTerrainLots] candidate x=" + point.x.ToString("F1") +
                " z=" + point.z.ToString("F1") + " size=32x32m slope=" + slope);
        }

        public static void AddCenterVertex()
        {
            Vector3 point;
            if (!TryCenterHit(out point)) return;
            try
            {
                Draft.Add(new Point2(point.x, point.z));
                RebuildPreviewPlan();
                Debug.Log("[MetropolisTerrainLots] vertex " + Draft.Count + " x=" +
                    point.x.ToString("F1") + " z=" + point.z.ToString("F1") +
                    " elevation=" + point.y.ToString("F1") + " m; valid=" + Draft.IsValid);
            }
            catch (Exception error) { Debug.LogWarning("[MetropolisTerrainLots] " + error.Message); }
        }

        public static void UndoVertex()
        {
            Draft.Undo();
            RebuildPreviewPlan();
            Debug.Log("[MetropolisTerrainLots] vertices=" + Draft.Count);
        }

        public static void Clear()
        {
            Draft.Clear();
            CurrentPlan = null;
            Debug.Log("[MetropolisTerrainLots] sketch cleared / Bozza cancellata.");
        }

        public static void Preview()
        {
            if (!Draft.IsValid)
            {
                Debug.LogWarning("[MetropolisTerrainLots] Add at least 3 non-crossing vertices / Servono almeno 3 vertici senza incroci.");
                return;
            }
            Point2[] vertices = Draft.Vertices();
            float minX = vertices[0].X, maxX = minX, minZ = vertices[0].Y, maxZ = minZ;
            foreach (Point2 vertex in vertices)
            {
                minX = Mathf.Min(minX, vertex.X); maxX = Mathf.Max(maxX, vertex.X);
                minZ = Mathf.Min(minZ, vertex.Y); maxZ = Mathf.Max(maxZ, vertex.Y);
            }
            float originX = Mathf.Floor(minX / 8f) * 8f;
            float originZ = Mathf.Floor(minZ / 8f) * 8f;
            int columns = Mathf.CeilToInt((maxX - originX) / 8f);
            int rows = Mathf.CeilToInt((maxZ - originZ) / 8f);
            if ((long)columns * rows > 16384)
            {
                Debug.LogWarning("[MetropolisTerrainLots] Sketch is too large / Bozza troppo grande.");
                return;
            }
            bool[] cells = Draft.Rasterize(originX, originZ, columns, rows, 8f);
            int selected = 0;
            foreach (bool cell in cells) if (cell) selected++;
            RebuildPreviewPlan();
            Debug.Log("[MetropolisTerrainLots] preview: area=" + Draft.AreaSquareMeters.ToString("F1") +
                " m2; 8m overlay cells=" + selected + "/" + cells.Length +
                "; vertices=" + Draft.Count);
        }

        public static void RenderMapOverlay(RenderManager.CameraInfo cameraInfo)
        {
            if (RenderManager.instance == null || RenderManager.instance.OverlayEffect == null) return;
            TerrainManager tm = TerrainManager.instance;
            if (tm == null) return;

            OverlayEffect overlay = RenderManager.instance.OverlayEffect;

            // 1. Render live hover cursor when map picking is active
            if (MapPickActive && HasHoverPoint)
            {
                Vector3 hPos = ToTerrainVec3(tm, HoverPoint);
                Color cursorColor = HoverSnappedToCurb
                    ? new Color(0.2f, 0.95f, 1.0f, 0.9f)
                    : new Color(0.3f, 1.0f, 0.4f, 0.85f);
                float ringSize = Config.Mode == SelectionMode.PolygonPoints ? 4.5f : Config.BrushSize;
                overlay.DrawCircle(cameraInfo, cursorColor, hPos, ringSize, -1f, 1024f, false, true);

                // Rubber-band line from last vertex to cursor in PolygonPoints mode
                if (Config.Mode == SelectionMode.PolygonPoints && Draft.Count > 0)
                {
                    Point2[] pts = Draft.Vertices();
                    Vector3 lastPos = ToTerrainVec3(tm, pts[pts.Length - 1]);
                    overlay.DrawSegment(cameraInfo, cursorColor, new Segment3(lastPos, hPos), 0.9f, 2.5f, -1f, 1024f, false, true);
                }
            }

            if (Draft.Count == 0) return;

            // 2. Render polygon vertices & boundary segments
            Point2[] poly = Draft.Vertices();
            Color edgeColor = Draft.IsValid
                ? new Color(0.25f, 0.95f, 0.55f, 0.9f)
                : new Color(1.0f, 0.55f, 0.2f, 0.9f);

            for (int i = 0; i < poly.Length; i++)
            {
                Vector3 pA = ToTerrainVec3(tm, poly[i]);
                overlay.DrawCircle(cameraInfo, edgeColor, pA, 2.8f, -1f, 1024f, false, true);

                if (poly.Length >= 2 && (i < poly.Length - 1 || poly.Length >= 3))
                {
                    Vector3 pB = ToTerrainVec3(tm, poly[(i + 1) % poly.Length]);
                    overlay.DrawSegment(cameraInfo, edgeColor, new Segment3(pA, pB), 1.1f, 0f, -1f, 1024f, false, true);
                }
            }

            // 3. Render live Placemaker layout preview (frontage, building footprint, paths, parking stalls, trees, zone cells)
            PlacemakerPlan plan = CurrentPlan;
            if (plan == null) return;

            // Highlight road frontage edge in bright cyan
            Vector3 fA = ToTerrainVec3(tm, plan.FrontageStart);
            Vector3 fB = ToTerrainVec3(tm, plan.FrontageEnd);
            overlay.DrawSegment(cameraInfo, new Color(0.15f, 0.85f, 1.0f, 0.95f), new Segment3(fA, fB), 1.8f, 0f, -1f, 1024f, false, true);

            // Roadside building footprint circle
            if (plan.HasRoadsideBuilding)
            {
                Vector3 bPos = ToTerrainVec3(tm, plan.BuildingPosition);
                overlay.DrawCircle(cameraInfo, new Color(1.0f, 0.85f, 0.25f, 0.85f), bPos, plan.BuildingReservedRadius * 1.4f, -1f, 1024f, false, true);
            }

            // Planned pedestrian / park paths
            Color pathColor = new Color(0.95f, 0.95f, 1.0f, 0.85f);
            for (int i = 0; i < plan.Paths.Count; i++)
            {
                Vector3 sA = ToTerrainVec3(tm, plan.Paths[i].Start);
                Vector3 sB = ToTerrainVec3(tm, plan.Paths[i].End);
                overlay.DrawSegment(cameraInfo, pathColor, new Segment3(sA, sB), 2.2f, 0f, -1f, 1024f, false, true);
            }

            // Planned props (parking bays in amber, lamps/benches in warm gold)
            int maxPropsPreview = Math.Min(plan.Props.Count, 160);
            for (int i = 0; i < maxPropsPreview; i++)
            {
                PlannedProp pp = plan.Props[i];
                Vector3 pPos = ToTerrainVec3(tm, pp.Position);
                Color pCol = pp.Kind == PlannedPropKind.ParkingBay
                    ? new Color(0.95f, 0.75f, 0.2f, 0.8f)
                    : new Color(1.0f, 0.95f, 0.5f, 0.8f);
                float sz = pp.Kind == PlannedPropKind.ParkingBay ? 3.2f : 1.6f;
                overlay.DrawCircle(cameraInfo, pCol, pPos, sz, -1f, 1024f, false, true);
            }

            // Planned trees & outgrow bushes
            int maxTreesPreview = Math.Min(plan.Trees.Count, 180);
            for (int i = 0; i < maxTreesPreview; i++)
            {
                PlannedTree pt = plan.Trees[i];
                Vector3 tPos = ToTerrainVec3(tm, pt.Position);
                Color tCol = pt.IsOutgrow
                    ? new Color(0.4f, 0.95f, 0.3f, 0.75f)
                    : new Color(0.15f, 0.8f, 0.25f, 0.8f);
                overlay.DrawCircle(cameraInfo, tCol, tPos, pt.IsBush ? 2.2f : 3.8f, -1f, 1024f, false, true);
            }

            // Planned zone cells
            int maxZonesPreview = Math.Min(plan.ZoneCells.Count, 160);
            Color zCol = new Color(0.3f, 0.7f, 1.0f, 0.65f);
            for (int i = 0; i < maxZonesPreview; i++)
            {
                Vector3 zPos = ToTerrainVec3(tm, plan.ZoneCells[i]);
                overlay.DrawCircle(cameraInfo, zCol, zPos, 5.5f, -1f, 1024f, false, true);
            }
        }

        private static Vector3 ToTerrainVec3(TerrainManager tm, Point2 p)
        {
            float y = tm.SampleRawHeightSmooth(p.X, p.Y);
            return new Vector3(p.X, y, p.Y);
        }
    }

    public sealed class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);
            if (mode != LoadMode.NewGame && mode != LoadMode.LoadGame && mode != LoadMode.NewGameFromScenario)
                return;

            PlacemakerOverlayManager.EnsureRegistered();
            PlacemakerInputBehaviour.Attach();
            PlacemakerButton.EnsureCreated();
        }

        public override void OnLevelUnloading()
        {
            LotScanner.MapPickActive = false;
            LotScanner.Clear();
            PlacemakerExecutor.ClearHistoryOnUnload();
            PlacemakerPanel.DestroyPanel();
            PlacemakerButton.DestroyButton();
            PlacemakerInputBehaviour.Detach();
            base.OnLevelUnloading();
        }
    }
}
