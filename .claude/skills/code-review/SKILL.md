---
name: code-review
description: "Architectural code review for Neon City — sim rules, Unity view layer, WebGL performance, tests. Use when asked to review code, a diff, a branch or a PR."
argument-hint: "[path | branch | PR number]  (default: current diff vs main)"
user-invocable: true
---

# Code Review

Adapted from Claude Code Game Studios (MIT, see THIRD_PARTY_NOTICES.md), trimmed to this repo.

## 0. Check inputs first

Work out what to review: the given path, branch or PR; otherwise `git diff main...HEAD`
plus uncommitted changes. If there is nothing to review, say **"NOT ASSESSED — no
changes found"** and stop. Never produce a clean report for code you didn't read.
A check you skipped is reported as skipped, never as passed.

## 1. Load context

- Read every changed file in full (not just the hunks).
- Read `CLAUDE.md` and the rules that apply to the paths:
  `.claude/rules/sim-code.md`, `.claude/rules/unity-code.md`, `.claude/rules/test-standards.md`.
- Unity code: check APIs against `docs/engine-reference/unity/` (Unity 6). Flag any API
  not documented there as **unverified**.

## 2. Verify, don't assume

- Sim changes: run `dotnet test sim.tests`. If the toolchain is unavailable, say so.
- Balance-affecting changes (`SimConfig`, demand, growth, economy): run
  `dotnet run --project sim.console -- 36 1` before and after and compare.

## 3. Review checklist

**Sim (`sim/`)**
- No UnityEngine / C# 10+ features; still `netstandard2.1`-clean.
- Deterministic: no `System.Random`, clocks, GUIDs, order-dependent hash iteration.
- New numbers live in `SimConfig`.
- State changes go through `City.SetCell`; commands are immutable and all-or-nothing.
- `Step()` stays cheap: no per-tick allocations in loops over 4096 cells.
- Replay still holds (seed + log → same `ComputeStateHash`).

**Unity (`unity/`)**
- No game rules in MonoBehaviours; UI reads the sim and sends commands.
- No allocations, `GetComponent`, `Find*`, LINQ in `Update`.
- Pooling for per-cell visuals; events unsubscribed in `OnDisable`/`OnDestroy`.
- `[SerializeField] private` over public fields; no static game-state singletons.
- WebGL-safe: no threads, no `Task.Run`, no synchronous file IO.

**Tests**
- New behaviour and every bug fix covered; regression tests seen failing first.
- Names describe scenario + result; arrange/act/assert; deterministic.

**General**
- SOLID where it pays; no speculative abstractions.
- Naming, dead code, comments that explain *why*.

## 4. Report

```
## Code Review: <scope>
Verdict: APPROVE | APPROVE WITH SUGGESTIONS | CHANGES REQUIRED | NOT ASSESSED
Tests: <ran, result> | NOT RUN (<why>)
Balance: <compared / not applicable / NOT RUN>

### Must fix
- file:line — problem — why it matters — suggested fix
### Should fix
### Nice to have
### Unverified Unity APIs
```

Rank by severity, cite file:line, keep it short. Don't edit files during the review
unless the user asks for fixes.
