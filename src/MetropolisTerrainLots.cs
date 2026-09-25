using ICities;
using UnityEngine;

namespace MetropolisTerrainLots
{
    public sealed class Mod : IUserMod
    {
        public string Name { get { return "Metropolis Terrain Lots (alpha)"; } }
        public string Description { get { return "Samples terrain under the camera and previews a 4x4-cell lot candidate."; } }

        public void OnSettingsUI(UIHelperBase helper)
        {
            UIHelperBase group = helper.AddGroup("Metropolis Terrain Lots / Lotti sul terreno");
            group.AddButton("Sample 32 m lot at screen centre / Campiona lotto", LotScanner.ScanCenter);
        }
    }

    public static class LotScanner
    {
        public static void ScanCenter()
        {
            Camera camera = Camera.main;
            TerrainManager terrain = TerrainManager.instance;
            if (camera == null || terrain == null)
            {
                Debug.LogWarning("[MetropolisTerrainLots] Load a city before sampling / Carica una città prima del campionamento.");
                return;
            }
            Ray ray = camera.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0));
            Plane ground = new Plane(Vector3.up, Vector3.zero);
            float distance;
            if (!ground.Raycast(ray, out distance) || distance < 0)
            {
                Debug.LogWarning("[MetropolisTerrainLots] Screen centre does not intersect the map plane.");
                return;
            }
            Vector3 point = ray.GetPoint(distance);
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
    }
}
