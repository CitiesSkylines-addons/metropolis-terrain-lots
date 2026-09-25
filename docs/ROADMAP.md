# Roadmap / Piano di sviluppo

Goal / Obiettivo: reproduce the **planning freedom** described for Citystate Metropolis within CS1's verified engine limits / riprodurre la **libertà di pianificazione** descritta per Citystate Metropolis entro i limiti verificati di CS1. See [comparison](CITYSTATE_COMPARISON.md) / Vedi [confronto](CITYSTATE_COMPARISON.md).

## Done in source / Fatto nel sorgente

- `0.1`: 32 × 32 m slope sample and polygon cell rasterizer / campione di pendenza e rasterizzazione.
- Current development: terrain raycast at screen centre, add/undo/clear polygon vertices, reject crossing or tiny lots, preview 8 m cell count in log; 12 geometry checks / raycast sul terreno al centro vista, vertici aggiungi/annulla/cancella, rifiuto di lotti incrociati o minuscoli, conteggio celle da 8 m nel log; 12 controlli geometrici.

## Next deliverables / Prossimi risultati

1. **Map preview / Anteprima sulla mappa.** Render outline and selected cells with CS1 overlays. Show slope, road frontage, water and occupied-cell warnings. Acceptance / Criterio: preview changes no zone, asset or save; copied-save game test confirms no exception / l'anteprima non modifica zona, asset o salvataggio; prova su copia senza eccezioni.
2. **Free-cell zoning adapter / Adattatore celle libere.** Resolve each 8 m cell to a valid `ZoneBlock` cell, keep free road-reachable cells, record exact before/after values and write on the simulation thread. Acceptance / Criterio: preview and commit report identical cells; undo restores prior values; copied-save reload retains the mask / anteprima e applicazione indicano le stesse celle; annullamento ripristina i valori; ricaricamento conserva la maschera.
3. **Terrain-aware subdivision / Suddivisione adattiva.** Split polygons along road frontage and grade thresholds. Acceptance / Criterio: deterministic concave, border, steep, water and road cases; no orphan parcel / casi deterministici per concavità, bordi, pendenze, acqua e strade; nessun lotto isolato.
4. **Building bridge / Collegamento agli edifici.** Versioned lot-plan contract with `metropolis-building-rules`, plus safe spawn-site identification. Acceptance / Criterio: paired growth, upgrade and reload tests with both mods and absent-mod fallback / prove abbinate di crescita, avanzamento e ricaricamento con entrambe le mod e fallback se una manca.

Map generation, dynamic water and replacement of the vanilla simulation require separate feasibility projects / Generazione mappe, acqua dinamica e sostituzione della simulazione vanilla richiedono progetti separati. No dates or parity claims / Nessuna data né promessa di equivalenza.
