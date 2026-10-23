# GAME DESIGN DOCUMENT

# Octagon Fighter

**Realistic 3D Mixed Martial Arts Simulation**

| Field | Detail |
| --- | --- |
| Document Version | 1.0 |
| Status | Concept / Pre-Production |
| Genre | Sports Simulation / Fighting |
| Platform | PC (Windows), Console (future), Mobile (future scalability target) |
| Engine | Unity 6 |
| Render Pipeline | Universal Render Pipeline (URP) |
| Camera | Cinemachine |
| Input | Unity Input System |
| Players | 1-2 local, online TBD |
| Rating Target | T (Teen) to M (Mature) - blood/impact toggle |
| Inspirations | EA Sports UFC 4/5, Undisputed, Fight Night Champion |

---

## 1. Executive Summary

**Octagon Fighter** is a simulation-first mixed martial arts game that prioritizes tactical
depth over arcade spectacle. Fights are decided by positioning, timing, stamina
management, and discipline-specific knowledge. Players build fighters across five combat
disciplines - Boxing, Kickboxing, Muay Thai, Wrestling, and Brazilian Jiu-Jitsu - and
progress through a structured career of training camps, injuries, rankings, and
championship runs.

The design targets a **AA scope with AAA-quality combat feel**. Every system is
data-driven, deterministic, and built to scale from single-player career to local
multiplayer and, eventually, online play.

### Elevator Pitch

> A grounded MMA simulator where every strike, grip, and transition matters. Build a
> fighter from a raw amateur into a world champion by mastering five disciplines, managing
> a body that wears down, and out-thinking opponents who fight like real people.

---

## 2. Design Pillars

| Pillar | Meaning | Player-Facing Result |
| --- | --- | --- |
| **Authentic Combat** | Mechanics reflect real MMA techniques and risks | Strikes telegraph, takedowns are contested, positions matter |
| **Stamina Is Currency** | Every action spends a finite resource | Patience and efficiency beat spam |
| **Readable Depth** | Systems are deep but legible | Animations, audio, and UI communicate intent and state |
| **Fighter Identity** | Attributes, body type, reach, and skills define a fighter | Two players never feel the same |
| **Consequence** | Injuries, fatigue, and losses persist | Career decisions carry weight |
| **Fair Competition** | Deterministic, symmetric rules for local multiplayer | Skill decides outcomes, not hidden bonuses |

### Non-Goals

- Arcade juggling, exaggerated physics, or health-bar spectacle.
- Roster-driven spectacle over simulation authenticity.
- Pay-to-win progression or stat advantages in competitive modes.

---

## 3. Target Audience & Positioning

| Segment | Description |
| --- | --- |
| **Core MMA Fans** | Want authentic techniques, real positions, and tactical pacing |
| **Fighting Game Players** | Want depth, mastery, and readable neutral game |
| **Sports Career Players** | Want progression, training, story, and legacy |
| **Couch Multiplayer Groups** | Want fair, immediately understandable local versus |

**Fantasy:** "I out-thought my opponent - I managed my gas tank, timed the takedown, and
finished the submission."

**Session Length:** 3-15 minutes per fight; 20-40 minute career sessions.

---

## 4. Core Gameplay Loop

```
Select/Manage Fighter
        |
        v
Pre-Fight: Scouting, Gameplan, Weight Cut
        |
        v
Fight: Stand-Up -> Clinch -> Takedown -> Ground -> Submission/Finish
   ^                                              |
   |                                              v
   +----------- Round Breaks / Stamina Recovery --+
        |
        v
Post-Fight: Result, Damage, Experience, Ranking Update
        |
        v
Training Camp -> Attribute Growth -> Next Fight
```

**Micro Loop (seconds):** Read opponent -> choose action -> manage stamina -> capitalize on
opening.
**Macro Loop (career):** Train -> fight -> recover -> rank up -> contend for title ->
defend or move divisions.

---

## 5. Combat Disciplines

Each fighter has proficiency levels (0-100) in five disciplines. Proficiency unlocks moves,
improves execution speed/accuracy, and modifies available options in each phase. Fighters
can be **specialists** (high in one, weak in others) or **well-rounded**.

### 5.1 Boxing

**Focus:** Hand speed, head movement, footwork, counters.

| Element | Design |
| --- | --- |
| Signature Techniques | Jab, cross, hook, uppercut, overhand, body shots |
| Defensive Tools | Slip, duck, roll, shoulder roll, high guard |
| Range | Close to mid |
| Strengths | Fastest strikes, best head movement, strong counters |
| Weaknesses | No kicks, vulnerable to leg attacks and takedowns |
| Key Attributes | Hand Speed, Accuracy, Footwork, Chin |

**Feel:** Crisp, rhythmic, punch-driven pressure. Boxing specialists dominate exchanges
at boxing range but must avoid the clinch and kicking distance.

### 5.2 Kickboxing

**Focus:** Long-range striking, combinations, distance control.

| Element | Design |
| --- | --- |
| Signature Techniques | Jab-cross-low kick, switch kick, teep, body kick, spinning back kick |
| Defensive Tools | Distance management, checks, lateral movement |
| Range | Mid to long |
| Strengths | Range control, high output combinations, leg damage |
| Weaknesses | Vulnerable in close range, limited clinch and ground game |
| Key Attributes | Kick Power, Speed, Footwork, Cardio |

**Feel:** Fluid, range-dominant striking with emphasis on combinations and leg kicks that
accumulate damage over rounds.

### 5.3 Muay Thai

**Focus:** Eight limbs, clinch dominance, knees and elbows.

| Element | Design |
| --- | --- |
| Signature Techniques | Teep, roundhouse, switch knee, elbow, clinch knee, sweep |
| Defensive Tools | Checks, catch-and-sweep, clinch control |
| Range | All ranges, strongest in clinch |
| Strengths | Devastating clinch, elbows, knees, sweeps |
| Weaknesses | Slower kicks, exposed to takedowns during kicks |
| Key Attributes | Power, Clinch Strength, Durability, Balance |

**Feel:** Aggressive, close-quarters violence. Muay Thai fighters control the clinch and
punish opponents who enter it unprepared.

### 5.4 Wrestling

**Focus:** Takedowns, control, positional dominance, ground-and-pound setup.

| Element | Design |
| --- | --- |
| Signature Techniques | Double leg, single leg, body lock, trip, suplex, sprawl |
| Defensive Tools | Sprawl, underhooks, whizzer, cage defense |
| Range | Clinch and ground |
| Strengths | Takedown offense and defense, top control, cardio |
| Weaknesses | Limited finishing ability on the feet |
| Key Attributes | Takedown, Takedown Defense, Top Control, Strength, Cardio |

**Feel:** Relentless pressure and control. Wrestling specialists dictate where the fight
happens and drain opponents on the mat.

### 5.5 Brazilian Jiu-Jitsu

**Focus:** Submissions, sweeps, positional advancement from the bottom.

| Element | Design |
| --- | --- |
| Signature Techniques | Rear-naked choke, armbar, triangle, guillotine, kimura, sweeps |
| Defensive Tools | Submission defense, guard retention, escapes |
| Range | Ground and clinch transitions |
| Strengths | Finishing threat from any ground position, sweeps |
| Weaknesses | Vulnerability to ground-and-pound, weaker stand-up |
| Key Attributes | Submission Offense, Submission Defense, Ground Control, Flexibility |

**Feel:** A constant threat on the mat. Opponents must respect every grip and transition or
get submitted.

### Discipline Interaction Matrix

| Attacker \ Defender | Boxing | Kickboxing | Muay Thai | Wrestling | BJJ |
| --- | --- | --- | --- | --- | --- |
| **Boxing** | Even | Disadvantage (range) | Disadvantage (clinch) | Advantage (hands) | Advantage (hands) |
| **Kickboxing** | Advantage (range) | Even | Even | Disadvantage (takedowns) | Advantage (stand-up) |
| **Muay Thai** | Advantage (clinch) | Even | Even | Disadvantage (takedowns) | Advantage (stand-up) |
| **Wrestling** | Advantage (takedown) | Advantage (takedown) | Even (clinch) | Even | Advantage (control) |
| **BJJ** | Disadvantage (stand-up) | Disadvantage (stand-up) | Disadvantage (stand-up) | Disadvantage (top pressure) | Even |

*Matrix expresses general strategic tendency, not hard counters. Player skill and stamina
always dominate.*

---

## 6. Match Systems

All phases share one **stamina economy** and one **shared grapple context** so transitions
between phases are continuous rather than discrete mini-games.

### 6.1 Striking

**Phase Owner:** Both fighters, simultaneous input.

| System | Specification |
| --- | --- |
| Attack Timing | Startup / Active / Recovery frames from `StrikeDefinition` |
| Damage Types | Impact, Accumulated (leg/body), Cut, Stun |
| Guard | High, Low, Body - mismatched guard increases damage |
| Head Movement | Slip/duck with vulnerability windows |
| Counters | Hitting during opponent startup/whiff grants multiplier |
| Combo Scaling | Damage decreases per consecutive hit (100% -> 85% -> 70% ...) |
| Stamina | Every strike costs stamina; whiffs cost more than landed strikes |
| Hit Reactions | Flinch, stagger, rocked, knockdown - driven by accumulated damage |
| Targeting | High (head), Mid (body), Low (legs) with distinct reactions |

**Range Bands:** Outside, Kick Range, Punch Range, Elbow/Knee Range, Clinch Range. Attacks
are only valid in their band; moving between bands is a first-class tactical decision.

**Rocked State:** After accumulating head damage, a fighter enters a vulnerable window
where defense is degraded and further hits can cause a knockdown or KO.

### 6.2 Clinch

**Phase Owner:** Contested, turn-based control priority.

| System | Specification |
| --- | --- |
| Entry | From striking range, after blocked kicks, or via cage pressure |
| Control | Underhooks, overhooks, collar tie, body lock determine advantage |
| Actions | Knees, elbows, dirty boxing, trips, takedown attempts, breaks |
| Contest | Control meter contested by strength, technique, stamina |
| Cage | Pinned fighter has reduced escape options; referee resets on inactivity |
| Exit | Break, takedown, or pull guard |

**Design Intent:** The clinch is the bridge between striking and grappling. Muay Thai and
Wrestling excel here; strikers must either dominate grips or disengage quickly.

### 6.3 Takedowns

**Phase Owner:** Attacker initiates, defender contests.

```
Takedown Attempt
      |
      v
Contest Window (setup + drive)
      |
      +--> Clean Takedown  -> Top Position
      +--> Partial         -> Clinch / Body Lock
      +--> Stuffed         -> Attacker vulnerable, stamina drained
      +--> Reversal        -> Defender gains top position
```

| Element | Specification |
| --- | --- |
| Types | Double leg, single leg, body lock, trip, throw |
| Success Formula | Attacker Takedown + Stamina + Timing - Defender TDD - Position - Stamina |
| Stamina | Attempt costs attacker; deep defense costs defender |
| Vulnerability | Failed attempts grant a punishment window |
| Cage Interaction | Takedowns near the cage gain control, reduce escape |
| Outcomes | Clean, Partial, Stuffed, Reversal, Scramble |

**Design Intent:** Takedowns are risky investments, not free control. Defending well is as
rewarding as attacking well.

### 6.4 Ground and Pound

**Phase Owner:** Top fighter controls, bottom fighter defends/advances.

| System | Specification |
| --- | --- |
| Positions | Guard, Half Guard, Side Control, Mount, Back, Crucifix |
| Attacks | Punches, elbows, hammerfists from top |
| Defense | Guard retention, frames, hip escapes, referee stand-up |
| Damage Model | Accumulates over time; forces defensive reactions |
| Risk | Posture breaks, sweeps, and submission attempts |
| Stand-Up | Referee stand-up on inactivity; fighter-initiated stand-up costs stamina |

**Design Intent:** Ground and pound is a pressure tool that forces the bottom fighter to
choose between eating damage, defending submissions, or risking a sweep. It should feel
claustrophobic and urgent.

### 6.5 Submissions

**Phase Owner:** Contested, multi-stage struggle.

| System | Specification |
| --- | --- |
| Stages | Setup -> Grip -> Struggle -> Finish / Escape |
| Contest | Submission Offense + Position + Stamina vs Submission Defense + Stamina |
| Progression | Cumulative meter over fixed ticks; not a single random roll |
| Escape | Windows and input sequences; stamina cost scales with danger |
| Taps | Voluntary tap available; AI taps based on health/stamina thresholds |
| Consequences | Failed attempt costs stamina and may concede position |
| Damage | Chokes vs joint locks differ: unconsciousness vs injury risk |

**Submission List (Launch Target)**

| Submission | Position | Type | Danger Window |
| --- | --- | --- | --- |
| Rear-Naked Choke | Back | Choke | High |
| Guillotine | Guard / Front Headlock | Choke | Medium |
| Triangle Choke | Guard | Choke | Medium |
| Armbar | Mount / Guard | Joint | High |
| Kimura | Side Control / Guard | Joint | Medium |
| Americana | Mount / Side Control | Joint | Medium |
| Heel Hook | Leg Entanglement | Joint | Very High |
| Anaconda Choke | Front Headlock | Choke | Medium |

**Design Intent:** Submissions are dramatic but fair. Both fighters can feel the finish
approaching and have counterplay, making the struggle itself the gameplay.

---

## 7. Player Progression

### 7.1 Fighter Attributes

Attributes are rated **1-100**, displayed with letter/star tiers, and influence combat
resolution. Growth is earned through training and fights, with diminishing returns and
age-based decline.

| Group | Attributes | Affects |
| --- | --- | --- |
| **Physical** | Strength, Speed, Cardio, Durability, Flexibility | Power, movement, stamina pool/regen, damage resistance |
| **Striking** | Hand Speed, Kick Power, Accuracy, Footwork, Head Movement, Chin | Strike speed, damage, evasion, stun resistance |
| **Grappling** | Takedown, Takedown Defense, Clinch Strength, Top Control, Ground Defense | Takedown success/defense, control, escapes |
| **Submission** | Submission Offense, Submission Defense, Ground Control | Finish rate, escape rate, positional retention |
| **Mental** | Fight IQ, Composure, Heart, Aggression | Decision-making, rocked recovery, AI weighting |
| **Discipline** | Boxing, Kickboxing, Muay Thai, Wrestling, BJJ | Move access and execution bonuses |

**Derived Stats:** Overall Rating, Reach, Weight Class, Stamina Pool, Damage Thresholds.

**Caps & Soft Caps:** Base cap 100; specialization beyond 90 is expensive, encouraging
build diversity.

### 7.2 Experience

| Source | XP |
| --- | --- |
| Fight Win | 500-1500 (by opponent rating) |
| Fight Loss | 150-400 |
| Finish Bonus | +25% |
| Performance Bonus | +10-30% (strikes, control, excitement) |
| Training Session | 50-150 |
| Title Fight | +50% |

XP funds attribute growth at a cost curve:

```
Cost = BaseCost * (1 + CurrentValue / 20) * DisciplineMultiplier
```

Growth is capped per camp; overtraining reduces gains.

### 7.3 Training Camps

Between fights, players schedule sessions across a limited number of camp weeks
(typically 4-8). Each week has energy slots.

| Session Type | Effect | Cost | Risk |
| --- | --- | --- | --- |
| Strength & Conditioning | Strength, Cardio | High energy | Fatigue |
| Striking Drills | Discipline/Striking attributes | Medium | Low |
| Grappling Rounds | Wrestling/BJJ attributes | Medium | Joint strain |
| Sparring | Fight IQ, Composure, XP | High energy | Injury risk |
| Recovery | Restore fatigue/injury | None | No growth |
| Film Study | Read opponent tendencies, AI prep | Low | None |

**Camp Rules:**
- Each session consumes energy; insufficient recovery reduces gains and raises injury risk.
- Opponent scouting reveals tendencies and recommended gameplan.
- Fight week enforces weight cut and reduces available training.
- Camp decisions create meaningful tradeoffs: growth vs freshness vs injury risk.

### 7.4 Injuries

| Severity | Effect | Recovery |
| --- | --- | --- |
| Minor | Stat penalty (e.g., -5 Speed) | 1-2 weeks |
| Moderate | Larger penalty, risk during fight | 3-6 weeks |
| Severe | Withdraw from fight, long absence | 8-16 weeks, possible career impact |

**Injury Sources:** Overtraining, sparring, fight damage, fighting injured.

**Design Intent:** Injuries force adaptation. A fighter with a damaged leg must protect it;
a fighter with a hand injury loses knockout power. Injuries are visible on the fighter
model and in UI.

**Fight Damage Persistence:** Post-fight damage carries into recovery; fighting too soon
carries stat penalties and medical suspension risk.

### 7.5 Rankings

- Each division maintains a **Top 15** ranking.
- Rankings update after fights based on result, opponent rank, and streak.
- Title shot eligibility after 2-3 ranked wins or a win streak threshold.
- Champions are excluded from the contender list; interim titles possible during long
  layoffs.
- Rankings are persistent and displayed in career hub UI.

**Ranking Algorithm (conceptual):**

```
Delta = K * (Result - Expected) + OpponentRankBonus + StreakBonus - InactivityPenalty
```

### 7.6 Championships

| Stage | Requirement |
| --- | --- |
| Contender | Top 5 ranking + 2+ ranked wins |
| Title Fight | Promotion matchmaking decision |
| Champion | Win title fight |
| Defense | Defend against top contenders |
| Legacy | Successful defenses, finish rate, longevity |

**Champion Mechanics:** Champions gain sponsorship/purse bonuses and media obligations that
consume camp time. Defenses are harder as contenders study footage. Losing the belt opens
a rematch path.

**Weight Class Movement:** Champions may vacate to move divisions; players can pursue
double-champion status.

---

## 8. Game Modes

### 8.1 Career Mode

**Overview:** Full progression from amateur to champion across a 15-20 year arc.

**Structure:**
1. **Create Fighter** - Appearance, body type, stance, background (starting disciplines).
2. **Amateur Circuit** - 3-5 fights to establish rating.
3. **Promotion Signing** - Contract negotiation, division choice.
4. **Ranked Run** - Fight cards, training camps, injuries, media.
5. **Title Contention** - Number-one contender fights, title shots.
6. **Title Reign** - Defenses, legacy tracking, potential division change.
7. **Retirement** - Legacy screen, Hall of Fame eligibility.

**Systems:**
- **Calendar:** Weeks advance through camp, fight week, recovery.
- **Contracts:** Purse, win bonus, sponsor bonuses, fight frequency.
- **Media/Relationships:** Optional interviews affect popularity and matchmaking.
- **Scouting:** Opponent tendencies, recent fights, injury status.
- **AI Roster:** Generated and persistent opponents with their own careers and rankings.
- **Save System:** Versioned, autosave, multiple slots.

**Player Agency:** Choose fight offers, training focus, weight class, aggression style, and
whether to fight injured. No forced narrative rails.

### 8.2 Quick Fight

**Overview:** Immediate exhibition match.

- Choose any fighter (roster or custom) and opponent.
- Configure rounds, difficulty, weight class, arena.
- No progression or persistence beyond records.
- Ideal for learning systems and testing matchups.

### 8.3 Tournament

**Overview:** Bracket-based competition.

| Option | Detail |
| --- | --- |
| Format | 4, 8, or 16 fighters |
| Seeding | Rating-based or random |
| Rules | Single elimination; optional title at stake |
| Fatigue | Damage carries between rounds optionally (simulation mode) |
| Rewards | XP, unlocks, cosmetics |
| AI | Mixed difficulty, adaptive final rounds |

**Design Intent:** High-stakes, short-form structure that tests adaptation across multiple
opponents and styles.

### 8.4 Local Multiplayer

**Overview:** Same-screen or split-screen (if supported) versus play.

| Element | Specification |
| --- | --- |
| Players | 2 local |
| Fighters | Full roster + custom fighters |
| Rules | Symmetric; no stat bonuses from progression |
| Input | Two controllers or keyboard + controller |
| Camera | Shared dynamic camera with distance-aware framing |
| Pause | Both players must confirm resume |
| Options | Rounds, timer, damage scaling, stamina drain |

**Fairness Rules:** All competitive local matches use normalized fighter stats or explicit
"simulation" toggles. Progression advantages never apply in versus.

---

## 9. User Experience & Interface

### 9.1 HUD

- **Health Bars:** Head and body separate; visible damage state.
- **Stamina Bar:** Prominent, with tier colors (Fresh/Working/Tired/Gassed).
- **Position Indicator:** Ground position map and control priority.
- **Submission Meter:** Struggle progress for both fighters.
- **Round/Time:** Clean, unobtrusive.
- **Damage Feedback:** Directional hit indicators, cut/rocked warnings.

### 9.2 Menus

- Career Hub: Calendar, rankings, training, offers, fighter profile.
- Fighter Profile: Attributes, disciplines, record, injuries, contract.
- Training Screen: Weekly planner with energy and risk visualization.
- Fight Night: Scouting report, gameplan, weigh-in.
- Accessibility: Remappable controls, colorblind palettes, subtitle/caption options,
  simplified struggle input, damage display toggle.

### 9.3 Camera

- Cinemachine-driven dynamic framing based on fighter distance and action phase.
- Ground camera reframes to show position and limbs.
- Impact shake, subtle hit-stop, and slow-motion on finishes (option to disable).

---

## 10. Audio & Presentation

| Area | Direction |
| --- | --- |
| Impact Audio | Layered thuds, bone impacts, glove contact, breathing |
| Crowd | Reactive to momentum, near-finishes, and decisions |
| Commentary | Contextual lines for strikes, takedowns, submissions, rounds |
| Animation | Motion-captured MMA techniques; blend trees for locomotion |
| VFX | Sweat, blood (toggle), dust, cage interaction, submission tension |
| Lighting | Arena-focused URP lighting; broadcast-style presentation |

**Presentation Goal:** Broadcast authenticity - camera angles, overlays, and pacing that
mirror a televised MMA event.

---

## 11. Technical Design Alignment

This document complements `OPENCODE.md`, which defines engineering rules. Key technical
constraints:

- **Determinism:** Tick-based combat, seeded randomness, serializable state.
- **Data-Driven:** All balance values in ScriptableObjects (`StrikeDefinition`,
  `TakedownDefinition`, `SubmissionDefinition`, `FighterDefinition`, `AiProfile`).
- **Architecture:** Clean Architecture with domain logic independent of Unity;
  intent-driven input shared by player and AI.
- **Performance:** Object pooling, no per-frame allocations in combat, fixed timestep
  physics, mobile-aware rendering budgets.
- **Scalability:** Local multiplayer first; architecture must not preclude online.

### System Ownership Map

| System | Layer | Key Types |
| --- | --- | --- |
| Fighter orchestration | Presentation | `FighterController`, `FighterFacade` |
| State machine | Domain | `FighterStateMachine`, `IFighterState` |
| Combat resolution | Domain | `CombatResolver`, `DamageEvent` |
| Strike data | Data | `StrikeDefinition` |
| Stamina | Domain | `StaminaComponent`, `StaminaTier` |
| Grappling | Domain | `GrappleContext`, `TakedownResolver` |
| Submissions | Domain | `SubmissionContest`, `SubmissionDefinition` |
| AI | Domain | `AiIntentSource`, `UtilityEvaluator`, `AiProfile` |
| Career | Application | `CareerService`, `CareerState` |
| Save | Infrastructure | `ISaveStore`, `JsonSaveStore` |

---

## 12. Content Scope (Launch Target)

| Category | Target |
| --- | --- |
| Fighters (roster) | 24-32 unique |
| Weight Classes | 4-5 (e.g., Lightweight, Welterweight, Middleweight, Light Heavyweight, Heavyweight) |
| Disciplines | 5 (Boxing, Kickboxing, Muay Thai, Wrestling, BJJ) |
| Strikes | 80+ unique techniques |
| Takedowns | 12+ |
| Submissions | 15+ |
| Ground Positions | 8+ |
| Arenas | 6-8 |
| Career Length | 15-20 in-game years |
| Training Sessions | 10+ types |
| Difficulty Tiers | 4 (Amateur, Pro, Veteran, Legend) |

---

## 13. Balance & Tuning Principles

1. **Data over constants.** Every value is a ScriptableObject field, tuned in-editor.
2. **Stamina dominates.** A gassed fighter must visibly underperform in every phase.
3. **Risk symmetry.** Every offensive action has a punish window.
4. **No invisible bonuses.** AI difficulty changes decision quality and reaction time, not
   hidden stats.
5. **Diminishing returns.** Specialization is rewarded but not mandatory; well-rounded
   fighters are viable.
6. **Readable feedback.** If a system is complex, its state must be communicated visually
   and audibly.
7. **Playtest-driven.** Numbers below are starting points, not final values.

### Starting Tuning Targets

| Parameter | Start Value |
| --- | --- |
| Round Length | 5 minutes (3 or 5 rounds) |
| Stamina Pool | 100 |
| Strike Stamina Cost | 3-8 |
| Whiff Cost Multiplier | 1.5x |
| Stamina Regen | 8/sec after 2s delay |
| Gassed Threshold | 20 |
| Takedown Success Range | 30-70% before modifiers |
| Submission Tick Interval | 0.5s |
| Combo Scaling | -15% per hit, floor 40% |
| Knockdown Threshold | Scales with head damage and chin |

---

## 14. Risk Assessment

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Combat depth overwhelms new players | High | Tutorialized career, contextual prompts, readable HUD |
| Ground game becomes a mini-game | High | Shared grapple context; continuous stamina; no mode switching |
| AI feels unfair or exploitable | High | Deterministic utility AI; telemetry; difficulty via reaction/errors |
| Animation scope explodes | High | Prioritize techniques; reuse rigs; phased content plan |
| Simulation frustrates casual players | Medium | Quick Fight accessibility, difficulty options, assistance toggles |
| Multiplayer determinism issues | Medium | Tick-based state, seeded RNG, serialize-friendly domain |
| Injury system feels punishing | Medium | Toggle difficulty, clear warnings, recovery options |

---

## 15. Milestones

| Milestone | Deliverable |
| --- | --- |
| **M1 - Combat Prototype** | Striking, stamina, health, one fighter pair, hit reactions |
| **M2 - Grappling Prototype** | Clinch, takedowns, ground positions, submissions |
| **M3 - Systems Complete** | State machine, AI, all five disciplines, data pipeline |
| **M4 - Career Alpha** | Progression, training camps, injuries, rankings, save/load |
| **M5 - Modes** | Quick Fight, Tournament, Local Multiplayer |
| **M6 - Content & Polish** | Roster, arenas, audio, UI, accessibility, performance |
| **M7 - Beta** | Balance pass, telemetry, bug fixing, platform certification |

---

## 16. Success Metrics

| Metric | Target |
| --- | --- |
| Fight completion rate (career) | > 85% |
| Average fight length | 3-8 minutes |
| Quick Fight retention (session) | > 70% play 3+ fights |
| Player-reported "fairness" | > 80% positive |
| Submission finish rate | 15-25% of fights |
| KO/TKO rate | 30-40% of fights |
| Decision rate | 35-50% of fights |
| Crash-free sessions | > 99.5% |
| Frame rate (PC target) | 60 FPS stable |

---

## 17. Glossary

| Term | Definition |
| --- | --- |
| **Clinch** | Close-range grappling while standing |
| **Guard** | Bottom ground position with opponent between legs |
| **Gassed** | Low-stamina state with severe performance penalties |
| **Rocked** | Vulnerable state after accumulated head damage |
| **Sprawl** | Takedown defense by driving hips down |
| **Sweep** | Reversing position from the bottom |
| **Tap** | Submission acknowledgment |
| **TDD** | Takedown Defense |
| **TKO** | Technical Knockout (referee stoppage) |
| **Whiff** | A missed strike |

---

*End of Document*
