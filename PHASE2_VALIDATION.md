# Phase 2 - Character Locomotion Validation Checklist

Use Unity `6000.6.7f1` with the `CombatTest` scene. Rebuild assets with
`OctagonFighter > Assets > Rebuild Fighter Assets` before validating.

## Prerequisites

- [ ] Mixamo `walk` FBX is under `Assets/_Project/Art/Animations` and imports as Humanoid.
- [ ] Mixamo `run` FBX is under `Assets/_Project/Art/Animations` and imports as Humanoid.
- [ ] `OctagonFighter > Assets > Validate Locomotion Assets` logs `Locomotion validation passed.`
- [ ] EditMode tests pass (`Window > General > Test Runner > EditMode > Run All`).

## Asset Validation

- [ ] `AC_Fighter_Base.controller` has a default `Locomotion` state.
- [ ] The `Locomotion` state uses a 1D blend tree driven by the float parameter `Speed`.
- [ ] The blend tree contains three non-null motions at thresholds `0`, `0.5`, and `1`.
- [ ] `PF_Fighter_Boxer.prefab` has `CharacterController`, `FighterMotor`, `FighterView`,
      `PlayerIntentSource`, and `FighterController`.
- [ ] `FD_Boxer.asset` has a non-zero walk speed multiplier and rotation speed.
- [ ] `CombatTest.unity` contains one player fighter and one passive opponent fighter.

## Play Mode Validation

- [ ] Enter Play Mode. Both fighters settle into the idle animation without errors.
- [ ] Press `W`, `A`, `S`, or `D`; the player enters `Walk`, moves, and rotates toward
      the movement direction.
- [ ] Hold `Left Ctrl` (or gamepad left trigger / left stick click) while moving; the
      player enters `Run`, moves faster, and the animation blends toward run.
- [ ] Release the run modifier while still moving; the player returns to `Walk` and the
      animation blends down.
- [ ] Release movement; the player returns to `Idle` and the animation blends to idle.
- [ ] Press `Space` while idle or moving; the player enters `Blocking` and returns to
      `Idle`/`Walk`/`Run` after release.
- [ ] Press `Left Shift`; the player enters `Dodging` for the configured duration and
      returns to `Idle`.
- [ ] During every transition the character does not slide, teleport, or jitter.
- [ ] The opponent remains idle and does not respond to player input.
- [ ] Console shows no errors, warnings, or missing-animation messages.

## Regression Checks

- [ ] Health and stamina values remain initialized from `FD_Boxer.asset`.
- [ ] Both fighters advance on the shared `CombatTickDriver` clock.
- [ ] Existing state transitions into `HitReaction`, `KnockedDown`, `Recovering`, and
      `Defeated` still compile and remain reachable by forced transitions.
- [ ] No striking, grappling, or AI behavior was added.
