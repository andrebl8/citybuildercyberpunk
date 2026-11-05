---
paths:
  - "sim.tests/**"
  - "unity/Assets/Tests/**/*.cs"
---

# Test Standards

Adapted from Claude Code Game Studios (MIT, see THIRD_PARTY_NOTICES.md).

- **Names say scenario and expected result.**
  - Sim (xUnit): `Snake_case_sentence_of_the_behaviour` (e.g. `Unpowered_zone_never_grows`).
  - Unity (NUnit): class `[System]Tests`, method `[Scenario]_[Expected]`.
- Clear arrange / act / assert in every test.
- Deterministic: fixed seeds, no wall clock, no filesystem or network.
- Build fixtures in the test or in `TestCities`; never share mutable state between tests.
- Tune `SimConfig` per test (e.g. high `PowerPlantCapacity`) so each test isolates one rule.
- **Every bug fix gets a regression test, and you must watch it fail** against the
  unfixed code before trusting it. A test only ever seen passing is not known to test
  anything — the fixture can quietly miss the bug.
- Never weaken an assertion to make a test pass; fix the code or explain why the
  expectation was wrong.
