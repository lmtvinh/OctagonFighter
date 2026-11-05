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
    /// Verifies how the runtime turns authored guard data into blocked-strike outcomes, block
    /// impact reactions, guard breaks, delayed recovery, and defeat precedence.
    /// </summary>
    public sealed class GuardRuntimeTests
    {
        private const float TickSeconds = 1f / 60f;

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

            TickUntilIdle();
        }

        [TearDown]
        public void TearDown()
        {
            _runtime?.Dispose();
            _runtime = null;
        }

        [Test]
        public void BlockedJabAppliesPartialChipDamageGuardDamageAndStaminaCost()
        {
            EnterBlocking();

            BlockResolution result = BlockLightAttack();

            float expectedGuardDamage = _data.Attacks.Light.GuardDamage *
                (1f + (_data.Guard.ImpactGuardDamageBonus * _data.Attacks.Light.ImpactStrength));
            Assert.IsTrue(result.WasBlocked);
            Assert.AreEqual(98f, _runtime.Health.CurrentHealth, 0.001f, "A blocked jab must chip 25% of 8 health.");
            Assert.AreEqual(100f - expectedGuardDamage, _runtime.Guard.CurrentGuard, 0.001f);
            Assert.AreEqual(100f - _data.Attacks.Light.BlockStaminaCost, _runtime.Stamina.CurrentStamina, 0.001f);
            Assert.AreEqual(_data.Guard.PushbackDistanceMeters, result.PushbackDistance, 0.001f);
            Assert.AreEqual(FighterStateId.BlockImpact, _runtime.StateMachine.CurrentStateId);
            Assert.IsTrue(_runtime.IsBlocking, "The guard must stay active during the block-impact reaction.");
        }

        [Test]
        public void CrossProducesDifferentBlockedResultThanJab()
        {
            EnterBlocking();

            float healthBeforeJab = _runtime.Health.CurrentHealth;
            float guardBeforeJab = _runtime.Guard.CurrentGuard;
            float staminaBeforeJab = _runtime.Stamina.CurrentStamina;
            BlockLightAttack();

            float jabHealthLoss = healthBeforeJab - _runtime.Health.CurrentHealth;
            float jabGuardLoss = guardBeforeJab - _runtime.Guard.CurrentGuard;
            float jabStaminaLoss = staminaBeforeJab - _runtime.Stamina.CurrentStamina;

            Tick(_data.Guard.BlockImpactSeconds + TickSeconds);
            float healthBeforeCross = _runtime.Health.CurrentHealth;
            float guardBeforeCross = _runtime.Guard.CurrentGuard;
            float staminaBeforeCross = _runtime.Stamina.CurrentStamina;
            BlockHeavyAttack();

            float crossHealthLoss = healthBeforeCross - _runtime.Health.CurrentHealth;
            float crossGuardLoss = guardBeforeCross - _runtime.Guard.CurrentGuard;
            float crossStaminaLoss = staminaBeforeCross - _runtime.Stamina.CurrentStamina;

            Assert.Greater(crossHealthLoss, jabHealthLoss, "The cross must chip more health than the jab.");
            Assert.Greater(crossGuardLoss, jabGuardLoss, "The cross must remove more guard than the jab.");
            Assert.Greater(crossStaminaLoss, jabStaminaLoss, "The cross must cost the defender more stamina.");
        }

        [Test]
        public void SecondBlockedHitRefreshesTheBlockImpactReaction()
        {
            EnterBlocking();
            BlockLightAttack();
            Tick(0.1f);
            Assert.AreEqual(FighterStateId.BlockImpact, _runtime.StateMachine.CurrentStateId);

            BlockLightAttack();
            Tick(0.1f);

            Assert.AreEqual(
                FighterStateId.BlockImpact,
                _runtime.StateMachine.CurrentStateId,
                "A second blocked hit must restart the impact duration instead of inheriting the first.");

            Tick(_data.Guard.BlockImpactSeconds - 0.01f);
            Assert.AreEqual(
                FighterStateId.Blocking,
                _runtime.StateMachine.CurrentStateId,
                "The held guard must resume once the refreshed impact ends.");
            Assert.IsTrue(_runtime.IsBlocking);
        }

        [Test]
        public void DepletingGuardForcesGuardBreakAndRestoresPartialGuardAfterTheStun()
        {
            EnterBlocking();
            BlockHeavyAttack();
            BlockHeavyAttack();

            BlockResolution breaking = BlockHeavyAttack();

            Assert.IsTrue(breaking.BrokeGuard, "The third cross must deplete the remaining guard.");
            Assert.IsTrue(_runtime.Guard.IsBroken);
            Assert.AreEqual(0f, _runtime.Guard.CurrentGuard, 0.001f);
            Assert.AreEqual(FighterStateId.GuardBreak, _runtime.StateMachine.CurrentStateId);
            Assert.IsFalse(_runtime.IsBlocking, "A broken guard must not block incoming strikes.");

            BlockResolution duringBreak = _runtime.ApplyBlock(new DamageEvent(
                AttackerId,
                _data.Id,
                20f,
                DamageType.Impact,
                HitRegion.Body,
                _clock.CurrentTick));
            Assert.IsFalse(duringBreak.WasBlocked, "A guard-break stun must resolve hits against health.");
            Assert.AreEqual(0f, duringBreak.HealthDamage.AppliedAmount);

            Tick(_data.Guard.GuardBreakSeconds - 0.01f);
            Assert.AreEqual(FighterStateId.GuardBreak, _runtime.StateMachine.CurrentStateId);
            Assert.IsTrue(_runtime.Guard.IsBroken);

            Tick(0.02f);

            Assert.AreEqual(FighterStateId.Idle, _runtime.StateMachine.CurrentStateId);
            Assert.IsFalse(_runtime.Guard.IsBroken);
            Assert.AreEqual(
                _data.Guard.MaxGuard * _data.Guard.GuardBreakRecoveryFraction,
                _runtime.Guard.CurrentGuard,
                0.001f,
                "Leaving guard break must restore the authored partial guard.");

            _intents.Next = FighterIntent.None;
            Tick(_data.Guard.RecoveryDelaySeconds + 0.2f);

            Assert.Greater(
                _runtime.Guard.CurrentGuard,
                _data.Guard.MaxGuard * _data.Guard.GuardBreakRecoveryFraction,
                "Guard must recover once the post-break delay elapses.");
        }

        [Test]
        public void BlockingPausesRecoveryAndReleaseStillHonoursTheDelay()
        {
            EnterBlocking();
            BlockLightAttack();
            float guardAfterImpact = _runtime.Guard.CurrentGuard;
            float recoveryDelay = _data.Guard.RecoveryDelaySeconds;

            for (int i = 0; i < 240; i++)
            {
                Tick(TickSeconds);
                Assert.AreEqual(
                    guardAfterImpact,
                    _runtime.Guard.CurrentGuard,
                    0.001f,
                    "Guard must not recover while the block is held, even after the delay would have elapsed.");
            }

            _intents.Next = FighterIntent.None;
            Tick();
            Tick(recoveryDelay - 0.1f);

            Assert.AreEqual(
                guardAfterImpact,
                _runtime.Guard.CurrentGuard,
                0.001f,
                "Dropping the guard must not skip the recovery delay.");

            Tick(0.3f);

            Assert.Greater(
                _runtime.Guard.CurrentGuard,
                guardAfterImpact,
                "Guard must recover after the delay following the release.");
        }

        [Test]
        public void DefeatOverridesGuardBreakWhenChipDamageIsLethal()
        {
            _runtime.ApplyDamage(new DamageEvent(
                AttackerId,
                _data.Id,
                80f,
                DamageType.Impact,
                HitRegion.Body,
                _clock.CurrentTick));
            TickUntilIdle();
            EnterBlocking();

            int defeats = 0;
            _runtime.Defeated += _ => defeats++;

            BlockResolution result = _runtime.ApplyBlock(new DamageEvent(
                AttackerId,
                _data.Id,
                100f,
                DamageType.Impact,
                HitRegion.Head,
                _clock.CurrentTick));

            Assert.IsTrue(result.WasBlocked);
            Assert.IsTrue(result.BrokeGuard, "The direct blocked hit must deplete the guard.");
            Assert.AreEqual(0f, _runtime.Health.CurrentHealth, 0.001f);
            Assert.AreEqual(
                FighterStateId.Defeated,
                _runtime.StateMachine.CurrentStateId,
                "Lethal chip damage must end in Defeat, never the guard-break stun.");
            Assert.IsFalse(_runtime.IsBlocking);

            Tick(TickSeconds);
            Assert.AreEqual(FighterStateId.Defeated, _runtime.StateMachine.CurrentStateId);
            Assert.AreEqual(1, defeats, "Defeat must be announced exactly once on the following tick.");
        }

        [Test]
        public void DirectBlockWithoutAuthoredDataUsesTheProfileFallbackValues()
        {
            EnterBlocking();

            BlockResolution result = _runtime.ApplyBlock(new DamageEvent(
                AttackerId,
                _data.Id,
                20f,
                DamageType.Impact,
                HitRegion.Body,
                _clock.CurrentTick));

            Assert.IsTrue(result.WasBlocked);
            Assert.AreEqual(5f, result.HealthDamage.AppliedAmount, 0.001f, "The blocked multiplier must chip 25%.");
            Assert.AreEqual(80f, _runtime.Guard.CurrentGuard, 0.001f);
            Assert.AreEqual(
                100f - _data.Guard.DirectBlockStaminaCost,
                _runtime.Stamina.CurrentStamina,
                0.001f);
        }

        [Test]
        public void BlockedChipDamageNeverStartsAHitReaction()
        {
            int reactions = 0;
            _runtime.HitReactionStarted += _ => reactions++;
            EnterBlocking();

            BlockLightAttack();

            Assert.AreEqual(0, reactions);
            Assert.IsFalse(_runtime.IsReactingToHit);
            Assert.AreEqual(default(HitReactionInfo), _runtime.ActiveHitReaction);
        }

        [Test]
        public void BlockedHitRaisesGuardAndHealthChangeNotificationsExactlyOnce()
        {
            EnterBlocking();
            int guardChanges = 0;
            int healthChanges = 0;
            _runtime.Guard.Changed += _ => guardChanges++;
            _health.Changed += _ => healthChanges++;

            BlockResolution result = BlockLightAttack();

            Assert.AreEqual(1, guardChanges, "A blocked hit must raise one guard change.");
            Assert.AreEqual(1, healthChanges, "The chip damage must raise one health change.");
            Assert.AreEqual(2f, result.HealthDamage.AppliedAmount, 0.001f);
        }

        private BlockResolution BlockLightAttack()
        {
            return _runtime.ApplyBlock(
                _data.Attacks.Light,
                new DamageEvent(
                    AttackerId,
                    _data.Id,
                    _data.Attacks.Light.Damage,
                    DamageType.Impact,
                    _data.Attacks.Light.Region,
                    _clock.CurrentTick));
        }

        private BlockResolution BlockHeavyAttack()
        {
            return _runtime.ApplyBlock(
                _data.Attacks.Heavy,
                new DamageEvent(
                    AttackerId,
                    _data.Id,
                    _data.Attacks.Heavy.Damage,
                    DamageType.Impact,
                    _data.Attacks.Heavy.Region,
                    _clock.CurrentTick));
        }

        private void EnterBlocking()
        {
            _intents.Next = new FighterIntent(
                Float2.Zero,
                FighterCommand.Block,
                true,
                true,
                MovementGait.Walk,
                _clock.CurrentTick);
            Tick();
            Assert.AreEqual(
                FighterStateId.Blocking,
                _runtime.StateMachine.CurrentStateId,
                "The fighter must enter blocking before the strike resolves.");
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

        private void Tick(float deltaSeconds = TickSeconds)
        {
            _clock.Advance();
            _runtime.Tick(deltaSeconds);
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
