# Metropolis Terrain Lots · Placemaker Edition (CS1)

**v0.3.0 — In-game Park, Parking Lot, Plaza & Polygonal Lot Builder for Cities: Skylines 1.**  
Compiles against the local Epic build `1.21.1-f9-epic-win` and passes 20 automated geometry and procedural layout checks. Inspired by both *Citystate Metropolis* polygonal lots and the *Placemaker* (`163638`) procedural area tool.

---

## Italiano

### Panoramica
**Metropolis Terrain Lots · Placemaker** trasforma la selezione poligonale sul terreno di Cities: Skylines 1 in uno strumento completo in-game per disegnare e generare **parcheggi strutturati, parchi urbani, giardini botanici/naturali, piazze pedonali e isolati zonizzati con cortili interni ("Outgrow")**, con anteprima in tempo reale sulla mappa e annullamento completo (`Undo`).

### Funzionalità Principali (ispirate a Placemaker)
1. **4 Strumenti di Selezione Area:**
   - **Vertici (Poligono / Points):** clicca sul terreno per tracciare qualsiasi forma poligonale, con **Snap automatico ai cordoli stradali** e **Snap ad angolo retto (90°)**.
   - **Secchiello (Riempi Isolato / Paint Bucket):** clicca all'interno di un isolato circondato da strade per rilevare i cordoli perimetrali e riempire automaticamente l'area interna.
   - **Pennello Rettangolare (Rectangle Brush):** genera istantaneamente un lotto rettangolare allineato alla strada più vicina (`16 m`, `24 m`, `32 m`, `48 m`, `64 m`).
   - **Pennello Circolare (Circle Brush):** genera una piazza o parco circolare/poligonale regolare.
2. **5 Preset Procedurali ("Cosa Costruire"):**
   - **Parcheggio (Parking Lot):** genera corsie pedonali/veicolari, file ordinate di stalli di parcheggio (`ParkingSpace` props), lampioni, alberature perimetrali ombreggianti ed eventuale asset fronte strada.
   - **Parco Urbano (City Park & Playground):** posiziona un parco fronte strada compatibile e riempie il poligono con viali principali e trasversali, panchine, lampioni e boschetti.
   - **Giardino & Natura (Garden & Nature Trail):** traccia sentieri sinuosi in ghiaia circondati da gruppi organici di alberi, cespugli e panchine panoramiche.
   - **Piazza Pedonale (Urban Plaza):** disegna percorsi pavimentati simmetrici con colonnato di alberi, lampioni e sedute.
   - **Zona + Cortile Interno (Zone + Courtyard Outgrow):** applica lo zoning `ZoneBlock` di CS1 lungo la fascia stradale (32 m) e riempie automaticamente il vuoto interno degli isolati più profondi con giardini o parcheggi condominiali.
3. **Outgrow (Bordi Organici):**
   - Smussa il confine rigido dei lotti aggiungendo siepi e vegetazione lungo i margini non stradali per integrare parchi e parcheggi col tessuto urbano circostante.
4. **Anteprima Live sulla Mappa & Annullamento Sicuro (`Undo`):**
   - Mostra in tempo reale su mappa (`RenderManager.OverlayEffect`) il cursore agganciato al cordolo, il perimetro, il fronte strada, i sentieri, gli stalli di parcheggio, gli alberi e le celle di zona prima di costruire.
   - Pulsante **Annulla Ultimo** per rimuovere con un clic tutti gli edifici, sentieri, alberi, prop e modifiche di zona dell'ultimo piazzamento.

### Compilazione e Uso
1. Eseguire `./build.ps1` in PowerShell per compilare `dist/MetropolisTerrainLots.dll` ed eseguire i 20 test geometrici e procedurali.
2. Copiare `dist/MetropolisTerrainLots.dll` in `Files/Mods/MetropolisTerrainLots/`.
3. In partita, cliccare sul pulsante flottante **`P/T`** o premere **`Alt+P`** per aprire il pannello **Metropolis Terrain Lots · Placemaker**.

---

## English

### Overview
**Metropolis Terrain Lots · Placemaker** upgrades polygonal terrain sketching in Cities: Skylines 1 into a full in-game procedural area builder for **parking lots, city parks, nature gardens, urban plazas, and zoned blocks with interior courtyard outgrow**, complete with a live map overlay and multi-step undo.

### Key Features (Placemaker-inspired)
- **4 Selection Modes:** *Points (Polygon)* with Curb & 90° Right-Angle Snapping, *Paint Bucket (Block Fill)* to automatically trace enclosed road blocks, *Rectangle Brush*, and *Circle Brush* (`16–64 m`).
- **5 Procedural Presets:** *Parking Lot*, *City Park*, *Garden & Nature*, *Urban Plaza*, and *Zone + Courtyard Outgrow*.
- **Outgrow Border Blending:** Organically blends non-road lot boundaries with hedges and shrubs so parks and car parks merge naturally with adjacent lots.
- **Live Map Overlay & Full Undo:** Previews paths, parking stalls, trees, props, and zone cells via `RenderManager.OverlayEffect` before committing, and supports one-click **Undo Last** on the simulation thread.

### Build & Run
Run `./build.ps1` in PowerShell to compile `dist/MetropolisTerrainLots.dll` and run all 20 automated checks. Press **`Alt+P`** or click the floating **`P/T`** button in-game to open the tool.

---

## Support / Donazioni

Optional / Facoltative: [Ko-fi](https://ko-fi.com/mrjonam) · [Buy Me a Coffee](https://www.buymeacoffee.com/mrjonam) · [PayPal](https://paypal.me/manorollo).

Original source: MIT. Game assemblies and third-party code are not redistributed.
