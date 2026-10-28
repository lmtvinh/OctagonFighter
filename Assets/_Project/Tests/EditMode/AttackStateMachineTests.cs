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
    /// Verifies light and heavy strike states, the timed contact window, stamina spending,
    /// cooldowns, and the input buffer.
    /// </summary>
    public sealed class AttackStateMachineTests
    {
        private const float TickSeconds = 1f / 60f;

        // Measured from the imported 30 fps clips with a 240 Hz hand sampling pass:
        // Lead Jab.fbx reaches 90% of the left hand's extension between 0.375 s and 0.442 s.
        private const float MeasuredJabReachStart = 0.375f;
        private const float MeasuredJabReachEnd = 0.442f;

        // Jab Cross.fbx reaches 90% of the right hand's extension between 0.613 s and 0.675 s.
        private const float MeasuredCrossReachStart = 0.613f;
        private const float MeasuredCrossReachEnd = 0.675f;

        private FighterData _data;
        private HealthModel _health;
        private StaminaModel _stamina;
        private CombatClock _clock;
        private AttackTracker _attacks;
        private FighterStateMachine _stateMachine;
        private FighterStateContext _context;

        [SetUp]
        public void SetUp()
        {
            ResetWithBook(AttackBook.CreateDefault());
        }

        [Test]
        public void PressingLightPunchEntersLightPunchAndSpendsStamina()
        {
            float staminaBefore = _stamina.CurrentStamina;

            Tick(FighterCommand.LightPunch, true, 0.01f);

            Assert.AreEqual(FighterStateId.LightPunch, _stateMachine.CurrentStateId);
            Assert.AreEqual(staminaBefore - 6f, _stamina.CurrentStamina, 0.001f);
            Assert.IsFalse(_attacks.CanStart(AttackSlot.Light));
        }

        [Test]
        public void PressingHeavyPunchEntersHeavyPunchAndSpendsStamina()
        {
            float staminaBefore = _stamina.CurrentStamina;

            Tick(FighterCommand.HeavyPunch, true, 0.01f);

            Assert.AreEqual(FighterStateId.HeavyPunch, _stateMachine.CurrentStateId);
            Assert.AreEqual(staminaBefore - 12f, _stamina.CurrentStamina, 0.001f);
        }

        [Test]
        public void LightPunchDoesNotRequestAHitBeforeStartupCompletes()
        {
            Tick(FighterCommand.LightPunch, true, 0.01f);

            Tick(FighterCommand.None, false, 0.3f);
            Assert.IsFalse(_attacks.TryTakeHitRequest(out _), "Damage must not resolve during startup.");

            Tick(FighterCommand.None, false, 0.05f);
            Assert.IsFalse(
                _attacks.TryTakeHitRequest(out _),
                "The contact window must not open before the measured extension begins.");

            Tick(FighterCommand.None, false, 0.02f);
            Assert.IsTrue(_attacks.TryTakeHitRequest(out AttackHitRequest request));
            Assert.AreEqual(_data.Id, request.AttackerId);
            Assert.AreEqual(AttackSlot.Light, request.Slot);
            Assert.AreEqual(8f, request.Attack.Damage, 0.001f);
            Assert.AreEqual(HitRegion.Head, request.Attack.Region);
            Assert.AreEqual(StrikeHand.Left, request.Attack.Hand);
            Assert.IsFalse(request.HitId.IsEmpty);
        }

        [Test]
        public void ContactWindowRepeatsRequestsWithOneStableHitId()
        {
            Tick(FighterCommand.LightPunch, true, 0.01f);
            Tick(FighterCommand.None, false, 0.37f);

            Assert.IsTrue(_attacks.TryTakeHitRequest(out AttackHitRequest first));
            Tick(FighterCommand.None, false, 0.04f);
            Assert.IsTrue(
                _attacks.TryTakeHitRequest(out AttackHitRequest second),
                "A sustained window must keep offering hits while it is open.");
            Assert.AreEqual(first.HitId, second.HitId, "One strike owns exactly one hit window.");
        }

        [Test]
        public void ContactWindowStopsRequestingHitsOnceRecoveryStarts()
        {
            Tick(FighterCommand.LightPunch, true, 0.01f);
            Tick(FighterCommand.None, false, 0.37f);
            Assert.IsTrue(_attacks.TryTakeHitRequest(out _));

            float remaining = AttackBook.CreateDefault().Light.RecoverySeconds + 0.02f;
            Tick(FighterCommand.None, false, remaining);

            Assert.IsFalse(
                _attacks.TryTakeHitRequest(out _),
                "A strike must not keep offering hits during recovery.");
        }

        [Test]
        public void HeavyPunchRequestsHeavyHitInsideTheCrossWindow()
        {
            Tick(FighterCommand.HeavyPunch, true, 0.01f);

            Tick(FighterCommand.None, false, 0.59f);
            Assert.IsFalse(_attacks.TryTakeHitRequest(out _), "The cross must not resolve during startup.");

            Tick(FighterCommand.None, false, 0.02f);
            Assert.IsTrue(_attacks.TryTakeHitRequest(out AttackHitRequest request));
            Assert.AreEqual(AttackSlot.Heavy, request.Slot);
            Assert.AreEqual(15f, request.Attack.Damage, 0.001f);
            Assert.AreEqual(StrikeHand.Right, request.Attack.Hand);
        }

        [Test]
        public void LightPunchWindowContainsTheMeasuredJabExtension()
        {
            AttackDefinition jab = AttackBook.CreateDefault().Light;

            Assert.LessOrEqual(
                jab.ContactStartSeconds,
                MeasuredJabReachStart,
                "The contact window must open at or before the measured 90% reach.");
            Assert.GreaterOrEqual(
                jab.ContactEndSeconds,
                MeasuredJabReachEnd,
                "The contact window must stay open through the measured 90% reach.");
        }

        [Test]
        public void HeavyPunchWindowContainsTheMeasuredCrossExtension()
        {
            AttackDefinition cross = AttackBook.CreateDefault().Heavy;

            Assert.LessOrEqual(cross.ContactStartSeconds, MeasuredCrossReachStart);
            Assert.GreaterOrEqual(cross.ContactEndSeconds, MeasuredCrossReachEnd);
        }

        [Test]
        public void StrikeStaysCommittedUntilTheAuthoredDurationElapses()
        {
            AttackDefinition jab = AttackBook.CreateDefault().Light;
            Tick(FighterCommand.LightPunch, true, 0.01f);

            Tick(FighterCommand.None, false, jab.DurationSeconds - 0.01f);
            Assert.AreEqual(
                FighterStateId.LightPunch,
                _stateMachine.CurrentStateId,
                "The strike must hold its recovery instead of cutting the clip short.");

            Tick(FighterCommand.None, false, 0.02f);
            Assert.AreEqual(FighterStateId.Idle, _stateMachine.CurrentStateId);
        }

        [Test]
        public void CommittedStrikeLocksFacingAndIdleRestoresIt()
        {
            Assert.AreEqual(FacingAuthority.Free, _stateMachine.CurrentFacingAuthority);

            Tick(FighterCommand.LightPunch, true, 0.01f);
            Assert.AreEqual(FacingAuthority.Locked, _stateMachine.CurrentFacingAuthority);

            Tick(FighterCommand.None, false, AttackBook.CreateDefault().Light.DurationSeconds);
            Assert.AreEqual(FighterStateId.Idle, _stateMachine.CurrentStateId);
            Assert.AreEqual(FacingAuthority.Free, _stateMachine.CurrentFacingAuthority);
        }

        [Test]
        public void BufferedAttackStartsFromIdleAndIsConsumed()
        {
            _attacks.BufferAttack(AttackSlot.Light);

            Tick(FighterCommand.None, false, TickSeconds);

            Assert.AreEqual(FighterStateId.LightPunch, _stateMachine.CurrentStateId);
            Assert.IsFalse(_attacks.HasBufferedAttack);
        }

        [Test]
        public void BufferedAttackExpiresBeforeIdleCanUseIt()
        {
            _attacks.BufferAttack(AttackSlot.Light);
            _attacks.Tick(1f);

            Tick(FighterCommand.None, false, TickSeconds);

            Assert.AreEqual(FighterStateId.Idle, _stateMachine.CurrentStateId);
            Assert.IsFalse(_attacks.HasBufferedAttack);
        }

        [Test]
        public void CooldownBlocksImmediateRestart()
        {
            AttackBook defaultBook = AttackBook.CreateDefault();
            var light = new AttackDefinition(
                "Jab",
                "Jab",
                0.01f,
                0.01f,
                0.01f,
                5f,
                0f,
                1f,
                1f,
                0.1f,
                HitRegion.Head,
                StrikeHand.Left);
            var book = new AttackBook(light, defaultBook.Heavy, defaultBook.InputBufferSeconds);
            ResetWithBook(book);

            Tick(FighterCommand.LightPunch, true, 0.01f);
            Tick(FighterCommand.None, false, 0.05f);
            Assert.AreEqual(FighterStateId.Idle, _stateMachine.CurrentStateId);
            Assert.IsTrue(_attacks.IsOnCooldown(AttackSlot.Light));

            Tick(FighterCommand.LightPunch, true, 0.01f);

            Assert.AreEqual(FighterStateId.Idle, _stateMachine.CurrentStateId);
        }

        [Test]
        public void InsufficientStaminaBlocksAttack()
        {
            _stamina.TrySpend(new StaminaCost(_stamina.CurrentStamina));

            Tick(FighterCommand.LightPunch, true, 0.01f);

            Assert.AreEqual(FighterStateId.Idle, _stateMachine.CurrentStateId);
        }

        private void ResetWithBook(AttackBook book)
        {
            _data = CreateFighterData(book);
            _health = new HealthModel(_data.Health);
            _stamina = new StaminaModel(_data.Stamina);
            _clock = new CombatClock();
            _attacks = new AttackTracker(book);

            var snapshot = new FighterSnapshot(
                _data.Id,
                FighterStateId.Idle,
                _health.CurrentHealth,
                _health.MaxHealth,
                _stamina.CurrentStamina,
                _stamina.MaxStamina,
                _stamina.Tier,
                _health.IsAlive,
                _health.IsKnockedOut,
                _clock.CurrentTick);

            _context = new FighterStateContext(
                _data,
                _health,
                _stamina,
                _clock,
                _attacks,
                snapshot);

            _stateMachine = FoundationStateMachineFactory.Create();
            _stateMachine.Initialize(FighterStateId.Idle, _context, FighterIntent.None);
        }

        private void Tick(FighterCommand command, bool isPressed, float deltaSeconds)
        {
            _clock.Advance();

            FighterIntent intent = new FighterIntent(
                Float2.Zero,
                command,
                isPressed,
                false,
                MovementGait.Walk,
                _clock.CurrentTick);

            _stateMachine.Tick(_context, intent, deltaSeconds);
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
    }
}
