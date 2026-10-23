using System;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.UI;
using UnityEngine;

namespace MetropolisTerrainLots
{
    public sealed class PlacemakerOverlayManager : IRenderableManager
    {
        private static bool _registered;

        public static void EnsureRegistered()
        {
            if (_registered) return;
            RenderManager.RegisterRenderableManager(new PlacemakerOverlayManager());
            _registered = true;
        }

        public string GetName() { return "MetropolisPlacemakerOverlay"; }
        public DrawCallData GetDrawCallData() { return default(DrawCallData); }
        public void BeginRendering(RenderManager.CameraInfo cameraInfo) { }
        public void EndRendering(RenderManager.CameraInfo cameraInfo) { }
        public void BeginOverlay(RenderManager.CameraInfo cameraInfo) { }
        public void UndergroundOverlay(RenderManager.CameraInfo cameraInfo) { }
        public void CheckReferences() { }
        public void InitRenderData() { }

        public bool CalculateGroupData(int groupX, int groupZ, int layer, ref int vertexCount, ref int triangleCount, ref int objectCount, ref RenderGroup.VertexArrays vertexArrays)
        {
            return false;
        }

        public void PopulateGroupData(int groupX, int groupZ, int layer, ref int vertexIndex, ref int triangleIndex, Vector3 groupPosition, RenderGroup.MeshData data, ref Vector3 min, ref Vector3 max, ref float maxRenderDistance, ref float maxInstanceDistance, ref bool requireSurfaceMaps)
        {
        }

        public void EndOverlay(RenderManager.CameraInfo cameraInfo)
        {
            try
            {
                LotScanner.RenderMapOverlay(cameraInfo);
            }
            catch (Exception)
            {
                // Avoid spamming logs during render frame
            }
        }
    }

    public sealed class PlacemakerInputBehaviour : MonoBehaviour
    {
        private static PlacemakerInputBehaviour _instance;

        public static void Attach()
        {
            if (_instance != null) return;
            GameObject go = new GameObject("MetropolisPlacemakerInput");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<PlacemakerInputBehaviour>();
        }

        public static void Detach()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        private void Update()
        {
            // Toggle panel with Alt + P
            if ((Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) && Input.GetKeyDown(KeyCode.P))
            {
                PlacemakerPanel.Toggle();
            }

            if (!LotScanner.MapPickActive) return;

            // Escape exits map pick mode
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                LotScanner.MapPickActive = false;
                if (PlacemakerPanel.Instance != null) PlacemakerPanel.Instance.RefreshUI();
                return;
            }

            // Backspace undoes last vertex
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                LotScanner.UndoVertex();
                if (PlacemakerPanel.Instance != null) PlacemakerPanel.Instance.RefreshUI();
                return;
            }

            // Enter applies current valid plan
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (PlacemakerPanel.Instance != null) PlacemakerPanel.Instance.TriggerApply();
                return;
            }

            // Ignore mouse clicks over UI
            if (UIView.IsInsideUI())
            {
                LotScanner.HasHoverPoint = false;
                return;
            }

            Vector3 rawHit;
            if (!LotScanner.TryMouseHit(out rawHit))
            {
                LotScanner.HasHoverPoint = false;
                return;
            }

            LotScanner.UpdateHoverPoint(rawHit);

            if (Input.GetMouseButtonDown(0))
            {
                LotScanner.HandleMapLeftClick();
                if (PlacemakerPanel.Instance != null) PlacemakerPanel.Instance.RefreshUI();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                LotScanner.UndoVertex();
                if (PlacemakerPanel.Instance != null) PlacemakerPanel.Instance.RefreshUI();
            }
        }
    }
}
