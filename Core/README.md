# Tower of Chaos - Game Core (pure C#)

Engine-independent game rules, written once and shared by the **headless dedicated server** and the **Unity client**.
No `UnityEngine` reference, so it builds and is tested with plain `dotnet`.

```
Core/
  TowerOfChaos.GameCore/          library (netstandard2.1, C# 9 - consumable by Unity 6)
  TowerOfChaos.GameCore.Tests/    console test runner, no NuGet packages, exit code 1 on failure
  Data/catalog.json               GENERATED from SPEC.md (traps, rewards, monsters, mutators)
  tools/export_catalog.py         regenerates catalog.json from the SPEC tables
```

## Run

```bash
python -X utf8 Core/tools/export_catalog.py                                   # after editing SPEC.md tables
dotnet run -c Release --project Core/TowerOfChaos.GameCore.Tests              # 20k seeds per floor
TOC_SEEDS=100000 dotnet run -c Release --project Core/TowerOfChaos.GameCore.Tests   # SPEC 24.2: 100k seeds per floor
```

## What is implemented (and verified by the tests)

| Area | Code | SPEC |
| :--- | :--- | :--- |
| Deterministic RNG (SplitMix64, identical on every platform) | `DeterministicRng` | 14.1 |
| Tower height parameter `H` (6 in v1.0, 10 after Update 1), crown room, progress | `TowerConfig` | 29.3 |
| Solvable room generator: golden path, crossing aids over full-width hazards, >= 1.2 s timed windows, trap-free entrance, TBP budget, tier-C cap, 45% occupancy, genre mix 70/10/20, mutator conflicts | `SolvableRoomGenerator` | 6, 15.2, 18.3, 18.4 |
| Independent validator (does not trust the generator) + BFS | `RoomValidator` | 6.1 |
| Whole-tower world per match, per-floor mutator cap (<= 2 of 10), portal plan, auto-reseed | `MatchWorld`, `PortalPlanner` | 14.2, 17.6 |
| Pacing: E(H), fog schedule, hard cap | `TowerRules` | 29.3 |
| Hero movement + tile collision, deterministic timed-trap clock, camera rig, per-player run (rooms, traps, death, fog, crown) | `HeroSim`, `TrapClock`, `CameraRig`, `TowerRun` | 2.2, 2.3, 11, 17 |
| Dependency-free JSON + catalog reader (client and server share it) | `MiniJson`, `CatalogReader` | 18.1 |
| Ranking, end conditions | `Ranking` | 17.3, 17.5 |
| Death / respawn, start dice | `MatchFlow` | 11, 14.2 |
| Scan cap (3 per match, idempotent, refundable) | `ScanBudget` | 21.2 |
| XP, level cap (40 in v1.0 / 60), level 1-60 rewards | `Progression` | 12.1, 19.2 |

## Unity

`python -X utf8 Core/tools/sync_to_unity.py` builds the DLL and copies it with `catalog.json` into `UnityClient/` (see `UnityClient/README.md`).
`Core/tools/UnityCompileCheck` compiles the Unity scripts against stubs + the real Core (no Unity needed): `dotnet build Core/tools/UnityCompileCheck`.

## What is NOT implemented yet

Everything that needs an engine, a server or art: Unity scenes/prefabs, hero controller and skills runtime, trap behaviour and VFX,
monster AI, UI, networking (Mirror), Nakama modules, payments, audio, localization, art. See `TASKS.md` (column *Status*).

## Notes on the old prototype

The old Unity prototype is archived untouched in `Legacy/UnityClient_prototype_v5/`; `UnityClient/` is now a new Unity 6 project that calls this core.
Why it was replaced: its `ProceduralRoomGenerator.cs` used a 7x10 grid and old trap ids, ignored TBP/occupancy/timed-window rules, and hid a BFS failure by
overwriting the centre column (the core reseeds instead). `Backend/` was not touched. Backend decision: **Nakama** (confirmed); `Backend/schema.sql` stays as the data-model reference.
