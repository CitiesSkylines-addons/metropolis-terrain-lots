# Citystate Metropolis comparison / Confronto con Citystate Metropolis

Sources checked on 2026-09-26: [Steam product page](https://store.steampowered.com/app/2828020/Citystate_Metropolis/) and [developer update, 2025-02-07](https://www.citystategame.com/post/citystate-metropolis-development-update-and-first-screenshots). The Steam page says the game is not yet available and lists 2026 as its planned release. Its feature descriptions are developer claims, not a tested reference implementation. This project is independent and does not use Citystate code, art, or branding.

Fonti verificate il 26-09-2026: [pagina Steam](https://store.steampowered.com/app/2828020/Citystate_Metropolis/) e [aggiornamento dello sviluppatore del 07-02-2025](https://www.citystategame.com/post/citystate-metropolis-development-update-and-first-screenshots). Steam indica che il gioco non è ancora disponibile e riporta il 2026 come uscita prevista. Le funzioni descritte sono dichiarazioni dello sviluppatore, non una versione verificata tramite gioco. Il progetto è indipendente e non usa codice, immagini o marchi Citystate.

| Citystate feature / Funzione | CS1 route / Percorso CS1 | Current evidence / Evidenza attuale |
| --- | --- | --- |
| Polygonal, curved lots / Lotti poligonali e curvi | Capture a polygon, sample terrain, rasterize to 8 m preview cells / Acquisire poligono, campionare terreno, rasterizzare celle di anteprima | Polygon sketch and log preview implemented; no visual map overlay / Bozza poligonale e anteprima nel log implementate; nessun overlay sulla mappa |
| Grid-less zoning / Zoning senza griglia | Optional cell mask over valid CS1 `ZoneBlock` cells / Maschera facoltativa sulle celle `ZoneBlock` valide | Research only; no zone writes / Solo ricerca; nessuna scrittura |
| Building footprint inside lot / Impronta dell'edificio nel lotto | Select compatible rectangular prefab footprints and reserve open cells / Selezionare prefab rettangolari compatibili e riservare celle libere | Depends on the [building rules mod](https://github.com/CitiesSkylines-addons/metropolis-building-rules); no cross-mod protocol yet / Dipende dalla mod edifici; nessun protocollo tra mod |
| Terrain-responsive subdivision / Suddivisione adattiva | Split polygon around slope, road access and usable cells / Suddividere il poligono secondo pendenza, accesso stradale e celle utili | Planned / Previsto |
| Procedural terrain and dynamic water / Terreno procedurale e acqua dinamica | Separate map-editor research / Ricerca separata sul map editor | Outside this mod / Fuori da questa mod |

CS1's installed Epic `1.21.1-f9-epic-win` assemblies expose terrain raycasting and zone-cell methods, but the lot sketch is only a plan. A map polygon is not a new CS1 growable footprint. We will not claim visual or simulation parity while buildings are tied to rectangular prefabs and CS1's existing zoning and simulation paths.

Le librerie della versione Epic `1.21.1-f9-epic-win` espongono raycast sul terreno e metodi delle celle di zona, ma la bozza è soltanto un piano. Un poligono disegnato sulla mappa non diventa automaticamente un nuovo ingombro growable di CS1. Non dichiareremo equivalenza visiva o simulativa finché gli edifici restano prefab rettangolari e valgono i percorsi di zoning e simulazione di CS1.
