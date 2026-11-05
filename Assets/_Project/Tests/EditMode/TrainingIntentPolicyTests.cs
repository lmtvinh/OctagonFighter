using NUnit.Framework;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.StateMachine;

namespace OctagonFighter.Tests.EditMode
{
    /// <summary>
    /// Verifies the pure, deterministic training opponent policy: mode selection, range and
    /// cooldown gating, and the block/stun/defeat suppression of strikes.
    /// </summary>
    public sealed class TrainingIntentPolicyTests
    {
        private static readonly TickId Tick = new TickId(7);

        private TrainingIntentPolicy _policy;

        [SetUp]
        public void SetUp()
        {
            _policy = new TrainingIntentPolicy();
        }

        [Test]
        public void PassiveModeNeverActs()
        {
            _policy.Mode = TrainingMode.Passive;
            Assert.AreEqual(TrainingAction.None, Decide(0.5f));
        }

        [Test]
        public void HoldBlockModeAlwaysBlocks()
        {
            _policy.Mode = TrainingMode.HoldBlock;
            Assert.AreEqual(TrainingAction.Block, Decide(10f));
            Assert.AreEqual(TrainingAction.Block, Decide(0.1f));
        }

        [Test]
        public void ApproachModeMovesForwardWhenFarAndStopsWhenNear()
        {
            _policy.Mode = TrainingMode.Approach;
            _policy.ApproachDistance = 1.2f;

            Assert.AreEqual(TrainingAction.MoveForward, Decide(2f));
            Assert.AreEqual(TrainingAction.None, Decide(1f));
            Assert.AreEqual(TrainingAction.None, Decide(1.2f));
        }

        [Test]
        public void RetreatModeMovesBackwardWhenNearAndStopsWhenFar()
        {
            _policy.Mode = TrainingMode.Retreat;
            _policy.ApproachDistance = 1.2f;

            Assert.AreEqual(TrainingAction.MoveBackward, Decide(0.5f));
            Assert.AreEqual(TrainingAction.None, Decide(2f));
            Assert.AreEqual(TrainingAction.None, Decide(1.2f));
        }

        [Test]
        public void JabModeStrikesWhenInRangeEligibleAndOffCooldown()
        {
            _policy.Mode = TrainingMode.Jab;
            _policy.StrikeRange = 0.95f;
            _policy.ApproachDistance = 1.2f;

            Assert.AreEqual(
                TrainingAction.Jab,
                Decide(0.5f, FighterStateId.Idle, canStartLight: true));
        }

        [Test]
        public void JabModeDoesNotStrikeOutOfRange()
        {
            _policy.Mode = TrainingMode.Jab;
            _policy.StrikeRange = 0.95f;
            _policy.ApproachDistance = 1.2f;

            Assert.AreEqual(
                TrainingAction.MoveForward,
                Decide(2f, FighterStateId.Idle, canStartLight: true),
                "Out of range must approach instead of striking.");
        }

        [Test]
        public void JabModeDoesNotStrikeOnCooldown()
        {
            _policy.Mode = TrainingMode.Jab;
            _policy.StrikeRange = 0.95f;
            _policy.ApproachDistance = 1.2f;

            Assert.AreEqual(
                TrainingAction.MoveForward,
                Decide(0.5f, FighterStateId.Idle, canStartLight: false),
                "On cooldown must not strike.");
        }

        [Test]
        public void JabModeDoesNotStrikeWhenNotAttackEligible()
        {
            _policy.Mode = TrainingMode.Jab;
            _policy.StrikeRange = 0.95f;

            Assert.AreEqual(
                TrainingAction.None,
                Decide(0.5f, FighterStateId.HitReaction, canStartLight: true),
                "Hit reaction is not attack-eligible.");
            Assert.AreEqual(
                TrainingAction.None,
                Decide(0.5f, FighterStateId.Defeated, canStartLight: true),
                "Defeated is not attack-eligible.");
            Assert.AreEqual(
                TrainingAction.None,
                Decide(0.5f, FighterStateId.GuardBreak, canStartLight: true),
                "Guard break is not attack-eligible.");
        }

        [Test]
        public void JabModeDoesNotStrikeWhileBlocking()
        {
            _policy.Mode = TrainingMode.Jab;
            _policy.StrikeRange = 0.95f;

            Assert.AreEqual(
                TrainingAction.MoveForward,
                Decide(0.5f, FighterStateId.Idle, canStartLight: true, isBlocking: true),
                "A blocking fighter must not jab.");
        }

        [Test]
        public void CrossModeStrikesWhenInRangeEligibleAndOffCooldown()
        {
            _policy.Mode = TrainingMode.Cross;
            _policy.StrikeRange = 1.05f;
            _policy.ApproachDistance = 1.2f;

            Assert.AreEqual(
                TrainingAction.Cross,
                Decide(0.5f, FighterStateId.Idle, canStartHeavy: true));
        }

        [Test]
        public void CrossModeDoesNotStrikeOnCooldown()
        {
            _policy.Mode = TrainingMode.Cross;
            _policy.StrikeRange = 1.05f;

            Assert.AreEqual(
                TrainingAction.MoveForward,
                Decide(0.5f, FighterStateId.Idle, canStartHeavy: false));
        }

        [Test]
        public void JabCrossModeAlternatesJabAndCross()
        {
            _policy.Mode = TrainingMode.JabCross;
            _policy.StrikeRange = 1.05f;
            _policy.ApproachDistance = 1.2f;

            Assert.AreEqual(TrainingAction.Jab, Decide(0.5f, FighterStateId.Idle, true, true));
            Assert.AreEqual(TrainingAction.Cross, Decide(0.5f, FighterStateId.Idle, true, true));
            Assert.AreEqual(TrainingAction.Jab, Decide(0.5f, FighterStateId.Idle, true, true));
            Assert.AreEqual(TrainingAction.Cross, Decide(0.5f, FighterStateId.Idle, true, true));
        }

        [Test]
        public void JabCrossModeSuppressesStrikeOnCooldownWithoutConsumingTurn()
        {
            _policy.Mode = TrainingMode.JabCross;
            _policy.StrikeRange = 1.05f;
            _policy.ApproachDistance = 1.2f;

            // First turn: jab (light off cooldown).
            Assert.AreEqual(TrainingAction.Jab, Decide(0.5f, FighterStateId.Idle, true, true));
            // Second turn: cross would be next, but heavy is on cooldown -> suppress, do not toggle.
            Assert.AreEqual(TrainingAction.MoveForward, Decide(0.5f, FighterStateId.Idle, true, false));
            // Third turn: cross should still be next and now off cooldown.
            Assert.AreEqual(TrainingAction.Cross, Decide(0.5f, FighterStateId.Idle, true, true));
        }

        [Test]
        public void JabCrossModeDoesNotStrikeWhileBlockingOrDefeated()
        {
            _policy.Mode = TrainingMode.JabCross;
            _policy.StrikeRange = 1.05f;

            Assert.AreEqual(
                TrainingAction.MoveForward,
                Decide(0.5f, FighterStateId.Idle, true, true, isBlocking: true));
            Assert.AreEqual(
                TrainingAction.None,
                Decide(0.5f, FighterStateId.Defeated, true, true));
        }

        [Test]
        public void StrikeRangeBoundaryIsInclusive()
        {
            _policy.Mode = TrainingMode.Jab;
            _policy.StrikeRange = 0.95f;

            Assert.AreEqual(
                TrainingAction.Jab,
                Decide(0.95f, FighterStateId.Idle, canStartLight: true),
                "Exactly at strike range must strike.");
            Assert.AreEqual(
                TrainingAction.MoveForward,
                Decide(0.96f, FighterStateId.Idle, canStartLight: true),
                "Just beyond strike range must approach.");
        }

        private TrainingAction Decide(
            float distance,
            FighterStateId state = FighterStateId.Idle,
            bool canStartLight = true,
            bool canStartHeavy = true,
            bool isBlocking = false)
        {
            return _policy.Decide(
                distance,
                state,
                MovementAuthority.Full,
                canStartLight,
                canStartHeavy,
                isBlocking);
        }
    }
}
