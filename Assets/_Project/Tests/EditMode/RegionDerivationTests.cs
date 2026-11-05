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
    /// Verifies that the resolved hit region is derived from the collided body volume, not from the
    /// attack's authored name, with a contact-height fallback when only the generic capsule is hit.
    /// </summary>
    public sealed class RegionDerivationTests
    {
        private static readonly FighterId Attacker = new FighterId("attacker");
        private static readonly FighterId Target = new FighterId("target");
        private static readonly TickId Tick = new TickId(1);

        private const float HeadHeight = 1.5f;
        private const float BodyHeight = 1.0f;
        private const float LegHeight = 0.3f;

        private readonly SweptHitDetector _detector = new SweptHitDetector();
        private GameObject _target;

        [TearDown]
        public void TearDown()
        {
            if (_target != null)
            {
                Object.DestroyImmediate(_target);
                _target = null;
            }
        }

        [Test]
        public void SweepHittingHeadVolumeDerivesHeadRegion()
        {
            CreateTargetWithRegionVolumes();
            HitQuery query = BuildSweep(HeadHeight);

            Assert.IsTrue(_detector.TryFindTarget(query, out HitCandidate candidate));
            Assert.AreEqual(Target, candidate.TargetId);
            Assert.AreEqual(HitRegion.Head, candidate.Region);
        }

        [Test]
        public void SweepHittingBodyVolumeDerivesBodyRegion()
        {
            CreateTargetWithRegionVolumes();
            HitQuery query = BuildSweep(BodyHeight);

            Assert.IsTrue(_detector.TryFindTarget(query, out HitCandidate candidate));
            Assert.AreEqual(Target, candidate.TargetId);
            Assert.AreEqual(HitRegion.Body, candidate.Region);
        }

        [Test]
        public void SweepHittingLegVolumeDerivesLegRegion()
        {
            CreateTargetWithRegionVolumes();
            HitQuery query = BuildSweep(LegHeight);

            Assert.IsTrue(_detector.TryFindTarget(query, out HitCandidate candidate));
            Assert.AreEqual(Target, candidate.TargetId);
            Assert.AreEqual(HitRegion.Leg, candidate.Region);
        }

        [Test]
        public void SweepHittingOnlyCapsuleFallsBackToContactHeight()
        {
            CreateTargetWithCapsuleOnly();

            Assert.IsTrue(_detector.TryFindTarget(BuildSweep(HeadHeight), out var head));
            Assert.AreEqual(HitRegion.Head, head.Region);

            Assert.IsTrue(_detector.TryFindTarget(BuildSweep(BodyHeight), out var body));
            Assert.AreEqual(HitRegion.Body, body.Region);

            Assert.IsTrue(_detector.TryFindTarget(BuildSweep(LegHeight), out var leg));
            Assert.AreEqual(HitRegion.Leg, leg.Region);
        }

        [Test]
        public void RegionVolumesTakePrecedenceOverTheGenericCapsule()
        {
            CreateTargetWithRegionVolumesAndCapsule();
            HitQuery query = BuildSweep(HeadHeight);

            Assert.IsTrue(_detector.TryFindTarget(query, out HitCandidate candidate));
            Assert.AreEqual(HitRegion.Head, candidate.Region,
                "A region volume must win over the generic capsule height fallback.");
        }

        private static HitQuery BuildSweep(float height)
        {
            return HitQuery.FromHandSweep(
                new HitId(1),
                Attacker,
                new Float3(0f, height, 0f),
                new Float3(0f, height, 0.6f),
                new Float3(0f, 0f, 1f),
                0.06f,
                Tick);
        }

        private void CreateTargetWithRegionVolumes()
        {
            _target = new GameObject("TargetUnderTest");
            _target.transform.position = new Vector3(0f, 0f, 0.6f);

            AddRegionVolume(new Vector3(0f, HeadHeight, 0f), HitRegion.Head);
            AddRegionVolume(new Vector3(0f, BodyHeight, 0f), HitRegion.Body);
            AddRegionVolume(new Vector3(0f, LegHeight, 0f), HitRegion.Leg);

            var identity = _target.AddComponent<FighterIdentity>();
            identity.Initialize(Target);

            Physics.SyncTransforms();
        }

        private void CreateTargetWithCapsuleOnly()
        {
            _target = new GameObject("TargetUnderTest");
            _target.transform.position = new Vector3(0f, 0.9f, 0.6f);

            var collider = _target.AddComponent<CapsuleCollider>();
            collider.radius = 0.3f;
            collider.height = 1.8f;
            collider.center = Vector3.zero;

            var identity = _target.AddComponent<FighterIdentity>();
            identity.Initialize(Target);

            Physics.SyncTransforms();
        }

        private void CreateTargetWithRegionVolumesAndCapsule()
        {
            CreateTargetWithRegionVolumes();
            var capsule = new GameObject("Hitbox");
            capsule.transform.SetParent(_target.transform, false);
            capsule.transform.localPosition = Vector3.zero;
            var collider = capsule.AddComponent<CapsuleCollider>();
            collider.radius = 0.3f;
            collider.height = 1.8f;
            collider.center = new Vector3(0f, 0.9f, 0f);
            collider.isTrigger = true;
        }

        private void AddRegionVolume(Vector3 localPosition, HitRegion region)
        {
            var volume = new GameObject($"Region{region}");
            volume.transform.SetParent(_target.transform, false);
            volume.transform.localPosition = localPosition;
            var collider = volume.AddComponent<BoxCollider>();
            collider.size = new Vector3(0.2f, 0.3f, 0.2f);
            collider.isTrigger = true;
            volume.AddComponent<HitRegionVolume>().SetRegion(region);
        }
    }
}
