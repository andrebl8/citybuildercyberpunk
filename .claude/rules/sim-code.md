---
paths:
  - "sim/**"
---

# Sim Code Rules

The sim is the single source of truth for game rules. It must compile both as
`netstandard2.1` and inside Unity (local package, `noEngineReferences: true`).

- **No UnityEngine, no engine types.** Plain C# only.
- **C# 9 max.** No file-scoped namespaces, global usings, `required`, raw strings or
  other C# 10+ features. No records unless an `IsExternalInit` polyfill is added.
- **Deterministic.** Use `SplitMix64`, never `System.Random`, `DateTime.Now`,
  `Guid.NewGuid()` or unordered `Dictionary`/`HashSet` iteration where order affects
  state. Seed + `CommandLog` must rebuild the exact city (`CityReplay`).
- **All tuning numbers live in `SimConfig`.** No magic numbers in rules code.
- **Player actions are `CityCommand`s**: immutable, plan edits first, `City.Execute`
  applies all or nothing. Never mutate cells from outside `City`.
- **Cells are value types**; the view gets copies. State changes go through
  `City.SetCell` so counters and `CellChanged` stay correct.
- **Cheap ticks.** WebGL has no threads; `Step()` on 64x64 must stay well under 2 ms.
  Avoid per-tick allocations in hot loops.
- Every rule change ships with a test in `sim.tests/`, and a balance-affecting change
  with a `sim.console` run (see `/balance-check`).
- `TreatWarningsAsErrors` is on. Keep it that way.
