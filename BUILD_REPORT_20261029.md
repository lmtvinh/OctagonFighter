# Build and Validation Report — 2026-10-29

**Verdict: FAILED — PC shutdown was NOT scheduled.**

The Unity player build itself succeeded, but automated validation failed. Per the safety
requirements, the machine was left running for investigation.

## Environment

| Item | Value |
| --- | --- |
| Unity | `6000.6.7f1` |
| Editor | `C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe` |
| Project | `C:\unity\OctagonFighter` |
| Target | Standalone Windows 64-bit (`-buildWindows64Player`) |
| Build settings source | `ProjectSettings/EditorBuildSettings.asset` (no project build script) |
| Scenes | `Assets/_Project/Scenes/SampleScene.unity`, `Assets/_Project/Scenes/CombatTest.unity` |

## Build result — SUCCESS

Command:

```powershell
Unity.exe -batchmode -quit -projectPath C:\unity\OctagonFighter `
  -buildWindows64Player C:\unity\OctagonFighter\Builds\Windows\OctagonFighter.exe `
  -logFile C:\unity\OctagonFighter\Builds\Logs\build-20261029-2108.log
```

| Metric | Value |
| --- | --- |
| Process exit code | `0` |
| Wall-clock duration | 210.5 s |
| Build marker | `Build Finished, Result: Success.` |
| `error CS` / `Compilation failed` | 0 / 0 |
| `warning CS` | 4 (pre-existing `FindObjectsByType` deprecation, `FighterMotor.cs:202`) |
| Unhandled / null-reference exceptions | 0 |
| Remaining Unity processes after exit | 0 |
| Output | `C:\unity\OctagonFighter\Builds\Windows\OctagonFighter.exe` |

The output folder was rewritten: `OctagonFighter_Data\globalgamemanagers` is dated
`2026-10-29 21:11:35`, and the managed assemblies are dated `2026-10-29 21:11:53`.
The `.exe` itself is byte-identical to the 2026-10-26 build (SHA-256
`9806ABDE4A406ED68647E9505852C49F6AA72A372F4F084E2AA2821AFFEA57A2`), which is expected for a
deterministic script-only rebuild.

Build-log "error" matches were non-fatal: licensing access-token notices, blocked
`*.cloud.unity3d.com` analytics calls, and shader files literally named `FallbackError`.
None are compilation or build failures.

## Validation result — FAILED

### EditMode: 40 / 41 passed

Command:
`Unity.exe -batchmode -projectPath C:\unity\OctagonFighter -runTests -testPlatform EditMode -testResults Builds\Logs\editmode-tests-20261029.xml -logFile Builds\Logs\editmode-tests-20261029.log`

| Metric | Value |
| --- | --- |
| Total / Passed / Failed | 41 / 40 / 1 |
| Process exit code | `2` (test failures) |

Failing test:
`OctagonFighter.Tests.EditMode.AttackStateMachineTests.LightPunchDoesNotRequestAHitBeforeStartupCompletes`

```
Damage must not resolve during startup.
Expected: False
But was:  True
at AttackStateMachineTests.cs:75
```

Cause: the jab timing was changed to `startup 0.26 s`, `active 0.16 s`, `recovery 0.16 s`,
but the test still advances `0.01 + 0.30 = 0.31 s` and asserts that no hit may exist. With the
new timing the contact window is already open at `0.31 s` (window `0.26–0.42 s`). The test body
was not updated when the timing constants were changed.

### PlayMode: 13 / 14 passed

Command:
`Unity.exe -batchmode -projectPath C:\unity\OctagonFighter -runTests -testPlatform PlayMode -testResults Builds\Logs\playmode-tests-20261029.xml -logFile Builds\Logs\playmode-tests-20261029.log`

| Metric | Value |
| --- | --- |
| Total / Passed / Failed | 14 / 13 / 1 |
| Process exit code | `2` (test failures) |

Failing test: `OctagonFighter.Tests.PlayMode.StrikeScenarioTests.T02_CrossAtCloseRange`

```
A close cross must damage the opponent exactly once.
Expected: 1
But was:  0
at StrikeScenarioTests.cs:107
```

Measured evidence from `artifacts-20261029/t02_cross_close.csv`:

- Closest approach: `elapsed=0.763 s`, `state=HeavyPunch`, `clearance=0.029 m`
- Right fist: `(0.158, 1.309, -0.243)`; defender at `(0.380, 0, 0)`
- The contact window was open at that moment (cross window `0.60–0.78 s` after attack start).
- `[HITDBG]` shows the query origin `0.089 m` from the defender capsule surface with a hit-sphere
  radius of `0.070 m`, so the sweep misses by about `0.019 m`.

Cause: the cross's reach at the test's `0.76 m` spacing is still ~2 cm short of the hit sphere.
The new overlap handling in `SweptHitDetector` is working, but there is no overlap to find.

### Other tests

All 13 other PlayMode scenarios passed, including capture smoke, input probe, jab at close range,
range sweep, out-of-range misses, retreat/strafe/evade, repeated jabs, return to locomotion, and
locomotion stability.

## Additional issue found

Temporary debug instrumentation is still present in
`Assets/_Project/Scripts/Infrastructure/Combat/SweptHitDetector.cs` (the `[HITDBG]` block and the
`HITDBG` string). It is compiled into the built `OctagonFighter.Infrastructure.dll` and logs every
tick when the radius is between `0.065` and `0.075`. It should be removed before a shipping build.

## What needs to be fixed

1. **EditMode timing test** — update
   `LightPunchDoesNotRequestAHitBeforeStartupCompletes` to the new `0.26 s` startup
   (e.g. assert no hit at `0.25 s`, then assert a hit by `0.28 s`), or restore timing consistency.
   Do not simply loosen the assertion.
2. **Cross reach** — at `0.76 m` the right fist's closest clearance is `0.029 m`, but the hit
   sphere radius is `0.070 m`. Either extend the cross reach/rig calibration, raise the hit radius
   to ~`0.09 m`, or re-measure and adjust the scenario distance. This is a gameplay decision, not
   a test-only change.
3. **Remove `[HITDBG]` debug logging** from `SweptHitDetector.cs`.
4. Rebuild, rerun EditMode and PlayMode, then shut down only if both suites are fully green.

## Saved artifacts

- `Builds/Logs/build-20261029-2108.log`
- `Builds/Logs/editmode-tests-20261029.log`
- `Builds/Logs/editmode-tests-20261029.xml`
- `Builds/Logs/playmode-tests-20261029.log`
- `Builds/Logs/playmode-tests-20261029.xml`
- `Builds/Logs/artifacts-20261029/` — all scenario CSVs, `pose_probe.csv`, and the 24 T02 frames

Note: the persistent `%LOCALAPPDATA%\Unity\Editor\Editor.log` was last written on 2026-10-27
because these batch runs redirect their editor log via `-logFile`. The timestamped build/test logs
above are the authoritative logs for this run.

## Shutdown status

`shutdown.exe` was **never invoked**. No shutdown is pending, no Unity process remains, and the
machine is still running.
