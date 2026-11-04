using System.Collections;
using NUnit.Framework;
using OctagonFighter.Domain.StateMachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Verifies the trimmed Cross presentation: the right hand leads toward the opponent, the
    /// animator uses the Cross state, and the fighter does not pick up root translation.
    /// </summary>
    public sealed class CrossScenarioTests : CombatSceneFixture
    {
        private const float CloseDistance = 0.76f;

        [UnityTest]
        public IEnumerator CrossExtendsTheRightHandTowardTheOpponent()
        {
            PlaceFighters(CloseDistance);
            yield return Settle();

            Transform leftHand = AttackerAnimator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform rightHand = AttackerAnimator.GetBoneTransform(HumanBodyBones.RightHand);
            Assert.IsNotNull(leftHand);
            Assert.IsNotNull(rightHand);

            float maxLeftForward = float.MinValue;
            float maxRightForward = float.MinValue;
            Vector3 startPosition = Attacker.transform.position;
            int crossHash = Animator.StringToHash("Cross");
            bool sawCrossState = false;

            BeginScenario("cross_right_hand");
            yield return Press(Key.K);
            yield return Sample(0.7f, default, 0f, _ =>
            {
                Vector3 left = Attacker.transform.InverseTransformPoint(leftHand.position);
                Vector3 right = Attacker.transform.InverseTransformPoint(rightHand.position);
                maxLeftForward = Mathf.Max(maxLeftForward, left.z);
                maxRightForward = Mathf.Max(maxRightForward, right.z);
                sawCrossState |= AttackerAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == crossHash;
            });
            EndScenario();

            Assert.IsTrue(sawCrossState, "The animator must enter the Cross state.");
            Assert.AreEqual(1, DamageEvents.Count, "The trimmed cross must still land exactly once.");
            Assert.AreEqual(15, DamageEvents[0].Amount);
            Assert.Greater(
                maxRightForward,
                maxLeftForward,
                "A cross must extend the right hand farther forward than the left hand.");
            Assert.AreEqual(
                startPosition.x,
                Attacker.transform.position.x,
                0.08f,
                "Cross must not introduce unintended planar root translation.");
            Assert.AreEqual(
                startPosition.z,
                Attacker.transform.position.z,
                0.08f,
                "Cross must not introduce unintended planar root translation.");
            Assert.AreEqual(FighterStateId.Idle, Attacker.Snapshot.State);
        }
    }
}
