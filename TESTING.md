# Testing OctagonFighter

How to run the automated suites and to reproduce the combat scenarios by hand. The automated
tests are the source of truth for behavior; the manual steps are for watching the same scenarios
live in the Game view.

## Prerequisites

- Unity `6000.6.7f1` at `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`.
- A free project lock: close the interactive Unity Editor (or wait for it to close) before
  running batch-mode commands, or the runner will abort with a project-in-use error.

## Automated suites (batch mode)

From the repository root:

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode `
  -projectPath "C:\unity\OctagonFighter" -runTests -testPlatform EditMode `
  -testResults "$env:TEMP\editmode-tests.xml" -logFile "$env:TEMP\editmode-tests.log"
```

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode `
  -projectPath "C:\unity\OctagonFighter" -runTests -testPlatform PlayMode `
  -testResults "$env:TEMP\playmode-tests.xml" -logFile "$env:TEMP\playmode-tests.log"
```

Expected results (2026-11-05, Phase C complete):

- EditMode: **122/122 passed** (Domain models, state machine, health/stamina/guard resources,
  guard runtime integration, hit-region derivation from collision data, hit-feedback events,
  defeated terminal/facing behavior, and the training-opponent decision policy).
- PlayMode: **66/66 passed** (scene-level scenarios via `CombatSceneFixture`, including guarded
  strikes, pushback, guard break, the Guard HUD, hit-feedback events, and the training opponent).

The PlayMode fixture reloads `Assets/_Project/Scenes/CombatTest.unity` for every test, places
the two shipped fighters `CloseDistance` (0.76 m) apart, drives the player through a **virtual
keyboard**, and renders frames to a RenderTexture (capture clock is fixed at 60 Hz, so runs are
deterministic). Output logs and captured frames are written to `%TEMP%\p41_shots`.

To run a single test class:

```powershell
-runTests -testPlatform PlayMode -testFilter "BlockScenarioTests"
```

## Windows build

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe" -batchmode -quit `
  -projectPath "C:\unity\OctagonFighter" `
  -buildWindows64Player "C:\unity\OctagonFighter\Builds\Windows\OctagonFighter.exe" `
  -logFile "$env:TEMP\windows-build.log"
```

A successful log ends with `Build Finished, Result: Success.` and reports zero `error CS`.

## Rebuilding the HUD

`OctagonFighter/Assets/Build Combat HUD` (editor menu) regenerates `CombatHud` in
`CombatTest.unity` from `CombatHudBuilder.cs`: it deletes the existing HUD root and recreates it,
rebinding all six bars plus per-side HEAD/BODY/LEG hit-region feedback to the two scene fighters.
Do not hand-edit HUD objects in the scene — change the builder instead and re-run it.

`OctagonFighter/Assets/Setup Phase C Defeated Presentation` (editor menu) configures the
`KnockOut_Loser_mixamo.fbx` import as Humanoid/CreateFromThisModel and adds the `Defeated`
Animator state with an `AnyState -> Defeated (IsDefeated)` transition to
`AC_Fighter_Base.controller`. Run this once after the KO import is finalized.

## Phase C: hit feedback and training opponent

### New behavior

- **Hit regions from collision data.** `HitRegion.Limb` was renamed to `HitRegion.Leg`. The
  `SweptHitDetector` now resolves the body region from the collided volume: discrete
  `HitRegionVolume` BoxColliders (Head ~1.32-1.78 m, Body ~0.83-1.27 m, Leg ~0.01-0.79 m) take
  precedence over the full-body Hitbox capsule, with a contact-height fallback when only the
  capsule is hit. The region propagates through `HitCandidate` → `DamageEvent` and is preserved
  for blocked and unblocked hits alike. No region-specific health pools exist.
- **Hit feedback event.** `FighterController.HitFeedback` fires exactly once per resolved
  strike, carrying the attacker, target, collision-derived region, a `Blocked` flag, the damage
  amount, and the tick. Blocked strikes raise it from `ApplyBlock`; landed strikes raise it from
  `ApplyDamage`. No duplicate events per strike.
- **Hit-region HUD.** `HitRegionFeedback` shows HEAD/BODY/LEG (prefixed `BLOCKED ` when blocked)
  with a configurable expiry, wired per side through `CombatHudBuilder`.
- **Defeated presentation.** `DefeatedState` now locks facing (`FacingAuthority.Locked`), and
  `FighterView` cross-fades into the `Defeated` Animator state (playing the fall/KO clip) when
  the fighter enters defeat. Defeat remains terminal: it overrides `BlockImpact`/`GuardBreak`
  and suppresses all further movement, attack, and block input.
- **Training opponent.** `TrainingIntentPolicy` (pure, deterministic, Domain layer) decides one of
  `Passive`, `Approach`, `Retreat`, `Hold Block`, `Jab`, `Cross`, or `Jab/Cross` each tick.
  `TrainingIntentSource` (Presentation `MonoBehaviour` adapter) converts the decision into a
  `FighterIntent` using the fighter's real state, cooldowns (`AttackTracker.CanStart`), and
  position relative to the opponent. Training attacks only when distance is within `StrikeRange`,
  state is attack-eligible (`Idle`/`Walk`/`Run`), the cooldown has elapsed, and the fighter is
  not blocking, stunned, or defeated. Behavior is Inspector-configured and separate from future
  autonomous AI.

### New test files

- `Assets/_Project/Tests/EditMode/RegionDerivationTests.cs` — head/body/leg from volumes,
  height fallback from the capsule, volume precedence.
- `Assets/_Project/Tests/EditMode/TrainingIntentPolicyTests.cs` — all seven modes, range and
  cooldown gating, block/stun/defeat suppression, Jab/Cross alternation, boundary inclusivity.
- `Assets/_Project/Tests/PlayMode/HitFeedbackTests.cs` — blocked/landed/repeat hit-feedback
  events (region, blocked flag, no duplicates).
- `Assets/_Project/Tests/PlayMode/TrainingOpponentTests.cs` — passive, hold-block, approach,
  retreat, jab, cross, jab/cross alternation, and block/stun/defeat restrictions.

Extended: `HitReactionTests.cs` (defeated facing lock, defeat ignores block/attack intents).

## Manual scenarios in the Game view

Open `Assets/_Project/Scenes/CombatTest.unity`, press **Play**.

| Scenario | Steps | Expected |
| --- | --- | --- |
| HUD spawn state | Observe both fighters at round start | Health, Stamina, Guard bars all 100 / 100, mirrored corners, readable labels |
| Jab damage | Press `J` once at close range | Opponent Health 92 / 100; HEAD hit-feedback text appears briefly; player Stamina 94 / 100 |
| Cross damage | Press `K` once at close range | Opponent Health 85 / 100; BODY hit-feedback text; player Stamina 88 / 100 |
| Defeat | Pound the opponent until Health reaches 0 | Defeated state; fall/KO animation plays; facing locked; all three bars empty; defeated color |
| Hit feedback (blocked) | Give opponent held block, press `J` | HEAD hit-feedback text prefixed `BLOCKED`, chip damage only, guard reduced |
| Training: passive | Opponent intent source = `TrainingIntentSource`, Mode = Passive | Opponent stands still, never attacks |
| Training: approach | Mode = Approach, start 2.5 m apart | Opponent walks toward player until ~0.9 m, then stops |
| Training: retreat | Mode = Retreat, start close | Opponent walks away until ~1.5 m, then stops |
| Training: hold block | Mode = Hold Block | Opponent keeps guard raised |
| Training: jab | Mode = Jab, in range, off cooldown | Opponent throws a jab (LightPunch state) |
| Training: cross | Mode = Cross, in range, off cooldown | Opponent throws a cross (HeavyPunch state) |
| Training: jab/cross | Mode = Jab/Cross, in range, off cooldown | Opponent alternates jab then cross |
| Training: no strike while blocked | Mode = Jab, opponent manually blocks | Opponent never enters LightPunch while blocking |
| Training: no strike while defeated | Lethal damage then Mode = Jab | Opponent stays defeated, never attacks |

### Blocking scenarios are test-driven, not AI-driven

The shipped opponent is passive by design and will never guard on its own. The deterministic
scenarios that verify blocking live in:

- `Assets/_Project/Tests/EditMode/BlockingTests.cs` — state-machine level (block, release, buffer, defeat overrides).
- `Assets/_Project/Tests/EditMode/GuardModelTests.cs` — guard resource (dynamic damage, delayed recovery, break).
- `Assets/_Project/Tests/EditMode/GuardRuntimeTests.cs` — runtime integration (chip damage, block impact, guard break, defeat precedence).
- `Assets/_Project/Tests/PlayMode/BlockScenarioTests.cs` — scene level (jab/cross vs held guard, block + movement, pushback, release, guard break, lethal vs guard).
- `Assets/_Project/Tests/PlayMode/GuardHudTests.cs` — scene-level Guard HUD (start state, blocked hit, break and recovery).

They replace the opponent's intent source with a scripted one (`HeldBlockIntentSource` et al.).
To watch the same flow live: open `CombatTest.unity`, add a `PassiveIntentSource` with the
`Hold Block` toggle enabled on the opponent in the inspector, press Play, and strike with `J`/`K`.

## Known manual-verification limits

- No dedicated block-impact or guard-break animation clips ship yet. Block impact holds the guard
  pose for its short duration; guard break cross-fades to locomotion. The Guard HUD bar is the
  clearest way to observe guard damage, break, and the post-stun partial recovery.
- The defeated fall/KO clip (`KnockOut_Loser_mixamo.fbx`) is imported as Humanoid and the
  `Defeated` state is added via the editor menu above; the full 20.5 s source clip includes a
  get-up at the end, so the presented fall segment should be trimmed to ~10.5-16 s through the
  import setup before shipping. Until then the `Defeated` state plays the full clip.
- The opponent never attacks on its own; all offensive testing is done by the player unless the
  training opponent intent source is active.
