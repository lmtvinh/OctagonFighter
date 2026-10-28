using NUnit.Framework;
using OctagonFighter.Core.Ids;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Health;
using OctagonFighter.Infrastructure.Combat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OctagonFighter.Tests.EditMode
{
    /// <summary>
    /// Verifies that strikes are resolved from the striking fist's swept path instead of a
    /// chest-centered reach, that a target outside that path is missed, and that a target which
    /// leaves the path evades the strike.
    /// </summary>
    public sealed class StrikeResolutionTests
    {
        private static readonly FighterId Attacker = new FighterId("attacker");
        private static readonly FighterId Target = new FighterId("target");
        private static readonly TickId Tick = new TickId(1);

        private readonly SweptHitDetector _detector = new SweptHitDetector();
        private GameObject _targetObject;

        [TearDown]
        public void TearDown()
        {
            if (_targetObject != null)
            {
                Object.DestroyImmediate(_targetObject);
                _targetObject = null;
            }
        }

        [Test]
        public void FistSweepConnectsWhenThePathCrossesTheTarget()
        {
            CreateTarget(new Vector3(0f, 0.9f, 1f));

            HitQuery query = BuildFistSweep(
                new Vector3(0f, 1.35f, 0.2f),
                new Vector3(0f, 1.35f, 0.8f));

            Assert.IsTrue(_detector.TryFindTarget(query, out HitCandidate candidate));
            Assert.AreEqual(Target, candidate.TargetId);
        }

        [Test]
        public void FistSweepMissesWhenTheTargetIsOutsideTheFistPath()
        {
            CreateTarget(new Vector3(0.9f, 0.9f, 1f));

            HitQuery query = BuildFistSweep(
                new Vector3(0f, 1.35f, 0.2f),
                new Vector3(0f, 1.35f, 0.8f));

            Assert.IsFalse(
                _detector.TryFindTarget(query, out _),
                "A strike must miss when the fist never reaches the target.");
        }

        [Test]
        public void TargetThatLeavesTheFistPathEvadesTheStrike()
        {
            CreateTarget(new Vector3(0f, 0.9f, 0.8f));

            HitQuery beforeEvade = BuildFistSweep(
                new Vector3(0f, 1.35f, 0.2f),
                new Vector3(0f, 1.35f, 0.6f));
            Assert.IsTrue(
                _detector.TryFindTarget(beforeEvade, out _),
                "Control: the target starts inside the fist's path.");

            // The defender slips sideways after the wind-up, before the next contact tick.
            _targetObject.transform.position = new Vector3(0.9f, 0.9f, 0.8f);
            Physics.SyncTransforms();

            HitQuery afterEvade = BuildFistSweep(
                new Vector3(0f, 1.35f, 0.6f),
                new Vector3(0f, 1.35f, 1f));
            Assert.IsFalse(
                _detector.TryFindTarget(afterEvade, out _),
                "A target that moved out of the sweep must not be pulled back into the strike.");
        }

        [Test]
        public void HandSampleBeyondTheArmEnvelopeCannotClaimAStrike()
        {
            AttackDefinition jab = CreateStrike(0.95f);
            var root = new Float3(0f, 0f, 0f);

            Assert.IsTrue(jab.IsHandInReach(root, new Float3(0.1f, 1.4f, 0.6f)));
            Assert.IsFalse(
                jab.IsHandInReach(root, new Float3(0f, 1.4f, 3f)),
                "A fist sampled across the arena is a rig fault, not a hit.");
        }

        [Test]
        public void ZeroLengthFistMotionStillProbesAlongTheAttackersFacing()
        {
            var hand = new Float3(0f, 1.4f, 0.5f);
            var facing = new Float3(0f, 0f, 1f);

            HitQuery query = HitQuery.FromHandSweep(
                new HitId(1),
                Attacker,
                hand,
                hand,
                facing,
                0.06f,
                Tick);

            Assert.AreEqual(hand, query.Origin);
            Assert.AreEqual(facing, query.Direction);
            Assert.AreEqual(0.02f, query.Distance, 0.0001f);
        }

        [Test]
        public void RigSweepsTheFistBetweenTicks()
        {
            var rigObject = new GameObject("RigUnderTest");
            try
            {
                var leftHand = new GameObject("LeftHand").transform;
                leftHand.SetParent(rigObject.transform, false);
                leftHand.localPosition = new Vector3(0f, 1.4f, 0.2f);
                var rightHand = new GameObject("RightHand").transform;
                rightHand.SetParent(rigObject.transform, false);
                rightHand.localPosition = new Vector3(0f, 1.4f, 0.2f);

                var rig = rigObject.AddComponent<FighterStrikeRig>();
                rig.Initialize();

                Assert.IsTrue(rig.IsInitialized, "Name-based bone lookup must work without an avatar.");
                Assert.IsTrue(rig.TrySampleSweep(StrikeHand.Left, out _, out Vector3 first));
                Assert.AreEqual(leftHand.position, first);

                rig.BeginSweep(StrikeHand.Left);
                leftHand.localPosition = new Vector3(0f, 1.4f, 0.7f);

                Assert.IsTrue(rig.TrySampleSweep(StrikeHand.Left, out Vector3 previous, out Vector3 current));
                Assert.AreEqual(0.2f, previous.z, 0.0001f, "The sweep must start where the window opened.");
                Assert.AreEqual(0.7f, current.z, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(rigObject);
            }
        }

        [Test]
        public void RigReportsMissingHandsWithoutThrowing()
        {
            var rigObject = new GameObject("RigUnderTest");
            try
            {
                var leftHand = new GameObject("LeftHand").transform;
                leftHand.SetParent(rigObject.transform, false);

                var rig = rigObject.AddComponent<FighterStrikeRig>();
                rig.Initialize();

                Assert.IsFalse(rig.IsInitialized);
                Assert.IsFalse(rig.TryGetHand(StrikeHand.Right, out _));
                Assert.IsFalse(rig.TrySampleSweep(StrikeHand.Right, out _, out _));
            }
            finally
            {
                Object.DestroyImmediate(rigObject);
            }
        }

        private static HitQuery BuildFistSweep(Vector3 previous, Vector3 current)
        {
            return HitQuery.FromHandSweep(
                new HitId(1),
                Attacker,
                new Float3(previous.x, previous.y, previous.z),
                new Float3(current.x, current.y, current.z),
                new Float3(0f, 0f, 1f),
                0.06f,
                Tick);
        }

        private static AttackDefinition CreateStrike(float range)
        {
            return new AttackDefinition(
                "Jab",
                "Jab",
                0.36f,
                0.11f,
                0.23f,
                0.35f,
                6f,
                8f,
                range,
                0.06f,
                HitRegion.Head,
                StrikeHand.Left);
        }

        private void CreateTarget(Vector3 position)
        {
            _targetObject = new GameObject("TargetUnderTest");
            _targetObject.transform.position = position;

            var collider = _targetObject.AddComponent<CapsuleCollider>();
            collider.radius = 0.3f;
            collider.height = 1.8f;
            collider.center = Vector3.zero;

            var identity = _targetObject.AddComponent<FighterIdentity>();
            identity.Initialize(Target);

            Physics.SyncTransforms();
        }
    }
}
