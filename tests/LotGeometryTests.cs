using System;
using MetropolisTerrainLots;

public static class LotGeometryTests
{
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
    }

    public static void Main()
    {
        Point2[] triangle = { new Point2(0, 0), new Point2(4, 0), new Point2(0, 4) };
        Check(LotGeometry.Contains(triangle, new Point2(1, 1)), "inside");
        Check(!LotGeometry.Contains(triangle, new Point2(3, 3)), "outside");
        Check(LotGeometry.Contains(triangle, new Point2(0, 2)), "boundary");
        bool[] cells = LotGeometry.Rasterize(triangle, 0, 0, 2, 2, 2);
        Check(cells.Length == 4 && cells[0] && cells[1] && cells[2] && !cells[3], "rasterize");
        Check(LotGeometry.Classify(new float[] { 0, 0, 0, 0 }) == SlopeClass.Flat, "flat");
        Check(LotGeometry.Classify(new float[] { 0, 2, 3, 4 }) == SlopeClass.Gentle, "gentle");
        Check(LotGeometry.Classify(new float[] { 0, 2, 3, 5 }) == SlopeClass.Steep, "steep");
        Console.WriteLine("7 geometry checks passed");
    }
}
