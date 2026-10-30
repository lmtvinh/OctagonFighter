using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OctagonFighter.Domain.StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Scripted Play Mode scenarios for the Phase 4 strike foundation: the real scene, the real
    /// input source, the real controller and state machine, sampled every frame and rendered.
    /// </summary>
    public sealed class StrikeScenarioTests : CombatSceneFixture
    {
        // Measured working distance: the imported jab/Cross clips extend the sampled hand ~0.52 m
        // forward, so the defender capsule surface (r = 0.30 m) must sit inside that reach. At
        // 1.05 m neither strike can physically touch the target; 0.76 m is the measured reliable range.
        private const float CloseDistance = 0.76f;
        private const float BackwardWalkSpeed = 2.1f * 0.55f;
        private const float LateralWalkSpeed = 0.818f * 0.55f;

        private readonly List<FrameSample> _frames = new List<FrameSample>();

        private struct FrameSample
        {
            public FighterStateId State;
            public float Yaw;
            public float FacingError;
            public Vector3 Position;
            public int AnimHash;
            public float Normalized;
        }

        [UnityTest]
        public IEnumerator T00_RangeSweep()
        {
            float[] distances = { 0.76f, 0.80f, 0.85f, 0.95f, 1.05f, 1.20f };

            foreach (float distance in distances)
            {
                PlaceFighters(distance);
                yield return Settle(0.5f);
                BeginScenario($"t00_range_{distance:0.00}");
                BeginFrames();

                yield return Press(Key.J);
                yield return Sample(1.0f, new Vector2(0.28f, 0.58f), 0.05f, _ => Track());

                string damage = DamageEvents.Count == 0
                    ? "none"
                    : string.Join(
                        ";",
                        DamageEvents.Select(entry => $"{entry.Amount}@{entry.Delay:0.000}s"));
                Debug.Log(
                    $"[P41] T00 distance={distance:0.00} drops={DamageEvents.Count} " +
                    $"damage={damage} minFistGap={MinFistGap():0.000}m " +
                    $"attackFacingDelta={MaxAttackFacingDelta():0.00}deg");

                EndScenario();
                yield return WaitForIdle();
                yield return new WaitForSeconds(0.25f);
            }
        }

        [UnityTest]
        public IEnumerator T01_JabAtCloseRange()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("t01_jab_close");
            BeginFrames();

            yield return Press(Key.J);
            yield return Sample(1.4f, new Vector2(0.22f, 0.75f), 0.03f, _ => Track());
            EndScenario();

            Log("T01 jab close");

            Assert.AreEqual(1, DamageEvents.Count, "A close jab must damage the opponent exactly once.");
            Assert.AreEqual(8, DamageEvents[0].Amount, "A jab must apply its authored 8 damage.");
            // Authored jab window: 0.26-0.42 s (FD_Boxer: startup 0.26 s, active 0.16 s). The
            // delay is measured from the first sampled attack frame, so allow one 60 Hz frame of
            // quantization on either side.
            Assert.That(
                DamageEvents[0].Delay,
                Is.InRange(0.24f, 0.44f),
                "Jab damage must land in the authored contact window (0.26-0.42 s).");
            Assert.Less(MaxAttackFacingDelta(), 0.5f, "The attacker must not turn during the jab.");
            Assert.Less(MinFistGap(), 0.08f, "The fist must actually reach the opponent's capsule.");
        }

        [UnityTest]
        public IEnumerator T02_CrossAtCloseRange()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("t02_cross_close");
            BeginFrames();

            yield return Press(Key.K);
            yield return Sample(1.6f, new Vector2(0.45f, 0.98f), 0.03f, _ => Track());
            EndScenario();

            Log("T02 cross close");

            Assert.AreEqual(1, DamageEvents.Count, "A close cross must damage the opponent exactly once.");
            Assert.AreEqual(15, DamageEvents[0].Amount, "A cross must apply its authored 15 damage.");
            // Authored cross window: 0.60-0.78 s (FD_Boxer: startup 0.60 s, active 0.18 s). The
            // delay is measured from the first sampled attack frame, so allow one 60 Hz frame of
            // quantization on either side.
            Assert.That(
                DamageEvents[0].Delay,
                Is.InRange(0.58f, 0.80f),
                "Cross damage must land in the authored contact window (0.60-0.78 s).");
            Assert.Less(MaxAttackFacingDelta(), 0.5f, "The attacker must not turn during the cross.");
            Assert.Less(MinFistGap(), 0.09f, "The fist must actually reach the opponent's capsule.");
        }

        [UnityTest]
        public IEnumerator T03_StrikesOutOfRangeDealNoDamage()
        {
            PlaceFighters(2.4f);
            yield return Settle();
            BeginScenario("t03_out_of_range");
            BeginFrames();

            yield return Press(Key.J);
            yield return Sample(1.0f, new Vector2(0.28f, 0.58f), 0.05f, _ => Track());
            yield return WaitForIdle();
            yield return new WaitForSeconds(0.2f);

            yield return Press(Key.K);
            yield return Sample(1.2f, new Vector2(0.5f, 0.85f), 0.05f, _ => Track());
            EndScenario();

            Log("T03 out of range");

            Assert.AreEqual(0, DamageEvents.Count, "Strikes thrown at 2.4 m must not reach the target.");
            Assert.AreEqual(
                FighterStateId.Idle,
                Attacker.Snapshot.State,
                "Both strikes must return to idle.");
        }

        [UnityTest]
        public IEnumerator T04_JabWhileOpponentRetreats()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("t04_retreat");
            BeginFrames();

            yield return Press(Key.J);
            yield return Sample(
                1.3f,
                new Vector2(0.22f, 0.75f),
                0.035f,
                delta =>
                {
                    Defender.transform.position += Vector3.right * (BackwardWalkSpeed * delta);
                    Physics.SyncTransforms();
                    Track();
                });
            EndScenario();

            Log("T04 retreat");

            Assert.Less(
                MaxAttackFacingDelta(),
                0.5f,
                "A retreating opponent must not drag the committed strike around.");
            Assert.AreEqual(0, DamageEvents.Count, "A jab cannot reach an opponent who walked away.");
            Assert.AreEqual(
                FighterStateId.Idle,
                Attacker.Snapshot.State,
                "The strike must finish and return to idle.");
        }

        [UnityTest]
        public IEnumerator T05_JabWhileOpponentStrafes()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("t05_strafe");
            BeginFrames();

            yield return Press(Key.J);
            yield return Sample(
                1.3f,
                new Vector2(0.22f, 0.75f),
                0.035f,
                delta =>
                {
                    Defender.transform.position += Vector3.forward * (LateralWalkSpeed * delta);
                    Physics.SyncTransforms();
                    Track();
                });
            EndScenario();

            Log("T05 strafe");

            Assert.Less(
                MaxAttackFacingDelta(),
                0.5f,
                "A strafing opponent must not drag the committed strike around.");
            Assert.AreEqual(
                FighterStateId.Idle,
                Attacker.Snapshot.State,
                "The strike must finish and return to idle.");
        }

        [UnityTest]
        public IEnumerator T06_JabWithAngularOffset()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();

            BeginScenario("t06_angular");
            BeginFrames();

            // Turn the attacker off-line at the last moment so the strike starts aiming away from
            // the opponent. The attack state owns facing, so the yaw measured while the strike runs
            // must not change again.
            PlaceFighters(CloseDistance, -75f);
            yield return Press(Key.J);
            yield return Sample(1.3f, new Vector2(0.20f, 0.70f), 0.035f, _ => Track());
            EndScenario();

            float firstOffset = FirstAttackFacingError();
            Log("T06 angular");
            Debug.Log($"[P41] T06 attack-start offset from opponent={firstOffset:0.0}deg");

            Assert.Less(
                MaxAttackFacingDelta(),
                0.5f,
                "A committed strike must keep the orientation it started with.");
            Assert.AreEqual(0, DamageEvents.Count, "A jab thrown 75 degrees off-line must miss.");
        }

        [UnityTest]
        public IEnumerator T07_RepeatedJabsEachRegisterOnce()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("t07_repeat");
            BeginFrames();

            for (int strike = 0; strike < 3; strike++)
            {
                yield return Press(Key.J);
                yield return Sample(1.0f, new Vector2(0.28f, 0.52f), 0.05f, _ => Track());
                yield return WaitForIdle();
                yield return new WaitForSeconds(0.2f);
            }

            EndScenario();

            Log("T07 repeat");

            Assert.AreEqual(3, DamageEvents.Count, "Each of the three jabs must land exactly once.");
            Assert.IsTrue(
                DamageEvents.All(entry => entry.Amount == 8),
                "Every jab must apply exactly 8 damage.");
            Assert.IsTrue(
                DamageEvents.All(entry => entry.Delay >= 0.24f && entry.Delay <= 0.46f),
                "Every jab must land inside the contact window.");
            Assert.AreEqual(
                76f,
                Defender.Snapshot.CurrentHealth,
                "Three jabs must remove exactly 24 health.");
        }

        [UnityTest]
        public IEnumerator T08_EvadedStrikeDealsNoDamage()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("t08_evade");
            BeginFrames();

            float retreating = 0f;
            yield return Press(Key.J);
            yield return Sample(
                1.3f,
                new Vector2(0.20f, 0.70f),
                0.035f,
                delta =>
                {
                    retreating += delta;

                    // The corrected 0.30 m body capsule puts the defender inside the jab's real
                    // reach if the evade starts 0.15 s late, so react before the contact window
                    // opens. The strike must still miss and finish.
                    if (retreating > 0.05f)
                    {
                        Defender.transform.position += Vector3.forward * (3f * delta);
                        Physics.SyncTransforms();
                    }

                    Track();
                });
            EndScenario();

            Log("T08 evade");

            Assert.AreEqual(0, DamageEvents.Count, "An evaded strike must not damage the opponent.");
            Assert.AreEqual(
                FighterStateId.Idle,
                Attacker.Snapshot.State,
                "The missed strike must still finish.");
        }

        [UnityTest]
        public IEnumerator T09_StrikeReturnsToLocomotion()
        {
            int jabHash = Animator.StringToHash("Jab");
            int locomotionHash = Animator.StringToHash("Locomotion");

            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("t09_return");
            BeginFrames();

            yield return Press(Key.J);
            yield return Sample(1.5f, new Vector2(0.30f, 1.05f), 0.03f, _ => Track());
            EndScenario();

            Log("T09 return");

            Assert.AreEqual(1, DamageEvents.Count, "The jab itself must still have landed.");

            List<FrameSample> attackFrames =
                _frames.Where(frame => frame.State == FighterStateId.LightPunch).ToList();
            Assert.IsNotEmpty(attackFrames, "The jab animation must have played.");
            Assert.IsTrue(
                attackFrames.Any(frame => frame.AnimHash == jabHash),
                "The animator must switch to the Jab state while the strike runs.");

            AnimatorStateInfo info = AttackerAnimator.GetCurrentAnimatorStateInfo(0);
            Debug.Log(
                $"[P41] T09 final animHash={info.fullPathHash} locomotion={locomotionHash} " +
                $"jab={jabHash} normalized={info.normalizedTime:0.00} " +
                $"state={Attacker.Snapshot.State}");

            Assert.AreEqual(
                FighterStateId.Idle,
                Attacker.Snapshot.State,
                "The strike must hand control back to locomotion.");
            Assert.AreEqual(
                locomotionHash,
                info.shortNameHash,
                "The animator must be back in the locomotion state.");

            // After the strike ends the animator must reach Locomotion within one return fade, and
            // must never fall back to an empty state in between.
            int firstLocomotion = _frames.FindIndex(
                frame => frame.State == FighterStateId.Idle && frame.AnimHash == locomotionHash);
            Assert.GreaterOrEqual(firstLocomotion, 0, "Locomotion must resume after the strike.");
            Assert.IsTrue(
                _frames.Skip(firstLocomotion).All(frame => frame.AnimHash != 0),
                "The animator must never be left in an empty state after the strike.");
        }

        [UnityTest]
        public IEnumerator T10_LocomotionStaysStableAroundAStrike()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            BeginScenario("t10_locomotion");
            BeginFrames();

            yield return Hold(Key.D);
            yield return Sample(0.8f, default, 0f, _ => Track());
            yield return Release(Key.D);
            yield return Sample(0.3f, default, 0f, _ => Track());

            yield return Press(Key.J);
            yield return Sample(1.1f, new Vector2(0.25f, 0.75f), 0.04f, _ => Track());

            yield return Hold(Key.A);
            yield return Sample(0.6f, default, 0f, _ => Track());
            yield return Release(Key.A);
            yield return Sample(0.4f, default, 0f, _ => Track());
            EndScenario();

            float maxStep = MaxFrameStep();
            float locomotionFacingError = _frames
                .Where(frame => !IsAttack(frame.State) && frame.State != FighterStateId.Idle)
                .Select(frame => Mathf.Abs(frame.FacingError))
                .DefaultIfEmpty(0f)
                .Max();
            float attackFacingDrift = MaxAttackFacingErrorDrift();

            Debug.Log(
                $"[P41] T10 locomotion: state={Attacker.Snapshot.State} " +
                $"maxStep={maxStep:0.000}m locomotionFacingError={locomotionFacingError:0.0}deg " +
                $"attackFacingDrift={attackFacingDrift:0.00}deg " +
                $"pos={Attacker.transform.position} yaw={AttackerYaw:0.0}");

            Assert.AreEqual(
                FighterStateId.Idle,
                Attacker.Snapshot.State,
                "The fighter must settle back into idle after strafing and striking.");
            Assert.Less(maxStep, 0.06f, "Locomotion must not teleport between frames.");
            Assert.Less(
                locomotionFacingError,
                12f,
                "The fighter must keep facing the opponent while strafing.");
            Assert.Less(
                attackFacingDrift,
                0.5f,
                "Facing must stay locked while the strike is committed.");
        }

        private static bool IsAttack(FighterStateId state)
        {
            return state == FighterStateId.LightPunch || state == FighterStateId.HeavyPunch;
        }

        private void BeginFrames()
        {
            _frames.Clear();
        }

        private void Track()
        {
            Vector3 toDefender = Defender.transform.position - Attacker.transform.position;
            toDefender.y = 0f;
            float wantedYaw = toDefender.sqrMagnitude > 0.0001f
                ? Mathf.Atan2(toDefender.x, toDefender.z) * Mathf.Rad2Deg
                : AttackerYaw;
            AnimatorStateInfo info = AttackerAnimator.GetCurrentAnimatorStateInfo(0);
            _frames.Add(new FrameSample
            {
                State = Attacker.Snapshot.State,
                Yaw = AttackerYaw,
                FacingError = Mathf.DeltaAngle(wantedYaw, AttackerYaw),
                Position = Attacker.transform.position,
                AnimHash = info.shortNameHash,
                Normalized = info.normalizedTime
            });
        }

        private void Log(string label)
        {
            string damage = DamageEvents.Count == 0
                ? "none"
                : string.Join(
                    ";",
                    DamageEvents.Select(entry => $"{entry.Amount}@{entry.Delay:0.000}s"));
            float contactFrame = -1f;
            float minGap = MinFistGap(out contactFrame);
            Debug.Log(
                $"[P41] {label}: drops={DamageEvents.Count} damage={damage} " +
                $"minFistGap={minGap:0.000}m atScenario={contactFrame:0.000}s " +
                $"attackStart={AttackStartedElapsed:0.000}s " +
                $"attackFacingDelta={MaxAttackFacingDelta():0.00}deg");
        }

        private float MaxFrameStep()
        {
            float max = 0f;
            for (int index = 1; index < _frames.Count; index++)
            {
                Vector3 a = _frames[index - 1].Position;
                Vector3 b = _frames[index].Position;
                max = Mathf.Max(max, Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)));
            }

            return max;
        }

        private float MaxAttackFacingDelta()
        {
            float first = float.NaN;
            float max = 0f;
            foreach (FrameSample frame in _frames)
            {
                if (!IsAttack(frame.State))
                {
                    continue;
                }

                if (float.IsNaN(first))
                {
                    first = frame.Yaw;
                }

                max = Mathf.Max(max, Mathf.Abs(Mathf.DeltaAngle(first, frame.Yaw)));
            }

            return max;
        }

        private float MaxAttackFacingErrorDrift()
        {
            float first = float.NaN;
            float max = 0f;
            foreach (FrameSample frame in _frames)
            {
                if (!IsAttack(frame.State))
                {
                    continue;
                }

                if (float.IsNaN(first))
                {
                    first = frame.FacingError;
                }

                max = Mathf.Max(max, Mathf.Abs(Mathf.DeltaAngle(first, frame.FacingError)));
            }

            return max;
        }

        private float FirstAttackFacingError()
        {
            foreach (FrameSample frame in _frames)
            {
                if (IsAttack(frame.State))
                {
                    return frame.FacingError;
                }
            }

            return float.NaN;
        }

        private float MinFistGap()
        {
            return MinimumFistGap;
        }

        /// <summary>Gets the smallest fist-to-defender-capsule gap across sampled frames.</summary>
        private float MinFistGap(out float atScenarioSeconds)
        {
            atScenarioSeconds = MinimumFistGapTime;
            return MinimumFistGap;
        }
    }
}
