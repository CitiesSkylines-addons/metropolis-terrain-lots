# Metropolis Terrain Lots · CS1

**v0.1.0-alpha.1 — experimental source release.** Compiles against the local Epic build `1.21.1-f9-epic-win`; in-game interaction has not yet been validated.

## English

The current mod adds a **32 × 32 m terrain sample** button in Mod Options. It samples four corners near the centre of the view, classifies their height range as flat (≤1 m), gentle (≤4 m), or steep (>4 m), and writes the result to `Cities_Data/output_log.txt`. The source also includes a tested polygon point-in-lot and cell rasterizer. The rasterizer is an independent geometry foundation and is not connected to CS1 zoning yet.

This alpha **does not draw persistent lots, paint zoning, place buildings, or replace the vanilla grid**. It does not change a save. CS1 growable assets remain constrained by their cell footprints; a polygon overlay cannot itself create a curved building mesh.

Run `./build.ps1` in PowerShell. The script builds `dist/MetropolisTerrainLots.dll` against installed game assemblies and runs the geometry checks. No game assembly is redistributed. To try the mod, copy only the DLL to a new folder under `Files/Mods`, enable it, load a copied save, then click the button in Mod Options. The view-centre estimate uses a sea-level plane; it can miss a steep hillside and must be checked visually.

Read [technical scope](docs/TECHNICAL.md), [roadmap](docs/ROADMAP.md), and the [suite overview](https://github.com/CitiesSkylines-addons/metropolis-performance-lab/blob/main/docs/SUITE.md).

## Italiano

La mod attuale aggiunge nelle opzioni un pulsante per **campionare 32 × 32 m di terreno**. Legge le quote dei quattro angoli vicino al centro della vista, classifica il dislivello come pianeggiante (≤1 m), lieve (≤4 m) o ripido (>4 m), e registra il risultato in `Cities_Data/output_log.txt`. Il sorgente comprende anche un motore verificato per stabilire se il centro di una cella appartiene a un poligono. Per ora questo motore non è collegato allo zoning di CS1.

Questa alfa **non disegna lotti permanenti, non dipinge zone, non piazza edifici e non sostituisce la griglia vanilla**. Non modifica il salvataggio. Gli asset growable di CS1 restano vincolati alle dimensioni in celle: un overlay poligonale non genera da solo una mesh curva.

Eseguire `./build.ps1` in PowerShell. Lo script compila `dist/MetropolisTerrainLots.dll` usando le librerie del gioco installato ed esegue i controlli geometrici. Non redistribuisce librerie del gioco. Per provarla, copiare solo la DLL in una nuova cartella sotto `Files/Mods`, abilitarla, caricare una copia del salvataggio e usare il pulsante nelle opzioni. La stima del centro vista usa il piano del livello del mare: su un pendio ripido va verificata visivamente.

Leggere [ambito tecnico](docs/TECHNICAL.md) e [roadmap](docs/ROADMAP.md).

## Discoverability / Ricerca

**Topics:** `cities-skylines`, `cities-skylines-1`, `cities-skylines-mod`, `csharp`, `terrain`, `zoning`, `polygon`

**Keywords:** terrain-aware lots, freeform zoning, polygon rasterization, slope, organic city; lotti, terreno, pendenza, zoning poligonale, città organiche.

## Support / Donazioni

Optional / Facoltative: [Ko-fi](https://ko-fi.com/mrjonam) · [Buy Me a Coffee](https://www.buymeacoffee.com/mrjonam) · [PayPal](https://paypal.me/manorollo).

Original source: MIT. Game assemblies and third-party code are not redistributed. Independent of Colossal Order, Paradox and Citystate Metropolis.
