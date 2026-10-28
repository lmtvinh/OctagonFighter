# Phase 3 - Striking MVP Validation Checklist

Use Unity `6000.6.7f1` with the `CombatTest` scene. Add the punch animator states
with `OctagonFighter > Assets > Ensure Attack Animator States` so the existing
animator controller is edited in place. Running
`OctagonFighter > Assets > Rebuild Fighter Assets` also works, but it recreates
the controller and prefab.

## Prerequisites

- [ ] Mixamo `Lead Jab.fbx` is under `Assets/_Project/Art/Animations` and imports as Humanoid.
- [ ] Mixamo `Jab Cross.fbx` is under `Assets/_Project/Art/Animations` and imports as Humanoid.
- [ ] `OctagonFighter > Assets > Ensure Attack Animator States` logs
      `Attack animator states ensured` and does not report an error.
- [ ] `OctagonFighter > Assets > Validate Locomotion Assets` logs
      `Locomotion validation passed.` (it now also verifies the jab and cross states).
- [ ] EditMode tests pass (`Window > General > Test Runner > EditMode > Run All`).

## Asset Validation

- [ ] `AC_Fighter_Base.controller` has trigger parameters `Jab` and `Cross`.
- [ ] The controller has `Jab` and `Cross` states with non-null punch motions.
- [ ] `Locomotion` has a trigger transition to each attack state and each attack state
      returns to `Locomotion` at the end of the clip.
- [ ] `PF_Fighter_Boxer.prefab` has `CharacterController`, `FighterMotor`, `FighterView`,
      `PlayerIntentSource`, and `FighterController`. `FighterController` adds a
      `FighterIdentity` at runtime when the prefab does not already contain one.
- [ ] `FD_Boxer.asset` contains light (Jab) and heavy (Cross) attack entries with
      non-zero startup, active, recovery, cooldown, damage, range, radius, and stamina cost.
- [ ] `CombatTest.unity` contains `CombatSession` wired to the tick driver and both fighters.

## Play Mode Validation

- [ ] Enter Play Mode. Both fighters settle into the idle animation without errors or
      missing-parameter messages.
- [ ] Press `J` (or gamepad west / left mouse) while idle; the player enters `LightPunch`,
      plays the jab, and returns to `Idle`.
- [ ] Press `K` (or gamepad north) while idle; the player enters `HeavyPunch`, plays the
      cross, and returns to `Idle`.
- [ ] Both punches can be started while walking or running; the fighter stops translating
      during the committed strike.
- [ ] Each light punch consumes `6` stamina and each heavy punch consumes `12` stamina.
- [ ] Stamina does not drop below zero and no attack starts without enough stamina.
- [ ] Spamming the light punch does not restart the same attack before its cooldown expires.
- [ ] The heavy punch has a visibly longer cooldown than the light punch.
- [ ] Pressing a punch during a strike buffers it; the buffered punch starts after the
      current strike ends (within the `0.25 s` buffer window).
- [ ] A buffered punch pressed too early expires instead of firing later.
- [ ] Walk close and face the opponent, then press `J`; the console logs a landed hit and
      the opponent's health drops by `8`.
- [ ] Walk close and face the opponent, then press `K`; the console logs a landed hit and
      the opponent's health drops by `15`.
- [ ] Punching from too far away does not log a hit and does not reduce opponent health.
- [ ] One strike can damage the opponent at most once, even if the opponent has multiple
      colliders.
- [ ] The attacker never damages itself.
- [ ] Defeating the opponent with repeated strikes moves it to `Defeated` and stops further
      damage.

## Regression Checks

- [ ] Walk, run, block, and dodge still behave as validated in Phase 2.
- [ ] Movement resumes normally after a strike finishes.
- [ ] Both fighters still advance on the shared `CombatTickDriver` clock.
- [ ] Health, stamina, movement, and attack values are loaded from `FD_Boxer.asset`.
- [ ] No combos, grappling, AI, or damage reactions were added.
- [ ] Console shows no errors or warnings during the checks above (hit logs are expected).

## Known MVP Limits

- There is no HUD; validate health changes through the landed-hit console log or by
  inspecting the opponent's `HealthComponent` in Play Mode.
- Punches only target `Head`, use a single active hit window, and do not apply hit reactions.
- Input priority is light punch, heavy punch, dodge, then block when several are pressed
  on the same tick.
- The passive opponent does not attack or react.
