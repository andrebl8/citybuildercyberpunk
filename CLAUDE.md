# Neon City — Claude instructions

SimCity-lite city builder. Unity 6 + URP, WebGL on Railway, Synty POLYGON Sci-Fi City art.
Plan: 6 weeks of evenings (grid+camera → road/power/WebGL → zones/growth → economy/UI →
save/juice → balance/launch).

## Architecture

- `sim/Runtime/` — **all game rules**, pure C# (netstandard2.1, C# 9, no UnityEngine).
  Also a Unity local package. Entry point: `City` (`Execute`, `Preview`, `Step`, events).
- `sim.tests/` — xUnit. `sim.console/` — headless balance harness.
- `unity/` — Unity project: view + input only.
- `deploy/` — Caddy container for Railway (`deploy/site/` = WebGL build).
- `docs/engine-reference/unity/` — Unity 6 API reference. Check it before using engine APIs.
  When Unity is installed, write the exact editor version into `VERSION.md` → "Installed at pin time"
  (that file mentions `/setup-engine`, which this repo does not have — edit it by hand).

## Commands

```bash
dotnet test sim.tests
dotnet run --project sim.console -- 36 1     # months, seed
```

## Working agreements

- Rules in `.claude/rules/` apply by path (sim, Unity, tests).
- Unity questions → `unity-specialist` agent; UI → `unity-ui-specialist`.
- `/code-review` and `/balance-check` are tailored to this repo.
- Never commit the Synty pack or other licensed assets anywhere public; the repo is private.
- Large binaries (fbx, png, wav, …) go through Git LFS (`.gitattributes`).
- Branch for changes; commit only when asked.
