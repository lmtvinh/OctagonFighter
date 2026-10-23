# Roadmap

Development roadmap for **Octagon Fighter**, a simulation-first 3D MMA game built with
Unity 6, URP, and the Unity Input System.

> **Current status:** Pre-production. Repository foundation is complete. No gameplay
> systems have been implemented yet. The next milestone is **Core Combat Foundation**.

---

## Guiding Principles

- **Domain first.** Combat math, state logic, and AI scoring live in pure C# and are
  testable without Unity.
- **Deterministic by default.** Fixed-tick combat, seeded randomness, and serializable
  state from the first implementation.
- **Data-driven.** Balance values live in ScriptableObjects, never in hardcoded constants.
- **Vertical slices.** Each milestone produces a playable, testable increment rather than
  isolated infrastructure.
- **Local multiplayer first.** The architecture must not preclude future online play.
- **Scope discipline.** Finish and validate a milestone before expanding content.

---

## Status Legend

| Symbol | Meaning |
| --- | --- |
| `[x]` | Complete |
| `[ ]` | Planned |
| `[~]` | In progress |

---

## Phase 0 - Repository Foundation

**Goal:** Establish a clean, production-ready Unity repository that is safe to clone and
build from a fresh checkout.

| Status | Deliverable |
| --- | --- |
| `[x]` | Unity 6 project created (`6000.6.7f1`, URP 17.6.0, Input System 1.20.1) |
| `[x]` | Production-ready Unity `.gitignore` covering generated folders and IDE output |
| `[x]` | `README.md` with setup, stack, structure, and documentation links |
| `[x]` | `ROADMAP.md` with milestone scope and sequencing |
| `[x]` | `CHANGELOG.md` with initial repository history |
| `[x]` | Engineering standards and game design documentation |
| `[x]` | Generated folders (`Library/`, `Temp/`, `Logs/`, `Obj/`, `UserSettings/`, `.vs/`, `.vscode/`) ignored |
| `[x]` | Generated IDE files (`*.csproj`, `*.sln`, `*.slnx`) ignored |
| `[x]` | Initial repository commit created |

**Exit criteria:** A fresh clone contains only source, assets, packages, and project
settings; Unity regenerates all local caches on first open.

---

## Phase 1 - Core Combat Foundation

**Goal:** Prove the combat loop with two fighters, readable strikes, and the stamina
economy. This is the first gameplay milestone and the current focus after repository
setup.

| Status | Deliverable |
| --- | --- |
| `[x]` | Core Combat Foundation architecture design ([COMBAT_ARCHITECTURE.md](COMBAT_ARCHITECTURE.md)) |
| `[ ]` | Layer assembly definitions (`Domain`, `Application`, `Infrastructure`, `Presentation`, `Editor`, tests) |
| `[ ]` | Fixed-tick `CombatClock` and seeded deterministic RNG |
| `[ ]` | Core combat contracts and immutable event/result structs |
| `[ ]` | `FighterRuntime` model sourced from `FighterDefinition` ScriptableObjects |
| `[ ]` | Health component with head/body damage accumulation and KO handling |
| `[ ]` | Stamina component with tiers, costs, regen delay, and fatigue effects |
| `[ ]` | Intent-driven fighter state machine (`Idle`, `Locomotion`, `Striking`, `Blocking`, `Dodging`, reactions) |
| `[ ]` | `StrikeDefinition` data model (startup, active, recovery, damage, cost, range band) |
| `[ ]` | `CombatResolver` with hit validation, guard interaction, counters, and combo scaling |
| `[ ]` | Swept hit detection through `Infrastructure` with pooled, non-allocating queries |
| `[ ]` | Player and AI intent sources sharing the same combat interface |
| `[ ]` | Minimal combat test scene with placeholder fighters and debug HUD |
| `[ ]` | EditMode tests for damage, stamina, and resolution math |
| `[ ]` | PlayMode smoke test for the strike-to-damage pipeline |

**Exit criteria:** Two fighters can exchange strikes in a test scene; damage, stamina, and
hit reactions resolve deterministically from data assets; automated tests pass.

**Explicit non-goals for Phase 1:** clinch, takedowns, ground game, submissions, career
mode, final animations, final art, and online play.

---

## Phase 2 - Grappling Foundation

**Goal:** Connect striking to clinch, takedowns, and ground positions through one shared
grapple context.

| Status | Deliverable |
| --- | --- |
| `[ ]` | `GrappleContext` with top/bottom roles, grips, control priority, and stamina pressure |
| `[ ]` | Clinch entry/exit, control contest, and dirty boxing/knee actions |
| `[ ]` | Contested takedown resolution (clean, partial, stuffed, reversal) |
| `[ ]` | Ground position graph (`Guard`, `HalfGuard`, `SideControl`, `Mount`, `Back`, `Crucifix`) |
| `[ ]` | Ground-and-pound pressure and defensive frames |
| `[ ]` | Multi-stage submission contest with escape windows |
| `[ ]` | Data assets for takedowns, positions, and submissions |
| `[ ]` | Domain tests for contests, transitions, and stamina costs |

**Exit criteria:** A fight can flow from standing to clinch, takedown, ground control, and
submission without loading separate modes or mini-games.

---

## Phase 3 - Combat Systems Complete

**Goal:** Deliver the full five-discipline combat model with a production-ready AI
opponent.

| Status | Deliverable |
| --- | --- |
| `[ ]` | Complete strike set and discipline-specific move access |
| `[ ]` | Combo graph with cancel windows and deterministic scaling |
| `[ ]` | Rocked, knockdown, TKO, and decision systems |
| `[ ]` | Referee logic: inactivity stand-ups, resets, and stoppages |
| `[ ]` | Utility AI with perception, evaluation, reaction delay, and difficulty profiles |
| `[ ]` | Round flow, corner recovery, and between-round state |
| `[ ]` | Complete HUD: health, stamina, position, submission meter, round state |
| `[ ]` | Animation, audio, and VFX event integration |
| `[ ]` | Balance tuning pass with data assets only |

**Exit criteria:** A complete three- or five-round fight is playable against AI or a second
local player using all five disciplines.

---

## Phase 4 - Career Alpha

**Goal:** Build the long-term progression loop around the combat system.

| Status | Deliverable |
| --- | --- |
| `[ ]` | Fighter creation and attribute allocation |
| `[ ]` | Calendar, training camps, energy, fatigue, and injury risk |
| `[ ]` | XP, attribute growth curves, and diminishing returns |
| `[ ]` | Contracts, purses, and fight offers |
| `[ ]` | Rankings, championships, and title defenses |
| `[ ]` | Persistent AI roster with simulated careers |
| `[ ]` | Scouting reports and gameplan preparation |
| `[ ]` | Versioned save/load with multiple slots and migration |

**Exit criteria:** A player can create a fighter, complete a training camp, take a fight,
earn progression, and advance a career across multiple sessions.

---

## Phase 5 - Game Modes

**Goal:** Ship the complete set of launch modes on top of the validated combat and career
systems.

| Status | Deliverable |
| --- | --- |
| `[ ]` | Quick Fight with configurable rules and fighters |
| `[ ]` | Tournament brackets (4/8/16 fighters, optional damage carry-over) |
| `[ ]` | Local multiplayer with symmetric, normalized stats |
| `[ ]` | Main menu, mode selection, and pause flow |
| `[ ]` | Settings: controls, difficulty, damage, accessibility |

**Exit criteria:** All launch modes are reachable from the main menu and share the same
deterministic combat core.

---

## Phase 6 - Content & Polish

**Goal:** Reach launch-quality presentation, content volume, and performance.

| Status | Deliverable |
| --- | --- |
| `[ ]` | Launch roster (24-32 fighters) with distinct attributes and move sets |
| `[ ]` | Arenas (6-8) with broadcast-style lighting and camera work |
| `[ ]` | Final animation set, hit reactions, and mocap integration |
| `[ ]` | Layered impact audio, crowd reactions, and commentary hooks |
| `[ ]` | UI polish, accessibility options, and localization readiness |
| `[ ]` | Performance pass: 60 FPS target, pooling, allocation budgets |
| `[ ]` | Mobile scalability review and quality tiers |

**Exit criteria:** Content complete, performance targets met, and accessibility settings
verified.

---

## Phase 7 - Beta & Release

**Goal:** Stabilize, balance, and prepare for release.

| Status | Deliverable |
| --- | --- |
| `[ ]` | Closed beta and telemetry integration |
| `[ ]` | Balance pass driven by playtest data |
| `[ ]` | Bug triage and crash-free session target (> 99.5%) |
| `[ ]` | Save migration and compatibility verification |
| `[ ]` | Platform certification requirements |
| `[ ]` | Release candidate build and version tag |

**Exit criteria:** Release candidate passes certification checks and stability targets.

---

## Cross-Cutting Workstreams

These run continuously across all phases:

- **Testing:** domain unit tests for every combat and progression rule; PlayMode smoke
  tests for scene wiring.
- **Determinism:** all combat state must be reproducible from a seed and serializable.
- **Performance:** no per-frame allocations in combat; pooled VFX, audio, and UI.
- **Accessibility:** remappable controls, colorblind palettes, simplified struggle input,
  damage display toggles.
- **Documentation:** update `README.md`, `CHANGELOG.md`, and `OPENCODE.md` as systems land.
- **Version control:** small Conventional Commits, focused branches, no generated files.

---

## Immediate Next Step

Implement **Phase 1 - Core Combat Foundation** only after this repository commit is
reviewed. Do not begin grappling, career, or content work until the Phase 1 exit criteria
are met.

---

*This roadmap is a living document. Update status as milestones complete and record all
notable changes in [CHANGELOG.md](CHANGELOG.md).*
