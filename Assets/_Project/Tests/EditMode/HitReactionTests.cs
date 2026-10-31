using System.Collections.Generic;
using NUnit.Framework;
using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Data.Fighters;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.Stamina;
using OctagonFighter.Domain.StateMachine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OctagonFighter.Tests.EditMode
{
    /// <summary>
    /// Verifies the Phase 5.1 hit-reaction foundation: confirmed hits start a region-specific
    /// reaction, misses and non-impact damage never do, hit stun owns movement and attack
    /// authority, and control returns when the authored duration elapses.
    /// </summary>
    public sealed class HitReactionTests
    {
        private const float TickSeconds = 1f / 60f;
        private const string FighterDefinitionPath =
            "Assets/_Project/ScriptableObjects/Fighters/FD_Boxer.asset";
        private const string AnimatorControllerPath =
            "Assets/_Project/Art/Animations/AC_Fighter_Base.controller";

        private static readonly FighterId AttackerId = new FighterId("attacker");

        private FighterData _data;
        private CombatClock _clock;
        private ScriptedIntentSource _intents;
        private HealthModel _health;
        private FighterRuntime _runtime;

        [SetUp]
        public void SetUp()
        {
            _data = CreateFighterData(AttackBook.CreateDefault());
            _clock = new CombatClock();
            _intents = new ScriptedIntentSource();
            _health = new HealthModel(_data.Health);

            _runtime = new FighterRuntime(
                _data,
                _health,
                new StaminaModel(_data.Stamina),
                FoundationStateMachineFactory.Create(),
                _intents,
                _clock);

            // Leave the spawn lock so every test starts from a controllable idle.
            TickUntilIdle();
        }

        [TearDown]
        public void TearDown()
        {
            _runtime?.Dispose();
            _runtime = null;
        }

        [Test]
        public void ConfirmedHeadHitStartsAHeadReaction()
        {
            Assert.IsTrue(React(HitRegion.Head));

            Assert.AreEqual(FighterStateId.HitReaction, _runtime.StateMachine.CurrentStateId);
            Assert.IsTrue(_runtime.IsReactingToHit);
            Assert.AreEqual(HitRegion.Head, _runtime.ActiveHitReaction.Region);
        }

        [Test]
        public void ConfirmedBodyHitStartsABodyReaction()
        {
            Assert.IsTrue(React(HitRegion.Body));

            Assert.AreEqual(FighterStateId.HitReaction, _runtime.StateMachine.CurrentStateId);
            Assert.AreEqual(HitRegion.Body, _runtime.ActiveHitReaction.Region);
        }

        [Test]
        public void ReactionStartsOnlyFromAppliedImpactDamage()
        {
            var reactions = new List<HitReactionInfo>();
            _runtime.HitReactionStarted += reactions.Add;

            DamageResult miss = _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                0f,
                DamageType.Impact,
                HitRegion.Head,
                _clock.CurrentTick));
            Assert.AreEqual(0f, miss.AppliedAmount);
            Assert.AreEqual(FighterStateId.Idle, _runtime.StateMachine.CurrentStateId);
            Assert.IsEmpty(reactions, "A zero-damage application must not start a reaction.");

            DamageResult wear = _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                5f,
                DamageType.Accumulated,
                HitRegion.Head,
                _clock.CurrentTick));
            Assert.Greater(wear.AppliedAmount, 0f);
            Assert.AreEqual(FighterStateId.Idle, _runtime.StateMachine.CurrentStateId);
            Assert.IsEmpty(reactions, "Accumulated wear is not a confirmed strike.");

            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                8f,
                DamageType.Impact,
                HitRegion.Body,
                _clock.CurrentTick));
            Assert.AreEqual(1, reactions.Count, "One applied impact hit must start exactly one reaction.");
            Assert.AreEqual(HitRegion.Body, reactions[0].Region);
        }

        [Test]
        public void MovementIntentCannotInterruptHitStun()
        {
            React(HitRegion.Head);

            _intents.Next = new FighterIntent(
                new Float2(1f, 1f),
                FighterCommand.None,
                false,
                false,
                MovementGait.Run,
                _clock.CurrentTick);
            Tick(TickSeconds);
            Tick(TickSeconds);

            Assert.AreEqual(
                FighterStateId.HitReaction,
                _runtime.StateMachine.CurrentStateId,
                "Movement input must not cancel hit stun.");
            Assert.AreEqual(MovementAuthority.None, _runtime.CurrentMovementAuthority);
            Assert.AreEqual(FacingAuthority.Locked, _runtime.CurrentFacingAuthority);
        }

        [Test]
        public void AttackIntentCannotInterruptHitStun()
        {
            React(HitRegion.Body);

            _intents.Next = new FighterIntent(
                Float2.Zero,
                FighterCommand.LightPunch,
                true,
                false,
                MovementGait.Walk,
                _clock.CurrentTick);
            Tick(TickSeconds);
            _intents.Next = FighterIntent.None;

            Assert.AreEqual(
                FighterStateId.HitReaction,
                _runtime.StateMachine.CurrentStateId,
                "Attack input must not cancel hit stun.");
            Assert.IsFalse(_runtime.Attacks.IsOnCooldown(AttackSlot.Light));

            // The 0.25 s input buffer expires before the 0.35 s stun ends, so no attack starts.
            TickUntilIdle();
            Assert.AreEqual(FighterStateId.Idle, _runtime.StateMachine.CurrentStateId);
        }

        [Test]
        public void ControlReturnsWhenTheReactionEnds()
        {
            React(HitRegion.Head);
            float duration = _data.Movement.HitReactionDurationSeconds;

            Tick(duration - (TickSeconds * 2f));
            Assert.AreEqual(FighterStateId.HitReaction, _runtime.StateMachine.CurrentStateId);

            TickUntilIdle();
            Assert.AreEqual(FighterStateId.Idle, _runtime.StateMachine.CurrentStateId);
            Assert.IsFalse(_runtime.IsReactingToHit);
            Assert.AreEqual(default(HitReactionInfo), _runtime.ActiveHitReaction);
            Assert.AreEqual(FacingAuthority.Free, _runtime.CurrentFacingAuthority);

            _intents.Next = new FighterIntent(
                new Float2(0f, 1f),
                FighterCommand.None,
                false,
                false,
                MovementGait.Walk,
                _clock.CurrentTick);
            Tick(TickSeconds);
            Assert.AreEqual(
                FighterStateId.Walk,
                _runtime.StateMachine.CurrentStateId,
                "Movement must work again once the reaction is over.");
        }

        [Test]
        public void SecondConfirmedHitRefreshesTheReactionInsteadOfStacking()
        {
            React(HitRegion.Head);
            Tick(0.2f);

            Assert.IsTrue(React(HitRegion.Body), "A new confirmed hit must refresh the running reaction.");
            Assert.AreEqual(HitRegion.Body, _runtime.ActiveHitReaction.Region);

            Tick(0.2f);
            Assert.AreEqual(
                FighterStateId.HitReaction,
                _runtime.StateMachine.CurrentStateId,
                "The refreshed reaction must restart its timer, not inherit the first hit's time.");

            TickUntilIdle();
            Assert.AreEqual(FighterStateId.Idle, _runtime.StateMachine.CurrentStateId);
        }

        [Test]
        public void OneReactionIsStartedPerConfirmedHit()
        {
            int started = 0;
            _runtime.HitReactionStarted += _ => started++;

            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                8f,
                DamageType.Impact,
                HitRegion.Head,
                _clock.CurrentTick));
            TickUntilIdle();
            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                15f,
                DamageType.Impact,
                HitRegion.Body,
                _clock.CurrentTick));
            TickUntilIdle();

            Assert.AreEqual(2, started, "Each confirmed hit must start exactly one reaction.");
        }

        [Test]
        public void DefeatedFighterDoesNotReact()
        {
            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                _data.Health.MaxHealth,
                DamageType.Impact,
                HitRegion.Head,
                _clock.CurrentTick));
            Tick(TickSeconds);

            Assert.AreEqual(FighterStateId.Defeated, _runtime.StateMachine.CurrentStateId);
            Assert.IsFalse(
                _runtime.ReactToHit(HitRegion.Body, _clock.CurrentTick),
                "Defeat is terminal; a defeated fighter must not enter a reaction.");
            Assert.AreEqual(FighterStateId.Defeated, _runtime.StateMachine.CurrentStateId);
        }

        [Test]
        public void LethalHitFromIdleEntersDefeatWithoutAReaction()
        {
            var reactions = new List<HitReactionInfo>();
            int defeatTransitions = 0;
            _runtime.HitReactionStarted += reactions.Add;
            _runtime.StateChanged += transition =>
            {
                if (transition.CurrentState == FighterStateId.Defeated)
                {
                    defeatTransitions++;
                }
            };

            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                _data.Health.MaxHealth,
                DamageType.Impact,
                HitRegion.Head,
                _clock.CurrentTick));

            Assert.AreEqual(
                FighterStateId.Defeated,
                _runtime.StateMachine.CurrentStateId,
                "A killing blow must enter the terminal Defeat state immediately.");
            Assert.IsEmpty(reactions, "A killing blow must never start a normal hit reaction.");
            Assert.IsFalse(_runtime.IsReactingToHit, "A killing blow must not leave the fighter in HitReaction.");
            Assert.AreEqual(default(HitReactionInfo), _runtime.ActiveHitReaction);
            Assert.AreEqual(1, defeatTransitions, "A lethal hit must raise exactly one defeat transition.");

            for (int i = 0; i < 5; i++)
            {
                Tick(TickSeconds);
            }

            Assert.AreEqual(FighterStateId.Defeated, _runtime.StateMachine.CurrentStateId);
            Assert.AreEqual(1, defeatTransitions, "Defeat must stay terminal with no duplicate transitions.");
            Assert.IsEmpty(reactions);
        }

        [Test]
        public void LethalHitDuringReactionEndsTheReactionAndEntersDefeat()
        {
            Assert.IsTrue(React(HitRegion.Head));
            Tick(0.1f);

            var reactions = new List<HitReactionInfo>();
            int defeatTransitions = 0;
            _runtime.HitReactionStarted += reactions.Add;
            _runtime.StateChanged += transition =>
            {
                if (transition.CurrentState == FighterStateId.Defeated)
                {
                    defeatTransitions++;
                }
            };

            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                _data.Health.MaxHealth,
                DamageType.Impact,
                HitRegion.Body,
                _clock.CurrentTick));

            Assert.AreEqual(
                FighterStateId.Defeated,
                _runtime.StateMachine.CurrentStateId,
                "A killing blow must replace the running reaction with the terminal Defeat state.");
            Assert.IsFalse(_runtime.IsReactingToHit);
            Assert.AreEqual(default(HitReactionInfo), _runtime.ActiveHitReaction);
            Assert.IsEmpty(reactions, "The lethal blow must not refresh the running reaction.");
            Assert.AreEqual(1, defeatTransitions);

            for (int i = 0; i < 5; i++)
            {
                Tick(TickSeconds);
            }

            Assert.AreEqual(FighterStateId.Defeated, _runtime.StateMachine.CurrentStateId);
            Assert.AreEqual(1, defeatTransitions, "Defeat must stay terminal with no duplicate transitions.");
        }

        [Test]
        public void NonlethalHitOnTheKillingBoundaryStillReacts()
        {
            var reactions = new List<HitReactionInfo>();
            _runtime.HitReactionStarted += reactions.Add;

            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                _data.Health.MaxHealth - 1f,
                DamageType.Impact,
                HitRegion.Body,
                _clock.CurrentTick));

            Assert.IsFalse(_runtime.Snapshot.IsKnockedOut, "The fighter must survive the boundary hit.");
            Assert.AreEqual(
                FighterStateId.HitReaction,
                _runtime.StateMachine.CurrentStateId,
                "A survivable hit must still start its region reaction.");
            Assert.AreEqual(HitRegion.Body, _runtime.ActiveHitReaction.Region);
            Assert.AreEqual(1, reactions.Count, "The survivable hit must start exactly one reaction.");

            TickUntilIdle();
            Assert.AreEqual(FighterStateId.Idle, _runtime.StateMachine.CurrentStateId);
        }

        [Test]
        public void KillingBlowThroughTheDamagedEventOrderNeverStartsAReaction()
        {
            // Reproduces the Unity health adapter's ordering: the health model raises KnockedOut
            // first, then the adapter raises Damaged and the controller forwards it to the runtime
            // before the next Tick. Defeat must already be decided at that point.
            var reactions = new List<HitReactionInfo>();
            _runtime.HitReactionStarted += reactions.Add;

            _health.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                _data.Health.MaxHealth,
                DamageType.Impact,
                HitRegion.Head,
                _clock.CurrentTick));

            Assert.IsTrue(_health.IsKnockedOut, "The lethal damage must knock the fighter out.");
            Assert.IsFalse(
                _runtime.ReactToHit(HitRegion.Head, _clock.CurrentTick),
                "The adapter's Damaged handler must not start a reaction for a killing blow.");
            Assert.AreEqual(
                FighterStateId.Defeated,
                _runtime.StateMachine.CurrentStateId,
                "The adapter's KnockedOut-then-Damaged ordering must end in Defeat.");
            Assert.IsEmpty(reactions);
            Assert.IsFalse(_runtime.IsReactingToHit);
            Assert.AreEqual(default(HitReactionInfo), _runtime.ActiveHitReaction);

            Tick(TickSeconds);
            Assert.AreEqual(FighterStateId.Defeated, _runtime.StateMachine.CurrentStateId);
            Assert.IsEmpty(reactions);
        }

        [Test]
        public void ShippedStrikesReachBothReactionRegions()
        {
            var definition = AssetDatabase.LoadAssetAtPath<FighterDefinition>(FighterDefinitionPath);
            Assert.IsNotNull(definition, $"Missing fighter definition at {FighterDefinitionPath}.");

            FighterData data = definition.CreateRuntimeData();

            Assert.AreEqual(
                HitRegion.Head,
                data.Attacks.Light.Region,
                "The shipped jab must remain a head strike.");
            Assert.AreEqual(
                HitRegion.Body,
                data.Attacks.Heavy.Region,
                "The shipped cross must be authored as a body strike so body reactions are reachable.");
        }

        [Test]
        public void ReactionAnimatorStatesUseTheRetargetedImpactClip()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimatorControllerPath);
            Assert.IsNotNull(controller, $"Missing animator controller at {AnimatorControllerPath}.");

            AnimatorState head = FindState(controller, "HitReactionHead");
            AnimatorState body = FindState(controller, "HitReactionBody");

            Assert.IsNotNull(head, "The animator must expose a HitReactionHead state.");
            Assert.IsNotNull(body, "The animator must expose a HitReactionBody state.");
            Assert.IsNotNull(head.motion, "The head reaction state must have a motion.");
            Assert.IsNotNull(body.motion, "The body reaction state must have a motion.");

            var headClip = head.motion as AnimationClip;
            Assert.IsNotNull(headClip, "The head reaction must present an animation clip.");
            Assert.IsTrue(
                headClip.isHumanMotion,
                "The impact clip must be imported as humanoid so the fighter's humanoid Animator " +
                "can play it.");
            Assert.That(
                headClip.length,
                Is.EqualTo(1.3f).Within(0.01f),
                "The impact clip's measured length must not drift.");
            Assert.Less(
                body.speed,
                head.speed,
                "The body reaction must stay visually distinct while no body-impact clip ships.");
        }

        private static AnimatorState FindState(AnimatorController controller, string stateName)
        {
            foreach (ChildAnimatorState child in controller.layers[0].stateMachine.states)
            {
                if (child.state.name == stateName)
                {
                    return child.state;
                }
            }

            return null;
        }

        private bool React(HitRegion region)
        {
            return _runtime.ReactToHit(region, _clock.CurrentTick);
        }

        private void TickUntilIdle()
        {
            for (int i = 0; i < 240; i++)
            {
                if (_runtime.StateMachine.CurrentStateId == FighterStateId.Idle)
                {
                    return;
                }

                Tick(TickSeconds);
            }

            Assert.Fail("The fighter never returned to idle.");
        }

        private void Tick(float deltaSeconds)
        {
            _clock.Advance();
            _runtime.Tick(deltaSeconds);
        }

        private static FighterData CreateFighterData(AttackBook attacks)
        {
            var tiers = new StaminaTierThresholds(0.75f, 0.5f, 0.25f);
            var stamina = new StaminaProfile(
                100f,
                8f,
                2f,
                tiers,
                StaminaTierModifiers.Default,
                new StaminaTierModifiers(0.65f, 0.7f, 0.75f, 0.5f));

            return new FighterData(
                new FighterId("test_fighter"),
                "Test Fighter",
                new FighterAttributes(50, 50, 50, 50, 50, 50, 50),
                new BodyProfile(1.8f, 1.8f, "Lightweight", FighterStance.Orthodox),
                new HealthProfile(100f, 100f, 100f, 80f),
                stamina,
                new MovementProfile(2.6f, 2.1f, 2.3f, 0.25f, 0.4f, 0.35f, 1.5f, 0.8f, 0.55f, 720f),
                attacks);
        }

        /// <summary>
        /// Intent source whose next intent is set explicitly by the test.
        /// </summary>
        private sealed class ScriptedIntentSource : IFighterIntentSource
        {
            public FighterIntent Next { get; set; } = FighterIntent.None;

            public bool TryGetIntent(TickId tick, out FighterIntent intent)
            {
                intent = Next;
                return true;
            }
        }
    }
}
