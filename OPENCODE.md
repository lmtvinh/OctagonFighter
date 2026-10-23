# OPENCODE.md - Octagon Fighter

> Unity 6 3D MMA simulation game. This document is the source of truth for AI assistants
> and contributors working in this repository. Read it fully before making changes.

---

## Project Overview

| Field | Value |
| --- | --- |
| Project | Octagon Fighter |
| Genre | Realistic 3D MMA Fighting / Sports Simulation |
| Inspirations | EA Sports UFC 4, UFC 5, Undisputed, Fight Night Champion |
| Engine | Unity 6 (`6000.6.7f1`) |
| Language | C# |
| Render Pipeline | Universal Render Pipeline (URP) |
| Input | Unity Input System (`com.unity.inputsystem`) |
| Camera | Cinemachine |
| Data | ScriptableObjects |
| Version Control | Git |

---

# Project Vision

Octagon Fighter is a **simulation-first** mixed martial arts game. The goal is not arcade
spectacle but the tension, pacing, and tactical depth of real MMA: measured footwork,
stamina as a resource, risk/reward grappling exchanges, and fights that are won through
decision-making rather than button mashing.

### Design Pillars

1. **Authenticity over accessibility shortcuts.** Striking, grappling, and transitions
   should reflect real MMA mechanics. Depth is revealed through mastery, not tutorials
   alone.
2. **Stamina is the core economy.** Every action spends a resource. Spamming drains the
   fighter; patience and efficient movement are rewarded. Fatigue degrades offense,
   defense, and recovery.
3. **Readable, deliberate combat.** Animations must telegraph intent. Hit reactions,
   hit-stop, and audio should communicate impact clearly.
4. **Meaningful fighter identity.** Attributes, body types, reach, stances, and
   tendencies make fighters feel distinct and produce different viable strategies.
5. **Simulation through data.** Combat values (damage, speed, stamina cost, AI weights)
   live in ScriptableObjects, not in code. Designers tune without recompiling.
6. **Long-term progression.** Career mode, AI opponents, and training create a loop of
   growth, scouting, and adaptation.

### Non-Goals

- Arcade combos, exaggerated juggling, or health-bar spectacle over realism.
- Online multiplayer in the initial milestone, but architecture must not preclude it.
- Photorealistic fidelity at the cost of frame-rate stability.

### Target Experience

A player should feel that **position, timing, and resource management** decide fights.
Mistakes (overcommitting a strike, gassing out, poor takedown defense) are punished;
smart pressure and transitions are rewarded.

---

# Architecture Rules

The codebase follows **Clean Architecture** with strict dependency direction:
`Presentation -> Application -> Domain`, and `Infrastructure` implementing domain
interfaces. Unity-specific types stay at the outer layers.

### Core Principles

- **Single Responsibility.** One class, one reason to change. A `FighterController`
  orchestrates; it does not compute damage or play animations directly.
- **Open/Closed.** Extend via interfaces and new implementations, not by editing stable
  core logic.
- **Liskov Substitution.** Derived fighters/states must honor their contracts.
- **Interface Segregation.** Small, focused interfaces (`IDamageable`, `IStaminaSource`,
  `ICombatState`) instead of god interfaces.
- **Dependency Inversion.** Higher-level systems depend on abstractions. Combat must not
  reference the input system, UI, or concrete AI.

### Modular Systems

Each gameplay system is a self-contained module with:

- A public **contract** (interface or event definition).
- A **domain model** (pure C#, no `UnityEngine` dependency where practical).
- An **adapter/presenter** (MonoBehaviour) that connects the domain to Unity.

Modules communicate through **events and interfaces**, never direct scene lookups
(`FindObjectOfType`, static singletons, or hard-wired scene references).

### Composition over Inheritance

Fighters are assembled from **components/capabilities** (striking, grappling, stamina,
health, locomotion) rather than deep `Fighter -> Striker -> Grappler` hierarchies.
Prefer:

```csharp
// Good: capability composition
[RequireComponent(typeof(StaminaComponent))]
[RequireComponent(typeof(HealthComponent))]
public sealed class FighterFacade : MonoBehaviour { /* wires capabilities */ }
```

```csharp
// Bad: inheritance pyramid
public class BaseFighter : MonoBehaviour { }
public class StrikingFighter : BaseFighter { }
public class GrapplingStriker : StrikingFighter { }
```

### Event-Driven Communication

- Use typed events (C# `event` / `Action<T>` or a lightweight message bus) for cross-system
  reactions: `OnDamageTaken`, `OnStaminaDepleted`, `OnStateChanged`, `OnTakedownResolved`.
- Events describe **facts that happened** (past tense), not commands.
- Never let event subscribers mutate the emitter synchronously in ways that create
  re-entrant loops. Queue or defer when ordering matters (e.g., damage -> KO -> round end).
- Domain events carry immutable data structs; avoid passing MonoBehaviours.

### Layer Map

```
Assets/_Project/Scripts/
  Core/            cross-cutting primitives, event bus, service locator-free DI
  Domain/          pure C# combat math, state logic, AI scoring (no UnityEngine)
  Application/     orchestration, match flow, career progression
  Infrastructure/  Unity adapters: input, animation, audio, physics, save, pooling
  Presentation/    MonoBehaviours, UI presenters, camera, VFX glue
```

Dependency rule: `Domain` depends on nothing. `Application` depends on `Domain`.
`Infrastructure` and `Presentation` depend on the abstractions defined inside `Domain`
and `Application`. No references point inward toward Unity from `Domain`.

---

# Coding Standards

### Naming Conventions

| Element | Convention | Example |
| --- | --- | --- |
| Namespace | `PascalCase`, company-qualified | `OctagonFighter.Combat` |
| Class / Struct / Enum | `PascalCase` | `StaminaComponent`, `FightState` |
| Interface | `I` + `PascalCase` | `IDamageable`, `IStaminaSource` |
| Method | `PascalCase` | `ResolveTakedown()` |
| Public property | `PascalCase` | `CurrentHealth` |
| Private field | `_camelCase` | `_staminaCost` |
| Serialized field | `_camelCase` | `[SerializeField] private float _staminaCost;` |
| Local variable / parameter | `camelCase` | `damageAmount` |
| Constant | `PascalCase` | `MaxComboWindow` |
| Static readonly | `PascalCase` | `DefaultReach` |
| Event | `On` + past tense | `OnFighterKnockedOut` |
| Boolean | prefixed `Is`, `Has`, `Can` | `IsBlocking`, `CanGrapple` |
| Enum member | `PascalCase` | `FightState.Standing` |

Avoid: Hungarian notation, single-letter names (except loop indices), abbreviations
(`dmg`, `stam`), and public mutable fields.

### Folder Organization

```
Assets/_Project/
  Art/            Models, Materials, Textures, VFX, Animations
  Audio/          Music, SFX, Mixers
  Prefabs/        Organized by domain (Fighters/, VFX/, UI/)
  Scenes/         Bootstrap, MainMenu, Career, Arenas/, Tests/
  Scripts/        Mirrors the layer map (Core, Domain, Application, ...)
  Settings/       URP assets, Input actions, Render pipeline config
  ScriptableObjects/
    Fighters/     FighterDefinition, MoveSetDefinition
    Combat/       StrikeDefinition, TakedownDefinition, SubmissionDefinition
    AI/           AiProfile, DifficultyDefinition
    Career/       PromotionDefinition, ContractDefinition
  Tests/          EditMode/, PlayMode/
```

### Namespace Rules

- Root namespace: **`OctagonFighter`**.
- Namespaces must mirror folders: `Scripts/Combat/Striking` -> `OctagonFighter.Combat.Striking`.
- One namespace per feature area; never use the global namespace for runtime code.
- Keep `using` directives sorted; remove unused imports. Do not use `using` aliases for
  core domain types.

### Script Organization

Every file should follow this order:

1. `using` directives
2. namespace
3. XML summary (public types only)
4. serialized fields (grouped with `[Header]`)
5. private fields
6. properties
7. Unity lifecycle methods (`Awake`, `OnEnable`, `Start`, `Update`, ...)
8. public API
9. event handlers
10. private helpers

Additional rules:

- **One public type per file**, named to match the file (`StaminaComponent.cs`).
- Prefer `sealed` on leaf MonoBehaviours unless inheritance is intentional.
- Use `readonly` for injected dependencies; assign in `Awake`/constructor.
- Avoid `public` fields. Expose read-only properties or `[SerializeField] private`.
- Comments explain **why**, not **what**. No commented-out code.
- All public APIs require XML documentation. Internal code should be self-explanatory.
- Use `TryGetComponent` over `GetComponent`; cache references in `Awake`.
- Never hardcode balance values; read from ScriptableObjects or config.

---

# Unity Standards

### Prefab Naming

- Prefix by type: `PF_`, `UI_`, `VFX_`, `SO_`, `MAT_`, `ANIM_`, `AC_` (AnimationClip).
- Pattern: `PF_Fighter_Striker_A`, `VFX_Hit_Blood`, `UI_HUD_HealthBar`.
- Variants append `_Variant` or the variant name.
- Never rename a prefab without updating references; avoid orphaned GUIDs.

### ScriptableObject Usage

- All tunable/design data lives in ScriptableObjects under `Assets/_Project/ScriptableObjects`.
- Data assets are **immutable at runtime**; runtime state is copied into runtime models.
- Provide a factory (`FighterRuntime` from `FighterDefinition`) to prevent accidental
  mutation of shared assets.
- Use `CreateAssetMenu` with a clear hierarchy (`OctagonFighter/Combat/Strike Definition`).
- Validate data with `OnValidate` and fail fast on missing references.

```csharp
[CreateAssetMenu(menuName = "OctagonFighter/Combat/Strike Definition", fileName = "SD_Jab")]
public sealed class StrikeDefinition : ScriptableObject
{
    [SerializeField] private float _damage = 5f;
    [SerializeField] private float _staminaCost = 4f;
    public float Damage => _damage;
    public float StaminaCost => _staminaCost;
}
```

### Assembly Definitions

- Organize code into `.asmdef` files per module:
  - `OctagonFighter.Domain` (no `UnityEngine` references)
  - `OctagonFighter.Application`
  - `OctagonFighter.Infrastructure`
  - `OctagonFighter.Presentation`
  - `OctagonFighter.Editor` (Editor platform only)
  - `OctagonFighter.Tests.EditMode`, `OctagonFighter.Tests.PlayMode`
- Set `"autoReferenced": false` on runtime modules to enforce explicit dependencies.
- Domain must not reference Presentation or Infrastructure. Enforce with asmdef
  references, not discipline alone.
- Tests reference only the modules under test plus test frameworks.

### Scene Organization

- Keep scenes thin: composition roots only, no gameplay logic.
- Scenes:
  - `Bootstrap` - persistent services, loads first.
  - `MainMenu`
  - `Career` (hub/UI)
  - `Arena_Octagon_A`
  - `Tests_Combat`
- Use additive loading for arenas and UI. One scene owns one responsibility.
- Never use `DontDestroyOnLoad` for gameplay objects; use an explicit `GameSession` root.
- Every scene must be playable from the editor without external manual setup.

---

# Gameplay Systems

### Fighter Controller

`FighterController` is an **orchestrator**, not a combat brain. It wires capabilities and
routes input/intent to the state machine.

Responsibilities:
- Hold references to capabilities (`HealthComponent`, `StaminaComponent`,
  `StrikeComponent`, `GrappleComponent`, `LocomotionComponent`).
- Convert `InputAction` callbacks into abstract **intents** (`MoveIntent`, `StrikeIntent`,
  `TakedownIntent`).
- Delegate to `FighterStateMachine`.
- Publish lifecycle events (`OnFighterSpawned`, `OnFighterDowned`).

Must not: compute damage, decide combos, know about AI implementation, or query UI.

```csharp
public interface IFighterIntentSource
{
    Vector2 Move { get; }
    bool TryConsumeStrike(out StrikeIntent intent);
    bool TryConsumeTakedown(out TakedownIntent intent);
}
```

`PlayerIntentSource` (Input System) and `AiIntentSource` both implement this. The
controller is identical for player and AI - the source is swapped.

### State Machine

Hierarchical, data-driven, and **intent-driven**.

- `FighterStateMachine` owns states: `Idle`, `Locomotion`, `Striking`, `Blocking`,
  `Dodging`, `Clinch`, `Ground`, `Transition`, `KnockedDown`, `Submission`, `Victory`,
  `Defeat`.
- States are classes implementing `IFighterState` with `Enter`, `Tick`, `Exit`, and
  `CanTransitionTo`. Avoid a giant `switch`.
- Transitions are validated against priority and locks (e.g., a committed strike cannot
  cancel into a takedown mid-animation without a cancel window).
- Each state declares animation tags, movement authority, and accepted intents.
- Ground game uses a **shared** state machine for the grappling pair with a
  `GrappleContext` (top/bottom, grips, position) so both fighters stay synchronized.

### Combat System

Pipeline:

```
Input Intent -> State validation -> StrikeDefinition -> Animation event
            -> Hit detection -> Damage calculation -> Reactions -> Events
```

- **Never** apply damage directly from input or animation callbacks. Route through
  `CombatResolver` so multiplayer/rollback can reproduce it.
- Damage formula composes: base damage, attribute scaling (strength), target defense
  (chin/body), counter/opening multiplier, stamina penalty, and random variance bounded
  by a deterministic seed.
- Attack data is data-driven (`StrikeDefinition`): reach, startup, active, recovery,
  damage type, stamina cost, hit reaction, guard break value.
- Use **frame/ticks-based timing** (fixed step) for combat windows so behavior is
  consistent across frame rates and network scenarios.

### Combo System

- Combos are **sequences of legal follow-ups**, not scripted animations.
- `ComboGraph` (ScriptableObject) defines valid transitions, cancel windows, and
  damage scaling per step.
- Track combo index and scaling; reset on whiff, block, or timeout.
- Damage scaling is monotonic decreasing per hit to prevent infinite loops.
- All combo state is deterministic and serializable for future netcode.

### Hit Detection

- Primary: **swept shape casts** (`Physics.SphereCastNonAlloc` / capsule) from bone
  origins along motion, using `LayerMask` hitbox layers. Avoid trigger spam.
- Secondary: explicit hit volumes only where animation-driven precision is needed.
- Each attack has a `HitId`; a `HitRegistry` prevents multi-hit from one active window.
- Hits resolve against `IHittable` implementations, not concrete MonoBehaviours.
- Run detection on fixed timestep; buffer results and apply once per combat tick.
- Use non-allocating physics queries and reusable `RaycastHit[]` buffers.

### Health System

- `HealthComponent` implements `IDamageable`. It owns health and **damage state**, not
  reactions.
- Separate **health** (body) from **head/body damage accumulation** and **rocked/KD
  states** for authenticity.
- Emits `OnDamaged(DamageEvent)` and `OnHealthDepleted`. Reactions (stagger, KO) are
  handled by listeners/state machine.
- Clamp, never allow negative health to propagate; handle KO idempotently.
- Include regeneration rules driven by data (between rounds, not during).

### Stamina System

The central resource economy.

- `StaminaComponent` tracks `Current`, `Max`, regeneration rate, and regen delay.
- Costs are declared by actions (strikes, takedown attempts, transitions, blocking while
  pressured). Costs come from data, never constants in code.
- Fatigue thresholds modulate: damage output, speed, recovery, defense, and AI
  aggression. Expose as `StaminaTier` (Fresh, Working, Tired, Gassed).
- Regen is delayed after exertion and reduced while in disadvantageous positions.
- Never consume stamina inside `Update`; consume when an action is **committed**.
- Emit `OnStaminaChanged`, `OnStaminaDepleted`, `OnStaminaTierChanged`.
- Design rule: a fighter at zero stamina cannot throw meaningful offense and defends
  poorly - this must be visibly exploitable.

### Takedown System

- Takedowns are **contested** actions: attacker `TakedownAttempt` vs defender
  `TakedownDefense`, modified by timing, stamina, attributes (wrestling), and position.
- Model as a short deterministic resolution window, not instant teleport.
- Outcomes: Clean, Partial (clinch), Stuffed, Reversed. Each maps to a state transition
  and stamina cost split between both fighters.
- Driven by `TakedownDefinition` (setup time, success curve, stamina cost, animation set).
- Emit `OnTakedownResolved(TakedownResult)`; both fighters' state machines react.

### Ground Game

- Position graph: `Guard`, `HalfGuard`, `SideControl`, `Mount`, `Back`, `Crucifix`,
  `Turtle`, plus transitions.
- `GrappleContext` holds position, top/bottom roles, grips/stamina pressure, and turn
  ownership. Positions are nodes; transitions are data-driven edges with costs and
  success chances.
- Both fighters share the context; only one has **control priority** at a time.
- Ground strikes, sweeps, escapes, and submissions all consume the same stamina economy.
- Keep ground logic in domain classes testable without Unity physics.

### Submission System

- Submissions are **multi-stage**: attempt -> struggle -> escape or finish.
- Model as a persistence contest over several ticks using stamina, submission offense,
  defense, and position advantage.
- `SubmissionDefinition` declares setup cost, finish meter rate, escape difficulty, and
  damage/health effect on failure.
- Never resolve via a single random roll. Track progression so players can feel the
  struggle and tap timing matters.
- Emit `OnSubmissionAttempted`, `OnSubmissionProgress`, `OnSubmissionResolved`.
- Provide accessibility option for simplified struggle input; do not change core math.

### AI System

- AI is an **intent producer**, never a direct controller. `AiIntentSource` generates the
  same intents a player does.
- Structure: `Perception -> Evaluation -> Decision -> Intent`.
  - Perception: distance, opponent state, stamina, openings, position.
  - Evaluation: utility scoring of candidate actions from `AiProfile` (ScriptableObject).
  - Decision: highest-utility action subject to cooldowns and personality weights.
- Profiles define aggression, patience, grappling preference, risk tolerance, and
  difficulty via reaction delay and error chance - not by cheating stats.
- Add reaction latency and deliberate imperfection on lower difficulties; higher
  difficulties use better prediction, not hidden bonuses.
- AI must be deterministic given a seed for reproducible testing.
- Use behavior trees or utility AI; avoid monolithic `if/else` chains. Keep decision
  logic in `Domain` and unit-test it with simulated snapshots.

### Career Mode

- `CareerService` orchestrates progression: calendar, contracts, training, fights,
  ranking, injuries, and retirement.
- Persist via a save abstraction (`ISaveStore`) with versioned schemas and migration.
- Career state is pure data (`CareerState`) decoupled from UI; presenters read from it.
- Training modifies attributes with diminishing returns and fatigue/injury risk.
- Matchmaking and AI generation use ScriptableObject pools plus seeded generation for
  consistent saves.
- Never couple career logic to scene loads or UI; career advances through domain events.

---

# Performance Guidelines

### Object Pooling

- Pool anything spawned frequently: hit VFX, damage numbers, audio emitters, projectiles,
  UI list items, crowd/particles.
- Use a generic `ObjectPool<T>` / Unity `ObjectPool` with prewarm and max size.
- Never `Instantiate`/`Destroy` in hot combat paths. Return to pool on despawn, reset
  state in `OnSpawn`.
- Pools are owned by a system, not scattered static caches.

### Avoid Unnecessary `Update()`

- Prefer event-driven and tick-driven systems. Centralize ticking in a `CombatClock`
  that advances on a fixed step.
- Cache components; avoid repeated `GetComponent`/LINQ in `Update`.
- Use `Update` only for genuinely per-frame presentation (camera follow smoothing,
  UI interpolation). Disable scripts when not needed (`enabled = false`).
- Batch AI evaluation on a staggered schedule, not every frame for every fighter.

### Reduce GC Allocations

- Avoid allocations in combat loops: no `new` arrays, LINQ, closures, string
  concatenation, or boxing per frame.
- Use `NonAlloc` physics queries with reused buffers.
- Cache `WaitForSeconds`/`yield` objects; avoid per-frame coroutine allocation.
- Prefer `struct` for high-frequency data (damage events, intents) and pass by `in`/
  `ref` where appropriate.
- Use `StringBuilder` for runtime UI strings; cache formatting.
- Profile with the Unity Profiler and Memory Profiler; treat GC spikes in combat as bugs.

### Mobile-Friendly Considerations

- Target 60 FPS on mid-tier mobile where feasible; budget draw calls and overdraw.
- Use URP with conservative shadow/light settings, baked lighting, and LODs.
- Prefer skinned mesh LODs and reduced bone counts; avoid high-cost post-processing on
  low-end tiers.
- Keep physics stable with fixed timestep; avoid large collision meshes on fighters.
- Scale VFX/particle counts by quality tier; use `QualitySettings` and LOD groups.
- Keep scripts platform-agnostic; input goes through the Input System bindings only.

---

# Git Workflow

### Branching Model

| Prefix | Purpose | Example |
| --- | --- | --- |
| `feature/*` | New features | `feature/takedown-contest` |
| `bugfix/*` | Non-critical fixes | `bugfix/stamina-regen-delay` |
| `hotfix/*` | Urgent production fixes | `hotfix/ko-state-lock` |
| `release/*` | Release stabilization | `release/0.9.0` |
| `chore/*` | Tooling/docs/maintenance | `chore/asmdef-cleanup` |

Rules:

- Branch from `develop` for features/bugfixes; branch from `main` only for `hotfix/*`.
- Keep branches short-lived and focused on one change.
- Rebase feature branches onto `develop` before opening a PR; do not merge `develop`
  into a feature repeatedly.
- Open a Pull Request; require at least one review. Never push directly to `main` or
  `develop`.

### Commit Message Convention

Conventional Commits with a scope:

```
<type>(<scope>): <short imperative summary>

<body: what and why, wrapped at 72 chars>

<footer: refs, breaking changes>
```

Types: `feat`, `fix`, `refactor`, `perf`, `test`, `docs`, `chore`, `build`, `ci`, `style`.
Scopes: `combat`, `stamina`, `grapple`, `ai`, `career`, `input`, `ui`, `core`, `build`.

Examples:

```
feat(grapple): add contested takedown resolution window
fix(stamina): apply regen delay after committed strikes
refactor(ai): extract utility scoring into domain service
perf(combat): replace per-frame allocations with pooled buffers
```

Guidelines:

- Imperative mood ("add", not "added").
- Subject under 72 characters, no trailing period.
- Explain **why** in the body when non-obvious.
- Reference issue IDs in the footer (`Refs: #142`).
- One logical change per commit.

### Unity-Specific Git Rules

- Commit `.meta` files with their assets. Never commit `Library/`, `Temp/`, `Logs/`,
  `obj/`, or `UserSettings/`.
- Use Unity's **Force Text** serialization and **Visible Meta Files**.
- Configure `.gitattributes` with `merge=unityyamlmerge` for scenes/prefabs and
  `text eol=lf` where appropriate.
- Do not commit secrets or credentials. Keep API keys in local-only config.

---

# AI Assistant Rules

When generating code in this repository:

1. **Explain architecture first.** State where the code lives in the layer map, which
   interfaces it implements/depends on, and how it communicates (events/intents) before
   writing it.
2. **Generate production-ready code.** No pseudocode, no `TODO` stubs unless explicitly
   requested, no placeholder `Debug.Log`-driven logic. Include null-guards, validation,
   and XML docs on public APIs.
3. **Avoid hardcoding.** Balance values, timings, and costs belong in ScriptableObjects or
   config. Magic numbers are acceptable only for math constants with a comment.
4. **Consider multiplayer support.** Keep combat deterministic, tick-based, and free of
   non-reproducible randomness. State must be serializable. Do not put authoritative
   logic in `Update` or in animation events that cannot be replayed.
5. **Write maintainable code.** Small, focused classes; clear names; explicit
   dependencies. Follow the existing folder/namespace/naming conventions in this document.
6. **Never create unnecessary singletons.** Prefer dependency injection through
   composition roots (`Bootstrap`), explicit service references, or events. A singleton
   requires a documented justification and a testable seam.
7. **Prefer interfaces.** Depend on abstractions (`IDamageable`, `IStaminaSource`,
   `IFighterIntentSource`, `ISaveStore`). Concrete types are wired only at composition
   roots.
8. **Explain tradeoffs.** When multiple approaches exist, list them, recommend one, and
   justify it in terms of performance, testability, determinism, and scope.

### Additional Expectations

- Do not introduce new third-party packages without approval; verify the package already
  exists in `Packages/manifest.json`.
- Do not modify `Domain` to reference `UnityEngine` or `Presentation`.
- Add or update tests in `Tests/` for combat math, stamina, and AI scoring.
- Preserve user changes and existing code style; do not mass-reformat unrelated files.
- If a requirement is ambiguous or would force a design tradeoff, ask before implementing.
- Never commit, push, or create PRs unless explicitly requested.

---

## Quick Reference

- **Root namespace:** `OctagonFighter`
- **Branch prefixes:** `feature/`, `bugfix/`, `hotfix/`, `release/`, `chore/`
- **Commits:** Conventional Commits with scope
- **Data:** ScriptableObjects only, never hardcoded balance
- **Communication:** intents in, events out, interfaces between modules
- **Determinism:** tick-based combat, seeded randomness, serializable state
- **Performance:** pool, no per-frame allocations, no unnecessary `Update`
