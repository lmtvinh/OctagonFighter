# Phase 4 — Striking Foundation

This document records the strike model that replaced the chest-centred sphere cast, the
measurements it is built on, and the work that is deliberately still deferred.

## Contact timing comes from the clips, not from guesses

The old defaults (`startup 0.120 s / active 0.080 s` for the jab, `0.220 s / 0.100 s` for the
cross) were invented before any clip had been measured. The current values were measured by
instantiating `PF_Fighter_Boxer`, playing each animator state, and sampling the animated fists at
240 Hz in an isolated copy of the project:

| Clip | Length | Hand | 90 % reach | Peak | Authored window |
| --- | --- | --- | --- | --- | --- |
| `Lead Jab.fbx` | 1.500 s @ 30 fps | Left | 0.375 – 0.442 s | 0.404 s | 0.36 – 0.47 s |
| `Jab Cross.fbx` | 2.233 s @ 30 fps | Right | 0.613 – 0.675 s | 0.638 s | 0.60 – 0.70 s |

`startup 0.36 / active 0.11 / recovery 0.23` (jab, 0.70 s total) and
`startup 0.60 / active 0.10 / recovery 0.20` (cross, 0.90 s total) bracket those intervals and stop
before the clips' long idle tails. `AttackDefinition.ContactStartSeconds` / `ContactEndSeconds` are
the explicit contact instants; `ActiveSeconds` stays meaningful as the length of the sustained
window, and startup / contact / recovery remain separate phases (`AttackDefinition.GetPhase`, which
is now the only phase source).

To re-measure, sample the animator state (not `AnimationMode.SampleAnimationClip`, which leaves a
humanoid rig in bind pose) and compare the fist-to-head distance against its guard value.

## Hit resolution follows the fist

`FighterStrikeRig` reads the animated `LeftHand` / `RightHand` bone and reports the hand's position
on the previous tick and on the current one. Every contact tick resolves a sweep from the previous
hand sample to the current one (`HitQuery.FromHandSweep`), so a fist travelling ~1 m per tick at
60 Hz cannot tunnel through a target. A hand that has not moved still probes a short distance
(0.02 m) along the attacker's facing. The rig never moves a bone, a fist, or a target.

`AttackDefinition.Range` is now a planar sanity envelope between the fighter root and the sampled
fist (0.95 m jab, 1.05 m cross); a sample outside it is treated as a rig fault and discarded.
`Radius` is the fist tolerance (0.06 m / 0.07 m), not a chest-sized sphere.

## One strike, one target

`AttackStateBase` allocates one `HitId` on entry and re-issues a hit request with that same id on
every tick inside the contact window. `HitRegistry` therefore sees one window per strike and lets
it claim exactly one target: the first target whose collider intersects the fist's swept path is
damaged, and every later contact for the same `HitId` — same target or different — is rejected.
`FighterController` releases the window when the attack state ends, so the table is bounded by the
attacks currently in flight rather than by session length. Later (multi-target or per-target
window) work can add a second window without changing the identity model.

## Facing and exit ownership

- `IFighterState.FacingAuthority` (`Free` / `Locked`) is consumed by `FighterMotor.Tick`. Attack
  states declare `Locked`, so a committed punch keeps the orientation it started with and a missed
  punch stays missed. Locomotion keeps `Free` and continues to face the nearest opponent.
- Locomotion facing was not changed: movement is still combat-relative and lateral speed is still
  calibrated at 0.818 m/s.
- `FighterView` is now the single authority that ends an attack: the animator controller's
  exit-time return transitions on `Jab` / `Cross` were removed, and the view cross-fades back to
  `Locomotion` on the same tick the domain state returns to `Idle`. `FighterAssetBuilder` fails
  validation if an exit-time return is reintroduced.

## Validation performed

- `git diff --check`: only pre-existing trailing whitespace in the serialized
  `AC_Fighter_Base.controller` YAML.
- Unity 6000.6.7f1, EditMode suite in the real project: 41/41 passed.
- `FighterAssetBuilder.ValidateLocomotionAssets`: passed (controller, prefab, definition, attack
  states, no exit-time return).

## Not verified

- No Play Mode or visual run: hand-to-target contact, animation blending, and foot planting were
  not observed in motion. The 0.818 m/s lateral figure is a measured clip calibration, not a
  visual foot-planting result.
- `CombatTest.unity` still spawns the fighters 3.0 m apart. The measured fist reach is roughly
  0.5 – 0.7 m from the root axis, so no strike can connect at that spacing; the scene needs
  distance tuning (and a visual pass) before it can demonstrate hits.

## Deferred on purpose

- Block mitigation and chip damage.
- Dodge invulnerability frames.
- Advanced hit reactions (directional reactions, stun, juggle) — the existing `HitReactionState`
  is unchanged.
- Combo cancellation and attack branching.
- Any new damage model, armor, or counter-hit rules.
- Trimming `Jab Cross.fbx`: the clip also contains a lead jab before the cross, so the cross state
  currently plays that lead-jab motion during its startup.
