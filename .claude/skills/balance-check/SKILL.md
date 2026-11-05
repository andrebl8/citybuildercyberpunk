---
name: balance-check
description: "Run the headless sim and analyse Neon City's balance — growth curve, money, power, RCI demand. Use when tuning SimConfig or asked whether the economy/growth feels right."
argument-hint: "[months=36] [seeds=1,2,3] [focus: growth | economy | power | demand | all]"
user-invocable: true
---

# Balance Check

Adapted from Claude Code Game Studios (MIT, see THIRD_PARTY_NOTICES.md), rebuilt around
this repo's `sim.console` harness.

## 0. Inputs

Data comes from running the sim, never from estimates. If `dotnet` is missing or the
run fails, report **"NOT ASSESSED — <reason>"** and stop. A section with no data says
so; it is never filled in by reasoning about the code alone.

## 1. Run

For each seed (default 1, 2, 3):

```bash
dotnet run --project sim.console -- <months> <seed>
```

Capture the monthly ledger (money, net, population, jobs, power, demand) and the map.
If the user is tuning, note the current `SimConfig` values that matter for the focus.

## 2. Analyse

Report what the numbers show, per focus area:

- **Growth**: months until first growth, until plateau; what caps it (zones full,
  power, road access, demand ≤ 0). Plateau before the zoned area is full = a signal.
- **Economy**: net per month, months to bankruptcy if negative, how fast money piles
  up if positive. Money growing without bound and nothing to spend it on = too easy.
- **Power**: demand vs capacity per month; when brownout starts and whether the
  player gets a clear signal (unpowered count) before growth stalls.
- **Demand (RCI)**: does each of R/C/I swing positive and negative, or is one stuck?
  A demand pinned near ±1 or near 0 for many months = poorly tuned softness/base.
- **Seed spread**: differences between seeds. Large spread = too random to read.

Flag degenerate strategies or dead ends (e.g. industry never grows, money only
grows, bankrupt with no warning).

## 3. Recommend

At most 3 concrete `SimConfig` changes, each with the expected effect. If asked to
apply them: change `SimConfig` defaults, re-run the same seeds, show before/after,
and run `dotnet test sim.tests` (some tests assume default ranges).

## 4. Report format

```
## Balance Check — <focus>, <months> months, seeds <list>
| Seed | First growth | Plateau pop | Final money | Net/mo (last) | Brownout from |
...
### Findings
### Recommended SimConfig changes
```

Offer to save it as `docs/balance/balance-check-<YYYY-MM-DD>.md`.
