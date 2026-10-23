# Octagon Fighter

**Realistic 3D MMA Fighting Game | Unity 6 | C# | URP | New Input System**

[![Unity](https://img.shields.io/badge/Unity-6000.6.7f1-black?logo=unity)](https://unity.com)
[![C#](https://img.shields.io/badge/C%23-9%2B-blue)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![URP](https://img.shields.io/badge/URP-17.6.0-orange)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.6/manual/index.html)
[![License: Unlicensed](https://img.shields.io/badge/License-Unlicensed-red)](#license)

---

## Overview

Octagon Fighter is a simulation-first mixed martial arts game built in Unity 6. Inspired by EA Sports UFC 4/5, Undisposed, Undisputed, and Fight Night Champion, the goal is to capture the tension, pacing, and tactical depth of real MMA through grounded striking, contested grappling, stamina management, fighter attributes, AI opponents, and career progression.

See [GAME_DESIGN_DOCUMENT.md](GAME_DESIGN_DOCUMENT.md) for full design details and [OPENCODE.md](OPENCODE.md) for engineering standards.

---

## Project Status

**Pre-production.** The repository foundation is complete. No gameplay systems have been
implemented yet. The next milestone is **Core Combat Foundation**; see
[ROADMAP.md](ROADMAP.md) for scope, sequencing, and exit criteria.

---

## Core Features

- **Five Disciplines** - Boxing, Kickboxing, Muay Thai, Wrestling, Brazilian Jiu-Jitsu
- **Complete Match Systems** - Striking, Clinch, Takedowns, Ground & Pound, Submission
- **Simulation Combat** - Stamina economy, contested actions, realistic damage, fatigue effects
- **Career Progression** - Fighter attributes, experience, training camps, injuries, rankings, championships
- **Game Modes** - Career Mode, Quick Fight, Tournament, Local Multiplayer
- **Data-Driven Design** - All balance via ScriptableObjects
- **Deterministic Architecture** - Tick-based combat, designed for future multiplayer
- **Clean Architecture** - SOLID, composition over inheritance, interface-driven

---

## Technical Stack

| Technology | Purpose |
| --- | --- |
| **Unity 6 (6000.6.7f1)** | Game Engine |
| **C#** | Scripting |
| **Universal Render Pipeline (17.6.0)** | Rendering |
| **Unity Input System (1.20.1)** | Player Input |
| **Cinemachine** | Dynamic Camera |
| **ScriptableObjects** | Configuration & Data |
| **Assembly Definitions** | Modular Architecture |
| **Git** | Version Control |

---

## Project Structure

```
OctagonFighter/
├── Assets/
│   └── _Project/
│       ├── Art/                # Models, Materials, Textures, VFX, Animations
│       ├── Audio/              # Music, SFX, Mixers
│       ├── Prefabs/            # GameObject Prefabs
│       ├── Scenes/             # Unity Scenes
│       ├── ScriptableObjects/  # Data Assets
│       ├── Scripts/            # Runtime Code (Clean Architecture Layers)
│       └── Tests/              # EditMode & PlayMode Tests
├── Packages/                   # UPM Packages (manifest tracked)
├── ProjectSettings/            # Unity Project Settings
├── OPENCODE.md                 # Engineering Standards & Architecture
├── COMBAT_ARCHITECTURE.md      # Core Combat Foundation Design
├── GAME_DESIGN_DOCUMENT.md     # Game Design Document
├── README.md                   # This File
├── ROADMAP.md                  # Development Roadmap
└── CHANGELOG.md                # Version History
```

---

## Getting Started

### Prerequisites

- [Unity Hub](https://unity.com/download) installed
- [Unity 6 Editor](#) 6000.6.7f1 or compatible
- [Git](https://git-scm.com/) installed
- A C# IDE (Rider, Visual Studio, VS Code with C# extensions)

### Setup

1. **Clone the repository**
   ```bash
   git clone https://github.com/your-org/OctagonFighter.git
   cd OctagonFighter
   ```
2. **Open with Unity Hub**
   - Add the project folder
   - Open with Unity 6000.6.7f1
3. **Wait for imports**
   - Unity will resolve packages and compile scripts
4. **Open Bootstrap scene** (once created) or first available scene

### Development Guidelines

- Follow all rules in [OPENCODE.md](OPENCODE.md) before contributing
- Use Conventional Commits (see OPENCODE.md#git-workflow)
- Branch from `develop` using `feature/*`, `bugfix/*`, etc.
- One public type per C# file
- Domain logic must not reference `UnityEngine`
- Use interfaces and composition over inheritance

---

## Documentation

| Document | Purpose |
| --- | --- |
| [OPENCODE.md](OPENCODE.md) | Architecture rules, coding standards, Unity standards, AI assistant rules |
| [COMBAT_ARCHITECTURE.md](COMBAT_ARCHITECTURE.md) | Core combat foundation design: fighter data, controller, state machine, health, stamina, hit detection |
| [GAME_DESIGN_DOCUMENT.md](GAME_DESIGN_DOCUMENT.md) | Complete game design, systems, modes, progression |
| [ROADMAP.md](ROADMAP.md) | Development milestones and priorities |
| [CHANGELOG.md](CHANGELOG.md) | Release history and changes |

---

## Contributing

This is a private/unlicensed project. All contributions follow the standards defined in OPENCODE.md. Pull requests require review. Never push directly to `main` or `develop`.

---

## License

**Unlicensed** - All rights reserved. No part of this project may be reproduced, distributed, or used commercially without explicit permission from the owners.
