using NUnit.Framework;
using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.Stamina;
using OctagonFighter.Domain.StateMachine;

namespace OctagonFighter.Tests.EditMode
{
    /// <summary>
    /// Verifies the Phase 5.2 blocking foundation at the state-machine level: holding the block
    /// command enters and sustains the guard, releasing it restores control, and defeated,
    /// reacting, and attacking fighters cannot enter the guard or start strikes from it.
    /// </summary>
    public sealed class BlockingTests
    {
        private const float TickSeconds = 1f / 60f;
        private const int BufferExpiryTicks = 30;

        private static readonly FighterId AttackerId = new FighterId("attacker");

        private FighterData _data;
        private CombatClock _clock;
        private ScriptedIntentSource _intents;
        private HealthModel _health;
        private FighterRuntime _runtime;

        [SetUp]
        public void SetUp()
        {
            _data = CreateFighterData();
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
        public void HoldingBlockEntersBlocking()
        {
            _intents.Next = BlockIntent(pressed: true);
            Tick();

            Assert.AreEqual(FighterStateId.Blocking, _runtime.StateMachine.CurrentStateId);
            Assert.IsTrue(_runtime.IsBlocking);
            Assert.AreEqual(MovementAuthority.Limited, _runtime.CurrentMovementAuthority);
            Assert.AreEqual(FacingAuthority.Free, _runtime.CurrentFacingAuthority);
        }

        [Test]
        public void HoldingBlockSustainsBlockingAcrossTicks()
        {
            _intents.Next = BlockIntent(pressed: true);
            Tick();

            _intents.Next = BlockIntent(pressed: false);
            for (int i = 0; i < 30; i++)
            {
                Tick();
            }

            Assert.AreEqual(
                FighterStateId.Blocking,
                _runtime.StateMachine.CurrentStateId,
                "The guard must stay up for as long as the block command is held.");
            Assert.IsTrue(_runtime.IsBlocking);
        }

        [Test]
        public void ReleasingBlockExitsToIdle()
        {
            EnterBlocking();

            _intents.Next = FighterIntent.None;
            Tick();

            Assert.AreEqual(FighterStateId.Idle, _runtime.StateMachine.CurrentStateId);
            Assert.IsFalse(_runtime.IsBlocking);
            Assert.AreEqual(MovementAuthority.None, _runtime.CurrentMovementAuthority);
        }

        [Test]
        public void ReleasingBlockRestoresLocomotion()
        {
            EnterBlocking();

            _intents.Next = MoveIntent();
            Tick();

            Assert.AreEqual(FighterStateId.Walk, _runtime.StateMachine.CurrentStateId);
            Assert.AreEqual(MovementAuthority.Full, _runtime.CurrentMovementAuthority);
        }

        [Test]
        public void DefeatedFighterCannotEnterBlocking()
        {
            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                _data.Health.MaxHealth,
                DamageType.Impact,
                HitRegion.Head,
                _clock.CurrentTick));

            Assert.AreEqual(FighterStateId.Defeated, _runtime.StateMachine.CurrentStateId);

            _intents.Next = BlockIntent(pressed: true);
            for (int i = 0; i < 5; i++)
            {
                Tick();
            }

            Assert.AreEqual(
                FighterStateId.Defeated,
                _runtime.StateMachine.CurrentStateId,
                "Defeat is terminal; the guard must never start.");
            Assert.IsFalse(_runtime.IsBlocking);
        }

        [Test]
        public void DefeatedFighterCannotStartAttacks()
        {
            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                _data.Health.MaxHealth,
                DamageType.Impact,
                HitRegion.Head,
                _clock.CurrentTick));

            Assert.AreEqual(FighterStateId.Defeated, _runtime.StateMachine.CurrentStateId);
            float staminaBefore = _runtime.Stamina.CurrentStamina;

            foreach (FighterCommand command in new[]
            {
                FighterCommand.LightPunch,
                FighterCommand.HeavyPunch,
                FighterCommand.Dodge,
                FighterCommand.Block,
            })
            {
                _intents.Next = new FighterIntent(
                    Float2.Zero,
                    command,
                    true,
                    true,
                    MovementGait.Walk,
                    _clock.CurrentTick);
                for (int i = 0; i < 10; i++)
                {
                    Tick();
                }

                Assert.AreEqual(
                    FighterStateId.Defeated,
                    _runtime.StateMachine.CurrentStateId,
                    $"A defeated fighter must ignore '{command}' input.");
            }

            Assert.AreEqual(
                staminaBefore,
                _runtime.Stamina.CurrentStamina,
                0.001f,
                "A defeated fighter must not spend stamina.");
            Assert.IsFalse(_runtime.Attacks.HasBufferedAttack, "A defeated fighter must never buffer a strike.");
            Assert.IsFalse(_runtime.Attacks.IsOnCooldown(AttackSlot.Light));
            Assert.IsFalse(_runtime.Attacks.IsOnCooldown(AttackSlot.Heavy));
        }

        [Test]
        public void DefeatedFighterStaysDefeatedAcrossMixedIntents()
        {
            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                _data.Health.MaxHealth,
                DamageType.Impact,
                HitRegion.Head,
                _clock.CurrentTick));

            int transitions = 0;
            _runtime.StateChanged += transition => transitions++;

            FighterCommand[] script =
            {
                FighterCommand.LightPunch,
                FighterCommand.None,
                FighterCommand.HeavyPunch,
                FighterCommand.Dodge,
                FighterCommand.Block,
                FighterCommand.None,
            };

            for (int i = 0; i < 120; i++)
            {
                FighterCommand command = i < script.Length ? script[i] : FighterCommand.None;
                bool pressed = command != FighterCommand.None;
                _intents.Next = new FighterIntent(
                    new Float2(0f, 1f),
                    command,
                    pressed,
                    pressed,
                    MovementGait.Walk,
                    _clock.CurrentTick);
                Tick();

                Assert.AreEqual(
                    FighterStateId.Defeated,
                    _runtime.StateMachine.CurrentStateId,
                    "Defeat is terminal; no intent may transition the fighter out of it.");
            }

            Assert.AreEqual(0, transitions, "A defeated fighter must raise no further state transitions.");
        }

        [Test]
        public void ReactingFighterCannotBlockUntilTheReactionEnds()
        {
            Assert.IsTrue(_runtime.ReactToHit(HitRegion.Head, _clock.CurrentTick));

            _intents.Next = BlockIntent(pressed: true);
            Tick();
            Tick();

            Assert.AreEqual(
                FighterStateId.HitReaction,
                _runtime.StateMachine.CurrentStateId,
                "Hit stun must not be cancelled by holding block.");
            Assert.IsFalse(_runtime.IsBlocking);

            for (int i = 0; i < 120 && _runtime.StateMachine.CurrentStateId == FighterStateId.HitReaction; i++)
            {
                Tick();
            }

            // The tick that ends the reaction returns to idle; the next tick may accept the guard.
            Tick();

            Assert.AreEqual(
                FighterStateId.Blocking,
                _runtime.StateMachine.CurrentStateId,
                "A still-held block must be accepted once the reaction is over.");
            Assert.IsTrue(_runtime.IsBlocking);
        }

        [Test]
        public void AttackingFighterCannotEnterBlocking()
        {
            _intents.Next = AttackIntent();
            Tick();

            Assert.AreEqual(FighterStateId.LightPunch, _runtime.StateMachine.CurrentStateId);

            _intents.Next = BlockIntent(pressed: true);
            for (int i = 0; i < 10; i++)
            {
                Tick();
                Assert.AreEqual(
                    FighterStateId.LightPunch,
                    _runtime.StateMachine.CurrentStateId,
                    "A committed strike must not be interrupted by block input.");
            }

            Assert.IsFalse(_runtime.IsBlocking);
        }

        [Test]
        public void BlockingFighterCannotStartAttacks()
        {
            EnterBlocking();

            // The player intent source keeps reporting the held guard while the block button is
            // down, so a punch tap during the guard never reaches the runtime as an attack
            // command. Sustained guard input must leave the fighter blocking and start nothing.
            _intents.Next = BlockIntent(pressed: false);
            for (int i = 0; i < 5; i++)
            {
                Tick();
            }

            Assert.AreEqual(FighterStateId.Blocking, _runtime.StateMachine.CurrentStateId);
            Assert.IsFalse(
                _runtime.Attacks.HasBufferedAttack,
                "A guard must not queue a strike.");
            Assert.IsFalse(
                _runtime.Attacks.IsOnCooldown(AttackSlot.Light),
                "No strike may start while the guard is held.");
        }

        [Test]
        public void AttackPressedWhileBlockingIsNotBuffered()
        {
            EnterBlocking();

            // An attack intent on a tick where the guard is still up represents releasing the
            // guard and pressing attack in the same frame. It may drop the guard, but it must not
            // start a strike and the press must not be remembered after the guard is gone.
            _intents.Next = AttackIntent();
            Tick();

            Assert.AreNotEqual(
                FighterStateId.LightPunch,
                _runtime.StateMachine.CurrentStateId,
                "A strike press must not start while the guard is still up.");
            Assert.IsFalse(
                _runtime.Attacks.HasBufferedAttack,
                "The rejected press must not stay buffered.");

            // Drop the guard without ever pressing attack again. A buffered press would fire now.
            _intents.Next = FighterIntent.None;
            for (int i = 0; i < BufferExpiryTicks; i++)
            {
                Tick();
                Assert.AreNotEqual(
                    FighterStateId.LightPunch,
                    _runtime.StateMachine.CurrentStateId,
                    "A punch pressed during the guard must not fire after it is released.");
            }

            Assert.IsFalse(_runtime.Attacks.IsOnCooldown(AttackSlot.Light));
            Assert.AreEqual(FighterStateId.Idle, _runtime.StateMachine.CurrentStateId);
        }

        [Test]
        public void ReleasingBlockRestoresAttackAvailability()
        {
            EnterBlocking();

            _intents.Next = FighterIntent.None;
            Tick();
            Assert.AreEqual(FighterStateId.Idle, _runtime.StateMachine.CurrentStateId);

            _intents.Next = AttackIntent();
            Tick();

            Assert.AreEqual(FighterStateId.LightPunch, _runtime.StateMachine.CurrentStateId);
            Assert.IsTrue(
                _runtime.Attacks.IsOnCooldown(AttackSlot.Light),
                "The jab must be available again once the guard is released.");
        }

        [Test]
        public void InvalidAttackTransitionLeavesBlockingIntact()
        {
            EnterBlocking();

            var context = new FighterStateContext(
                _data,
                _health,
                _runtime.Stamina,
                _clock,
                _runtime.Attacks,
                _runtime.Snapshot);

            Assert.IsFalse(
                _runtime.StateMachine.TryTransition(FighterStateId.LightPunch, context, AttackIntent()),
                "The blocking state must reject a direct attack transition.");
            Assert.AreEqual(FighterStateId.Blocking, _runtime.StateMachine.CurrentStateId);

            _intents.Next = BlockIntent(pressed: false);
            Tick();

            Assert.AreEqual(
                FighterStateId.Blocking,
                _runtime.StateMachine.CurrentStateId,
                "A rejected transition must not corrupt the running guard.");
            Assert.IsTrue(_runtime.IsBlocking);
        }

        private void EnterBlocking()
        {
            _intents.Next = BlockIntent(pressed: true);
            Tick();
            Assert.AreEqual(
                FighterStateId.Blocking,
                _runtime.StateMachine.CurrentStateId,
                "The fighter must enter blocking from idle.");
        }

        private FighterIntent BlockIntent(bool pressed)
        {
            return new FighterIntent(
                Float2.Zero,
                FighterCommand.Block,
                pressed,
                true,
                MovementGait.Walk,
                _clock.CurrentTick);
        }

        private FighterIntent AttackIntent()
        {
            return new FighterIntent(
                Float2.Zero,
                FighterCommand.LightPunch,
                true,
                false,
                MovementGait.Walk,
                _clock.CurrentTick);
        }

        private FighterIntent MoveIntent()
        {
            return new FighterIntent(
                new Float2(0f, 1f),
                FighterCommand.None,
                false,
                false,
                MovementGait.Walk,
                _clock.CurrentTick);
        }

        private void TickUntilIdle()
        {
            for (int i = 0; i < 240; i++)
            {
                if (_runtime.StateMachine.CurrentStateId == FighterStateId.Idle)
                {
                    return;
                }

                Tick();
            }

            Assert.Fail("The fighter never returned to idle.");
        }

        private void Tick()
        {
            _clock.Advance();
            _runtime.Tick(TickSeconds);
        }

        private static FighterData CreateFighterData()
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
                AttackBook.CreateDefault());
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
