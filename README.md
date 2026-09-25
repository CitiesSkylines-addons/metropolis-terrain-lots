# Metropolis Terrain Lots · CS1

**Development toward v0.2.0-alpha.1.** Compiles against the local Epic build `1.21.1-f9-epic-win`; in-game interaction has not yet been validated.

## English

The current mod adds a **32 × 32 m terrain sample** button in Mod Options. It raycasts the actual terrain at screen centre, samples four corners, classifies their height range as flat (≤1 m), gentle (≤4 m), or steep (>4 m), and writes the result to `Cities_Data/output_log.txt`. New buttons add, undo and clear polygon vertices at screen centre, then preview the count of selected 8 m cells and lot area in the log. The polygon validator rejects crossing edges and tiny lots. The rasterizer remains independent of CS1 zoning.

This alpha **does not display a map overlay, draw persistent lots, paint zoning, place buildings, or replace the vanilla grid**. It does not change a save. CS1 growable assets remain constrained by their cell footprints; a polygon preview cannot itself create a curved building mesh.

Run `./build.ps1` in PowerShell. The script builds `dist/MetropolisTerrainLots.dll` against installed game assemblies and runs 12 geometry checks. No game assembly is redistributed. To try the mod, copy only the DLL to a new folder under `Files/Mods`, enable it, load a copied save, and use the buttons in Mod Options. Aim the centre of the view at terrain for each vertex. Results appear in the game log; no on-map outline exists yet.

Read [Citystate comparison](docs/CITYSTATE_COMPARISON.md), [technical scope](docs/TECHNICAL.md), [roadmap](docs/ROADMAP.md), and the [suite overview](https://github.com/CitiesSkylines-addons/metropolis-performance-lab/blob/main/docs/SUITE.md).

## Italiano

La mod aggiunge nelle opzioni un pulsante per **campionare 32 × 32 m di terreno**. Un raycast individua il terreno al centro della vista, poi legge quattro quote, classifica il dislivello come pianeggiante (≤1 m), lieve (≤4 m) o ripido (>4 m), e registra il risultato in `Cities_Data/output_log.txt`. Nuovi pulsanti aggiungono, annullano e cancellano i vertici di un poligono al centro vista, poi mostrano nel log area e numero di celle da 8 m. La validazione rifiuta incroci e lotti minuscoli. La rasterizzazione non è ancora collegata allo zoning di CS1.

Questa alfa **non mostra un overlay sulla mappa, non disegna lotti permanenti, non dipinge zone, non piazza edifici e non sostituisce la griglia vanilla**. Non modifica il salvataggio. Gli asset growable di CS1 restano vincolati alle dimensioni in celle: una preview poligonale non genera da sola una mesh curva.

Eseguire `./build.ps1` in PowerShell. Lo script compila `dist/MetropolisTerrainLots.dll` usando le librerie installate ed esegue 12 controlli geometrici. Non redistribuisce librerie del gioco. Per provarla, copiare solo la DLL in una nuova cartella sotto `Files/Mods`, abilitarla, caricare una copia del salvataggio e usare i pulsanti nelle opzioni. Puntare il centro della vista verso il terreno per ogni vertice. I risultati appaiono nel log; il contorno sulla mappa non esiste ancora.

Leggere [confronto con Citystate](docs/CITYSTATE_COMPARISON.md), [ambito tecnico](docs/TECHNICAL.md) e [roadmap](docs/ROADMAP.md).

## Discoverability / Ricerca

**Topics:** `cities-skylines`, `cities-skylines-1`, `cities-skylines-mod`, `csharp`, `terrain`, `zoning`, `polygon`

**Keywords:** terrain-aware lots, freeform zoning, polygon rasterization, slope, organic city; lotti, terreno, pendenza, zoning poligonale, città organiche.

## Support / Donazioni

Optional / Facoltative: [Ko-fi](https://ko-fi.com/mrjonam) · [Buy Me a Coffee](https://www.buymeacoffee.com/mrjonam) · [PayPal](https://paypal.me/manorollo).

Original source: MIT. Game assemblies and third-party code are not redistributed. Independent of Colossal Order, Paradox and Citystate Metropolis.
