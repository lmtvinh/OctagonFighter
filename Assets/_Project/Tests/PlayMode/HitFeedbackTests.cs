using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Combat;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Verifies that exactly one defender-side <see cref="FighterController.HitFeedback"/> event is
    /// raised per resolved strike, carrying the collision-derived region and a blocked flag, for
    /// both blocked and unblocked hits.
    /// </summary>
    public sealed class HitFeedbackTests : CombatSceneFixture
    {
        private const float CloseDistance = 0.76f;

        [UnityTest]
        public IEnumerator BlockedHitRaisesExactlyOneHitFeedbackWithBlockedFlag()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            yield return BlockDefender();

            var feedback = new List<HitFeedback>();
            Defender.HitFeedback += feedback.Add;

            BeginScenario("hf_blocked");
            yield return Press(UnityEngine.InputSystem.Key.J);
            yield return Sample(1.0f);
            EndScenario();

            Defender.HitFeedback -= feedback.Add;

            Debug.Log(
                $"[HF] blocked: count={feedback.Count} region={((feedback.Count > 0) ? feedback[0].Region : HitRegion.Head)} blocked={((feedback.Count > 0) ? feedback[0].Blocked : false)}");

            Assert.AreEqual(1, feedback.Count, "A blocked jab must raise exactly one hit-feedback event.");
            Assert.AreEqual(1, BlockedHits.Count, "The jab must be reported as blocked.");
            Assert.AreEqual(HitRegion.Head, feedback[0].Region, "The jab must report the head region from collision data.");
            Assert.IsTrue(feedback[0].Blocked, "A blocked strike must set the blocked flag.");
        }

        [UnityTest]
        public IEnumerator UnblockedHitRaisesExactlyOneHitFeedback()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();

            var feedback = new List<HitFeedback>();
            Defender.HitFeedback += feedback.Add;

            BeginScenario("hf_unblocked");
            yield return Press(UnityEngine.InputSystem.Key.J);
            yield return Sample(1.0f);
            EndScenario();

            Defender.HitFeedback -= feedback.Add;

            Debug.Log(
                $"[HF] unblocked: count={feedback.Count} region={((feedback.Count > 0) ? feedback[0].Region : HitRegion.Head)} blocked={((feedback.Count > 0) ? feedback[0].Blocked : false)} damage={((feedback.Count > 0) ? feedback[0].DamageAmount : -1f)}");

            Assert.AreEqual(1, feedback.Count, "A landed jab must raise exactly one hit-feedback event.");
            Assert.AreEqual(1, DamageEvents.Count, "The jab must land exactly once.");
            Assert.AreEqual(HitRegion.Head, feedback[0].Region, "The jab must report the head region from collision data.");
            Assert.IsFalse(feedback[0].Blocked, "A landed strike must clear the blocked flag.");
            Assert.Greater(feedback[0].DamageAmount, 0f, "A landed strike must report applied damage.");
        }

        [UnityTest]
        public IEnumerator RepeatHitsRaiseOneFeedbackPerStrike()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            yield return BlockDefender();

            var feedback = new List<HitFeedback>();
            Defender.HitFeedback += feedback.Add;

            BeginScenario("hf_repeat");
            for (int strike = 0; strike < 3; strike++)
            {
                yield return Press(UnityEngine.InputSystem.Key.J);
                yield return Sample(1.0f, default, 0f);
                yield return WaitForIdle();
                yield return new WaitForSeconds(0.2f);
            }
            EndScenario();

            Defender.HitFeedback -= feedback.Add;

            Assert.AreEqual(3, feedback.Count, "Three blocked jabs must raise three feedback events, no duplicates per strike.");
            foreach (HitFeedback f in feedback)
            {
                Assert.IsTrue(f.Blocked, "Every repeat must remain blocked while the guard is held.");
            }
        }

        private IEnumerator BlockDefender()
        {
            SetDefenderIntentSource(new HeldBlockIntentSource());
            yield return WaitForDefenderState(FighterStateId.Blocking);
            yield return WaitForDefenderAnimatorState("Blocking");
        }

        private sealed class HeldBlockIntentSource : IFighterIntentSource
        {
            public bool TryGetIntent(TickId tick, out FighterIntent intent)
            {
                intent = new FighterIntent(
                    Float2.Zero,
                    FighterCommand.Block,
                    true,
                    true,
                    MovementGait.Walk,
                    tick);
                return true;
            }
        }
    }
}
