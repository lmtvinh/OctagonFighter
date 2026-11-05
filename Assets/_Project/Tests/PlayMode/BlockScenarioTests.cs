using System.Collections;
using NUnit.Framework;
using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Verifies the guard system in the shipped combat scene: a guarded opponent takes partial chip
    /// damage instead of full damage, the jab and cross produce different guard results, blocked
    /// hits enter a short block-impact reaction and push the defender back, a depleted guard breaks
    /// and restores a partial guard after the stun, and unguarded damage and defeat are unchanged.
    /// </summary>
    /// <remarks>
    /// The opponent in the shipped scene is passive, so the scenarios replace its intent source with
    /// a held-block source. The attacker is still driven through the real virtual keyboard and the
    /// real player intent source, and every strike resolves through the real controller, sweep, hit
    /// detector, registry, guard check, and health adapter.
    /// </remarks>
    public sealed class BlockScenarioTests : CombatSceneFixture
    {
        private const float CloseDistance = 0.76f;
        private const float MissDistance = 1.20f;

        [UnityTest]
        public IEnumerator JabAgainstBlockingDefenderTakesChipDamageAndLosesGuard()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_jab");
            yield return BlockDefender();

            float healthBefore = Defender.Snapshot.CurrentHealth;
            float guardBefore = Defender.CurrentGuard;
            float staminaBefore = Defender.Stamina.CurrentStamina;
            yield return Press(Key.J);
            yield return Sample(1.0f);
            EndScenario();

            Debug.Log(
                $"[P52] block jab: blocked={BlockedHits.Count} drops={DamageEvents.Count} " +
                $"reactions={DefenderReactionEntries} health={Defender.Snapshot.CurrentHealth:0.##} " +
                $"guard={Defender.CurrentGuard:0.##} state={Defender.Snapshot.State}");

            Assert.AreEqual(1, BlockedHits.Count, "The jab must be reported as blocked exactly once.");
            Assert.AreEqual(2f, healthBefore - Defender.Snapshot.CurrentHealth, 0.01f,
                "A blocked jab must chip 25% of its 8 damage.");
            Assert.AreEqual(11.875f, guardBefore - Defender.CurrentGuard, 0.01f,
                "The jab's authored 10 guard damage must be amplified by its impact strength.");
            Assert.AreEqual(3f, staminaBefore - Defender.Stamina.CurrentStamina, 0.01f,
                "Blocking the jab must spend its authored 3 block stamina.");
            Assert.AreEqual(0, DefenderReactionEntries, "A blocked jab must not start a hit reaction.");
            Assert.IsFalse(Defender.IsReactingToHit, "The defender must not be in hit stun after a block.");
            Assert.AreEqual(
                FighterStateId.Blocking,
                Defender.Snapshot.State,
                "The defender must still be guarding after the block-impact reaction.");
        }

        [UnityTest]
        public IEnumerator CrossAgainstBlockingDefenderTakesMoreChipAndGuardDamageThanJab()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_cross");
            yield return BlockDefender();

            float healthBefore = Defender.Snapshot.CurrentHealth;
            float guardBefore = Defender.CurrentGuard;
            float staminaBefore = Defender.Stamina.CurrentStamina;
            yield return Press(Key.K);
            yield return Sample(1.4f);
            EndScenario();

            Debug.Log(
                $"[P52] block cross: blocked={BlockedHits.Count} drops={DamageEvents.Count} " +
                $"reactions={DefenderReactionEntries} health={Defender.Snapshot.CurrentHealth:0.##} " +
                $"guard={Defender.CurrentGuard:0.##} state={Defender.Snapshot.State}");

            Assert.AreEqual(1, BlockedHits.Count, "The cross must be reported as blocked exactly once.");
            Assert.AreEqual(3.75f, healthBefore - Defender.Snapshot.CurrentHealth, 0.01f,
                "A blocked cross must chip 25% of its 15 damage.");
            Assert.AreEqual(38.4f, guardBefore - Defender.CurrentGuard, 0.01f,
                "The cross's 24 guard damage must be amplified by its heavier impact.");
            Assert.AreEqual(7f, staminaBefore - Defender.Stamina.CurrentStamina, 0.01f);
            Assert.AreEqual(0, DefenderReactionEntries, "A blocked cross must not start a hit reaction.");
            Assert.AreEqual(FighterStateId.Blocking, Defender.Snapshot.State);
        }

        [UnityTest]
        public IEnumerator BlockedHitEntersBlockImpactBeforeReturningToBlocking()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_impact");
            yield return BlockDefender();

            yield return Press(Key.J);
            yield return WaitForDefenderState(FighterStateId.BlockImpact, 0.6f);

            Assert.IsTrue(
                Defender.IsBlocking,
                "The guard must stay active during the block-impact reaction.");
            Assert.Greater(
                Defender.CurrentGuard,
                0f,
                "A light blocked hit must not deplete the full guard.");

            yield return WaitForDefenderState(FighterStateId.Blocking, 0.5f);
            EndScenario();

            Assert.IsTrue(Defender.IsBlocking, "The held guard must resume after the impact.");
        }

        [UnityTest]
        public IEnumerator BlockedJabPushesTheDefenderAwayFromTheAttacker()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_pushback");
            yield return BlockDefender();

            Vector3 start = Defender.transform.position;
            Vector3 away = -Defender.transform.forward;
            away.y = 0f;
            away.Normalize();

            yield return Press(Key.J);
            yield return Sample(0.8f);
            EndScenario();

            Vector3 displacement = Defender.transform.position - start;
            float pushedAway = Vector3.Dot(displacement, away);

            Debug.Log(
                $"[P52] block pushback: away={pushedAway:0.###} m displacement={displacement} " +
                $"state={Defender.Snapshot.State}");

            Assert.AreEqual(1, BlockedHits.Count);
            Assert.Greater(
                pushedAway,
                0.2f,
                "A blocked jab must push the defender roughly the authored 0.28 m away.");
            Assert.Less(pushedAway, 0.5f, "Blocked pushback must stay controlled.");
            Assert.AreEqual(FighterStateId.Blocking, Defender.Snapshot.State);
        }

        [UnityTest]
        public IEnumerator BlockedStrikesDoNotStartAHitReaction()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_no_reaction");
            yield return BlockDefender();

            yield return Press(Key.J);
            yield return Sample(1.0f);
            yield return WaitForIdle();
            yield return new WaitForSeconds(0.2f);

            // The blocked jab pushes the defender out of the cross's reach, so restore the
            // measured spacing before the second guarded strike.
            PlaceFighters(CloseDistance);
            yield return null;

            yield return Press(Key.K);
            yield return Sample(1.4f);
            EndScenario();

            Debug.Log(
                $"[P52] block reaction: blocked={BlockedHits.Count} drops={DamageEvents.Count} " +
                $"reactions={DefenderReactionEntries} activeReaction={Defender.ActiveHitReaction.Region}");

            Assert.AreEqual(2, BlockedHits.Count, "Both guarded strikes must be reported as blocked.");
            Assert.AreEqual(2, DamageEvents.Count, "Each blocked strike must chip exactly once.");
            Assert.AreEqual(0, DefenderReactionEntries, "No blocked strike may start a reaction.");
            Assert.AreEqual(default(HitReactionInfo), Defender.ActiveHitReaction);
            Assert.IsFalse(Defender.IsReactingToHit);
        }

        [UnityTest]
        public IEnumerator RepeatedContactTicksProduceExactlyOneBlockEventPerStrike()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_repeat_contact");
            yield return BlockDefender();

            int attackFrames = 0;
            yield return Press(Key.J);
            yield return Sample(
                1.0f,
                default,
                0f,
                _ =>
                {
                    if (Attacker.Snapshot.State == FighterStateId.LightPunch)
                    {
                        attackFrames++;
                    }
                });
            EndScenario();

            Debug.Log(
                $"[P52] block repeat: attackFrames={attackFrames} blocked={BlockedHits.Count} " +
                $"drops={DamageEvents.Count}");

            Assert.Greater(
                attackFrames,
                5,
                "The jab must spend several ticks in its active window for this test to be meaningful.");
            Assert.AreEqual(
                1,
                BlockedHits.Count,
                "Repeated contact ticks within one strike must produce one block event, not one per tick.");
            Assert.AreEqual(1, DamageEvents.Count, "Chip damage must apply exactly once per strike.");
        }

        [UnityTest]
        public IEnumerator MissAgainstBlockingDefenderStaysAMiss()
        {
            PlaceFighters(MissDistance);
            yield return Settle();
            BeginScenario("block_miss");
            yield return BlockDefender();

            yield return Press(Key.J);
            yield return Sample(1.0f);
            EndScenario();

            Debug.Log(
                $"[P52] block miss: blocked={BlockedHits.Count} drops={DamageEvents.Count} " +
                $"reactions={DefenderReactionEntries} state={Defender.Snapshot.State}");

            Assert.AreEqual(0, BlockedHits.Count, "A strike that never reaches the guard is still a miss.");
            Assert.AreEqual(0, DamageEvents.Count, "A missed strike must not damage the guard.");
            Assert.AreEqual(0, DefenderReactionEntries);
            Assert.AreEqual(FighterStateId.Blocking, Defender.Snapshot.State);
        }

        [UnityTest]
        public IEnumerator JabAgainstNonBlockingDefenderStillDealsEight()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_jab_open");

            yield return Press(Key.J);
            yield return Sample(1.0f);
            EndScenario();

            Debug.Log(
                $"[P52] open jab: drops={DamageEvents.Count} " +
                $"damage={(DamageEvents.Count > 0 ? DamageEvents[0].Amount : 0)} " +
                $"blocked={BlockedHits.Count} reactions={DefenderReactionEntries}");

            Assert.AreEqual(1, DamageEvents.Count, "An unguarded jab must still land exactly once.");
            Assert.AreEqual(8, DamageEvents[0].Amount, "The jab's authored 8 damage must not change.");
            Assert.AreEqual(0, BlockedHits.Count, "An unguarded strike must not report a block.");
            Assert.AreEqual(1, DefenderReactionEntries, "An unguarded jab must still start a reaction.");
        }

        [UnityTest]
        public IEnumerator CrossAgainstNonBlockingDefenderStillDealsFifteen()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_cross_open");

            yield return Press(Key.K);
            yield return Sample(1.4f);
            EndScenario();

            Debug.Log(
                $"[P52] open cross: drops={DamageEvents.Count} " +
                $"damage={(DamageEvents.Count > 0 ? DamageEvents[0].Amount : 0)} " +
                $"blocked={BlockedHits.Count} reactions={DefenderReactionEntries}");

            Assert.AreEqual(1, DamageEvents.Count, "An unguarded cross must still land exactly once.");
            Assert.AreEqual(15, DamageEvents[0].Amount, "The cross's authored 15 damage must not change.");
            Assert.AreEqual(0, BlockedHits.Count, "An unguarded strike must not report a block.");
            Assert.AreEqual(1, DefenderReactionEntries, "An unguarded cross must still start a reaction.");
        }

        [UnityTest]
        public IEnumerator ReleasingBlockAllowsTheNextStrikeToDealFullDamage()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_release");
            yield return BlockDefender();

            yield return Press(Key.J);
            yield return Sample(1.0f);
            Assert.AreEqual(1, BlockedHits.Count, "The first jab must be blocked.");
            Assert.AreEqual(98f, Defender.Snapshot.CurrentHealth, 0.01f, "The blocked jab must chip 2 health.");

            SetDefenderIntentSource(NullIntentSource.Instance);
            yield return WaitForDefenderState(FighterStateId.Idle);
            yield return WaitForIdle();
            yield return new WaitForSeconds(0.2f);

            // The first blocked jab pushed the defender out of reach; restore the measured spacing.
            PlaceFighters(CloseDistance);
            yield return null;

            yield return Press(Key.J);
            yield return Sample(1.0f);
            EndScenario();

            Debug.Log(
                $"[P52] block release: blocked={BlockedHits.Count} drops={DamageEvents.Count} " +
                $"health={Defender.Snapshot.CurrentHealth:0.##} state={Defender.Snapshot.State}");

            Assert.AreEqual(1, BlockedHits.Count, "Only the guarded jab may be reported as blocked.");
            Assert.AreEqual(2, DamageEvents.Count, "The chip and the landed jab must each report once.");
            Assert.AreEqual(8, DamageEvents[1].Amount);
            Assert.AreEqual(90f, Defender.Snapshot.CurrentHealth, 0.01f,
                "The released guard must take full jab damage after the 2 chip damage.");
        }

        [UnityTest]
        public IEnumerator ReleasingBlockBeforeTheStrikeLandsTakesFullDamage()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_release_before_impact");
            yield return BlockDefender();

            // The jab's contact window opens at 0.26 s. Release the guard during startup, well
            // before contact, so the same strike must resolve against an open defender.
            yield return Press(Key.J);
            yield return Sample(0.12f);

            Assert.AreEqual(0, BlockedHits.Count, "The strike must not have reached the guard yet.");
            Assert.AreEqual(0, DamageEvents.Count, "The strike must still be in startup.");

            SetDefenderIntentSource(NullIntentSource.Instance);
            yield return WaitForDefenderState(FighterStateId.Idle);
            yield return Sample(0.8f);
            EndScenario();

            Debug.Log(
                $"[A3] block release before impact: blocked={BlockedHits.Count} " +
                $"drops={DamageEvents.Count} health={Defender.Snapshot.CurrentHealth:0.##} " +
                $"reactions={DefenderReactionEntries}");

            Assert.AreEqual(0, BlockedHits.Count, "A guard released before contact must not block.");
            Assert.AreEqual(1, DamageEvents.Count, "The strike must land exactly once against the open defender.");
            Assert.AreEqual(8, DamageEvents[0].Amount, "The jab's full authored damage must apply.");
            Assert.AreEqual(92f, Defender.Snapshot.CurrentHealth, 0.01f);
            Assert.AreEqual(1, DefenderReactionEntries, "An unguarded hit must still start its reaction.");
        }

        [UnityTest]
        public IEnumerator HeldBlockOutranksPunchTapInThePlayerIntentSource()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_input_priority");

            yield return Hold(Key.Space);
            yield return WaitForState(FighterStateId.Blocking, 1.5f);

            // The punch tap arrives while the block button is still down. The player intent
            // source must keep reporting the held guard, so the tap is discarded instead of
            // dropping the guard and starting a strike.
            yield return PressWhileHolding(Key.Space, Key.J);
            yield return Sample(0.8f);

            Assert.AreEqual(
                FighterStateId.Blocking,
                Attacker.Snapshot.State,
                "A punch tap must not drop a held guard.");
            Assert.AreEqual(
                100f,
                Attacker.Stamina.CurrentStamina,
                0.01f,
                "No strike may spend stamina while the guard is held.");
            Assert.AreEqual(0, DamageEvents.Count, "No strike may be thrown from the guard.");
            Assert.AreEqual(0, DefenderReactionEntries, "No strike may reach the defender.");

            yield return Release(Key.Space);
            yield return WaitForIdle();
            EndScenario();
        }

        [UnityTest]
        public IEnumerator LethalDamageAgainstNonBlockingDefenderStillDefeats()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_lethal_open");

            float healthBefore = Defender.Snapshot.CurrentHealth;
            Defender.ApplyDamage(new DamageEvent(
                Attacker.Id,
                Defender.Id,
                healthBefore,
                DamageType.Impact,
                HitRegion.Head,
                TickId.Zero));
            yield return null;

            EndScenario();

            Debug.Log(
                $"[P52] lethal open: health={Defender.Snapshot.CurrentHealth:0.##} " +
                $"state={Defender.Snapshot.State} defeats={DefenderDefeatEntries}");

            Assert.AreEqual(0f, Defender.Snapshot.CurrentHealth);
            Assert.AreEqual(
                FighterStateId.Defeated,
                Defender.Snapshot.State,
                "A lethal unguarded hit must still enter the terminal defeat state.");
            Assert.AreEqual(1, DefenderDefeatEntries, "Defeat must be announced exactly once.");
            Assert.IsFalse(Defender.IsBlocking);
        }

        [UnityTest]
        public IEnumerator LethalDamageAgainstBlockingDefenderIsMitigatedToChipAndBreaksGuard()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_lethal_guard");
            yield return BlockDefender();

            float healthBefore = Defender.Snapshot.CurrentHealth;
            float guardBefore = Defender.CurrentGuard;
            DamageResult blocked = Defender.ApplyDamage(new DamageEvent(
                Attacker.Id,
                Defender.Id,
                healthBefore,
                DamageType.Impact,
                HitRegion.Head,
                TickId.Zero));
            yield return null;

            Assert.AreEqual(
                25f,
                blocked.AppliedAmount,
                0.01f,
                "The guard must convert lethal damage into the authored 25% chip.");
            Assert.AreEqual(healthBefore - 25f, Defender.Snapshot.CurrentHealth, 0.01f);
            Assert.AreEqual(0f, Defender.CurrentGuard, 0.01f);
            Assert.IsTrue(Defender.IsBroken, "A 100-damage blocked strike must break the full guard.");
            Assert.AreEqual(guardBefore - 100f, Defender.CurrentGuard, 0.01f);
            Assert.AreEqual(FighterStateId.GuardBreak, Defender.Snapshot.State);
            Assert.IsFalse(Defender.IsBlocking, "A broken guard must stop blocking until the stun ends.");
            Assert.AreEqual(0, DefenderDefeatEntries, "Chip damage must not defeat a fighter at 75 health.");

            yield return WaitForDefenderState(FighterStateId.Blocking, 2.0f);

            Assert.IsFalse(Defender.IsBroken, "The guard must recover from the break after the stun.");
            Assert.AreEqual(
                35f,
                Defender.CurrentGuard,
                0.01f,
                "Leaving guard break must restore the authored 35% guard.");

            SetDefenderIntentSource(NullIntentSource.Instance);
            yield return WaitForDefenderState(FighterStateId.Idle);

            Defender.ApplyDamage(new DamageEvent(
                Attacker.Id,
                Defender.Id,
                Defender.Snapshot.CurrentHealth,
                DamageType.Impact,
                HitRegion.Head,
                TickId.Zero));
            yield return null;
            EndScenario();

            Assert.AreEqual(
                FighterStateId.Defeated,
                Defender.Snapshot.State,
                "A lethal hit on the released guard must still defeat the fighter.");
            Assert.AreEqual(1, DefenderDefeatEntries);
        }

        [UnityTest]
        public IEnumerator ThreeBlockedCrossesBreakTheGuardAndRestorePartialGuard()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("block_break");
            yield return BlockDefender();

            yield return Press(Key.K);
            yield return Sample(1.4f);

            // Each blocked cross pushes the defender back out of reach; restore the measured
            // spacing so all three strikes can reach the guard.
            PlaceFighters(CloseDistance);
            yield return null;
            yield return Press(Key.K);
            yield return Sample(1.4f);

            Assert.AreEqual(2, BlockedHits.Count, "The first two crosses must be blocked.");
            PlaceFighters(CloseDistance);
            yield return null;
            yield return Press(Key.K);
            yield return Sample(0.5f);

            Debug.Log(
                $"[P52] block break: blocked={BlockedHits.Count} guard={Defender.CurrentGuard:0.##} " +
                $"broken={Defender.IsBroken} state={Defender.Snapshot.State}");

            Assert.AreEqual(3, BlockedHits.Count, "All three crosses must reach the guard.");
            Assert.IsTrue(Defender.IsBroken, "The third cross must deplete the guard.");
            Assert.AreEqual(0f, Defender.CurrentGuard, 0.01f);
            Assert.AreEqual(FighterStateId.GuardBreak, Defender.Snapshot.State);
            Assert.IsFalse(Defender.IsBlocking);

            yield return Sample(1.0f);

            Assert.IsFalse(Defender.IsBroken);
            Assert.AreEqual(35f, Defender.CurrentGuard, 0.01f);
            Assert.AreEqual(FighterStateId.Blocking, Defender.Snapshot.State);
            EndScenario();
        }

        [UnityTest]
        public IEnumerator BlockingFighterMovesSlowerThanWalking()
        {
            // Wide spacing: the open walk covers ~1.4 m, so a close spawn would end the sample at
            // the attacker's capsule and understate the unguarded speed.
            PlaceFighters(5.0f);
            yield return Settle();

            SetDefenderIntentSource(new HeldBlockAndForwardIntentSource());
            yield return WaitForDefenderState(FighterStateId.Blocking);

            BeginScenario("block_move_guarded");
            Vector3 guardedStart = Defender.transform.position;
            yield return Sample(1.0f);
            float guardedDistance = Vector3.Distance(guardedStart, Defender.transform.position);
            EndScenario();

            SetDefenderIntentSource(NullIntentSource.Instance);
            yield return WaitForDefenderState(FighterStateId.Idle);

            BeginScenario("block_move_open");
            SetDefenderIntentSource(new ForwardOnlyIntentSource());
            Vector3 openStart = Defender.transform.position;
            yield return Sample(1.0f);
            float openDistance = Vector3.Distance(openStart, Defender.transform.position);
            EndScenario();

            Debug.Log(
                $"[A3] block move: guarded={guardedDistance:0.###} m, open={openDistance:0.###} m");

            Assert.Greater(
                guardedDistance,
                0.15f,
                "A guarded fighter must still be able to move.");
            Assert.Less(
                guardedDistance,
                openDistance * 0.7f,
                "A guarded fighter must move markedly slower than an unguarded walker.");
        }

        private IEnumerator BlockDefender()
        {
            SetDefenderIntentSource(new HeldBlockIntentSource());
            yield return WaitForDefenderState(FighterStateId.Blocking);
            yield return WaitForDefenderAnimatorState("Blocking");
            Assert.IsTrue(Defender.IsBlocking, "The opponent must be guarding before the strike is thrown.");
        }

        /// <summary>
        /// Intent source that holds the block command for as long as it is installed.
        /// </summary>
        private sealed class HeldBlockIntentSource : IFighterIntentSource
        {
            public bool TryGetIntent(TickId tick, out FighterIntent intent)
            {
                intent = new FighterIntent(
                    Float2.Zero,
                    FighterCommand.Block,
                    true,
                    true,
                    MovementGait.Walk,
                    tick);
                return true;
            }
        }

        /// <summary>
        /// Intent source that walks toward the opponent while holding the guard.
        /// </summary>
        private sealed class HeldBlockAndForwardIntentSource : IFighterIntentSource
        {
            public bool TryGetIntent(TickId tick, out FighterIntent intent)
            {
                intent = new FighterIntent(
                    new Float2(0f, 1f),
                    FighterCommand.Block,
                    true,
                    true,
                    MovementGait.Walk,
                    tick);
                return true;
            }
        }

        /// <summary>
        /// Intent source that walks toward the opponent without guarding.
        /// </summary>
        private sealed class ForwardOnlyIntentSource : IFighterIntentSource
        {
            public bool TryGetIntent(TickId tick, out FighterIntent intent)
            {
                intent = new FighterIntent(
                    new Float2(0f, 1f),
                    FighterCommand.None,
                    false,
                    false,
                    MovementGait.Walk,
                    tick);
                return true;
            }
        }
    }
}
