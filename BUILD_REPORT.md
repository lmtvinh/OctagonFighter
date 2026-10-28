# Build and Validation Report

Date: 2026-10-26
Unity: `6000.6.7f1` (`C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe`)
Project: `C:\unity\OctagonFighter`
Result: **SUCCESS**

## Build

| Item | Value |
| --- | --- |
| Command | `Unity.exe -batchmode -quit -projectPath C:\unity\OctagonFighter -buildWindows64Player C:\unity\OctagonFighter\Builds\Windows\OctagonFighter.exe -logFile %TEMP%\p31_build.log` |
| Target | StandaloneWindows64 (`BuildTarget 19`) |
| Build settings source | `ProjectSettings/EditorBuildSettings.asset` (no project build script exists) |
| Scenes | `Assets/_Project/Scenes/SampleScene.unity`, `Assets/_Project/Scenes/CombatTest.unity` |
| Process exit code | `0` |
| Build marker | `Build Finished, Result: Success.` |
| Build duration | ~295 s |
| `error CS` | 0 |
| `warning CS` | 4 (pre-existing `FindObjectsByType` / `FindObjectsSortMode` deprecation in `FighterMotor.cs:191`) |
| `Compilation failed` / `compiler errors` | 0 / 0 |
| Unhandled / null-reference exceptions | 0 |

## Output

- Executable: `C:\unity\OctagonFighter\Builds\Windows\OctagonFighter.exe` (667,136 bytes)
- Runtime files: `UnityPlayer.dll`, `OctagonFighter_Data\`, `MonoBleedingEdge\`, `D3D12\`, `UnityCrashHandler64.exe`
- Output folder: `C:\unity\OctagonFighter\Builds\Windows`

## Tests

Command:
`Unity.exe -batchmode -projectPath C:\unity\OctagonFighter -runTests -testPlatform EditMode -testResults %TEMP%\p31_tests_postbuild.xml -logFile %TEMP%\p31_tests_postbuild.log`

| Metric | Value |
| --- | --- |
| Total | 20 |
| Passed | 20 |
| Failed | 0 |
| Skipped | 0 |
| Inconclusive | 0 |
| Process exit code | 0 |

Covers `MovementProfileTests` (walk `0.45 m/s`, run `0.818 m/s`, diagonal), `LocomotionStateMachineTests`, and `AttackStateMachineTests`.

## Locomotion asset validation

Command:
`Unity.exe -batchmode -quit -projectPath C:\unity\OctagonFighter -executeMethod OctagonFighter.Editor.FighterAssetBuilder.ValidateLocomotionAssets -logFile %TEMP%\p31_validate_postbuild.log`

- Result: `Locomotion validation passed.`
- Failure throws: 0
- `error CS`: 0
- Process exit code: 0

Note: the first attempt used the wrong namespace (`OctagonFighter.EditorTools`) and exited 1 with
`executeMethod class 'FighterAssetBuilder' could not be found`; re-running with the correct
namespace `OctagonFighter.Editor` passed. The build itself was unaffected.

## Saved artifacts

- `Builds/Logs/build.log`
- `Builds/Logs/editmode-tests.log`
- `Builds/Logs/editmode-tests.xml`
- `Builds/Logs/locomotion-validation.log`

Phase 3.1 evidence remains outside the repo at `%TEMP%\p31_vis_0818`, `%TEMP%\p31_vis_1636`, and
`%TEMP%\p31_probes`. Temporary probe scripts were removed from `Assets` before the build.

## Verdict

Build, EditMode tests, and locomotion asset validation all succeeded, with no compilation errors or
critical runtime exceptions in the logs. The build is verified and the machine may shut down.
