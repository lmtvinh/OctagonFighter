# Changelog

All notable changes to **Octagon Fighter** are documented in this file.

The project follows [Semantic Versioning](https://semver.org/) and
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) conventions.

---

## [Unreleased]

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
