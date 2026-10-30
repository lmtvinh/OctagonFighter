using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace OctagonFighter.Tests.PlayMode
{
    /// <summary>
    /// Runtime guard for the defender's body hurtbox. The shipped scene instance must expose the
    /// authored upright capsule, and the animated torso and head bones must stay inside it while a
    /// jab and a cross are thrown at close range.
    /// </summary>
    /// <remarks>
    /// Only the torso/head bones are checked. The guard hands of a boxing idle can legitimately sit
    /// close to, or slightly inside, the body capsule; the striking fist is what the combat system
    /// resolves against the capsule, so hands are covered by the strike scenarios instead.
    /// </remarks>
    public sealed class FighterHurtboxTests : CombatSceneFixture
    {
        private const float InsideMargin = 0.02f;
        private const float FloorTolerance = 0.03f;

        private static readonly HumanBodyBones[] TorsoBones =
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.Neck,
            HumanBodyBones.Head
        };

        private CapsuleCollider _hurtbox;
        private float _maxPlanar;
        private float _minLocalY;
        private float _maxLocalY;
        private float _maxHurtboxOffset;
        private int _boneSamples;

        [UnityTest]
        public IEnumerator HurtboxMatchesTheAuthoredCapsule()
        {
            yield return Settle(0.4f);

            CharacterController controller = Defender.GetComponent<CharacterController>();
            _hurtbox = Defender.GetComponentInChildren<CapsuleCollider>(true);

            Assert.IsNotNull(controller, "The defender must have a CharacterController.");
            Assert.IsNotNull(_hurtbox, "The defender must have a body hurtbox CapsuleCollider.");
            Assert.IsTrue(_hurtbox.isTrigger, "The body hurtbox must be a trigger.");
            Assert.AreEqual(1, _hurtbox.direction, "The body hurtbox must be an upright capsule.");
            Assert.That(_hurtbox.radius, Is.GreaterThan(0f));
            Assert.That(_hurtbox.height, Is.GreaterThanOrEqualTo(_hurtbox.radius * 2f));

            Assert.That(
                _hurtbox.radius,
                Is.EqualTo(controller.radius).Within(0.001f),
                "The hurtbox and CharacterController must agree on radius.");
            Assert.That(
                _hurtbox.height,
                Is.EqualTo(controller.height).Within(0.001f),
                "The hurtbox and CharacterController must agree on height.");
            Assert.That(
                Vector3.Distance(_hurtbox.center, controller.center),
                Is.LessThan(0.001f),
                "The hurtbox and CharacterController must agree on center.");

            Vector3 center = Defender.transform.InverseTransformPoint(
                _hurtbox.transform.TransformPoint(_hurtbox.center));
            float bottom = center.y - (_hurtbox.height * 0.5f);
            Assert.That(
                bottom,
                Is.EqualTo(0f).Within(FloorTolerance),
                "The body hurtbox must stand on the floor plane.");
        }

        [UnityTest]
        public IEnumerator TorsoAndHeadStayInsideTheHurtboxWhileStriking()
        {
            PlaceFighters(0.76f);
            yield return Settle(0.6f);

            _hurtbox = Defender.GetComponentInChildren<CapsuleCollider>(true);
            Assert.IsNotNull(_hurtbox, "The defender must have a body hurtbox CapsuleCollider.");

            BeginScenario("hurtbox_torso");
            ResetTracking();

            yield return Sample(0.4f, default, 0f, _ => TrackTorso());
            yield return Press(Key.J);
            yield return Sample(1.2f, default, 0f, _ => TrackTorso());
            yield return WaitForIdle();
            yield return Press(Key.K);
            yield return Sample(1.4f, default, 0f, _ => TrackTorso());

            EndScenario();

            float allowed = _hurtbox.radius - InsideMargin;
            float bottom = _hurtbox.center.y - (_hurtbox.height * 0.5f);
            float top = _hurtbox.center.y + (_hurtbox.height * 0.5f);

            Debug.Log(
                $"[P41] hurtbox runtime: maxPlanar={_maxPlanar:0.000}m " +
                $"minY={_minLocalY:0.000} maxY={_maxLocalY:0.000} " +
                $"capsule=[{bottom:0.000},{top:0.000}] radius={_hurtbox.radius:0.000} " +
                $"samples={_boneSamples}");

            Assert.That(_boneSamples, Is.GreaterThan(60), "The torso bones must be sampled.");
            Assert.That(
                _maxPlanar,
                Is.LessThanOrEqualTo(allowed),
                $"Torso/head bones must stay inside the hurtbox radius " +
                $"({_maxPlanar:0.000} m from the axis, allowed {allowed:0.000} m).");
            Assert.That(
                _minLocalY,
                Is.GreaterThanOrEqualTo(bottom - 0.01f),
                "Torso/head bones must stay above the bottom of the hurtbox.");
            Assert.That(
                _maxLocalY,
                Is.LessThanOrEqualTo(top + 0.01f),
                "Torso/head bones must stay below the top of the hurtbox.");
            Assert.That(
                _maxHurtboxOffset,
                Is.LessThan(0.001f),
                "The hurtbox must stay attached to the defender root during strikes.");
        }

        private void ResetTracking()
        {
            _maxPlanar = 0f;
            _minLocalY = float.MaxValue;
            _maxLocalY = float.MinValue;
            _maxHurtboxOffset = 0f;
            _boneSamples = 0;
        }

        private void TrackTorso()
        {
            if (_hurtbox == null)
            {
                return;
            }

            _maxHurtboxOffset = Mathf.Max(
                _maxHurtboxOffset,
                _hurtbox.transform.localPosition.magnitude);

            Vector3 axis = _hurtbox.center;
            foreach (HumanBodyBones bone in TorsoBones)
            {
                Transform transform = DefenderAnimator.GetBoneTransform(bone);
                if (transform == null)
                {
                    continue;
                }

                Vector3 local = _hurtbox.transform.InverseTransformPoint(transform.position);
                float planar = new Vector2(local.x - axis.x, local.z - axis.z).magnitude;
                _maxPlanar = Mathf.Max(_maxPlanar, planar);
                _minLocalY = Mathf.Min(_minLocalY, local.y);
                _maxLocalY = Mathf.Max(_maxLocalY, local.y);
                _boneSamples++;
            }
        }
    }
}
