# COMBAT_ARCHITECTURE.md - Octagon Fighter

> **Design only.** This document defines the architecture for the Core Combat Foundation.
> No runtime code, strike data, or punch mechanics are implemented yet.

| Field | Value |
| --- | --- |
| Document Version | 1.0 |
| Status | Design complete - implementation not started |
| Scope | Fighter data, controller, state machine, health, stamina, hit detection |
| Last Updated | 2026-10-23 |
| Related Documents | [OPENCODE.md](OPENCODE.md), [GAME_DESIGN_DOCUMENT.md](GAME_DESIGN_DOCUMENT.md), [ROADMAP.md](ROADMAP.md) |

---

## 1. Purpose

This document specifies the foundation that every later combat system will build on:
striking, clinch, takedowns, ground game, submissions, and AI. The foundation must be
stable enough that adding those systems does not require rewriting health, stamina, state
management, input routing, or hit detection.

### In Scope

- `FighterData` - immutable runtime fighter data and its ScriptableObject source.
- `FighterController` - the Unity-facing orchestrator for one fighter.
- `FighterStateMachine` - intent-driven, extensible state management.
- Health System - damage application, region trauma, depletion, and KO state.
- Stamina System - costs, regeneration delay, tiers, and fatigue exposure.
- Hit Detection - generic swept queries, target filtering, and duplicate-hit prevention.

### Out of Scope

- **Punches, strikes, combos, and attack definitions.** No strike types, punch data, hit
  windows, or damage formulas are defined in this milestone.
- Grappling, clinch, takedowns, ground positions, and submissions.
- AI decision-making beyond the intent-source contract.
- Animation, audio, VFX, UI implementation, and final art.
- Online multiplayer and rollback netcode.

### Design Goal

A new combat action should be addable by supplying data and one new action/state
implementation. It should not require changes to `FighterController`, the health model,
the stamina model, or the hit-detection pipeline.

---

## 2. Architectural Principles

1. **Domain owns rules, Unity owns adaptation.**
   Combat math, health, stamina, and state transitions are pure C#. MonoBehaviours adapt
   input, physics, and presentation to those rules.

2. **One owner per piece of state.**
   `HealthModel` owns health. `StaminaModel` owns stamina. `FighterStateMachine` owns the
   current state. `FighterController` owns composition, not duplicate state.

3. **Data flows inward as immutable values.**
   A `FighterDefinition` ScriptableObject never enters the domain. It is converted into an
   immutable `FighterData` snapshot at spawn time.

4. **Intents in, facts out.**
   Controllers receive `FighterIntent` values and publish past-tense events
   (`OnDamaged`, `OnStaminaDepleted`, `OnStateChanged`, `OnHitDetected`).

5. **Fixed-tick combat.**
   Combat advances through an explicit `CombatClock`, not `Update()` or `Time.deltaTime`
   inside domain logic. Frame-rate independence and future determinism depend on this.

6. **Hit detection reports; resolution decides.**
   Hit detection returns candidates. A future `CombatResolver` will be the only system
   allowed to turn a hit into damage.

7. **Composition over inheritance.**
   Fighters are assembled from data, models, services, and adapters. No
   `Fighter -> Striker -> Grappler` hierarchy.

---

## 3. Assembly and Folder Architecture

### 3.1 Assemblies

The existing layer map is extended with an explicit `Data` assembly so that Unity
ScriptableObjects never leak into `Domain`.

| Assembly | Responsibility | References | Unity Types |
| --- | --- | --- | --- |
| `OctagonFighter.Core` | Shared primitives, IDs, math types, clock contracts, deterministic RNG | none | No |
| `OctagonFighter.Domain` | Fighter data snapshot, health, stamina, state machine, hit registry | Core | **No** |
| `OctagonFighter.Application` | Fighter runtime orchestration, hit-detection orchestration, phase ordering | Core, Domain | No |
| `OctagonFighter.Data` | ScriptableObject definitions and mapping to domain snapshots | Core, Domain | Yes |
| `OctagonFighter.Infrastructure` | Input adapters, Unity physics hit detector, MonoBehaviour health/stamina adapters | Core, Domain, Application | Yes |
| `OctagonFighter.Presentation` | `FighterController`, views, debug HUD, bootstrap | Core, Domain, Application, Data, Infrastructure | Yes |
| `OctagonFighter.Tests.EditMode` | Pure domain and orchestration tests | Core, Domain, Application, Data | Editor only |
| `OctagonFighter.Tests.PlayMode` | Physics, controller, and scene-wiring tests | all runtime assemblies | Yes |

Dependency direction:

```
Presentation ──> Infrastructure ──> Application ──> Domain ──> Core
      │                │                 │
      └──> Data ───────┴─────────────────┘
```

Rules:

- `Domain` and `Application` set `noEngineReferences: true`.
- `Domain` must never reference `UnityEngine`, `UnityEditor`, or the Input System.
- `Data` may reference `UnityEngine` because ScriptableObjects are Unity assets.
- `Infrastructure` owns all Unity API usage for combat: `Physics`, `Collider`, `LayerMask`,
  `InputAction`, and MonoBehaviour lifecycle.
- `Presentation` owns scene composition and read-only presentation. It must not perform
  combat math.

### 3.2 Target Folder Layout

```
Assets/_Project/Scripts/
  Core/
    OctagonFighter.Core.asmdef
    Ids/                  FighterId, HitId, TickId
    Math/                 Float2, Float3
    Time/                 CombatClock, ICombatClock
    Random/               DeterministicRandom, IRandomSource
    Events/               EventBuffer<T>, IEventSink<T>
  Domain/
    OctagonFighter.Domain.asmdef
    Fighters/             FighterData, FighterAttributes, FighterSnapshot
    Health/               HealthModel, HealthProfile, DamageEvent, DamageResult
    Stamina/              StaminaModel, StaminaProfile, StaminaTier
    StateMachine/         FighterStateMachine, IFighterState, FighterStateContext
    Combat/               HitRegistry, HitId, HitCandidate
    Contracts/            IDamageable, IHealthSource, IStaminaSource
  Application/
    OctagonFighter.Application.asmdef
    Fighters/             FighterRuntime
    Combat/               HitDetectionSystem, CombatPhaseRunner
  Data/
    OctagonFighter.Data.asmdef
    Fighters/             FighterDefinition, FighterDefinitionValidator
    Profiles/             HealthProfileAsset, StaminaProfileAsset
  Infrastructure/
    OctagonFighter.Infrastructure.asmdef
    Input/                PlayerIntentSource, InputIntentReader
    Health/               HealthComponent
    Stamina/              StaminaComponent
    Physics/              SweptHitDetector, FighterIdentity, HitTarget
    Time/                 CombatTickDriver
  Presentation/
    OctagonFighter.Presentation.asmdef
    Fighter/              FighterController, FighterView, FighterDebugHud
    Scenes/               CombatTestBootstrap
Assets/_Project/Tests/
  EditMode/               OctagonFighter.Tests.EditMode.asmdef
  PlayMode/               OctagonFighter.Tests.PlayMode.asmdef
```

---

## 4. Runtime Composition

```text
Bootstrap / Scene
        │
        ├── CombatTickDriver (fixed tick)
        ├── HitDetectionSystem
        │       └── IHitDetector -> SweptHitDetector (Unity Physics)
        │
        └── FighterController (one per fighter)
                ├── IFighterIntentSource   (player or AI)
                ├── FighterRuntime         (pure Application orchestration)
                │       ├── FighterData
                │       ├── HealthModel / HealthComponent adapter
                │       ├── StaminaModel / StaminaComponent adapter
                │       └── FighterStateMachine
                ├── FighterView            (animation/visual adapter, later)
                └── FighterDebugHud        (test scene only)
```

`FighterController` is the only class that knows all of the fighter's parts. It wires them
once during initialization and then forwards ticks and events.

---

## 5. FighterData

### 5.1 Two Representations

| Type | Layer | Lifetime | Mutability |
| --- | --- | --- | --- |
| `FighterDefinition` | `Data` | Authored asset | Immutable at runtime |
| `FighterData` | `Domain` | Session | Immutable |

`FighterDefinition : ScriptableObject` is the designer-facing asset. It contains only
authored values and references. It must not hold runtime state, current health, current
stamina, or mutable caches.

`FighterData` is a readonly domain snapshot created from `FighterDefinition` when the
fighter spawns. All combat systems read `FighterData`; none read the ScriptableObject.

### 5.2 Proposed Domain Model

```csharp
namespace OctagonFighter.Domain.Fighters
{
    public readonly struct FighterData
    {
        public FighterId Id { get; }
        public string DisplayName { get; }
        public FighterAttributes Attributes { get; }
        public BodyProfile Body { get; }
        public HealthProfile Health { get; }
        public StaminaProfile Stamina { get; }
        public MovementProfile Movement { get; }
    }
}
```

`FighterAttributes` starts with the attributes required by health, stamina, and movement:

```csharp
public readonly struct FighterAttributes
{
    public int Strength { get; }     // 1-100
    public int Speed { get; }        // 1-100
    public int Cardio { get; }       // 1-100
    public int Durability { get; }   // 1-100
    public int Flexibility { get; }  // 1-100
    public int Composure { get; }    // 1-100
    public int FightIq { get; }      // 1-100
}
```

Striking, grappling, discipline, and mental attributes are added in later milestones
without changing the contract above. They are new fields, not new dependencies.

`BodyProfile` contains reach, height, stance, and weight class. These are data only; no
punch or reach logic is implemented yet.

### 5.3 ScriptableObject Shape

```csharp
[CreateAssetMenu(menuName = "OctagonFighter/Fighters/Fighter Definition")]
public sealed class FighterDefinition : ScriptableObject
{
    [SerializeField] private string _id;
    [SerializeField] private string _displayName;
    [SerializeField] private FighterAttributesAuthoring _attributes;
    [SerializeField] private BodyProfileAuthoring _body;
    [SerializeField] private HealthProfileAuthoring _health;
    [SerializeField] private StaminaProfileAuthoring _stamina;
    [SerializeField] private MovementProfileAuthoring _movement;

    public FighterData CreateRuntimeData();
    private void OnValidate();
}
```

### 5.4 Validation Rules

`OnValidate` and `CreateRuntimeData` fail fast on:

- Empty or duplicate fighter IDs.
- Attribute values outside `1-100`.
- Non-positive max health or max stamina.
- Negative regeneration values.
- Regen delay below zero.
- Tier thresholds that are not strictly descending.
- Missing required profiles.

Invalid data throws `InvalidOperationException` with the asset name and field in the
message. It never silently substitutes defaults in a build.

### 5.5 Ownership Rules

- `FighterDefinition` is never passed into `Domain`.
- `FighterData` contains no `UnityEngine` types.
- Runtime mutable state lives only in `HealthModel`, `StaminaModel`, and the state machine.
- A future fighter factory may cache `FighterData` snapshots, but never the models.

---

## 6. FighterController

### 6.1 Responsibility

`FighterController` is the Unity-facing orchestrator for exactly one fighter. It is the
composition root for that fighter and the boundary between Unity and the simulation.

It **does**:

- Validate and convert `FighterDefinition` into `FighterData` at spawn.
- Construct the runtime, health/stamina adapters, and state machine.
- Bind an `IFighterIntentSource` (player or AI) and a `FighterView`.
- Receive fixed ticks from `CombatTickDriver`.
- Expose read-only `FighterSnapshot` data for views, AI perception, and debugging.
- Forward `IDamageable` calls to the health system.
- Publish lifecycle and state-change events.

It **does not**:

- Compute damage, hit chance, combo scaling, or stamina costs.
- Know whether its intent source is a player or an AI.
- Read input actions directly.
- Query the scene with `FindObjectOfType`.
- Mutate another fighter's state.
- Contain punch, strike, or move logic.

### 6.2 Proposed Public Surface

```csharp
public sealed class FighterController : MonoBehaviour
{
    public FighterId Id { get; }
    public FighterSnapshot Snapshot { get; }
    public IDamageable Damageable { get; }
    public IHealthSource Health { get; }
    public IStaminaSource Stamina { get; }

    public void Initialize(
        FighterData data,
        IFighterIntentSource intentSource,
        ICombatServices services);

    public void Tick(CombatTick tick, float deltaSeconds);

    public event Action<FighterStateChangedEvent> StateChanged;
    public event Action<FighterSpawnedEvent> Spawned;
    public event Action<FighterDefeatedEvent> Defeated;
}
```

`ICombatServices` is a small composition bundle rather than a service locator:

```csharp
public readonly struct ICombatServices
{
    public ICombatClock Clock { get; }
    public IHitDetectionSink HitDetection { get; }
    public IRandomSource Random { get; }
}
```

`FighterSnapshot` is a readonly struct containing only what presentation and perception
need: fighter ID, current state ID, health, stamina tier, position, facing, and flags. It
contains no references to MonoBehaviours.

### 6.3 Lifecycle

1. `Awake` - cache local components and validate required references.
2. `Initialize` - build `FighterData`, health, stamina, state machine, and view binding.
3. `OnEnable` - subscribe to the global tick and health/stamina events.
4. `Tick` - run the per-tick phase sequence.
5. `OnDisable` - unsubscribe from all events and return pooled objects.
6. `OnDestroy` - release runtime references; no static cleanup required.

`FighterController.Tick` does not exist in the current codebase. It is the designed entry
point for `CombatTickDriver` once implementation starts.

### 6.4 Per-Tick Phase Order

Each tick runs in a consistent order so behavior is reproducible:

1. **Intent** - poll the intent source for this tick.
2. **Stamina** - regenerate or apply delayed recovery.
3. **State** - tick the current state and resolve requested transitions.
4. **Actions** - future combat actions open/close hit windows and emit hit queries.
5. **Hit detection** - process all queries against physics.
6. **Resolution** - future `CombatResolver` applies damage through `IDamageable`.
7. **Reaction** - health/stamina events are drained and routed to the state machine.
8. **Presentation** - update `FighterSnapshot` and notify views.

No domain logic may read `Time.deltaTime`, `UnityEngine.Random`, or Unity physics state.

---

## 7. FighterStateMachine

### 7.1 Design

The state machine is pure domain code. It is data-driven, intent-driven, and extensible by
registration rather than by a central `switch`.

- Each state is a class implementing `IFighterState`.
- States declare accepted intents and movement authority.
- Transitions are validated by the state machine, not by the caller.
- Transition priority and interrupt rules are explicit.
- The state machine owns only the current state and state-local time. It does not own
  health or stamina.

### 7.2 Proposed Contracts

```csharp
public interface IFighterState
{
    FighterStateId Id { get; }
    MovementAuthority MovementAuthority { get; }
    IntentMask AcceptedIntents { get; }
    int TransitionPriority { get; }

    void Enter(in FighterStateContext context, in FighterIntent intent);
    StateTickResult Tick(in FighterStateContext context, float deltaSeconds);
    void Exit(in FighterStateContext context);
    bool CanTransitionTo(FighterStateId target, in FighterStateContext context);
}

public sealed class FighterStateMachine
{
    public FighterStateId CurrentStateId { get; }
    public float TimeInState { get; }

    public void Register(IFighterState state);
    public bool TryTransition(FighterStateId target, in FighterStateContext context);
    public StateTransition? Tick(
        in FighterStateContext context,
        in FighterIntent intent,
        float deltaSeconds);
}
```

`FighterStateContext` exposes the models and services a state may read:

```csharp
public readonly struct FighterStateContext
{
    public FighterData Data { get; }
    public IHealthSource Health { get; }
    public IStaminaSource Stamina { get; }
    public ICombatClock Clock { get; }
    public FighterSnapshot CurrentSnapshot { get; }
}
```

### 7.3 Foundation States

No strike or punch states are defined yet. The foundation includes lifecycle and
interruption states only:

| State | Purpose | Movement Authority |
| --- | --- | --- |
| `Spawning` | One-time initialization and spawn protection | None |
| `Idle` | Default standing state | None |
| `Locomotion` | Intent-driven movement | Full |
| `Blocking` | Held defensive stance | Limited |
| `Dodging` | Timed evasive action | Limited |
| `CommittedAction` | Generic locked state reserved for future combat actions | None |
| `HitReaction` | Reaction to received damage | None |
| `KnockedDown` | Downed state after a knockdown | None |
| `Recovering` | Get-up or recovery window | None |
| `Defeated` | Terminal KO/TKO/decision state | None |

`CommittedAction` is intentionally empty. Strikes, takedowns, and other actions will each
provide their own state or action implementation later without modifying the state machine
core.

### 7.4 Transition Rules

Foundation transitions:

| From | Allowed Targets |
| --- | --- |
| `Spawning` | `Idle`, `Defeated` |
| `Idle` | `Locomotion`, `Blocking`, `Dodging`, `HitReaction`, `KnockedDown`, `Defeated` |
| `Locomotion` | `Idle`, `Blocking`, `Dodging`, `HitReaction`, `KnockedDown`, `Defeated` |
| `Blocking` | `Idle`, `Locomotion`, `HitReaction`, `KnockedDown`, `Defeated` |
| `Dodging` | `Idle`, `Locomotion`, `HitReaction`, `KnockedDown`, `Defeated` |
| `CommittedAction` | `Idle`, `Locomotion`, `HitReaction`, `KnockedDown`, `Defeated` |
| `HitReaction` | `Idle`, `Locomotion`, `KnockedDown`, `Defeated` |
| `KnockedDown` | `Recovering`, `Defeated` |
| `Recovering` | `Idle`, `Defeated` |
| `Defeated` | none |

Rules:

- `Defeated` is terminal.
- Health depletion forces `Defeated` regardless of priority.
- Knocked-down and hit-reaction transitions are interrupts and outrank normal intents.
- A committed action cannot be canceled unless its state explicitly allows it. This is the
  future cancel-window hook.
- Invalid transitions are rejected and logged in development builds; they do not throw in
  release builds.

### 7.5 Intents

```csharp
public readonly struct FighterIntent
{
    public Float2 Move { get; }
    public FighterCommand Command { get; }
    public bool IsCommandPressed { get; }
    public bool IsCommandHeld { get; }
    public TickId Tick { get; }
}

public enum FighterCommand : byte
{
    None,
    Block,
    Dodge,
    Cancel,
    Interact,
    StandUp
}
```

Attacks are deliberately absent. A future milestone adds an attack channel or extends
`FighterCommand` without changing the controller-to-state-machine flow.

`IFighterIntentSource` is implemented by both `PlayerIntentSource` and a future
`AiIntentSource`:

```csharp
public interface IFighterIntentSource
{
    bool TryGetIntent(TickId tick, out FighterIntent intent);
}
```

The intent source may buffer inputs between ticks. It must not call into the state machine
or health system directly.

### 7.6 Extending the State Machine

To add a future strike:

1. Add strike data to the `Data` assembly.
2. Add the action/state implementation in `Domain` or `Application`.
3. Register the state with `FighterStateMachine.Register`.
4. Emit hit queries through the existing hit-detection pipeline.
5. Let the future `CombatResolver` apply damage through `IDamageable`.

The state machine core, controller, health, stamina, and hit detection require no
modification.

---

## 8. Health System

### 8.1 Responsibility

The health system owns life state and accumulated regional trauma. It does not know how
damage is produced, which strike caused it, or how animations should react.

### 8.2 Proposed Model

```csharp
public sealed class HealthModel
{
    public float CurrentHealth { get; }
    public float MaxHealth { get; }
    public float HeadTrauma { get; }
    public float BodyTrauma { get; }
    public bool IsKnockedOut { get; }
    public bool IsAlive { get; }

    public DamageResult ApplyDamage(in DamageEvent damage);
}
```

Profiles:

```csharp
public readonly struct HealthProfile
{
    public float MaxHealth { get; }
    public float HeadTraumaLimit { get; }
    public float BodyTraumaLimit { get; }
    public float KnockoutThreshold { get; }
    public float DurabilityScale { get; }
}
```

Damage events:

```csharp
public readonly struct DamageEvent
{
    public FighterId AttackerId { get; }
    public FighterId TargetId { get; }
    public float Amount { get; }
    public DamageType Type { get; }
    public HitRegion Region { get; }
    public TickId Tick { get; }
}
```

`DamageType` starts as `Impact`, `Accumulated`, `Cut`, and `Stun`. `HitRegion` starts as
`Head`, `Body`, and `Limb`. These are generic categories; no strike definitions exist yet.

### 8.3 Rules

- Final damage is calculated by a future `CombatResolver`. `HealthModel` receives an
  already-resolved amount.
- Health is clamped to `[0, MaxHealth]`; negative values never propagate.
- Regional trauma is clamped to its profile limit.
- `IsKnockedOut` becomes true when health reaches zero or the profile's knockout threshold
  is crossed.
- KO is idempotent: repeated damage after KO does not emit duplicate KO events.
- Reactions (stagger, hit reaction, knockdown) are selected by listeners or the state
  machine, not by `HealthModel`.

### 8.4 Unity Adapter

`HealthComponent : MonoBehaviour` is a thin adapter implementing `IDamageable` and
`IHealthSource`. It delegates to a `HealthModel` created during initialization. It does
not contain combat math and does not own duplicate values.

```csharp
public interface IDamageable
{
    DamageResult ApplyDamage(in DamageEvent damage);
}

public interface IHealthSource
{
    float CurrentHealth { get; }
    float MaxHealth { get; }
    bool IsAlive { get; }
    bool IsKnockedOut { get; }
    event Action<HealthChangedEvent> Changed;
    event Action<HealthDepletedEvent> Depleted;
}
```

### 8.5 Events

- `HealthChangedEvent` - previous/current health and trauma values.
- `HealthDepletedEvent` - zero health reached; includes the lethal `DamageEvent`.
- `FighterKnockedOutEvent` - emitted once when KO state becomes true.

Events are readonly structs. They are buffered during the resolution phase and drained
during the reaction phase to prevent re-entrant damage loops.

---

## 9. Stamina System

### 9.1 Responsibility

Stamina is the central combat economy. The model owns the current value, regeneration,
delay, tier, and tier modifiers. It does not decide what an action costs; callers supply
a `StaminaCost`.

### 9.2 Proposed Model

```csharp
public sealed class StaminaModel
{
    public float CurrentStamina { get; }
    public float MaxStamina { get; }
    public StaminaTier Tier { get; }
    public float TimeSinceLastSpend { get; }
    public bool IsRegenerating { get; }

    public bool CanAfford(in StaminaCost cost);
    public bool TrySpend(in StaminaCost cost);
    public void Tick(float deltaSeconds);
}
```

Profiles and tiers:

```csharp
public readonly struct StaminaProfile
{
    public float MaxStamina { get; }
    public float RegenPerSecond { get; }
    public float RegenDelaySeconds { get; }
    public StaminaTierThresholds Tiers { get; }
    public StaminaTierModifiers TiredModifiers { get; }
    public StaminaTierModifiers GassedModifiers { get; }
}

public enum StaminaTier : byte
{
    Fresh,
    Working,
    Tired,
    Gassed
}
```

`StaminaCost` is supplied by the future action:

```csharp
public readonly struct StaminaCost
{
    public float Amount { get; }
    public float RecoveryDelaySeconds { get; }
}
```

### 9.3 Rules

- Stamina is spent only when an action is **committed**, never passively in `Update`.
- `TrySpend` fails if the fighter cannot afford the cost; the action must be validated
  before it starts.
- Spending resets the regeneration delay.
- Regeneration does not run during the delay.
- Current stamina is clamped to `[0, MaxStamina]`.
- Tier is derived from configured thresholds. Crossing a tier emits exactly one tier-change
  event.
- Tier modifiers are data only for now. Future systems consume them for speed, damage,
  defense, and recovery; no punch logic is defined here.
- A gassed fighter must be visibly and mechanically disadvantaged once those dependent
  systems exist.

### 9.4 Unity Adapter

`StaminaComponent : MonoBehaviour` is a thin adapter implementing `IStaminaSource` and
delegating to `StaminaModel`. The controller and state machine depend on `IStaminaSource`,
not on the MonoBehaviour.

```csharp
public interface IStaminaSource
{
    float CurrentStamina { get; }
    float MaxStamina { get; }
    StaminaTier Tier { get; }
    bool CanAfford(in StaminaCost cost);
    bool TrySpend(in StaminaCost cost);
    event Action<StaminaChangedEvent> Changed;
    event Action<StaminaTierChangedEvent> TierChanged;
}
```

### 9.5 Events

- `StaminaChangedEvent` - current/max value and regeneration state.
- `StaminaTierChangedEvent` - previous and new tier.
- `StaminaDepletedEvent` - current stamina reached zero.

All events are readonly structs and are published after the state update phase.

---

## 10. Hit Detection

### 10.1 Responsibility

Hit detection answers a single question: **which fighters did a swept volume overlap, and
where?** It does not apply damage, calculate hit chance, or know about punches.

The pipeline is intentionally split:

```text
Future combat action
        │  HitQuery
        ▼
HitDetectionSystem ──> IHitDetector ──> SweptHitDetector (Unity Physics)
        │                                    │
        │  HitCandidate[]                    │  non-alloc physics casts
        ▼                                    ▼
HitRegistry (dedupe)                  FighterIdentity / Hurtbox
        │
        │  HitDetectedEvent
        ▼
Future CombatResolver (damage authority)
        │
        ▼
IDamageable / HealthModel
```

### 10.2 Contracts

```csharp
public interface IHitDetector
{
    int Detect(in HitQuery query, Span<HitCandidate> results);
}

public readonly struct HitQuery
{
    public HitId HitId { get; }
    public FighterId AttackerId { get; }
    public Float3 Origin { get; }
    public Float3 Direction { get; }
    public float Distance { get; }
    public float Radius { get; }
    public int CollisionMask { get; }
    public TickId Tick { get; }
}

public readonly struct HitCandidate
{
    public FighterId TargetId { get; }
    public Float3 Point { get; }
    public Float3 Normal { get; }
    public float Distance { get; }
    public int ColliderId { get; }
}
```

`CollisionMask` is an opaque integer at the domain boundary. Infrastructure maps it to
Unity's `LayerMask`.

`HitId` identifies one active hit window from one attacker. `HitRegistry` prevents the same
`HitId` from damaging the same target more than once:

```csharp
public sealed class HitRegistry
{
    public bool TryClaim(HitId hitId, FighterId targetId);
    public void Release(HitId hitId);
    public void ClearExpired(TickId currentTick);
}
```

### 10.3 Infrastructure Implementation

`SweptHitDetector`:

- Uses `Physics.SphereCastNonAlloc` or `Physics.CapsuleCastNonAlloc` depending on the
  query shape.
- Reuses a preallocated `RaycastHit[]` buffer. No per-tick allocations.
- Uses `QueryTriggerInteraction.Ignore`.
- Resolves `Collider` to `FighterId` through an `IFighterIdentity` component or an injected
  target registry. It never calls `FindObjectOfType`.
- Selects the nearest valid candidate per target.
- Runs on the fixed combat tick, never in `Update`.

`FighterIdentity` is a lightweight MonoBehaviour placed on the fighter root or hurtbox. It
exposes the stable `FighterId` and the owning `FighterController`.

### 10.4 Deduplication and Buffering

- Each active hit window has a unique `HitId`.
- `HitRegistry.TryClaim(hitId, targetId)` returns false for duplicate overlaps in the same
  active window.
- Released or expired hit IDs are cleared by the hit-detection system.
- Results are written into a per-tick buffer and processed once during the resolution
  phase. This avoids duplicate damage when multiple colliders overlap.

### 10.5 Layers and Volumes

Planned layers (added when hit volumes are implemented):

| Layer | Purpose |
| --- | --- |
| `FighterHurtbox` | Receives hit queries |
| `FighterHitbox` | Future attack volumes |
| `Arena` | Environment occlusion |

No hitboxes, hurtboxes, or punch volumes are created in this milestone. The layer names and
contracts are designed now so later volumes plug in without refactoring.

### 10.6 Determinism Notes

Unity physics is not guaranteed to be deterministic across platforms. Hit detection is
therefore isolated behind `IHitDetector` and treated as an input to the simulation. Local
play can use physics casts. Future rollback or online play may replace the detector with a
deterministic volume implementation without changing the domain.

---

## 11. Contracts and Event Catalog

### 11.1 Interfaces

| Interface | Layer | Purpose |
| --- | --- | --- |
| `IDamageable` | Domain | Receives resolved damage |
| `IHealthSource` | Domain | Read-only health state and events |
| `IStaminaSource` | Domain | Read-only stamina state and events |
| `IFighterIntentSource` | Application | Produces player or AI intents |
| `IFighterState` | Domain | One state-machine state |
| `IHitDetector` | Application | Returns hit candidates for a query |
| `IHitDetectionSink` | Application | Accepts hit queries and returns deduplicated detections |
| `ICombatClock` | Core | Supplies fixed ticks |
| `IRandomSource` | Core | Seeded deterministic randomness |
| `IFighterIdentity` | Infrastructure | Maps colliders to stable fighter IDs |

### 11.2 Events

| Event | Emitted By | Consumed By |
| --- | --- | --- |
| `FighterSpawnedEvent` | Controller | Scene/AI registration |
| `FighterStateChangedEvent` | State machine | View, debug HUD, AI perception |
| `HealthChangedEvent` | Health model | HUD, AI, debug |
| `HealthDepletedEvent` | Health model | State machine, match flow |
| `FighterKnockedOutEvent` | Health model | Match flow, presentation |
| `StaminaChangedEvent` | Stamina model | HUD, AI |
| `StaminaTierChangedEvent` | Stamina model | HUD, AI, future modifiers |
| `HitDetectedEvent` | Hit-detection system | Future `CombatResolver` |
| `FighterDefeatedEvent` | Controller | Match flow |

Event rules:

- Past tense and immutable.
- No `UnityEngine` types in domain event payloads.
- Buffered per phase where re-entrancy is possible.
- Subscribers must not mutate the emitter synchronously.

---

## 12. Determinism and Time

### 12.1 Combat Clock

`CombatClock` advances simulation in fixed steps:

- Recommended simulation rate: **60 ticks per second** (configurable).
- Accumulator-based tick driver lives in `Infrastructure`.
- Domain systems receive `CombatTick` and `deltaSeconds` as explicit inputs.
- No domain code reads `Time.time`, `Time.deltaTime`, or `UnityEngine.Random`.

### 12.2 Randomness

`DeterministicRandom` is a seeded, documented algorithm (recommended: PCG32 or xorshift128+)
implemented in `Core`. It is owned per combat session, not per fighter. Future damage
variance, AI error, and sequence checks draw from it in a fixed order.

### 12.3 Serialization Direction

The following state is expected to become serializable for saves and future netcode:

- Current health and regional trauma.
- Current stamina and regeneration delay.
- Current state ID and time in state.
- Pending intent for the current tick.
- Active hit IDs and claimed targets.

The foundation should avoid types that cannot be serialized: no `MonoBehaviour` references,
no delegates in state, no Unity-only math types.

---

## 13. Dependency and Forbidden Reference Rules

| From | May Reference | Must Not Reference |
| --- | --- | --- |
| Core | .NET standard library | UnityEngine, UnityEditor, Domain |
| Domain | Core | UnityEngine, Data, Infrastructure, Presentation |
| Application | Core, Domain | UnityEngine, Data, Infrastructure, Presentation |
| Data | Core, Domain, UnityEngine | Infrastructure, Presentation |
| Infrastructure | Core, Domain, Application, UnityEngine, Input System | Presentation |
| Presentation | everything runtime | Editor-only assemblies |

Additional rules:

- No `FindObjectOfType`, static singletons, or service locators in combat code.
- No `UnityEngine.Random` in domain or application logic.
- No damage application outside the future `CombatResolver`.
- No ScriptableObject references stored in domain models.
- No per-frame allocations in hit detection or state-machine ticks.

---

## 14. Testing Strategy

### 14.1 EditMode Tests

| Test Area | Coverage |
| --- | --- |
| `HealthModelTests` | Clamping, regional trauma, KO idempotence, event payloads |
| `StaminaModelTests` | Spend/deny, regen delay, tier transitions, clamping |
| `FighterStateMachineTests` | Valid/invalid transitions, interrupt priority, intent gating |
| `HitRegistryTests` | Duplicate hit rejection, per-target claims, expiry |
| `DeterministicRandomTests` | Same seed produces same sequence |
| `FighterDataTests` | Validation failures, immutable snapshot creation |

### 14.2 PlayMode Tests

| Test Area | Coverage |
| --- | --- |
| `FighterControllerTests` | Spawn, tick, intent routing, snapshot updates |
| `SweptHitDetectorTests` | Sphere/capsule casts on configured layers, nearest target |
| `HitDeduplicationTests` | One hit window damages a target once |
| `HealthIntegrationTests` | Damage through `IDamageable` reaches `HealthModel` |
| `StaminaIntegrationTests` | Committed costs and delayed regeneration across ticks |

### 14.3 Test Doubles

- `FakeIntentSource` - queues `FighterIntent` values by tick.
- `FakeHitDetector` - returns configured candidates without physics.
- `FakeCombatClock` - manual tick advancement.
- `FakeRandomSource` - deterministic sequence for assertions.

No gameplay test may depend on wall-clock time, frame rate, or unseeded randomness.

---

## 15. Extension Points

The foundation is considered extensible if each of the following can be added without
modifying health, stamina, the state-machine core, or the hit-detection contracts:

| Future Feature | Additive Work |
| --- | --- |
| Strikes and punches | `StrikeDefinition`, strike action/state, hit queries |
| Combos | Combo graph, action sequencing, scaling |
| Blocking and dodging depth | State refinements, guard data, timing windows |
| Grappling | `GrappleContext`, new states/actions, shared contest service |
| Submissions | Multi-stage contest state and data |
| AI | New `IFighterIntentSource` implementation and perception model |
| Animation | New `FighterView` bindings driven by state/events |
| Netcode | Replace hit detector and clock source; serialize existing models |

The surface that must remain stable:

- `FighterController.Initialize/Tick` flow.
- `IFighterIntentSource`.
- `IFighterState` and state registration.
- `IHealthSource`, `IStaminaSource`, and `IDamageable`.
- `IHitDetector`, `HitQuery`, `HitCandidate`, and `HitRegistry`.
- Event payloads for health, stamina, state, and hits.

---

## 16. Implementation Sequence

No implementation begins until this design is approved.

| Step | Deliverable | Depends On |
| --- | --- | --- |
| 1 | Assembly definitions and folder skeleton | none |
| 2 | Core IDs, math types, clock contracts, deterministic RNG | 1 |
| 3 | `FighterData` and profile structs | 2 |
| 4 | `HealthModel` and `StaminaModel` | 3 |
| 5 | `FighterStateMachine` and foundation states | 3 |
| 6 | `FighterDefinition` ScriptableObject and validation | 3 |
| 7 | `FighterRuntime` and phase runner | 4, 5 |
| 8 | `HealthComponent` and `StaminaComponent` adapters | 4 |
| 9 | `PlayerIntentSource` and `IFighterIntentSource` | 2 |
| 10 | `SweptHitDetector`, `HitRegistry`, `HitDetectionSystem` | 2, 3 |
| 11 | `FighterController`, `FighterView`, debug HUD | 7, 8, 9, 10 |
| 12 | EditMode and PlayMode tests | all above |
| 13 | Minimal two-fighter test scene (no punches) | 11, 12 |

### Definition of Done for the Foundation

- Two fighters can spawn from `FighterDefinition` assets into a test scene.
- The combat clock advances both fighters at a fixed rate.
- Movement/block/dodge intents reach the state machine; invalid transitions are rejected.
- Health and stamina change only through their public models and emit correct events.
- A test hit query can be detected on the configured layers and deduplicated.
- No strike or punch types exist in the codebase.
- All EditMode tests pass; PlayMode smoke test passes.
- No domain or application assembly references `UnityEngine`.

---

## 17. Risks and Mitigations

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Domain accidentally references Unity | High | `noEngineReferences: true`; code review; dependency test |
| Health and stamina become duplicated in MonoBehaviours | High | Thin adapters only; model is sole state owner |
| Event re-entrancy causes double damage | High | Phase-buffered events; idempotent KO; hit registry |
| State machine becomes a giant switch | High | `IFighterState` registration; no central switch |
| Hit detection allocates or runs per frame | High | NonAlloc casts, pooled buffers, fixed tick only |
| Physics non-determinism blocks future netcode | Medium | Isolate behind `IHitDetector`; domain remains pure |
| Over-engineering before strikes exist | Medium | Keep foundation states minimal; `CommittedAction` empty |
| Data assets drift from code schema | Medium | `OnValidate` and fail-fast runtime validation |

---

## 18. Decision Log

| ID | Decision | Rationale |
| --- | --- | --- |
| D1 | Add an explicit `OctagonFighter.Data` assembly | Keeps ScriptableObjects out of the pure domain |
| D2 | Use pure domain models plus thin MonoBehaviour adapters | Testability, determinism, single state ownership |
| D3 | Fixed-tick simulation driven by `CombatTickDriver` | Frame-rate independence and future netcode readiness |
| D4 | Hit detection returns candidates only | Keeps damage authoritative in a future `CombatResolver` |
| D5 | Buffer cross-system events per simulation phase | Prevents re-entrant damage and ordering bugs |
| D6 | Define no strike or punch types yet | Deliberate scope control; foundation first |
| D7 | Use custom `Float2`/`Float3` in Core | Domain math without a Unity dependency |
| D8 | State machine uses registration, not a switch | Extensibility and testability |

---

## 19. Open Questions

These do not block foundation implementation but must be resolved before dependent systems:

1. **Tick rate:** 60 Hz recommended; confirm against animation and input latency targets.
2. **Health model:** Confirm regional trauma (head/body) versus a single health pool plus
   regional modifiers.
3. **State set:** Confirm whether block/dodge belong in the foundation or the next
   milestone.
4. **Physics layers:** Finalize layer names and collision matrix before creating hurtboxes.
5. **Serialization format:** Confirm JSON versus binary for career saves before persistence
   work begins.

---

## 20. Summary

The Core Combat Foundation is a deterministic, intent-driven simulation with one owner per
state, Unity confined to adapters, and a hit-detection pipeline that reports rather than
resolves. It deliberately contains no punches. Striking, grappling, and AI are added
through new data, actions, states, and intent sources while the contracts in this document
remain stable.

---

*End of Document*
