# Technical scope / Ambito tecnico

## English

`LotGeometry.Contains` uses an even-odd polygon test and treats points on an edge as inside. `Rasterize` tests each cell centre, so its boolean result is an overlay mask and not an instruction to alter `ZoneBlock`. `Classify` uses the maximum minus minimum of four terrain heights. The current in-game button samples raw terrain height around an approximate centre-of-view point. No existing zoning cells are written.

CS1's installed assemblies expose `ZoneBlock.SetZone`, `ZoneBlock.GetZone`, `ZoneManager.m_blocks`, and `TerrainManager.SampleRawHeightSmooth`. A future zoning adapter must map each selected polygon cell to a valid vanilla zone block, check road access, terrain and occupancy, and use the game's update path. It must preview exactly which cells will change and support undo on a copied save before any write feature is released.

## Italiano

`LotGeometry.Contains` usa la regola pari-dispari e considera interni i punti sul bordo. `Rasterize` verifica il centro di ogni cella: il risultato è una maschera per un overlay, non un ordine di modifica di `ZoneBlock`. `Classify` calcola la differenza tra la quota massima e minima dei quattro angoli. Il pulsante nel gioco campiona il terreno grezzo vicino a un centro vista approssimato. Non scrive celle di zoning.

Le librerie installate di CS1 espongono `ZoneBlock.SetZone`, `ZoneBlock.GetZone`, `ZoneManager.m_blocks` e `TerrainManager.SampleRawHeightSmooth`. Un futuro adattatore dovrà collegare ogni cella poligonale a un blocco vanilla valido, verificare accesso stradale, terreno e occupazione, usare il percorso di aggiornamento del gioco, mostrare esattamente le celle coinvolte e offrire annullamento su un salvataggio copiato prima di rilasciare modifiche.
