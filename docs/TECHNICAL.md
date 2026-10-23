# Technical scope / Ambito tecnico

## English

- **Geometry & Snapping ([`LotGeometry.cs`](../src/LotGeometry.cs), [`LotDraft.cs`](../src/LotDraft.cs))**:
  - `LotGeometry.Contains` uses an even-odd polygon test and treats boundary points as inside.
  - `SnapToRoadCurb` projects the mouse terrain raycast onto the nearest road segment curb (`centerline ± normal * halfWidth`) within `9.5 m`.
  - `SnapToRightAngle` aligns new polygon edges to 0°/90°/180°/270° relative to the previous segment within `0.22 rad`.
  - `BuildBlockFillPolygon` (*Paint Bucket*) casts 16 radial rays from the clicked point against nearby road segments (`NetSegment`), stops at the inner curb line, and simplifies collinear curb points into an enclosed block polygon.
- **Procedural Layout Planner ([`PlacemakerPlanner.cs`](../src/PlacemakerPlanner.cs))**:
  - Identifies the dominant road frontage edge (`FindFrontageEdge`), sets up a local $(u, v)$ coordinate frame aligned with the frontage, and generates deterministic layouts for 5 presets: `ParkingLot`, `CityPark`, `GardenNature`, `UrbanPlaza`, and `ZoneAndOutgrow`.
  - *Outgrow* adds organic shrub/tree border blending along non-road edges and fills deep block interiors (`> 32 m` from boundary) beyond CS1's 4-cell zoning limit.
- **Live Overlay & Execution ([`PlacemakerOverlay.cs`](../src/PlacemakerOverlay.cs), [`PlacemakerExecutor.cs`](../src/PlacemakerExecutor.cs), [`AssetCatalog.cs`](../src/AssetCatalog.cs))**:
  - `PlacemakerOverlayManager : IRenderableManager` draws the live preview via `RenderManager.instance.OverlayEffect` (`DrawCircle`, `DrawSegment`).
  - `PlacemakerExecutor` runs all mutations on the simulation thread via `SimulationManager.instance.AddAction` (`BuildingManager.CreateBuilding`, `NetManager.CreateNode`/`CreateSegment`, `TreeManager.CreateTree`, `PropManager.CreateProp`, `ZoneBlock.SetZone`) and records every entity ID and prior zone state in a `Stack<PlacementRecord>` for one-click `UndoLast`.

## Italiano

- **Geometria e Snap ([`LotGeometry.cs`](../src/LotGeometry.cs), [`LotDraft.cs`](../src/LotDraft.cs))**:
  - `LotGeometry.Contains` usa la regola pari-dispari includendo i bordi.
  - `SnapToRoadCurb` aggancia il cursore al cordolo della strada più vicina (`asse ± normale * halfWidth`) entro `9,5 m`.
  - `SnapToRightAngle` allinea i nuovi lati del poligono a 90° rispetto al lato precedente.
  - `BuildBlockFillPolygon` (*Secchiello*) lancia 16 raggi radiali dal punto cliccato verso i segmenti stradali (`NetSegment`), fermandosi al cordolo interno e semplificando i vertici collineari per ottenere il poligono dell'isolato.
- **Pianificatore Procedurale ([`PlacemakerPlanner.cs`](../src/PlacemakerPlanner.cs))**:
  - Individua il fronte strada principale (`FindFrontageEdge`), costruisce un sistema di coordinate locale $(u, v)$ allineato alla strada e genera layout deterministici per 5 preset: `ParkingLot`, `CityPark`, `GardenNature`, `UrbanPlaza` e `ZoneAndOutgrow`.
  - La funzione *Outgrow* aggiunge bordi vegetali organici lungo i lati non stradali e riempie i cortili interni profondi (`> 32 m` dal bordo), superando il limite di 4 celle dello zoning vanilla di CS1.
- **Overlay Live ed Esecuzione ([`PlacemakerOverlay.cs`](../src/PlacemakerOverlay.cs), [`PlacemakerExecutor.cs`](../src/PlacemakerExecutor.cs), [`AssetCatalog.cs`](../src/AssetCatalog.cs))**:
  - `PlacemakerOverlayManager : IRenderableManager` disegna l'anteprima in tempo reale tramite `RenderManager.instance.OverlayEffect`.
  - `PlacemakerExecutor` esegue tutte le creazioni sul thread di simulazione (`SimulationManager.instance.AddAction`) e salva ogni ID creato o cella di zona modificata in uno `Stack<PlacementRecord>` per l'annullamento immediato (`UndoLast`).
