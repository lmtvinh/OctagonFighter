# Phase 3 - Lateral Locomotion Validation

## Root Cause

The strafe animation was driven by input intent rather than the velocity the motor actually
applied, and the walk lateral speed was `0.46 m/s` while the strafe clip was played at `3.1x`.
The clip needs about `0.9 m/s` at that playback, so the body covered only about half the
distance the footfall timing expected. The view also jumped straight to full blend/playback on
input, even while the motor was still accelerating or had limited authority. The result was
scuffing, a running-in-place look at walk speed, and mismatched footwork during starts, stops,
and direction changes.

The clip itself is `1.033 s` at `30 fps`, looping and in-place, with about `0.281 m` of root-X
travel per cycle and about `0.30 m` of measured foot stride. That gives two coherent
body-to-playback ratios:

- `0.46 m/s` at about `1.55x`
- `0.9 m/s` at `3.1x`

An `8x` playback for `0.9 m/s` was rejected: it implies roughly `15` footfalls per second and a
`0.13 s` cycle, which the asset cannot support.

## Selected Configuration

| Parameter | Before | After |
| --- | ---: | ---: |
| Lateral run speed | `0.836 m/s` | `1.636 m/s` |
| Lateral walk speed (`0.55x`) | `0.460 m/s` | `0.900 m/s` |
| Walk strafe playback | `1.0` animator x `3.1` child | `1.0` animator x `3.1` child |
| Run strafe playback | `1.818` animator x `3.1` child | `1.818` animator x `3.1` child |
| Forward walk / run | unchanged | unchanged (`2.6 m/s` run) |
| Backward walk / run | unchanged | unchanged (`2.1 m/s` run) |

`LateralSpeed` is now `1.636`, so `0.55 x 1.636 = 0.9 m/s` at walk and `1.636 m/s` at run.

## Implementation

- `MovementProfile.GetPlanarSpeed(lateralDirection, forwardDirection, speedScale)` returns the
  direction-aware reference speed, including the separate backward speed.
- `FighterView` now receives the motor's applied `PlanarVelocity` and the simulation delta.
  It converts that velocity into the fighter's combat-relative frame and drives `MoveX`/`MoveY`.
- The blend magnitude is `clamp(appliedSpeed / walkReferenceSpeed, 0, 1)`. Partial input therefore
  blends toward idle instead of slowing the clip into slow motion.
- Playback is `clamp(appliedSpeed / walkReferenceSpeed, 1, 1 / WalkSpeedMultiplier)`. This gives
  `1.0` at walk and `1.818` at run while keeping the `3.1x` strafe child calibrated.
- Attack and other non-locomotion states reset the animator to `1.0` so punch clips play at their
  authored speed. The idle state still allows the deceleration tail to drive the blend.
- `FighterController` passes the movement profile at initialization and passes
  `_motor.PlanarVelocity` plus `deltaSeconds` on every tick.
- `FighterAssetBuilder` now generates a 2D `FreeformDirectional2D` tree driven by `MoveX`/`MoveY`
  with idle, forward, backward, left, and right children. Left uses the mirrored strafe clip.
  Validation now checks the 2D parameter, tree type, and cardinal child coverage.
- Added `MovementProfileTests` for lateral, forward, backward, and diagonal reference speeds.

## Validation Results

Commands run with Unity `6000.6.7f1` in batch mode:

- `OctagonFighter.Editor.FighterAssetBuilder.EnsureAttackAnimatorStates` - passed.
- `OctagonFighter.Editor.FighterAssetBuilder.ValidateLocomotionAssets` - passed.
- `OctagonFighter.Editor.FighterAssetBuilder.BuildAll` - passed, including the 2D tree rebuild
  and final validation.
- EditMode tests - `20/20` passed.
- Temporary Play Mode locomotion probe - passed all scenarios below.
- Temporary integrated Play Mode probe (`FighterController` + state machine + scripted input) -
  passed all scenarios below.

Probe results (60 fps unless noted; body speed is measured from actual displacement):

| Scenario | Body speed | MoveX | MoveY | Animator speed | Facing / notes |
| --- | ---: | ---: | ---: | ---: | --- |
| Walk right | `0.900` | `1.000` | `0.000` | `1.000` | `0.0 deg` yaw drift |
| Walk left | `0.900` | `-1.000` | `0.000` | `1.000` | `0.0 deg` yaw drift |
| Run right | `1.636` | `1.000` | `0.000` | `1.818` | `0.0 deg` yaw drift |
| Partial walk (`0.5`) | `0.450` | `0.500` | `0.000` | `1.000` | Blend magnitude follows speed |
| Backward walk | `1.155` | `0.000` | `-1.000` | `1.000` | Uses backward speed |
| Start from rest | avg `0.842`, max `0.900` | settles to `1.000` | `0.000` | `1.000` | Accel ramp active |
| Stop from walk | `<0.05` by `0.25 s` | decays to `0.000` | `0.000` | `1.000` | Decel tail reaches idle |
| Rapid left/right alternation | `0.401` average | `0.016` average | `0.000` | `1.000` | Yaw stable at `0.0 deg` |
| Circling opponent | `0.900` | `1.000` | `-0.002` | `1.000` | Yaw tracked `-12.7 deg`, blend stayed lateral |
| Attack from walk | decelerates | decays | `0.000` | `1.000` | Punch playback forced to authored speed |
| Walk right at 30 Hz | `0.900` | `1.000` | `0.000` | `1.000` | Matches 60 Hz |

Integrated path probe (`FighterController` + state machine + scripted `IFighterIntentSource` +
motor + view, 60 fps unless noted):

| Scenario | Body speed | MoveX / MoveY | Animator speed | State / notes |
| --- | ---: | ---: | ---: | --- |
| Walk right | `0.900` | `1.000 / 0.000` | `1.000` | `Walk` |
| Walk left | `0.900` | `-1.000 / 0.000` | `1.000` | `Walk` |
| Run right | `1.636` | `1.000 / 0.000` | `1.818` | `Run` |
| Partial walk (`0.5`) | `0.450` | `0.500 / 0.000` | `1.000` | `Walk` |
| Stop from walk | tail `0.005` | `0.037 / 0.000` | `1.000` | `Idle` in `0.267 s` |
| Attack from walk | resumes `0.900` | `1.000 / 0.000` | `1.000` | `LightPunch` for `0.400 s`, `Jab` clip active, playback min `1.000`, returns to `Walk` |
| Attack from idle | — | `0.000 / 0.000` | `1.000` | `LightPunch` for `0.400 s`, `Jab` clip active, returns to `Idle` |
| Circling opponent | `0.900` | `1.000 / -0.002` | `1.000` | Yaw tracked `-12.4 deg`, lateral blend preserved |
| Walk right at 30 Hz | `0.900` | `1.000 / 0.000` | `1.000` | Matches 60 Hz |

## Known Limitations

- The strafe asset still has inconsistent foot planting. The planted-foot probe reports about
  `52%` residual slip at walk and `71%` at run even at the calibrated `0.9 / 3.1x` ratio. The
  metric is sensitive to the contact band and clip phase; the 30 Hz run reports `0%` because the
  sampling aliases the cycle, so that number is not evidence of zero slip.
- No replacement strafe clip or foot IK was added. Visual Play Mode review is still required to
  judge whether the remaining asset-level slip is acceptable.
- The integrated probe drives `FighterController`, the state machine, the motor, and the view
  through a scripted `IFighterIntentSource`. The real `PlayerIntentSource` keyboard/gamepad
  bindings and the visual result still need a manual Play Mode pass.
- The deceleration ramp is an exponential time constant. With `0.1 s` configured, the integrated
  probe reaches below `0.05 m/s` in about `0.267 s`. If a hard fixed-time stop is desired, the ramp
  needs to be changed to linear deceleration.
- The temporary probes were removed from the project after the runs. Their logs are kept in
  `%TEMP%\Phase3LocomotionProbe.log` and `%TEMP%\Phase3IntegratedProbe.log`.

## Next Steps

1. Enter Play Mode in `CombatTest` and inspect walk, run, stop, and rapid direction changes at
   normal and low frame rates, including the real keyboard/gamepad input path.
2. Check attack starts and returns to locomotion visually, especially while strafing.
3. If the remaining foot slip is visible, replace the strafe clip with one that has clean planted
   contacts at a `0.30 m` stride, or add foot IK.
4. If the `0.267 s` effective stop feels loose, change the deceleration ramp to a fixed-time linear
   stop and re-run the integrated probe.

---

# Phase 3.1 Addendum - Real Play Mode Validation and Foot Contact

Date: 2026-10-26. Unity `6000.6.7f1`. This addendum supersedes the Phase 3 foot-slip and stride
claims; the Phase 3 sections above are kept as historical context.

## What actually ran

- The real `Assets/_Project/Scenes/CombatTest.unity` scene was opened in Unity Play Mode (GUI
  editor, Direct3D11, 60 fps capture). The player was `Fighter_Player`, the opponent
  `Fighter_Opponent`.
- Input was injected as Input System keyboard state into the real `PlayerIntentSource` (`D`, `A`,
  `D + LeftCtrl`, `J`). This exercises `FighterController`, the state machine, `FighterMotor`,
  `FighterView`, the animator controller, and the real input bindings.
- Scenarios covered: idle, walk-right start and sustained, reversal to walk-left, sustained
  walk-left, run-right start and sustained, release/stop, strafe-to-jab, jab startup/active,
  recovery back to walk, and final idle.
- Rendered evidence: 13 full-scene stills plus 8-frame walk and run bursts at 60 fps from a
  fixed foot-level camera over a 0.2 m world grid (`%TEMP%\p31_vis_0818`, `%TEMP%\p31_vis_1636`).
- Not run in this pass: real gamepad analog input (keyboard only), reduced frame rate (the Phase 3
  30 Hz probe was run before the speed change), audio, and interactive manual play. Motion
  judgment is based on rendered stills/bursts, not real-time video.

## Real Play Mode results at the corrected configuration

| Scenario | Observed |
| --- | --- |
| Walk right | `0.450 m/s` applied velocity, `MoveX = 1.000`, animator `1.000`, state `Walk` |
| Walk left | `0.450 m/s`, `MoveX = -1.000`, animator `1.000`, state `Walk` |
| Run right | `0.818 m/s`, `MoveX = 1.000`, animator `1.818`, state `Run` |
| Rapid reversal | direction changed without a state or clip reset; velocity crossed zero and rebuilt to `0.450` |
| Stop from run | state `Idle` on release; applied speed below `0.05 m/s` 0.20 s after the first `Idle` sample, 0.27-0.30 s after the key-release step including tick/input latency |
| Strafe to jab | `LightPunch` entered, punch playback reset to `1.000`, residual speed bled to `0.023 m/s`, then `Walk` at `0.450 m/s` |
| Facing | yaw tracked the opponent through the lateral travel (about `89` to `79 deg` over walk-right, `69 deg` at run, `52-59 deg` during the punch/recovery) while `MoveX` stayed lateral |

The rendered frames show the character upright and grounded, with feet on the floor and no obvious
pose breakage, knee inversion, or floating in the captured frames. At `1.636` lateral speed the
character visibly translates across the fixed grid while both feet travel with it; at `0.818` the
lead foot holds its ground position better, but the rear foot still pivots/drags. This is the
visual counterpart of the measurements below. A real-time manual pass and a true 30 Hz re-run are
still outstanding.

## Foot-contact diagnosis

The asset audit was run deterministically at 240 Hz, independent of Play Mode frame timing.

- `Strafe.fbx` contains `mixamo.com` (`1.033 s`, 30 fps, looping, Humanoid). It is in-place:
  root translation `0.000 m`, hips local translation about `0.002 m`, `hasMotionCurves = false`.
  Import: `animationType = Human`, `loopTime = true`, `keepOriginalPositionY = true`,
  `keepOriginalPositionXZ = false`, resample on, keyframe reduction.
- The lead (right) foot has one clear stance per cycle. At the controller's `3.1x` child time
  scale, the stance lasts about `0.092 s` and the foot travels about `0.042 m` laterally, i.e.
  about `0.448 m/s`. At effective `1x` playback the same stance is `0.148 m/s` (median both feet).
- The rear (left) foot is asymmetric: its low-height phase spans about `53%` of the cycle but its
  lateral travel is only about `0.041 m/s`. It is a pivot/drag, not a planted traveling contact.
- The Phase 3 `0.281 m` root-X travel and `0.30 m` foot-stride figures are not reproducible. The
  clip is in-place; total foot excursion is `0.15-0.17 m` and the actual stance-phase travel is
  `0.042-0.055 m`. The `0.30 m` value appears to have been taken over the whole cycle (or the
  preview clip) and divided by the cycle, instead of measuring the planted phase and dividing by
  stance time. That is why `3.1x` was believed to correspond to `0.9 m/s`.
- At `0.9 m/s` walk the lead foot retreats at `0.448 m/s`, so `0.452 m/s` (about `50%`) remains
  as world-space slide. At `1.636 m/s` run the effective playback is `5.636x`, the foot retreats
  at about `0.814 m/s`, and about `0.822 m/s` (about `50%`) remains. The ratio is fixed because
  playback scales with body speed. This matches the original Phase 3 `52%` walk figure; the
  earlier `71%` run figure was sensitive to the toe/foot contact band.
- Left and right are symmetric: the mirrored child uses the same clip with mirroring, and the
  mirrored audit shows the same stance timing with the lateral sign reversed.
- The blend tree and the view are not the primary cause: `FighterView` is driven by the velocity
  the motor actually applied, and the animator parameters reached `MoveX = +/-1` with playback
  `1.000 / 1.818`. The asset's planted stride and the applied speed do not agree.

## Selected correction

`LateralSpeed` was changed from `1.636` to `0.818 m/s` (walk `0.45 m/s`, run `0.818 m/s`). The
matching value is `0.448 / 0.55 = 0.815` for walk and `0.448 * 1.818 = 0.814` for run, so `0.818`
is within about `0.5%`. This plants the lead foot at both gaits.

Increasing playback instead was rejected: keeping `0.9 m/s` would need about `6.2x` total playback
at walk (cycle `0.166 s`, roughly `12` footfalls/s) and about `11.3x` at run. That is not a
credible animation cadence. Foot IK was not added: the rig has the required bones, but the project
has no validated grounding/IK setup and the asset's rear-foot pivot would still need a constraint
scheme that preserves knee orientation, stance width, and hip motion. The smallest evidence-based
correction is to match the motor speed to the clip's measured stride, not to hide the mismatch.

Trade-off: lateral movement is now slower (`0.818` run versus `1.636` in Phase 3). If the faster
speed is a gameplay requirement, the strafe clip must be replaced with a traveling strafe whose
planted phase advances by at least `0.30 m` at the intended cadence, then the playback and speed
calibration must be re-derived.

## Stopping behavior

`FighterMotor.ApplyVelocityRamp` uses `maximumDelta = (currentSpeed / decelerationSeconds) * dt`
with `decelerationSeconds = 0.1`. This is an exponential time constant, not a `0.1 s` total stop:
the speed reaches `0.05 m/s` from `0.45` after `0.1 * ln(9) = 0.22 s`, and from `0.9` after
`0.1 * ln(18) = 0.29 s`. The Play Mode probe observed `0.20 s` from the first `Idle` sample at walk
speed and `0.27-0.30 s` from the key-release step; the extra time is the 50 Hz motor tick and the
input/state latency, not an extra deceleration constant.

The exponential ramp was kept. It matches the `0.1 s` `MoveX`/`MoveY` damping in `FighterView`, so
the body and the blend decay together. A fixed-time linear stop would stop the body before the
view's blend finishes unless both were retuned; the current stop is stable, reaches combat-idle
quickly, and did not show a visible pop in the captured frames.

## Verification summary

| Check | Result |
| --- | --- |
| EditMode tests (after test updates) | `20/20` passed |
| `FighterAssetBuilder.ValidateLocomotionAssets` | passed |
| Real `PlayerIntentSource` Play Mode probe, 60 fps | passed all scenarios above |
| Clip/foot audit at 240 Hz | completed; numbers above |
| 30 Hz real-input re-run after the speed change | not run |
| Real gamepad analog / partial input | not run (keyboard only) |
| Real-time manual motion review | not run (stills/bursts only) |

The 60 fps world-foot velocities from the probe are not used as a contact metric: the body is
fixed-step (`50 Hz`) while frames are sampled at about `30 Hz`, so per-frame world-foot velocity
contains step aliasing. The `30 Hz` probe's Phase 3 `0%` result remains a sampling artifact, not
evidence of zero slip. Contact numbers above come from the deterministic clip audit.

## Remaining limitations and Phase 3 closure

- The clip is a boxing shuffle, not a traveling strafe. Even at `0.818` the rear foot pivots/drags
  for about half the cycle; only the lead foot is well planted. At `1.636` both feet slide.
- No replacement strafe clip and no foot IK were added. The correct long-term fix is a traveling
  strafe asset (or root-motion strafe) with clean stance phases; IK would only be justified after
  such an asset and a validated rig setup.
- Motion quality still needs a real-time manual pass and a 30 Hz check; captured stills cannot
  prove smoothness, and no video encoder was available.
- **Closure:** the controller/input/state-machine/blend implementation is validated and can close.
  Foot contact is asset-limited. Close Phase 3.1 only if the slower `0.818 m/s` shuffle is
  acceptable for now; otherwise keep the foot-contact item open and replace the clip before
  restoring the `1.636 m/s` target.

## Files changed in Phase 3.1

- `Assets/_Project/ScriptableObjects/Fighters/FD_Boxer.asset` - `_lateralSpeed: 0.818`.
- `Assets/_Project/Scripts/Data/Fighters/MovementProfileAuthoring.cs` - default `0.818f`.
- `Assets/_Project/Tests/EditMode/MovementProfileTests.cs` - expectations `0.45` walk /
  `0.818` run, diagonal recomputed.
- `PHASE3_LOCOMOTION.md` - this addendum.
- Temporary probes (`Phase31FootAudit`, `Phase31PlayProbe`, `Phase31ShotTest`) were removed from
  `Assets`; sources are kept in `%TEMP%\p31_probes`, evidence in `%TEMP%\p31_vis_0818` and
  `%TEMP%\p31_vis_1636`.

