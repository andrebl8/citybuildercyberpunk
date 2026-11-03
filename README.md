# Neon City

SimCity-lite city builder in a neon sci-fi city. Unity + POLYGON Sci-Fi City (Synty), WebGL build hosted on Railway.

> **Private repo.** The Synty asset pack is licensed per seat and must never be pushed to a public repo.

## Layout

| Path | What |
| --- | --- |
| `sim/` | Simulation core: pure C# (`netstandard2.1`, C# 9), no UnityEngine. |
| `sim/Runtime/` | The sources. Also a Unity local package (`package.json` + `asmdef` with `noEngineReferences`). |
| `sim.tests/` | xUnit tests for the sim. |
| `sim.console/` | Headless balance harness: builds a starter town, prints a monthly ledger + ASCII map. |
| `unity/` | Unity project (created in the first Unity session). Has its own `.gitignore`. |
| `deploy/` | Railway: Caddy container serving the WebGL build from `deploy/site/`. |

## Sim at a glance

- **Grid** of `Cell`s: empty, road, power plant, or zoned (R/C/I) with density 0–3.
- **Commands** (`ZoneCommand`, `BuildRoadCommand`, `PlacePowerPlantCommand`, `BulldozeCommand`) plan their edits, then `City.Execute` checks funds and applies all or nothing. `City.Preview` is the dry run for UI cost tooltips.
- **Tick** (`City.Step`): power → growth/decay → economy at month end.
  - Power flows from plants through any non-empty cell; over capacity, cells farthest from a plant brown out.
  - Zones grow only with power **and** a road within `RoadAccessRadius`, and only while RCI demand is positive.
  - RCI demand: residents want jobs, shops want customers, industry wants workers + exports. Squashed to (-1, 1).
  - Month end: taxes in, upkeep out. `MonthsInDebtBeforeBankruptcy` months below zero → bankrupt.
- **Deterministic** (own PRNG). Seed + `CommandLog` rebuilds the exact city (`CityReplay.Rebuild`) — that's the save format and the basis for undo.
- **Events** for the view: `CellChanged`, `MoneyChanged`, `MonthEnded`, `WentBankrupt`.
- All tuning lives in `SimConfig` (public fields → easy to wrap in a ScriptableObject).

## Run

```bash
dotnet test sim.tests                      # tests
dotnet run --project sim.console -- 36 1   # 36 months, seed 1
```

## Using the sim in Unity

Package Manager → **Add package from disk…** → `sim/Runtime/package.json`
(or add `"no.neoncity.sim": "file:../../sim/Runtime"` to `unity/Packages/manifest.json`).
Then drive it from a MonoBehaviour: `Execute` on player input, `Step` on a fixed timer (e.g. 4×/s), redraw cells on `CellChanged`.

## First Unity session — setup checklist

- [ ] `git lfs install` (models, textures and audio are LFS-tracked via `.gitattributes`)
- [ ] Create the Unity project **inside `unity/`** (URP template), switch platform to WebGL
- [ ] Import the Synty pack; convert materials to URP if needed
- [ ] Add the sim package (above)
- [ ] Map prefabs to zone × density

## Deploy (Railway)

1. New service from this repo, **Root Directory = `/deploy`** (uses `deploy/Dockerfile`).
2. Unity WebGL build (Brotli) → copy output (`index.html`, `Build/`, `TemplateData/`) into `deploy/site/`.
3. Until then, `deploy/site/index.html` is a placeholder page.

CI (`.github/workflows/sim.yml`) runs the tests and a 24-month balance smoke run. A GameCI WebGL build job comes once the Unity project exists (needs Unity license secrets).
