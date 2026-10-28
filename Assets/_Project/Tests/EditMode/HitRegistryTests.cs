using NUnit.Framework;
using OctagonFighter.Core.Ids;
using OctagonFighter.Domain.Combat;

namespace OctagonFighter.Tests.EditMode
{
    /// <summary>
    /// Verifies per-strike and per-target hit bookkeeping.
    /// </summary>
    public sealed class HitRegistryTests
    {
        private static readonly FighterId FirstTarget = new FighterId("target_a");
        private static readonly FighterId SecondTarget = new FighterId("target_b");

        private HitRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = new HitRegistry();
        }

        [Test]
        public void OneStrikeDamagesOneTargetOnlyOnce()
        {
            var hitId = new HitId(1);

            Assert.IsTrue(_registry.TryClaim(hitId, FirstTarget));
            Assert.IsFalse(_registry.TryClaim(hitId, FirstTarget), "Repeat contact must not damage twice.");
            Assert.IsFalse(_registry.TryClaim(hitId, SecondTarget), "One fist can only claim one target.");
            Assert.IsTrue(_registry.IsClaimed(hitId));
            Assert.AreEqual(1, _registry.Count);
        }

        [Test]
        public void SeparateStrikesClaimIndependently()
        {
            Assert.IsTrue(_registry.TryClaim(new HitId(1), FirstTarget));
            Assert.IsTrue(_registry.TryClaim(new HitId(2), SecondTarget));
            Assert.AreEqual(2, _registry.Count);
        }

        [Test]
        public void ReleaseLetsTheNextContactWindowClaimAgain()
        {
            var hitId = new HitId(1);
            Assert.IsTrue(_registry.TryClaim(hitId, FirstTarget));

            _registry.Release(hitId);

            Assert.IsFalse(_registry.IsClaimed(hitId));
            Assert.AreEqual(0, _registry.Count);
            Assert.IsTrue(_registry.TryClaim(hitId, SecondTarget));
        }

        [Test]
        public void ReleasingAnUnknownWindowIsHarmless()
        {
            Assert.DoesNotThrow(() => _registry.Release(new HitId(99)));
            Assert.AreEqual(0, _registry.Count);
        }

        [Test]
        public void TableStaysBoundedWhenStrikesAreReleasedAtTheirLifecycleBoundary()
        {
            for (int index = 1; index <= 500; index++)
            {
                var hitId = new HitId(index);
                Assert.IsTrue(_registry.TryClaim(hitId, FirstTarget));
                _registry.Release(hitId);
            }

            Assert.AreEqual(0, _registry.Count);
        }

        [Test]
        public void ClearRemovesEveryClaim()
        {
            _registry.TryClaim(new HitId(1), FirstTarget);
            _registry.TryClaim(new HitId(2), SecondTarget);

            _registry.Clear();

            Assert.AreEqual(0, _registry.Count);
        }
    }
}
