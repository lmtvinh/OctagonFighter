using System;
using NUnit.Framework;
using OctagonFighter.Domain.Guard;

namespace OctagonFighter.Tests.EditMode
{
    /// <summary>
    /// Verifies the guard resource independently of the state machine: blocking guard damage is
    /// amplified by impact strength and low guard within authored bounds, a depleted guard breaks,
    /// and recovery is delayed while blocking or toggling the guard cannot refresh it.
    /// </summary>
    public sealed class GuardModelTests
    {
        private GuardProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _profile = GuardProfile.CreateDefault();
        }

        [Test]
        public void GuardStartsFullAndUnbroken()
        {
            var guard = new GuardModel(_profile);

            Assert.AreEqual(_profile.MaxGuard, guard.MaxGuard, 0.001f);
            Assert.AreEqual(_profile.MaxGuard, guard.CurrentGuard, 0.001f);
            Assert.IsFalse(guard.IsBroken);
        }

        [Test]
        public void ZeroImpactHitRemovesExactlyTheAuthoredGuardDamage()
        {
            var guard = new GuardModel(_profile);
            int changes = 0;
            guard.Changed += _ => changes++;

            GuardDamageResult result = guard.ApplyGuardDamage(10f, impactStrength: 0f);

            Assert.AreEqual(1f, result.Multiplier, 0.0001f);
            Assert.AreEqual(10f, result.AppliedGuardDamage, 0.001f);
            Assert.AreEqual(90f, guard.CurrentGuard, 0.001f);
            Assert.IsFalse(result.BecameBroken);
            Assert.IsFalse(guard.IsBroken);
            Assert.AreEqual(1, changes, "Applying guard damage must raise exactly one change event.");
        }

        [Test]
        public void ImpactStrengthAmplifiesGuardDamage()
        {
            var lowImpact = new GuardModel(_profile);
            var highImpact = new GuardModel(_profile);

            GuardDamageResult low = lowImpact.ApplyGuardDamage(10f, impactStrength: 0f);
            GuardDamageResult high = highImpact.ApplyGuardDamage(10f, impactStrength: 1f);

            Assert.AreEqual(1f, low.Multiplier, 0.0001f);
            Assert.AreEqual(1f + _profile.ImpactGuardDamageBonus, high.Multiplier, 0.0001f);
            Assert.Greater(
                high.AppliedGuardDamage,
                low.AppliedGuardDamage,
                "A heavier impact must remove more guard than a light one.");
        }

        [Test]
        public void LowGuardAmplifiesGuardDamage()
        {
            var guard = new GuardModel(_profile);

            // Three unamplified hits leave 10 guard at full-impact-free strength.
            guard.ApplyGuardDamage(30f, 0f);
            guard.ApplyGuardDamage(30f, 0f);
            guard.ApplyGuardDamage(30f, 0f);
            Assert.AreEqual(10f, guard.CurrentGuard, 0.001f);

            GuardDamageResult result = guard.ApplyGuardDamage(5f, impactStrength: 0f);

            float expectedMultiplier = 1f + (_profile.LowGuardDamageBonus * 0.9f);
            Assert.AreEqual(expectedMultiplier, result.Multiplier, 0.0001f);
            Assert.AreEqual(5f * expectedMultiplier, result.AppliedGuardDamage, 0.001f);
            Assert.AreEqual(10f - (5f * expectedMultiplier), guard.CurrentGuard, 0.001f);
            Assert.IsFalse(result.BecameBroken);
        }

        [Test]
        public void GuardDamageMultiplierIsBoundedAtTheConfiguredMaximum()
        {
            var profile = new GuardProfile(
                maxGuard: 1000f,
                blockedHealthDamageMultiplier: _profile.BlockedHealthDamageMultiplier,
                recoveryDelaySeconds: _profile.RecoveryDelaySeconds,
                recoveryPerSecond: _profile.RecoveryPerSecond,
                blockImpactSeconds: _profile.BlockImpactSeconds,
                guardBreakSeconds: _profile.GuardBreakSeconds,
                pushbackDistanceMeters: _profile.PushbackDistanceMeters,
                impactGuardDamageBonus: _profile.ImpactGuardDamageBonus,
                lowGuardDamageBonus: _profile.LowGuardDamageBonus,
                minimumGuardDamageMultiplier: _profile.MinimumGuardDamageMultiplier,
                maximumGuardDamageMultiplier: _profile.MaximumGuardDamageMultiplier,
                directGuardDamagePerDamagePoint: _profile.DirectGuardDamagePerDamagePoint,
                directBlockStaminaCost: _profile.DirectBlockStaminaCost,
                guardBreakRecoveryFraction: _profile.GuardBreakRecoveryFraction);

            var guard = new GuardModel(profile);
            guard.ApplyGuardDamage(999f, 0f);

            GuardDamageResult result = guard.ApplyGuardDamage(5f, impactStrength: 1f);

            Assert.AreEqual(
                _profile.MaximumGuardDamageMultiplier,
                result.Multiplier,
                0.001f,
                "The dynamic low-guard/impact multiplier must never exceed the authored cap.");
        }

        [Test]
        public void HitThatDepletesTheRemainingGuardBreaksIt()
        {
            var guard = new GuardModel(_profile);
            int brokenEvents = 0;
            guard.Broken += _ => brokenEvents++;

            guard.ApplyGuardDamage(90f, 0f);
            GuardDamageResult result = guard.ApplyGuardDamage(10f, 0f);

            Assert.IsTrue(result.BecameBroken);
            Assert.IsTrue(guard.IsBroken);
            Assert.AreEqual(0f, guard.CurrentGuard, 0.001f);
            Assert.AreEqual(1, brokenEvents, "A guard break must be announced exactly once.");
        }

        [Test]
        public void GuardDamageIsClampedToTheRemainingGuard()
        {
            var guard = new GuardModel(_profile);
            guard.ApplyGuardDamage(90f, 0f);

            GuardDamageResult result = guard.ApplyGuardDamage(50f, 0f);

            Assert.AreEqual(10f, result.AppliedGuardDamage, 0.001f);
            Assert.AreEqual(0f, guard.CurrentGuard, 0.001f);
            Assert.IsTrue(result.BecameBroken);
        }

        [Test]
        public void ZeroGuardDamageNeverBreaksTheGuard()
        {
            var guard = new GuardModel(_profile);

            GuardDamageResult result = guard.ApplyGuardDamage(0f, impactStrength: 1f);

            Assert.IsFalse(result.BecameBroken);
            Assert.IsFalse(guard.IsBroken);
            Assert.AreEqual(_profile.MaxGuard, guard.CurrentGuard, 0.001f);
        }

        [Test]
        public void RecoveryWaitsForTheAuthoredDelay()
        {
            var guard = new GuardModel(_profile);
            guard.ApplyGuardDamage(20f, 0f);

            guard.Tick(1f, recoveryPaused: false);
            Assert.AreEqual(
                80f,
                guard.CurrentGuard,
                0.001f,
                "Guard must not recover before the delay elapses.");

            guard.Tick(0.6f, recoveryPaused: false);

            float expected = 80f + (_profile.RecoveryPerSecond * 0.1f);
            Assert.AreEqual(expected, guard.CurrentGuard, 0.001f);
        }

        [Test]
        public void BlockingPausesRecoveryAndRestartsTheDelay()
        {
            var guard = new GuardModel(_profile);
            guard.ApplyGuardDamage(20f, 0f);
            guard.Tick(1f, recoveryPaused: false);

            guard.Tick(0.1f, recoveryPaused: true);

            Assert.AreEqual(
                _profile.RecoveryDelaySeconds,
                guard.RecoveryDelayRemaining,
                0.001f,
                "Blocking must pause the delay, not let it run down.");
            Assert.AreEqual(80f, guard.CurrentGuard, 0.001f);

            guard.Tick(1.4f, recoveryPaused: false);
            Assert.AreEqual(80f, guard.CurrentGuard, 0.001f, "The restarted delay must be honoured.");

            guard.Tick(0.2f, recoveryPaused: false);
            Assert.Greater(guard.CurrentGuard, 80f);
        }

        [Test]
        public void TogglingBlockCannotRefreshGuard()
        {
            var guard = new GuardModel(_profile);
            guard.ApplyGuardDamage(50f, 0f);

            for (int tick = 0; tick < 240; tick++)
            {
                // Alternate between guard-up and guard-down faster than the recovery delay. The
                // paused ticks keep restarting the delay, so no tick may regenerate guard.
                guard.Tick(1f / 60f, recoveryPaused: tick % 2 == 0);
            }

            Assert.AreEqual(
                50f,
                guard.CurrentGuard,
                0.001f,
                "Toggling the guard must not be a faster recovery path.");
        }

        [Test]
        public void BlockedHitRestartsTheRecoveryDelay()
        {
            var guard = new GuardModel(_profile);
            guard.ApplyGuardDamage(20f, 0f);
            guard.Tick(1f, recoveryPaused: false);

            guard.ApplyGuardDamage(5f, 0f);
            guard.Tick(1f, recoveryPaused: false);

            Assert.AreEqual(
                75f,
                guard.CurrentGuard,
                0.001f,
                "A new blocked hit must extend the delay before recovery resumes.");

            guard.Tick(0.6f, recoveryPaused: false);
            Assert.Greater(guard.CurrentGuard, 75f);
        }

        [Test]
        public void CompleteGuardBreakRestoresAPartialGuardAfterTheStun()
        {
            var guard = new GuardModel(_profile);
            guard.ApplyGuardDamage(100f, 0f);
            Assert.IsTrue(guard.IsBroken);

            guard.Tick(5f, recoveryPaused: true);
            Assert.AreEqual(0f, guard.CurrentGuard, 0.001f, "A broken guard must not recover during the stun.");

            guard.CompleteGuardBreak();

            float expected = _profile.MaxGuard * _profile.GuardBreakRecoveryFraction;
            Assert.IsFalse(guard.IsBroken);
            Assert.AreEqual(expected, guard.CurrentGuard, 0.001f);
            Assert.AreEqual(
                _profile.RecoveryDelaySeconds,
                guard.RecoveryDelayRemaining,
                0.001f,
                "The post-break guard must still respect the recovery delay.");
        }

        [Test]
        public void ChangedEventCarriesPreviousAndCurrentValues()
        {
            var guard = new GuardModel(_profile);
            GuardChangedEvent observed = default;
            guard.Changed += changed => observed = changed;

            guard.ApplyGuardDamage(15f, 0f);

            Assert.AreEqual(100f, observed.PreviousGuard, 0.001f);
            Assert.AreEqual(85f, observed.CurrentGuard, 0.001f);
            Assert.AreEqual(100f, observed.MaxGuard, 0.001f);
        }

        [Test]
        public void ProfileRejectsInvalidConfiguration()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new GuardProfile(
                maxGuard: 0f,
                blockedHealthDamageMultiplier: 0.25f,
                recoveryDelaySeconds: 1f,
                recoveryPerSecond: 10f,
                blockImpactSeconds: 0.2f,
                guardBreakSeconds: 1f,
                pushbackDistanceMeters: 0.2f,
                impactGuardDamageBonus: 0.5f,
                lowGuardDamageBonus: 0.5f,
                minimumGuardDamageMultiplier: 1f,
                maximumGuardDamageMultiplier: 2f,
                directGuardDamagePerDamagePoint: 1f,
                directBlockStaminaCost: 4f,
                guardBreakRecoveryFraction: 0.35f));

            Assert.Throws<ArgumentOutOfRangeException>(() => new GuardProfile(
                maxGuard: 100f,
                blockedHealthDamageMultiplier: 1.25f,
                recoveryDelaySeconds: 1f,
                recoveryPerSecond: 10f,
                blockImpactSeconds: 0.2f,
                guardBreakSeconds: 1f,
                pushbackDistanceMeters: 0.2f,
                impactGuardDamageBonus: 0.5f,
                lowGuardDamageBonus: 0.5f,
                minimumGuardDamageMultiplier: 1f,
                maximumGuardDamageMultiplier: 2f,
                directGuardDamagePerDamagePoint: 1f,
                directBlockStaminaCost: 4f,
                guardBreakRecoveryFraction: 0.35f));
        }
    }
}
