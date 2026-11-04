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

Expected results (2026-11-04, foundation remediation complete):

- EditMode: **76/76 passed** (Domain models, state machine, blocking/stamina/health logic).
- PlayMode: **48/48 passed** (scene-level scenarios via `CombatSceneFixture`).

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
rebinding the four bars to the two scene fighters. Do not hand-edit HUD objects in the scene —
change the builder instead and re-run it.

## Manual scenarios in the Game view

Open `Assets/_Project/Scenes/CombatTest.unity`, press **Play**.

| Scenario | Steps | Expected |
| --- | --- | --- |
| HUD spawn state | Observe both fighters at round start | Both Health bars 100 / 100 (green/orange), both Stamina bars 100 / 100, labels readable, mirrored corners |
| Jab damage | Press `J` once | Opponent Health 92 / 100 immediately; amber damage trail lags ~0.2 s then eases to 92; player Stamina 94 / 100 |
| Cross damage | Press `K` once | Opponent Health 85 / 100; player Stamina 88 / 100; right-straight animation extends the right hand toward the opponent |
| Defeat | Pound the opponent until Health reaches 0 | Defeated state; both of the opponent's bars empty; bar turns defeated color; fighter stays down |
| Block | Hold `Space` (or right mouse) | Opponent's strikes deal 0 damage, no reaction; blocker keeps guard pose; no counterattack buffered |
| Block + move | Hold `Space` + `W/S` | Blocking fighter shuffles at reduced speed |
| Block + attack | Hold `Space`, tap `J` | No strike, no stamina spent, press does not fire after release |
| Stamina refund | Spend stamina, wait 2 s | Stamina refills at 8/s (visible bar refill) |
| Guard (Phase B) | See below |

### Blocking scenarios are test-driven, not AI-driven

The shipped opponent is passive by design and will never guard on its own. The deterministic
scenarios that verify blocking live in:

- `Assets/_Project/Tests/EditMode/BlockingTests.cs` — state-machine level (block, release, buffer, defeat overrides).
- `Assets/_Project/Tests/PlayMode/BlockScenarioTests.cs` — scene level (jab/cross vs held guard,
  block + movement, release, lethal vs guard).

They replace the opponent's intent source with a scripted one (`HeldBlockIntentSource` et al.).
To watch the same flow live: open `CombatTest.unity`, add a `PassiveIntentSource` with the
`Hold Block` toggle enabled on the opponent in the inspector, press Play, and strike with `J`/`K`.

## Known manual-verification limits

- Guard break / guard recovery (Phase B) is only visible through its HUD meter and the guard
  break banner; there is no block-impact animation asset yet (see IMPLEMENTATION_NOTES or the
  final report for the current status).
- The opponent never attacks on its own; all offensive testing is done by the player.
