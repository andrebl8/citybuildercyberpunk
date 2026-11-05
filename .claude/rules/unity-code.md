---
paths:
  - "unity/Assets/**/*.cs"
---

# Unity Code Rules

Adapted from Claude Code Game Studios (MIT, see THIRD_PARTY_NOTICES.md).

## Layering

- Unity is the **view and input layer** over `NeonCity.Sim`. Game rules, numbers and
  state live in the sim. MonoBehaviours translate input into `CityCommand`s, call
  `City.Step()` on a fixed timer, and redraw from sim events.
- UI never owns or modifies game state: it reads the sim and sends commands. Use
  `City.Preview(cmd)` to show costs before the player commits.
- Cross-system communication via C# events / ScriptableObject event channels, not
  direct references between UI and gameplay objects.
- No static singletons for game state; pass the `City` in (one bootstrap object owns it).

## Data

- Visual/tuning values (camera speed, prefab per zone × density, colours) come from
  ScriptableObjects or `[SerializeField] private` fields, never hardcoded.
- Use delta time for anything time-dependent in the view (camera, animations).

## Performance (WebGL)

- Zero allocations in `Update`/`LateUpdate`: no LINQ, string concat, `new List` per frame.
- Pool building instances per cell; swap prefab on `CellChanged`, don't Destroy/Instantiate in bulk.
- Cache component references in `Awake`; never `GetComponent`, `Find*` or `SendMessage` in hot paths.
- Raycast against the grid plane mathematically or with `RaycastNonAlloc`.
- No threads, no `Task.Run` (WebGL is single-threaded).

## API version

- Before using an engine API, check `docs/engine-reference/unity/` (Unity 6 changed a lot).
  If an API isn't documented there, mark it unverified.
- Scene/prefab/asset YAML: never hand-edit while the Unity Editor may be open; never
  invent GUIDs or touch `.meta` files by hand.
