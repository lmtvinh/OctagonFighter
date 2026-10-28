using NUnit.Framework;
using OctagonFighter.Domain.Fighters;

namespace OctagonFighter.Tests.EditMode
{
    /// <summary>
    /// Verifies direction-aware reference speeds used to normalize locomotion playback.
    /// </summary>
    public sealed class MovementProfileTests
    {
        [Test]
        public void WalkLateralReferenceUsesTheWalkMultiplier()
        {
            MovementProfile profile = CreateProfile();

            Assert.AreEqual(0.45f, profile.GetPlanarSpeed(1f, 0f, 0.55f), 0.001f);
        }

        [Test]
        public void RunLateralReferenceUsesConfiguredLateralSpeed()
        {
            MovementProfile profile = CreateProfile();

            Assert.AreEqual(0.818f, profile.GetPlanarSpeed(1f, 0f), 0.001f);
        }

        [Test]
        public void ForwardAndBackwardReferencesUseTheirOwnSpeeds()
        {
            MovementProfile profile = CreateProfile();

            Assert.AreEqual(1.43f, profile.GetPlanarSpeed(0f, 1f, 0.55f), 0.001f);
            Assert.AreEqual(1.155f, profile.GetPlanarSpeed(0f, -1f, 0.55f), 0.001f);
        }

        [Test]
        public void DiagonalReferenceCombinesLateralAndForwardSpeeds()
        {
            MovementProfile profile = CreateProfile();
            const float component = 0.70710678f;
            float expected = UnityEngine.Mathf.Sqrt(
                (component * 0.818f * component * 0.818f) +
                (component * 2.6f * component * 2.6f)) * 0.55f;

            Assert.AreEqual(
                expected,
                profile.GetPlanarSpeed(component, component, 0.55f),
                0.001f);
        }

        private static MovementProfile CreateProfile()
        {
            return new MovementProfile(
                2.6f,
                2.1f,
                0.818f,
                0.25f,
                0.4f,
                0.35f,
                1.5f,
                0.8f,
                0.55f,
                720f);
        }
    }
}
