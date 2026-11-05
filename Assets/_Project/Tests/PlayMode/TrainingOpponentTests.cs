using System.Collections;
using System.Reflection;
using NUnit.Framework;
using OctagonFighter.Application.Fighters;
using OctagonFighter.Core.Math;
using OctagonFighter.Core.Time;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.Health;
using OctagonFighter.Domain.StateMachine;
using OctagonFighter.Presentation.Fighter;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Verifies the training opponent reuses the real runtime, movement, attack, and guard
    /// systems: it approaches, retreats, holds block, and strikes only when in range, off
    /// cooldown, and not blocking/stunned/defeated.
    /// </summary>
    public sealed class TrainingOpponentTests : CombatSceneFixture
    {
        private const float CloseDistance = 0.76f;
        private const float FarDistance = 2.5f;

        private GameObject _trainingSourceObject;
        private TrainingIntentSource _trainingSource;
        private TrainingIntentPolicy _trainingPolicy;

        [UnityTearDown]
        public new void TearDown()
        {
            if (_trainingSourceObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_trainingSourceObject);
                _trainingSourceObject = null;
                _trainingSource = null;
                _trainingPolicy = null;
            }
        }

        [UnityTest]
        public IEnumerator PassiveModeNeverMovesOrAttacks()
        {
            PlaceFighters(FarDistance);
            yield return Settle();
            SetTrainingMode(TrainingMode.Passive);

            BeginScenario("train_passive");
            yield return Sample(1f);
            EndScenario();

            Vector3 start = Defender.transform.position;
            // Sample a bit more to give movement a chance.
            yield return new WaitForSeconds(0.5f);

            Assert.AreEqual(FighterStateId.Idle, Defender.Snapshot.State, "Passive defender must stay idle.");
            Assert.AreEqual(0f, Defender.CurrentGuard, 0f, "Passive defender must not block.");
            Assert.Less(
                Vector3.Distance(start, Defender.transform.position),
                0.01f,
                "Passive defender must not move.");
        }

        [UnityTest]
        public IEnumerator HoldBlockModeKeepsGuardRaised()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            SetTrainingMode(TrainingMode.HoldBlock);

            BeginScenario("train_holdblock");
            yield return WaitForDefenderState(FighterStateId.Blocking, 2f);
            EndScenario();

            Assert.AreEqual(FighterStateId.Blocking, Defender.Snapshot.State, "Hold block must keep the guard raised.");
            Assert.IsTrue(Defender.IsBlocking, "The defender must be blocking.");
        }

        [UnityTest]
        public IEnumerator ApproachModeClosesDistanceUntilNear()
        {
            PlaceFighters(FarDistance);
            yield return Settle();
            SetTrainingMode(TrainingMode.Approach);
            _policy().StrikeRange = 0.95f;
            _policy().ApproachDistance = 0.9f;

            BeginScenario("train_approach");
            Vector3 start = Defender.transform.position;
            yield return Sample(1.5f);
            EndScenario();

            // The defender should move toward the attacker (defender starts at +distance/2, attacker at -).
            Assert.Greater(
                Defender.transform.position.z,
                start.z,
                "Approach mode must move the defender toward the attacker.");
            Assert.Less(
                Vector3.Distance(Defender.transform.position, Attacker.transform.position),
                FarDistance,
                "Approach mode must close the distance.");
        }

        [UnityTest]
        public IEnumerator RetreatModeOpensDistanceUntilFar()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            SetTrainingMode(TrainingMode.Retreat);
            _policy().ApproachDistance = 1.5f;

            BeginScenario("train_retreat");
            Vector3 start = Defender.transform.position;
            yield return Sample(1.5f);
            EndScenario();

            // The defender should move away from the attacker.
            Assert.Greater(
                Vector3.Distance(Defender.transform.position, Attacker.transform.position),
                Vector3.Distance(start, Attacker.transform.position),
                "Retreat mode must open the distance.");
        }

        [UnityTest]
        public IEnumerator JabModeStrikesWhenInRangeAndOffCooldown()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            SetTrainingMode(TrainingMode.Jab);
            _policy().StrikeRange = 1.0f;

            BeginScenario("train_jab");
            yield return WaitForDefenderState(FighterStateId.LightPunch, 2f);
            EndScenario();

            Assert.AreEqual(
                FighterStateId.LightPunch,
                Defender.Snapshot.State,
                "Jab mode must throw a jab when in range and off cooldown.");
        }

        [UnityTest]
        public IEnumerator CrossModeStrikesWhenInRangeAndOffCooldown()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            SetTrainingMode(TrainingMode.Cross);
            _policy().StrikeRange = 1.1f;

            BeginScenario("train_cross");
            yield return WaitForDefenderState(FighterStateId.HeavyPunch, 2f);
            EndScenario();

            Assert.AreEqual(
                FighterStateId.HeavyPunch,
                Defender.Snapshot.State,
                "Cross mode must throw a cross when in range and off cooldown.");
        }

        [UnityTest]
        public IEnumerator JabCrossModeAlternatesStrikes()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            SetTrainingMode(TrainingMode.JabCross);
            _policy().StrikeRange = 1.1f;
            _policy().ApproachDistance = 0.5f;

            BeginScenario("train_jabcross");
            yield return WaitForDefenderState(FighterStateId.LightPunch, 2f);
            // After the jab's cooldown (0.35 s), the cross should fire.
            yield return new WaitForSeconds(0.5f);
            yield return WaitForDefenderState(FighterStateId.HeavyPunch, 2f);
            EndScenario();

            Assert.AreEqual(
                FighterStateId.HeavyPunch,
                Defender.Snapshot.State,
                "Jab/cross must alternate jab then cross.");
        }

        [UnityTest]
        public IEnumerator JabModeNeverStrikesWhileBlocking()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            SetTrainingMode(TrainingMode.Jab);
            _policy().StrikeRange = 1.0f;

            // The defender holds its own block via the training source? No: HoldBlock is a separate
            // mode. Here, manually hold the defender's block while the mode is Jab.
            yield return BlockDefender();
            Assert.AreEqual(FighterStateId.Blocking, Defender.Snapshot.State);

            BeginScenario("train_jab_blocking");
            yield return Sample(1.5f);
            EndScenario();

            Assert.AreNotEqual(
                FighterStateId.LightPunch,
                Defender.Snapshot.State,
                "Jab mode must not strike while the defender is blocking.");
        }

        [UnityTest]
        public IEnumerator JabModeNeverStrikesWhileDefeated()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();
            SetTrainingMode(TrainingMode.Jab);
            _policy().StrikeRange = 1.0f;

            // Lethally damage the defender so it is defeated before training starts.
            float healthBefore = Defender.Snapshot.CurrentHealth;
            Defender.ApplyDamage(new DamageEvent(
                Attacker.Id,
                Defender.Id,
                healthBefore,
                DamageType.Impact,
                HitRegion.Head,
                TickId.Zero));
            yield return null;
            Assert.AreEqual(FighterStateId.Defeated, Defender.Snapshot.State, "The defender must be defeated.");

            BeginScenario("train_jab_defeated");
            yield return Sample(1.5f);
            EndScenario();

            Assert.AreEqual(
                FighterStateId.Defeated,
                Defender.Snapshot.State,
                "A defeated defender must remain defeated and never jab.");
        }

        private void SetTrainingMode(TrainingMode mode)
        {
            Assert.IsNotNull(Defender, "The defender must be initialized.");
            _trainingSourceObject = new GameObject("TrainingIntentSource");
            _trainingSource = _trainingSourceObject.AddComponent<TrainingIntentSource>();
            _trainingPolicy = new TrainingIntentPolicy { Mode = mode };
            SetPrivate(_trainingSource, "_fighter", Defender);
            SetPrivate(_trainingSource, "_opponent", Attacker.transform);
            SetPrivate(_trainingSource, "_policy", _trainingPolicy);

            Defender.SetIntentSource(_trainingSource);
        }

        private TrainingIntentPolicy _policy() => _trainingPolicy;

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().FullName}.");
            field.SetValue(target, value);
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
