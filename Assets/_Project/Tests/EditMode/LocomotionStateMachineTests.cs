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
    /// Verifies idle, walk, and run transitions in the foundation state machine.
    /// </summary>
    public sealed class LocomotionStateMachineTests
    {
        private const float TickSeconds = 1f / 60f;

        private FighterData _data;
        private FighterStateMachine _stateMachine;
        private FighterStateContext _context;

        [SetUp]
        public void SetUp()
        {
            _data = CreateFighterData();
            var health = new HealthModel(_data.Health);
            var stamina = new StaminaModel(_data.Stamina);
            var clock = new CombatClock();
            var snapshot = new FighterSnapshot(
                _data.Id,
                FighterStateId.Idle,
                health.CurrentHealth,
                health.MaxHealth,
                stamina.CurrentStamina,
                stamina.MaxStamina,
                stamina.Tier,
                health.IsAlive,
                health.IsKnockedOut,
                clock.CurrentTick);

            _context = new FighterStateContext(
                _data,
                health,
                stamina,
                clock,
                new AttackTracker(_data.Attacks),
                snapshot);
            _stateMachine = FoundationStateMachineFactory.Create();
            _stateMachine.Initialize(FighterStateId.Idle, _context, FighterIntent.None);
        }

        [Test]
        public void IdleTransitionsToWalkWhenMovingAtWalkGait()
        {
            Tick(MovementGait.Walk);

            Assert.AreEqual(FighterStateId.Walk, _stateMachine.CurrentStateId);
        }

        [Test]
        public void IdleTransitionsToRunWhenMovingAtRunGait()
        {
            Tick(MovementGait.Run);

            Assert.AreEqual(FighterStateId.Run, _stateMachine.CurrentStateId);
        }

        [Test]
        public void WalkTransitionsToRunWhenGaitChanges()
        {
            Tick(MovementGait.Walk);
            Tick(MovementGait.Run);

            Assert.AreEqual(FighterStateId.Run, _stateMachine.CurrentStateId);
        }

        [Test]
        public void RunTransitionsToWalkWhenGaitChanges()
        {
            Tick(MovementGait.Run);
            Tick(MovementGait.Walk);

            Assert.AreEqual(FighterStateId.Walk, _stateMachine.CurrentStateId);
        }

        [Test]
        public void WalkTransitionsToIdleWhenMovementStops()
        {
            Tick(MovementGait.Walk);
            TickMove(Float2.Zero, MovementGait.Walk);

            Assert.AreEqual(FighterStateId.Idle, _stateMachine.CurrentStateId);
        }

        [Test]
        public void RunTransitionsToIdleWhenMovementStops()
        {
            Tick(MovementGait.Run);
            TickMove(Float2.Zero, MovementGait.Run);

            Assert.AreEqual(FighterStateId.Idle, _stateMachine.CurrentStateId);
        }

        [Test]
        public void WalkTransitionsToBlockingWhenBlockIsHeld()
        {
            Tick(MovementGait.Walk);

            var intent = new FighterIntent(
                new Float2(0f, 1f),
                FighterCommand.Block,
                true,
                true,
                MovementGait.Walk,
                _context.Clock.CurrentTick);

            _stateMachine.Tick(_context, intent, TickSeconds);

            Assert.AreEqual(FighterStateId.Blocking, _stateMachine.CurrentStateId);
        }

        private void Tick(MovementGait gait)
        {
            TickMove(new Float2(0f, 1f), gait);
        }

        private void TickMove(Float2 move, MovementGait gait)
        {
            var intent = new FighterIntent(
                move,
                FighterCommand.None,
                false,
                false,
                gait,
                _context.Clock.CurrentTick);

            _stateMachine.Tick(_context, intent, TickSeconds);
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
    }
}
