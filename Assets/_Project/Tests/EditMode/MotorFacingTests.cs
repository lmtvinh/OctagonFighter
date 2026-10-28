using NUnit.Framework;
using OctagonFighter.Core.Math;
using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.StateMachine;
using OctagonFighter.Infrastructure.Movement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OctagonFighter.Tests.EditMode
{
    /// <summary>
    /// Verifies that facing is opt-in per tick so a committed strike cannot home onto the opponent.
    /// </summary>
    public sealed class MotorFacingTests
    {
        private const float Step = 1f / 60f;

        private GameObject _attackerObject;
        private GameObject _opponentObject;
        private FighterMotor _attacker;

        [SetUp]
        public void SetUp()
        {
            var profile = new MovementProfile(
                2.6f,
                2.1f,
                2.3f,
                0.35f,
                0.4f,
                0.35f,
                1.5f,
                0.8f,
                0.55f,
                720f);

            _attackerObject = new GameObject("FacingTestAttacker");
            _attackerObject.transform.position = Vector3.zero;
            _attackerObject.transform.rotation = Quaternion.identity;
            _attacker = _attackerObject.AddComponent<FighterMotor>();
            _attacker.Initialize(profile);

            // The opponent stands 90 degrees to the attacker's right.
            _opponentObject = new GameObject("FacingTestOpponent");
            _opponentObject.transform.position = new Vector3(3f, 0f, 0f);
            _opponentObject.AddComponent<FighterMotor>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_attackerObject != null)
            {
                Object.DestroyImmediate(_attackerObject);
            }

            if (_opponentObject != null)
            {
                Object.DestroyImmediate(_opponentObject);
            }
        }

        [Test]
        public void LockedFacingKeepsTheCommittedStrikeOffLine()
        {
            for (int index = 0; index < 120; index++)
            {
                _attacker.Tick(
                    Float2.Zero,
                    MovementGait.Walk,
                    MovementAuthority.None,
                    false,
                    Step);
            }

            Assert.AreEqual(
                0f,
                _attacker.transform.eulerAngles.y,
                0.01f,
                "A punch thrown off-line must not rotate onto the opponent.");
        }

        [Test]
        public void FreeFacingStillTurnsTowardTheOpponent()
        {
            for (int index = 0; index < 120; index++)
            {
                _attacker.Tick(
                    Float2.Zero,
                    MovementGait.Walk,
                    MovementAuthority.None,
                    true,
                    Step);
            }

            Assert.AreEqual(
                90f,
                _attacker.transform.eulerAngles.y,
                1f,
                "Locomotion and guard states must keep facing the opponent.");
        }

        [Test]
        public void LockedFacingStillAppliesAuthorizedMovement()
        {
            for (int index = 0; index < 30; index++)
            {
                _attacker.Tick(
                    new Float2(0f, 1f),
                    MovementGait.Walk,
                    MovementAuthority.None,
                    false,
                    Step);
            }

            Assert.AreEqual(
                0f,
                _attacker.transform.eulerAngles.y,
                0.01f,
                "Facing is locked while movement authority is none.");

            for (int index = 0; index < 30; index++)
            {
                _attacker.Tick(
                    new Float2(0f, 1f),
                    MovementGait.Walk,
                    MovementAuthority.Full,
                    false,
                    Step);
            }

            Assert.Greater(
                _attacker.transform.position.z,
                0.01f,
                "A state that allows facing to be locked still moves when it has authority.");
        }
    }
}
