# Roadmap / Piano di sviluppo

Goal / Obiettivo: reproduce the **planning freedom** described for *Citystate Metropolis* and the procedural park/parking workflow of *Placemaker (`163638`)* within CS1's verified engine limits / riprodurre la **libertà di pianificazione** di *Citystate Metropolis* e il piazzamento procedurale di parchi e parcheggi di *Placemaker (`163638`)* entro i limiti verificati di CS1.

## Done in source / Fatto nel sorgente

- `0.1`: 32 × 32 m slope sample and polygon cell rasterizer / campione di pendenza e rasterizzazione.
- `0.2`: terrain raycast at screen centre, add/undo/clear polygon vertices, reject crossing or tiny lots, preview 8 m cell count in log; 12 geometry checks.
- `0.3 (Placemaker Edition)`:
  - **In-game interactive UI & map picking (`Alt+P` / floating `P/T` button)** with bilingual IT/EN interface.
  - **4 Selection Modes**: *Points (Polygon)* with Curb & 90° Right-Angle Snapping, *Paint Bucket (Block Fill)* to automatically fill enclosed road blocks, *Rectangle Brush*, and *Circle Brush*.
  - **Live Map Overlay (`RenderManager.OverlayEffect`)**: previews polygon boundary, road frontage, building footprint, pedestrian paths, parking bays, trees, props, and zone cells before building.
  - **5 Procedural Presets**: *Parking Lot*, *City Park*, *Garden & Nature*, *Urban Plaza*, and *Zone + Courtyard Outgrow*.
  - **Simulation-Thread Execution & Multi-Step Undo (`PlacemakerExecutor`)**: creates buildings, paths, trees, props, and zone cells safely and restores them on Undo; 20 automated checks passing.

## Next deliverables / Prossimi risultati

1. **Custom prop/tree theme palettes / Palette personalizzate di alberi e prop.** Allow picking specific loaded tree species, fence props, or decals per preset.
2. **Building bridge / Collegamento agli edifici.** Optional contract with `metropolis-building-rules` for coordinated perimeter building height rules around courtyard lots.
