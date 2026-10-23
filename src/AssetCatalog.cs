using System;
using System.Collections.Generic;
using UnityEngine;

namespace MetropolisTerrainLots
{
    public sealed class AssetCatalog
    {
        public NetInfo PavedPath;
        public NetInfo GravelPath;

        public readonly List<BuildingInfo> ParkBuildings = new List<BuildingInfo>();
        public readonly List<BuildingInfo> PlazaBuildings = new List<BuildingInfo>();
        public readonly List<BuildingInfo> ParkingBuildings = new List<BuildingInfo>();

        public readonly List<PropInfo> ParkingBayProps = new List<PropInfo>();
        public readonly List<PropInfo> StreetLampProps = new List<PropInfo>();
        public readonly List<PropInfo> ParkBenchProps = new List<PropInfo>();

        public readonly List<TreeInfo> TallTrees = new List<TreeInfo>();
        public readonly List<TreeInfo> LowBushes = new List<TreeInfo>();

        public static AssetCatalog ScanLoadedAssets()
        {
            AssetCatalog cat = new AssetCatalog();
            cat.ScanNetworks();
            cat.ScanBuildings();
            cat.ScanTrees();
            cat.ScanProps();
            return cat;
        }

        private void ScanNetworks()
        {
            int count = PrefabCollection<NetInfo>.LoadedCount();
            for (uint i = 0; i < count; i++)
            {
                NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                if (info == null || info.m_class == null) continue;

                string n = info.name ?? string.Empty;
                if (PavedPath == null && n.IndexOf("Pedestrian Pavement", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    PavedPath = info;
                }
                else if (GravelPath == null && n.IndexOf("Pedestrian Gravel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    GravelPath = info;
                }
            }

            if (PavedPath == null || GravelPath == null)
            {
                for (uint i = 0; i < count; i++)
                {
                    NetInfo info = PrefabCollection<NetInfo>.GetLoaded(i);
                    if (info == null) continue;
                    if (info.m_hasPedestrianLanes && !info.m_hasForwardVehicleLanes && !info.m_hasBackwardVehicleLanes &&
                        info.m_halfWidth <= 6f && info.m_flattenTerrain)
                    {
                        if (PavedPath == null) PavedPath = info;
                        else if (GravelPath == null) GravelPath = info;
                    }
                }
            }

            if (GravelPath == null) GravelPath = PavedPath;
            if (PavedPath == null) PavedPath = GravelPath;
        }

        private void ScanBuildings()
        {
            int count = PrefabCollection<BuildingInfo>.LoadedCount();
            for (uint i = 0; i < count; i++)
            {
                BuildingInfo info = PrefabCollection<BuildingInfo>.GetLoaded(i);
                if (info == null || info.m_buildingAI == null) continue;
                if (info.m_placementMode != BuildingInfo.PlacementMode.Roadside) continue;
                if (info.m_cellWidth < 1 || info.m_cellLength < 1) continue;

                ParkAI parkAi = info.m_buildingAI as ParkAI;
                if (parkAi == null) continue;

                string n = info.name ?? string.Empty;
                if (n.IndexOf("Parking", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Car Park", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Garage", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ParkingBuildings.Add(info);
                }
                else if (n.IndexOf("Plaza", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Square", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    PlazaBuildings.Add(info);
                    ParkBuildings.Add(info);
                }
                else
                {
                    ParkBuildings.Add(info);
                    if (info.m_hasParkingSpaces != VehicleInfo.VehicleType.None)
                    {
                        ParkingBuildings.Add(info);
                    }
                }
            }
        }

        private void ScanTrees()
        {
            int count = PrefabCollection<TreeInfo>.LoadedCount();
            for (uint i = 0; i < count; i++)
            {
                TreeInfo info = PrefabCollection<TreeInfo>.GetLoaded(i);
                if (info == null) continue;
                string n = info.name ?? string.Empty;
                if (n.IndexOf("Bush", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Shrub", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Hedge", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Plant", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    LowBushes.Add(info);
                }
                else
                {
                    TallTrees.Add(info);
                }
            }

            if (LowBushes.Count == 0 && TallTrees.Count > 0)
            {
                LowBushes.AddRange(TallTrees);
            }
            if (TallTrees.Count == 0 && LowBushes.Count > 0)
            {
                TallTrees.AddRange(LowBushes);
            }
        }

        private void ScanProps()
        {
            int count = PrefabCollection<PropInfo>.LoadedCount();
            for (uint i = 0; i < count; i++)
            {
                PropInfo info = PrefabCollection<PropInfo>.GetLoaded(i);
                if (info == null) continue;

                string n = info.name ?? string.Empty;
                if (info.m_parkingSpaces != null && info.m_parkingSpaces.Length > 0)
                {
                    ParkingBayProps.Add(info);
                    continue;
                }

                if (n.IndexOf("Street Lamp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Park Light", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Garden Light", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("Avenue Light", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    StreetLampProps.Add(info);
                }
                else if (n.IndexOf("Bench", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Picnic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         n.IndexOf("Park Table", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    ParkBenchProps.Add(info);
                }
            }
        }

        public BuildingInfo ChooseBuilding(PlacePreset preset, int maxW, int maxD, System.Random rng)
        {
            List<BuildingInfo> pool = ParkBuildings;
            if (preset == PlacePreset.ParkingLot && ParkingBuildings.Count > 0) pool = ParkingBuildings;
            else if (preset == PlacePreset.UrbanPlaza && PlazaBuildings.Count > 0) pool = PlazaBuildings;

            List<BuildingInfo> fitting = new List<BuildingInfo>();
            for (int i = 0; i < pool.Count; i++)
            {
                BuildingInfo b = pool[i];
                if (b.m_cellWidth <= maxW && b.m_cellLength <= maxD)
                {
                    fitting.Add(b);
                }
            }

            if (fitting.Count == 0 && pool != ParkBuildings)
            {
                for (int i = 0; i < ParkBuildings.Count; i++)
                {
                    BuildingInfo b = ParkBuildings[i];
                    if (b.m_cellWidth <= maxW && b.m_cellLength <= maxD)
                    {
                        fitting.Add(b);
                    }
                }
            }

            if (fitting.Count == 0) return null;
            return fitting[rng.Next(fitting.Count)];
        }

        public TreeInfo ChooseTree(bool bush, System.Random rng)
        {
            List<TreeInfo> pool = bush ? LowBushes : TallTrees;
            if (pool.Count == 0) return null;
            return pool[rng.Next(pool.Count)];
        }

        public PropInfo ChooseProp(PlannedPropKind kind, System.Random rng)
        {
            switch (kind)
            {
                case PlannedPropKind.ParkingBay:
                    if (ParkingBayProps.Count > 0) return ParkingBayProps[rng.Next(ParkingBayProps.Count)];
                    break;
                case PlannedPropKind.StreetLamp:
                    if (StreetLampProps.Count > 0) return StreetLampProps[rng.Next(StreetLampProps.Count)];
                    break;
                case PlannedPropKind.ParkBench:
                case PlannedPropKind.GardenTable:
                    if (ParkBenchProps.Count > 0) return ParkBenchProps[rng.Next(ParkBenchProps.Count)];
                    break;
            }
            return null;
        }
    }
}
