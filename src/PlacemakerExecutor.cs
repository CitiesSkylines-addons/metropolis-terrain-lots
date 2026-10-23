using System;
using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace MetropolisTerrainLots
{
    public struct ZoneCellRestore
    {
        public ushort BlockId;
        public int Col;
        public int Row;
        public ItemClass.Zone PreviousZone;
    }

    public sealed class PlacementRecord
    {
        public readonly List<ushort> Buildings = new List<ushort>();
        public readonly List<ushort> Segments = new List<ushort>();
        public readonly List<ushort> Nodes = new List<ushort>();
        public readonly List<uint> Trees = new List<uint>();
        public readonly List<ushort> Props = new List<ushort>();
        public readonly List<ZoneCellRestore> ZoneRestores = new List<ZoneCellRestore>();

        public int TotalItems
        {
            get { return Buildings.Count + Segments.Count + Trees.Count + Props.Count + ZoneRestores.Count; }
        }
    }

    public static class PlacemakerExecutor
    {
        private static readonly Stack<PlacementRecord> History = new Stack<PlacementRecord>();
        private static AssetCatalog _catalog;

        public static int HistoryCount { get { return History.Count; } }

        public static void ClearHistoryOnUnload()
        {
            History.Clear();
            _catalog = null;
        }

        public static CurbSegment[] ScanNearbyRoadCurbs(Vector3 center, float radius)
        {
            List<CurbSegment> list = new List<CurbSegment>();
            if (!Singleton<NetManager>.exists) return list.ToArray();

            NetManager nm = Singleton<NetManager>.instance;
            float rSq = radius * radius;
            int maxSegs = nm.m_segments.m_buffer.Length;

            for (ushort s = 1; s < maxSegs; s++)
            {
                NetSegment seg = nm.m_segments.m_buffer[s];
                if ((seg.m_flags & NetSegment.Flags.Created) == 0) continue;
                if ((seg.m_flags & NetSegment.Flags.Deleted) != 0) continue;

                NetInfo info = seg.Info;
                if (info == null || info.m_class == null) continue;
                if (info.m_class.m_service != ItemClass.Service.Road) continue;

                float dx = seg.m_middlePosition.x - center.x;
                float dz = seg.m_middlePosition.z - center.z;
                if (dx * dx + dz * dz > rSq) continue;

                Vector3 posA = nm.m_nodes.m_buffer[seg.m_startNode].m_position;
                Vector3 posB = nm.m_nodes.m_buffer[seg.m_endNode].m_position;
                list.Add(new CurbSegment(new Point2(posA.x, posA.z), new Point2(posB.x, posB.z), info.m_halfWidth));
            }
            return list.ToArray();
        }

        public static void ExecuteOnSimulationThread(PlacemakerPlan plan, PlacemakerConfig config, Action<PlacementRecord, string> onCompleted)
        {
            if (plan == null)
            {
                if (onCompleted != null) onCompleted(null, "Null plan");
                return;
            }

            if (_catalog == null)
            {
                _catalog = AssetCatalog.ScanLoadedAssets();
            }

            AssetCatalog catalog = _catalog;
            Singleton<SimulationManager>.instance.AddAction(delegate
            {
                PlacementRecord rec = new PlacementRecord();
                string err = null;
                try
                {
                    ExecuteImmediate(plan, config, catalog, rec);
                    if (rec.TotalItems > 0)
                    {
                        History.Push(rec);
                    }
                }
                catch (Exception ex)
                {
                    err = ex.Message;
                    Debug.LogWarning("[MetropolisTerrainLots] Execution error: " + ex);
                }

                if (onCompleted != null)
                {
                    onCompleted(rec, err);
                }
            });
        }

        private static void ExecuteImmediate(PlacemakerPlan plan, PlacemakerConfig config, AssetCatalog catalog, PlacementRecord rec)
        {
            SimulationManager sim = Singleton<SimulationManager>.instance;
            TerrainManager tm = Singleton<TerrainManager>.instance;
            BuildingManager bm = Singleton<BuildingManager>.instance;
            NetManager nm = Singleton<NetManager>.instance;
            TreeManager trm = Singleton<TreeManager>.instance;
            PropManager pm = Singleton<PropManager>.instance;
            ZoneManager zm = Singleton<ZoneManager>.instance;

            System.Random rng = new System.Random(config.Seed);

            // 1. Roadside Park / Parking / Plaza Building
            if (plan.HasRoadsideBuilding && catalog != null)
            {
                BuildingInfo bInfo = catalog.ChooseBuilding(config.Preset, plan.BuildingMaxWidthCells, plan.BuildingMaxDepthCells, rng);
                if (bInfo != null)
                {
                    Vector3 bPos = new Vector3(plan.BuildingPosition.X, 0f, plan.BuildingPosition.Y);
                    bPos.y = tm.SampleRawHeightSmooth(bPos);
                    ushort bId;
                    if (bm.CreateBuilding(out bId, ref sim.m_randomizer, bInfo, bPos, plan.BuildingAngle, bInfo.m_cellLength, sim.m_currentBuildIndex++))
                    {
                        rec.Buildings.Add(bId);
                    }
                }
            }

            // 2. Pedestrian / Park Paths
            if (catalog != null && (catalog.PavedPath != null || catalog.GravelPath != null))
            {
                for (int i = 0; i < plan.Paths.Count; i++)
                {
                    PlannedPathSegment seg = plan.Paths[i];
                    NetInfo pathInfo = seg.IsPaved ? (catalog.PavedPath ?? catalog.GravelPath) : (catalog.GravelPath ?? catalog.PavedPath);
                    if (pathInfo == null) continue;

                    Vector3 startPos = new Vector3(seg.Start.X, 0f, seg.Start.Y);
                    startPos.y = tm.SampleRawHeightSmooth(startPos);
                    Vector3 endPos = new Vector3(seg.End.X, 0f, seg.End.Y);
                    endPos.y = tm.SampleRawHeightSmooth(endPos);

                    if ((endPos - startPos).sqrMagnitude < 36f) continue;

                    ushort nA, nB;
                    if (nm.CreateNode(out nA, ref sim.m_randomizer, pathInfo, startPos, sim.m_currentBuildIndex++))
                    {
                        rec.Nodes.Add(nA);
                        if (nm.CreateNode(out nB, ref sim.m_randomizer, pathInfo, endPos, sim.m_currentBuildIndex++))
                        {
                            rec.Nodes.Add(nB);
                            Vector3 dir = (endPos - startPos).normalized;
                            ushort segId;
                            if (nm.CreateSegment(out segId, ref sim.m_randomizer, pathInfo, nA, nB, dir, -dir, sim.m_currentBuildIndex++, sim.m_currentBuildIndex, false))
                            {
                                rec.Segments.Add(segId);
                            }
                        }
                    }
                }
            }

            // 3. Props (Parking Bays, Street Lamps, Park Benches)
            if (catalog != null)
            {
                for (int i = 0; i < plan.Props.Count; i++)
                {
                    PlannedProp pp = plan.Props[i];
                    PropInfo pInfo = catalog.ChooseProp(pp.Kind, rng);
                    if (pInfo != null)
                    {
                        Vector3 pPos = new Vector3(pp.Position.X, 0f, pp.Position.Y);
                        pPos.y = tm.SampleRawHeightSmooth(pPos);
                        ushort propId;
                        if (pm.CreateProp(out propId, ref sim.m_randomizer, pInfo, pPos, pp.Angle, true))
                        {
                            rec.Props.Add(propId);
                        }
                    }
                    else if (pp.Kind == PlannedPropKind.ParkingBay)
                    {
                        // Fallback if no parking prop is loaded: plant a neat hedge/shrub marker
                        TreeInfo fallbackBush = catalog.ChooseTree(true, rng);
                        if (fallbackBush != null)
                        {
                            Vector3 tPos = new Vector3(pp.Position.X, 0f, pp.Position.Y);
                            tPos.y = tm.SampleRawHeightSmooth(tPos);
                            uint treeId;
                            if (trm.CreateTree(out treeId, ref sim.m_randomizer, fallbackBush, tPos, true))
                            {
                                rec.Trees.Add(treeId);
                            }
                        }
                    }
                }
            }

            // 4. Trees & Bushes (including Outgrow perimeter planting)
            if (catalog != null)
            {
                for (int i = 0; i < plan.Trees.Count; i++)
                {
                    PlannedTree pt = plan.Trees[i];
                    TreeInfo tInfo = catalog.ChooseTree(pt.IsBush, rng);
                    if (tInfo == null) continue;

                    Vector3 tPos = new Vector3(pt.Position.X, 0f, pt.Position.Y);
                    tPos.y = tm.SampleRawHeightSmooth(tPos);
                    uint treeId;
                    if (trm.CreateTree(out treeId, ref sim.m_randomizer, tInfo, tPos, true))
                    {
                        rec.Trees.Add(treeId);
                    }
                }
            }

            // 5. ZoneBlock Adapter for ZoneAndOutgrow preset
            if (plan.ZoneCells.Count > 0 && zm != null)
            {
                ApplyPolygonZoning(plan, config, zm, rec);
            }
        }

        private static void ApplyPolygonZoning(PlacemakerPlan plan, PlacemakerConfig config, ZoneManager zm, PlacementRecord rec)
        {
            ItemClass.Zone targetZone = ToGameZone(config.Zone);
            if (targetZone == ItemClass.Zone.None) return;

            float minX = plan.Polygon[0].X, maxX = minX, minZ = plan.Polygon[0].Y, maxZ = minZ;
            for (int i = 1; i < plan.Polygon.Length; i++)
            {
                if (plan.Polygon[i].X < minX) minX = plan.Polygon[i].X;
                if (plan.Polygon[i].X > maxX) maxX = plan.Polygon[i].X;
                if (plan.Polygon[i].Y < minZ) minZ = plan.Polygon[i].Y;
                if (plan.Polygon[i].Y > maxZ) maxZ = plan.Polygon[i].Y;
            }

            int maxBlocks = zm.m_blocks.m_buffer.Length;
            for (ushort bId = 1; bId < maxBlocks; bId++)
            {
                ZoneBlock block = zm.m_blocks.m_buffer[bId];
                if ((block.m_flags & ZoneBlock.FLAG_CREATED) == 0) continue;
                if ((block.m_flags & ZoneBlock.FLAG_DELETED) != 0) continue;

                if (block.m_position.x < minX - 40f || block.m_position.x > maxX + 40f ||
                    block.m_position.z < minZ - 40f || block.m_position.z > maxZ + 40f) continue;

                Vector3 axisX = new Vector3(Mathf.Cos(block.m_angle), 0f, Mathf.Sin(block.m_angle)) * 8f;
                Vector3 axisZ = new Vector3(axisX.z, 0f, -axisX.x);
                int rows = block.RowCount;
                bool changed = false;

                for (int r = 0; r < rows; r++)
                {
                    Vector3 rowOffset = (r - 3.5f) * axisZ;
                    for (int c = 0; c < 4; c++)
                    {
                        if ((block.m_valid & (1UL << ((r << 3) | c))) == 0UL) continue;

                        Vector3 colOffset = (c - 3.5f) * axisX;
                        Vector3 cellPos = block.m_position + colOffset + rowOffset;
                        Point2 pt = new Point2(cellPos.x, cellPos.z);

                        if (!LotGeometry.Contains(plan.Polygon, pt)) continue;

                        ItemClass.Zone prev = block.GetZone(c, r);
                        if (prev != targetZone && block.SetZone(c, r, targetZone))
                        {
                            rec.ZoneRestores.Add(new ZoneCellRestore
                            {
                                BlockId = bId,
                                Col = c,
                                Row = r,
                                PreviousZone = prev
                            });
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    block.RefreshZoning(bId);
                    zm.m_blocks.m_buffer[bId] = block;
                }
            }
        }

        public static void UndoLast(Action<int> onDone)
        {
            if (History.Count == 0)
            {
                if (onDone != null) onDone(0);
                return;
            }

            Singleton<SimulationManager>.instance.AddAction(delegate
            {
                int removed = 0;
                if (History.Count > 0)
                {
                    PlacementRecord rec = History.Pop();
                    removed = UndoRecordImmediate(rec);
                }
                if (onDone != null) onDone(removed);
            });
        }

        private static int UndoRecordImmediate(PlacementRecord rec)
        {
            int count = 0;
            BuildingManager bm = Singleton<BuildingManager>.instance;
            NetManager nm = Singleton<NetManager>.instance;
            TreeManager trm = Singleton<TreeManager>.instance;
            PropManager pm = Singleton<PropManager>.instance;
            ZoneManager zm = Singleton<ZoneManager>.instance;

            for (int i = rec.Props.Count - 1; i >= 0; i--)
            {
                ushort id = rec.Props[i];
                if (id != 0 && (pm.m_props.m_buffer[id].m_flags & (ushort)PropInstance.Flags.Created) != 0)
                {
                    pm.ReleaseProp(id);
                    count++;
                }
            }

            for (int i = rec.Trees.Count - 1; i >= 0; i--)
            {
                uint id = rec.Trees[i];
                if (id != 0 && (trm.m_trees.m_buffer[id].m_flags & (ushort)TreeInstance.Flags.Created) != 0)
                {
                    trm.ReleaseTree(id);
                    count++;
                }
            }

            for (int i = rec.Segments.Count - 1; i >= 0; i--)
            {
                ushort id = rec.Segments[i];
                if (id != 0 && (nm.m_segments.m_buffer[id].m_flags & NetSegment.Flags.Created) != 0)
                {
                    nm.ReleaseSegment(id, false);
                    count++;
                }
            }

            for (int i = rec.Nodes.Count - 1; i >= 0; i--)
            {
                ushort id = rec.Nodes[i];
                if (id != 0 && (nm.m_nodes.m_buffer[id].m_flags & NetNode.Flags.Created) != 0)
                {
                    nm.ReleaseNode(id);
                }
            }

            for (int i = rec.Buildings.Count - 1; i >= 0; i--)
            {
                ushort id = rec.Buildings[i];
                if (id != 0 && (bm.m_buildings.m_buffer[id].m_flags & Building.Flags.Created) != 0)
                {
                    bm.ReleaseBuilding(id);
                    count++;
                }
            }

            HashSet<ushort> dirtyBlocks = new HashSet<ushort>();
            for (int i = rec.ZoneRestores.Count - 1; i >= 0; i--)
            {
                ZoneCellRestore zr = rec.ZoneRestores[i];
                ZoneBlock block = zm.m_blocks.m_buffer[zr.BlockId];
                if ((block.m_flags & ZoneBlock.FLAG_CREATED) != 0)
                {
                    block.SetZone(zr.Col, zr.Row, zr.PreviousZone);
                    zm.m_blocks.m_buffer[zr.BlockId] = block;
                    dirtyBlocks.Add(zr.BlockId);
                    count++;
                }
            }

            foreach (ushort bId in dirtyBlocks)
            {
                zm.m_blocks.m_buffer[bId].RefreshZoning(bId);
            }

            return count;
        }

        private static ItemClass.Zone ToGameZone(ZoneChoice zc)
        {
            switch (zc)
            {
                case ZoneChoice.ResidentialLow: return ItemClass.Zone.ResidentialLow;
                case ZoneChoice.ResidentialHigh: return ItemClass.Zone.ResidentialHigh;
                case ZoneChoice.CommercialLow: return ItemClass.Zone.CommercialLow;
                case ZoneChoice.CommercialHigh: return ItemClass.Zone.CommercialHigh;
                case ZoneChoice.Office: return ItemClass.Zone.Office;
                default: return ItemClass.Zone.ResidentialLow;
            }
        }
    }
}
