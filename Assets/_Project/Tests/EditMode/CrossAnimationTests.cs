using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace OctagonFighter.Tests.EditMode
{
    /// <summary>
    /// Verifies that the Cross animator state presents the trimmed right-straight clip rather than
    /// the full Mixamo jab-cross combination, and that root motion stays disabled on the fighter.
    /// </summary>
    public sealed class CrossAnimationTests
    {
        private const string ControllerPath = "Assets/_Project/Art/Animations/AC_Fighter_Base.controller";
        private const string CrossClipPath = "Assets/_Project/Art/Animations/BoxerCross.anim";
        private const string PrefabPath = "Assets/_Project/Prefabs/Fighters/PF_Fighter_Boxer.prefab";

        [Test]
        public void CrossClipIsTheTrimmedRightStraightSegment()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CrossClipPath);
            Assert.IsNotNull(clip, "BoxerCross.anim must exist as the dedicated Cross presentation.");

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            Assert.Greater(
                clip.length,
                0.5f,
                "The clip must contain the full right-straight, not a fraction of it.");
            Assert.LessOrEqual(
                clip.length,
                0.85f,
                "The Cross clip must be the right-straight segment, not the 2.233 s combo.");
            Assert.AreEqual(0f, settings.startTime, 0.0001f, "The trimmed clip starts at time zero.");
            Assert.AreEqual(
                clip.length,
                settings.stopTime,
                0.001f,
                "The clip's active range matches its baked length.");
            Assert.IsFalse(settings.loopTime, "A committed cross must not loop.");
        }

        [Test]
        public void CrossAnimatorStateUsesTheTrimmedClipAndCompatibleAvatar()
        {
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            Assert.IsNotNull(controller);

            AnimatorState cross = controller.layers[0].stateMachine.states
                .Select(child => child.state)
                .FirstOrDefault(state => state.name == "Cross");
            Assert.IsNotNull(cross, "The controller must contain a Cross state.");
            Assert.AreEqual("BoxerCross", cross.motion.name);
            Assert.GreaterOrEqual(cross.motion.averageDuration, 0.6f);
            Assert.LessOrEqual(cross.motion.averageDuration, 0.85f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Animator animator = prefab.GetComponentInChildren<Animator>();
            Assert.IsNotNull(animator);
            Assert.IsNotNull(animator.avatar);
            Assert.IsTrue(animator.avatar.isHuman, "The boxer avatar must remain Humanoid.");
            Assert.IsTrue(animator.avatar.isValid);
            Assert.IsFalse(animator.applyRootMotion, "Cross must not translate the fighter via root motion.");
        }
    }
}
