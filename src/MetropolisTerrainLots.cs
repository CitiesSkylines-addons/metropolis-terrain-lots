using ICities;
using UnityEngine;
using ColossalFramework.Math;

namespace MetropolisTerrainLots
{
    public sealed class Mod : IUserMod
    {
        public string Name { get { return "Metropolis Terrain Lots (alpha)"; } }
        public string Description { get { return "Sketches and validates a temporary polygon lot from terrain hits; no zoning writes."; } }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Metropolis Terrain Lots / Lotti sul terreno");
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

        private static bool TryCenterHit(out Vector3 point)
        {
            point = Vector3.zero;
            Camera camera = Camera.main;
            TerrainManager terrain = TerrainManager.instance;
            if (camera == null || terrain == null)
            {
                Debug.LogWarning("[MetropolisTerrainLots] Load a city before sampling / Carica una città.");
                return false;
            }
            Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
            Segment3 segment = new Segment3(ray.origin, ray.GetPoint(camera.farClipPlane));
            if (!terrain.RayCast(segment, out point))
            {
                Debug.LogWarning("[MetropolisTerrainLots] Screen centre does not hit terrain / Nessuna intersezione col terreno.");
                return false;
            }
            return true;
        }

        public static void ScanCenter()
        {
            Vector3 point;
            if (!TryCenterHit(out point)) return;
            TerrainManager terrain = TerrainManager.instance;
            float[] heights = new float[4];
            heights[0] = terrain.SampleRawHeightSmooth(point.x - 16, point.z - 16);
            heights[1] = terrain.SampleRawHeightSmooth(point.x + 16, point.z - 16);
            heights[2] = terrain.SampleRawHeightSmooth(point.x + 16, point.z + 16);
            heights[3] = terrain.SampleRawHeightSmooth(point.x - 16, point.z + 16);
            SlopeClass slope = LotGeometry.Classify(heights);
            Debug.Log("[MetropolisTerrainLots] candidate x=" + point.x.ToString("F1") +
                " z=" + point.z.ToString("F1") + " size=32x32m slope=" + slope +
                " heights=" + string.Join(",", new string[] {
                    heights[0].ToString("F1"), heights[1].ToString("F1"),
                heights[2].ToString("F1"), heights[3].ToString("F1") }));
        }

        public static void AddCenterVertex()
        {
            Vector3 point;
            if (!TryCenterHit(out point)) return;
            try
            {
                Draft.Add(new Point2(point.x, point.z));
                Debug.Log("[MetropolisTerrainLots] vertex " + Draft.Count + " x=" +
                    point.x.ToString("F1") + " z=" + point.z.ToString("F1") +
                    " elevation=" + point.y.ToString("F1") + " m; valid=" + Draft.IsValid);
            }
            catch (System.Exception error) { Debug.LogWarning("[MetropolisTerrainLots] " + error.Message); }
        }

        public static void UndoVertex()
        {
            Draft.Undo();
            Debug.Log("[MetropolisTerrainLots] vertices=" + Draft.Count);
        }

        public static void Clear()
        {
            Draft.Clear();
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
            Debug.Log("[MetropolisTerrainLots] preview only: area=" + Draft.AreaSquareMeters.ToString("F1") +
                " m2; 8m overlay cells=" + selected + "/" + cells.Length +
                "; vertices=" + Draft.Count + ". No zoning or save changes / Nessuna modifica a zone o salvataggio.");
        }
    }

    public sealed class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelUnloading()
        {
            LotScanner.Clear();
            base.OnLevelUnloading();
        }
    }
}
