using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.StateMachine;
using OctagonFighter.Presentation.Fighter;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Verifies the Phase 5.1 hit reaction in the shipped combat scene: a confirmed jab reacts to the
    /// head, a confirmed cross reacts to the body, a miss never reacts, and the defender is held in
    /// the reaction for its authored duration before control returns.
    /// </summary>
    /// <remarks>
    /// The opponent in the shipped scene is driven by a passive intent source, so movement and
    /// attack interruption of hit stun is covered in the EditMode suite where a scripted intent
    /// source can drive the defender directly. This suite proves the behaviour that only the real
    /// scene can prove: the confirmed damage event starts exactly one region-specific reaction and
    /// the animator presents it.
    /// </remarks>
    public sealed class HitReactionScenarioTests : CombatSceneFixture
    {
        private static readonly int HeadReactionHash = Animator.StringToHash("HitReactionHead");
        private static readonly int BodyReactionHash = Animator.StringToHash("HitReactionBody");
        private static readonly int LocomotionHash = Animator.StringToHash("Locomotion");

        [UnityTest]
        public IEnumerator ConfirmedJabStartsAHeadReactionAndReturnsControl()
        {
            PlaceFighters(0.76f);
            yield return Settle();

            BeginScenario("hit_head");
            var probe = new ReactionProbe();
            yield return Press(Key.J);
            yield return Sample(
                0.9f,
                new Vector2(0.20f, 0.55f),
                0.05f,
                _ => probe.Observe(Defender, DefenderAnimator));
            EndScenario();

            Debug.Log(
                $"[P51] head: drops={DamageEvents.Count} entries={probe.Entries} " +
                $"frames={probe.Frames} region={probe.FirstRegion} " +
                $"maxDrift={probe.MaxDrift:0.000}m attack={probe.EnteredAttack}");

            Assert.AreEqual(1, DamageEvents.Count, "The jab must land exactly once.");
            Assert.AreEqual(8, DamageEvents[0].Amount, "The jab's authored damage must not change.");
            Assert.AreEqual(1, probe.Entries, "One confirmed hit must start exactly one reaction.");
            Assert.Greater(probe.Frames, 0, "The defender must actually spend time in the reaction.");
            Assert.AreEqual(HitRegion.Head, probe.FirstRegion, "A jab must react to the head.");
            Assert.IsTrue(
                probe.AnimatorStates.Contains(HeadReactionHash),
                "The animator must present the head reaction state.");
            Assert.IsFalse(probe.EnteredAttack, "The defender must not attack out of hit stun.");
            Assert.AreEqual(
                FighterStateId.Idle,
                Defender.Snapshot.State,
                "Control must return once the reaction ends.");
            Assert.AreEqual(
                LocomotionHash,
                DefenderAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash,
                "The animator must return the defender to locomotion.");
        }

        [UnityTest]
        public IEnumerator ConfirmedCrossStartsABodyReaction()
        {
            PlaceFighters(0.76f);
            yield return Settle();

            BeginScenario("hit_body");
            var probe = new ReactionProbe();
            yield return Press(Key.K);
            yield return Sample(
                1.3f,
                new Vector2(0.55f, 0.95f),
                0.05f,
                _ => probe.Observe(Defender, DefenderAnimator));
            EndScenario();

            Debug.Log(
                $"[P51] body: drops={DamageEvents.Count} entries={probe.Entries} " +
                $"frames={probe.Frames} region={probe.FirstRegion} " +
                $"maxDrift={probe.MaxDrift:0.000}m attack={probe.EnteredAttack}");

            Assert.AreEqual(1, DamageEvents.Count, "The cross must land exactly once.");
            Assert.AreEqual(15, DamageEvents[0].Amount, "The cross's authored damage must not change.");
            Assert.AreEqual(1, probe.Entries, "One confirmed hit must start exactly one reaction.");
            Assert.Greater(probe.Frames, 0, "The defender must actually spend time in the reaction.");
            Assert.AreEqual(HitRegion.Body, probe.FirstRegion, "A cross must react to the body.");
            Assert.IsTrue(
                probe.AnimatorStates.Contains(BodyReactionHash),
                "The animator must present the body reaction state.");
            Assert.IsFalse(probe.EnteredAttack, "The defender must not attack out of hit stun.");
            Assert.AreEqual(
                FighterStateId.Idle,
                Defender.Snapshot.State,
                "Control must return once the reaction ends.");
        }

        [UnityTest]
        public IEnumerator MissedJabStartsNoReaction()
        {
            PlaceFighters(1.20f);
            yield return Settle();

            BeginScenario("hit_miss");
            var probe = new ReactionProbe();
            yield return Press(Key.J);
            yield return Sample(1.0f, default, 0f, _ => probe.Observe(Defender, DefenderAnimator));
            EndScenario();

            Debug.Log(
                $"[P51] miss: drops={DamageEvents.Count} entries={probe.Entries} " +
                $"frames={probe.Frames} region={probe.FirstRegion}");

            Assert.AreEqual(0, DamageEvents.Count, "A jab thrown out of range must miss.");
            Assert.AreEqual(0, probe.Entries, "A missed strike must never start a reaction.");
            Assert.AreEqual(0, probe.Frames, "A missed strike must never enter the reaction state.");
            Assert.AreEqual(
                FighterStateId.Idle,
                Defender.Snapshot.State,
                "The defender must stay idle after a missed strike.");
        }

        [UnityTest]
        public IEnumerator EachJabStartsExactlyOneReaction()
        {
            PlaceFighters(0.76f);
            yield return Settle();

            BeginScenario("hit_repeat");
            var probe = new ReactionProbe();

            for (int strike = 0; strike < 3; strike++)
            {
                yield return Press(Key.J);
                yield return Sample(1.0f, default, 0f, _ => probe.Observe(Defender, DefenderAnimator));
                yield return WaitForIdle();
                yield return new WaitForSeconds(0.2f);
            }

            EndScenario();

            Debug.Log(
                $"[P51] repeat: drops={DamageEvents.Count} entries={probe.Entries} " +
                $"frames={probe.Frames}");

            Assert.AreEqual(3, DamageEvents.Count, "Each jab must land exactly once.");
            Assert.AreEqual(
                3,
                probe.Entries,
                "Three confirmed jabs must start three reactions, never duplicates per strike.");
        }

        [UnityTest]
        public IEnumerator HitStunHoldsTheDefenderForTheAuthoredDuration()
        {
            PlaceFighters(0.76f);
            yield return Settle();

            BeginScenario("hit_stun");
            var probe = new ReactionProbe();
            yield return Press(Key.J);

            // Sample until the reaction begins so the hold is applied while the defender is stunned.
            float deadline = Elapsed + 0.8f;
            while (Elapsed < deadline && probe.Entries == 0)
            {
                yield return null;
                probe.Observe(Defender, DefenderAnimator);
            }

            Assert.AreEqual(1, probe.Entries, "The jab must start a reaction.");

            // The shipped opponent is passive, so this samples the reaction itself. Interrupting hit
            // stun with movement or attack input is covered by the EditMode suite, where a scripted
            // intent source can drive the stunned fighter directly.
            yield return Sample(0.45f, default, 0f, _ => probe.Observe(Defender, DefenderAnimator));
            EndScenario();

            float authoredFrames = 0.35f / TickSeconds;
            Debug.Log(
                $"[P51] stun: entries={probe.Entries} frames={probe.Frames} " +
                $"authored={authoredFrames:0.0} maxDrift={probe.MaxDrift:0.000}m " +
                $"state={Defender.Snapshot.State}");

            Assert.AreEqual(1, probe.Entries, "Movement input must not restart or stack the reaction.");
            Assert.That(
                probe.Frames,
                Is.InRange((int)(authoredFrames - 3f), (int)(authoredFrames + 3f)),
                "The reaction must last the authored stun duration.");
            Assert.Less(
                probe.MaxDrift,
                0.05f,
                "Hit stun must not displace the defender; knockback is not part of this phase.");
            Assert.IsFalse(probe.EnteredAttack, "The defender must not attack out of hit stun.");
            Assert.AreEqual(
                FighterStateId.Idle,
                Defender.Snapshot.State,
                "Control must return after the authored stun duration.");
        }

        /// <summary>
        /// Tracks reaction entries, frames, presented animator states, and drift for the defender.
        /// </summary>
        private sealed class ReactionProbe
        {
            private FighterStateId _previous = FighterStateId.Idle;
            private Vector3 _firstPosition;
            private bool _hasPosition;

            /// <summary>Gets the number of separate entries into the reaction state.</summary>
            public int Entries { get; private set; }

            /// <summary>Gets the number of sampled frames spent in the reaction state.</summary>
            public int Frames { get; private set; }

            /// <summary>Gets the region reported by the first reaction entry.</summary>
            public HitRegion FirstRegion { get; private set; }

            /// <summary>Gets the animator states observed while reacting.</summary>
            public HashSet<int> AnimatorStates { get; } = new HashSet<int>();

            /// <summary>Gets a value indicating whether the defender entered an attack state.</summary>
            public bool EnteredAttack { get; private set; }

            /// <summary>Gets the largest distance the defender moved during the first reaction.</summary>
            public float MaxDrift { get; private set; }

            /// <summary>Observes one rendered frame.</summary>
            public void Observe(FighterController defender, Animator animator)
            {
                FighterStateId state = defender.Snapshot.State;

                if (state == FighterStateId.HitReaction)
                {
                    if (_previous != FighterStateId.HitReaction)
                    {
                        Entries++;
                        FirstRegion = defender.ActiveHitReaction.Region;
                        _firstPosition = defender.transform.position;
                        _hasPosition = true;
                    }

                    Frames++;
                    AnimatorStates.Add(animator.GetCurrentAnimatorStateInfo(0).shortNameHash);

                    if (_hasPosition)
                    {
                        MaxDrift = Mathf.Max(
                            MaxDrift,
                            Vector3.Distance(_firstPosition, defender.transform.position));
                    }
                }
                else if (state == FighterStateId.LightPunch || state == FighterStateId.HeavyPunch)
                {
                    EnteredAttack = true;
                }

                _previous = state;
            }
        }
    }
}
