using System;
using MetropolisTerrainLots;

public static class LotGeometryTests
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception("Check failed: " + name);
    }

    public static void Main()
    {
        // Original 12 geometry checks
        Point2[] triangle = { new Point2(0, 0), new Point2(4, 0), new Point2(0, 4) };
        Check(LotGeometry.Contains(triangle, new Point2(1, 1)), "inside");
        Check(!LotGeometry.Contains(triangle, new Point2(3, 3)), "outside");
        Check(LotGeometry.Contains(triangle, new Point2(0, 2)), "boundary");
        bool[] cells = LotGeometry.Rasterize(triangle, 0, 0, 2, 2, 2);
        Check(cells.Length == 4 && cells[0] && cells[1] && cells[2] && !cells[3], "rasterize");
        Check(LotGeometry.Classify(new float[] { 0, 0, 0, 0 }) == SlopeClass.Flat, "flat");
        Check(LotGeometry.Classify(new float[] { 0, 2, 3, 4 }) == SlopeClass.Gentle, "gentle");
        Check(LotGeometry.Classify(new float[] { 0, 2, 3, 5 }) == SlopeClass.Steep, "steep");
        LotDraft draft = new LotDraft();
        draft.Add(new Point2(0, 0)); draft.Add(new Point2(16, 0));
        draft.Add(new Point2(16, 16)); draft.Add(new Point2(0, 16));
        Check(draft.IsValid && Math.Abs(draft.AreaSquareMeters - 256) < 0.01f, "valid area");
        Check(draft.Rasterize(0, 0, 2, 2, 8).Length == 4, "draft preview");
        draft.Undo();
        Check(draft.Count == 3 && draft.IsValid, "undo vertex");
        draft.Clear();
        Check(draft.Count == 0 && !draft.IsValid, "clear draft");
        LotDraft bow = new LotDraft();
        bow.Add(new Point2(0, 0)); bow.Add(new Point2(16, 16));
        bow.Add(new Point2(0, 16)); bow.Add(new Point2(16, 0));
        Check(!bow.IsValid, "reject self crossing");

        // Placemaker extension checks:
        // 13. Curb snapping
        CurbSegment[] roads = { new CurbSegment(new Point2(0, 0), new Point2(100, 0), 8f) };
        bool snapped;
        float roadAngle;
        Point2 snappedPt = LotGeometry.SnapToRoadCurb(new Point2(50, 10), roads, 5f, out snapped, out roadAngle);
        Check(snapped && Math.Abs(snappedPt.Y - 8f) < 0.01f, "curb snap");

        // 14. Right-angle (90 deg) snapping
        Point2 ortho = LotGeometry.SnapToRightAngle(new Point2(0, 0), new Point2(20, 0), new Point2(21, 15), 0.25f);
        Check(Math.Abs(ortho.X - 20f) < 0.01f && Math.Abs(ortho.Y - 15f) < 0.5f, "right angle snap");

        // 15. Block Fill (Paint Bucket) inside enclosed 4-road city block
        CurbSegment[] blockRoads =
        {
            new CurbSegment(new Point2(0, 0), new Point2(80, 0), 8f),
            new CurbSegment(new Point2(80, 0), new Point2(80, 80), 8f),
            new CurbSegment(new Point2(80, 80), new Point2(0, 80), 8f),
            new CurbSegment(new Point2(0, 80), new Point2(0, 0), 8f)
        };
        Point2[] blockPoly = LotGeometry.BuildBlockFillPolygon(new Point2(40, 40), blockRoads, 100f, 32f);
        LotDraft blockDraft = new LotDraft();
        blockDraft.SetPolygon(blockPoly);
        Check(blockDraft.IsValid && blockDraft.AreaSquareMeters > 900f, "paint bucket block fill");

        // 16. Parking Lot procedural generation
        PlacemakerConfig cfg = new PlacemakerConfig
        {
            Preset = PlacePreset.ParkingLot,
            Density = DensityLevel.Balanced,
            OutgrowEnabled = true,
            IncludeRoadsideBuilding = true,
            Seed = 42
        };
        PlacemakerPlan parkingPlan = PlacemakerPlanner.Generate(blockDraft, blockRoads, cfg, SlopeClass.Flat);
        Check(parkingPlan.EstimatedParkingSpaces > 0 && parkingPlan.Props.Count > 0 && parkingPlan.Paths.Count > 0, "parking lot plan");

        // 17. City Park procedural generation
        cfg.Preset = PlacePreset.CityPark;
        PlacemakerPlan parkPlan = PlacemakerPlanner.Generate(blockDraft, blockRoads, cfg, SlopeClass.Flat);
        Check(parkPlan.Paths.Count >= 2 && parkPlan.Trees.Count > 0 && parkPlan.HasRoadsideBuilding, "city park plan");

        // 18. Garden & Nature procedural generation
        cfg.Preset = PlacePreset.GardenNature;
        PlacemakerPlan gardenPlan = PlacemakerPlanner.Generate(blockDraft, blockRoads, cfg, SlopeClass.Gentle);
        Check(gardenPlan.Paths.Count > 0 && gardenPlan.Trees.Count > 5, "garden nature plan");

        // 19. Urban Plaza procedural generation
        cfg.Preset = PlacePreset.UrbanPlaza;
        PlacemakerPlan plazaPlan = PlacemakerPlanner.Generate(blockDraft, blockRoads, cfg, SlopeClass.Flat);
        Check(plazaPlan.Paths.Count >= 2 && plazaPlan.Props.Count > 0, "urban plaza plan");

        // 20. Zone + Outgrow courtyard generation on a large 120x120 block
        LotDraft bigBlock = new LotDraft();
        bigBlock.SetPolygon(LotGeometry.CreateRectanglePolygon(new Point2(60, 60), 100f, 100f, 0f));
        cfg.Preset = PlacePreset.ZoneAndOutgrow;
        PlacemakerPlan zonePlan = PlacemakerPlanner.Generate(bigBlock, blockRoads, cfg, SlopeClass.Flat);
        Check(zonePlan.ZoneCells.Count > 0 && (zonePlan.Trees.Count > 0 || zonePlan.Props.Count > 0), "zone and outgrow plan");

        Console.WriteLine("20 geometry and Placemaker layout checks passed");
    }
}
