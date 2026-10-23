# Changelog

All notable changes to **Octagon Fighter** are documented in this file.

The project follows [Semantic Versioning](https://semver.org/) and
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) conventions.

---

## [Unreleased]

### Added

- Phase 2 Character Locomotion: dedicated `WalkState` and `RunState` in the fighter state machine, `MovementGait` intent data, data-driven walk speed scaling, and profile-driven character rotation.
- Player movement input with walk/run selection (`Left Ctrl`, gamepad left trigger, or left stick click), a passive intent source for the demo opponent, and `FighterView` speed blending into the animator.
- Generated `AC_Fighter_Base.controller` with a 1D `Locomotion` blend tree driven by the `Speed` parameter and idle/walk/run Mixamo clips.
- EditMode locomotion transition tests and `PHASE2_VALIDATION.md` demo scene checklist.
- `FighterAssetBuilder` replacing `Phase1AssetBuilder`, with asset rebuild and locomotion validation menu items.
- Phase 1 Core Combat Foundation runtime: `FighterDefinition` ScriptableObject with validated authoring profiles, domain `FighterData` snapshots, `HealthModel`/`StaminaModel`, foundation `FighterStateMachine`, `FighterRuntime`, and `FoundationStateMachineFactory`.
- Unity adapters and orchestration: `HealthComponent`, `StaminaComponent`, `PlayerIntentSource`, `FighterMotor`, `CombatTickDriver`, `CombatSession`, `FighterController`, `FighterView` using the imported Mixamo `boxer.fbx` with a generated base animator controller.
- Playable fighter prefab architecture: `Assets/_Project/Prefabs/Fighters/PF_Fighter_Boxer.prefab` built from the Mixamo character with intent-driven movement/block/dodge, fixed-tick simulation, and readable state transitions. Demo scene `Assets/_Project/Scenes/CombatTest.unity` added to build settings.
- Editor tooling: `OctagonFighter.Editor` assembly and `FighterAssetBuilder` to rebuild fighter assets/scenes from Mixamo imports.
- `COMBAT_ARCHITECTURE.md` defining the Core Combat Foundation design for fighter data,
  controller orchestration, state machine, health, stamina, and hit detection. This
  milestone is design-only; no runtime combat code or punch mechanics were implemented.
- Missing `Assets/_Project/Art`, `Assets/_Project/Audio`, and `Assets/_Project/Prefabs`
  folders required by the documented project structure.

### Changed

- Replaced the generic `Locomotion` state with separate `Walk` and `Run` states so gait,
  movement speed, and animation blending are explicit and independently extensible.
- Moved character rotation speed and walk speed scaling out of the Unity motor and into
  the authored `MovementProfile`.
- Moved the sample scene from `Assets/Scenes` to `Assets/_Project/Scenes` to match the
  documented project structure. Scene GUIDs were preserved.

### Planned

- Core Combat Foundation: fixed-tick combat clock, health, stamina, strikes, hit
  detection, fighter state machine, and deterministic combat resolution.

---

## [0.1.0] - 2026-10-23

### Added

- Initial Unity 6 project setup (`6000.6.7f1`) with Universal Render Pipeline 17.6.0 and
  Input System 1.20.1.
- Production-ready Unity `.gitignore` covering Unity-generated folders, IDE output, build
  artifacts, and OS files.
- `README.md` documenting the project overview, technical stack, structure, and setup.
- `ROADMAP.md` defining phased milestones from repository foundation through release.
- `CHANGELOG.md` establishing version history and change tracking.
- `GAME_DESIGN_DOCUMENT.md` describing combat, progression, modes, and presentation.
- `OPENCODE.md` defining architecture rules, coding standards, Unity standards, and Git
  workflow.
- Project folder skeleton under `Assets/_Project` for scripts, ScriptableObjects, and
  tests.

### Changed

- Ignored generated Unity folders: `Library/`, `Temp/`, `Logs/`, `Obj/`,
  `UserSettings/`, `.vs/`, and `.vscode/`.
- Ignored generated IDE solution and project files, including `*.csproj`, `*.sln`, and
  `*.slnx`.

### Notes

- No gameplay systems have been implemented. This release contains repository and
  documentation scaffolding only.

---

<!--
Version headings are added when a release is tagged.
Keep entries grouped under Added, Changed, Deprecated, Removed, Fixed, or Security.
-->
