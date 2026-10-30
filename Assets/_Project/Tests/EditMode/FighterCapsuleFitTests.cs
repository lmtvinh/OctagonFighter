using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OctagonFighter.Tests.EditMode
{
    /// <summary>
    /// Guards the fighter's body hurtbox against the shipped prefab. The hurtbox is an upright
    /// capsule that covers the torso and head only. The arms and fists deliberately sit outside it:
    /// strikes are resolved by the swept fist query, not by the arm volume, so widening the body
    /// capsule to the T-pose arm span would make every off-line punch connect.
    /// </summary>
    /// <remarks>
    /// Every dimension is read from the prefab (CharacterController, CapsuleCollider and skinned
    /// meshes); only the tolerances below are authored here. The checks use bind-pose geometry, so
    /// they stay independent of any animation clip.
    /// </remarks>
    public sealed class FighterCapsuleFitTests
    {
        private const string FighterPrefabPath =
            "Assets/_Project/Prefabs/Fighters/PF_Fighter_Boxer.prefab";

        /// <summary>How far the capsule may sit above or below the floor plane.</summary>
        private const float FloorTolerance = 0.02f;

        /// <summary>Allowance for skinned-mesh bounds versus the head-top bone.</summary>
        private const float MeshTolerance = 0.02f;

        /// <summary>How far inside the capsule surface a body part must sit.</summary>
        private const float InsideMargin = 0.02f;

        /// <summary>The capsule may not tower more than this above the model's highest vertex.</summary>
        private const float TopOvershootTolerance = 0.15f;

        private GameObject _root;
        private CharacterController _controller;
        private CapsuleCollider _hurtbox;

        [SetUp]
        public void LoadPrefab()
        {
            _root = PrefabUtility.LoadPrefabContents(FighterPrefabPath);
            Assert.IsNotNull(_root, $"Could not load {FighterPrefabPath}.");

            _controller = _root.GetComponent<CharacterController>();
            _hurtbox = _root.GetComponentInChildren<CapsuleCollider>(true);
        }

        [TearDown]
        public void UnloadPrefab()
        {
            if (_root != null)
            {
                PrefabUtility.UnloadPrefabContents(_root);
                _root = null;
            }
        }

        [Test]
        public void HurtboxCapsuleMatchesTheCharacterController()
        {
            Assert.IsNotNull(_controller, "The fighter root must have a CharacterController.");
            Assert.IsNotNull(_hurtbox, "The fighter must have a body hurtbox CapsuleCollider.");

            Assert.IsTrue(_hurtbox.isTrigger, "The body hurtbox must be a trigger.");
            Assert.AreEqual(
                1,
                _hurtbox.direction,
                "The body hurtbox must be an upright (Y-axis) capsule.");
            Assert.That(_hurtbox.radius, Is.GreaterThan(0f), "The hurtbox radius must be positive.");
            Assert.That(
                _hurtbox.height,
                Is.GreaterThanOrEqualTo(_hurtbox.radius * 2f),
                "A capsule's height must be at least its diameter.");

            Assert.That(
                _hurtbox.radius,
                Is.EqualTo(_controller.radius).Within(0.001f),
                "The hurtbox and CharacterController must agree on radius.");
            Assert.That(
                _hurtbox.height,
                Is.EqualTo(_controller.height).Within(0.001f),
                "The hurtbox and CharacterController must agree on height.");
            Assert.That(
                Vector3.Distance(_hurtbox.center, _controller.center),
                Is.LessThan(0.001f),
                "The hurtbox and CharacterController must agree on center.");

            Assert.That(
                _hurtbox.transform.localPosition.magnitude,
                Is.LessThan(0.001f),
                "The hurtbox must sit at the fighter's origin so it follows the root exactly.");
            Assert.That(
                Quaternion.Angle(_hurtbox.transform.localRotation, Quaternion.identity),
                Is.LessThan(0.1f),
                "The hurtbox must not be rotated relative to the fighter root.");
        }

        [Test]
        public void CapsuleStandsOnTheFloorAndReachesTheTopOfTheHead()
        {
            Vector3 center = ToRootSpace(_hurtbox.transform.TransformPoint(_hurtbox.center));
            float bottom = center.y - (_hurtbox.height * 0.5f);
            float top = center.y + (_hurtbox.height * 0.5f);

            float meshBottom = MeshBounds().min.y;
            float meshTop = MeshBounds().max.y;
            Transform headTop = FindRequiredBone("HeadTop_End");
            float headTopY = ToRootSpace(headTop.position).y;

            TestContext.WriteLine(
                $"capsule bottom={bottom:0.000} top={top:0.000} " +
                $"mesh bottom={meshBottom:0.000} top={meshTop:0.000} headTop={headTopY:0.000}");

            Assert.That(
                bottom,
                Is.EqualTo(0f).Within(FloorTolerance),
                "The hurtbox must stand on the floor plane (fighter root at y = 0).");
            Assert.That(
                bottom,
                Is.LessThanOrEqualTo(meshBottom + FloorTolerance),
                "The hurtbox must not float above the lowest vertex (the feet).");

            Assert.That(
                top,
                Is.GreaterThanOrEqualTo(headTopY - MeshTolerance),
                "The hurtbox must reach the head-top bone.");
            Assert.That(
                top,
                Is.GreaterThanOrEqualTo(meshTop - MeshTolerance),
                "The hurtbox must cover the highest vertex (head or hair).");
            Assert.That(
                top,
                Is.LessThanOrEqualTo(meshTop + TopOvershootTolerance),
                "The hurtbox must not extend far above the model.");
        }

        [Test]
        public void TorsoAndHeadBonesStayInsideTheCapsule()
        {
            Vector3 axis = ToRootSpace(_hurtbox.transform.TransformPoint(_hurtbox.center));
            float bottom = axis.y - (_hurtbox.height * 0.5f);
            float top = axis.y + (_hurtbox.height * 0.5f);

            string[] required = { "Hips", "Spine", "Neck", "Head", "HeadTop_End" };
            foreach (string boneName in required)
            {
                AssertBoneInside(boneName, axis, bottom, top, required: true);
            }

            // Optional spine helpers: present on this rig, skipped if a future rig drops them.
            foreach (string boneName in new[] { "Spine1", "Spine2" })
            {
                AssertBoneInside(boneName, axis, bottom, top, required: false);
            }
        }

        [Test]
        public void HeadGeometryFitsInsideTheCapsule()
        {
            Vector3 axis = ToRootSpace(_hurtbox.transform.TransformPoint(_hurtbox.center));
            float neckY = ToRootSpace(FindRequiredBone("Neck").position).y;
            float allowed = _hurtbox.radius - InsideMargin;

            float widest = 0f;
            string widestSource = null;
            foreach (SkinnedMeshRenderer renderer in _root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 local = ToRootSpace(renderer.transform.TransformPoint(vertex));
                    if (local.y < neckY)
                    {
                        continue;
                    }

                    float planar = new Vector2(local.x - axis.x, local.z - axis.z).magnitude;
                    if (planar > widest)
                    {
                        widest = planar;
                        widestSource = renderer.name;
                    }
                }
            }

            Assert.IsNotNull(widestSource, "No geometry was found above the neck bone.");
            Assert.That(
                widest,
                Is.LessThanOrEqualTo(allowed),
                $"Head geometry ('{widestSource}') must stay inside the capsule radius " +
                $"({widest:0.000} m from the axis, allowed {allowed:0.000} m).");
        }

        [Test]
        public void ExtendedArmsAreIntentionallyOutsideTheBodyHurtbox()
        {
            Vector3 axis = ToRootSpace(_hurtbox.transform.TransformPoint(_hurtbox.center));
            float minimumArmGap = _hurtbox.radius + 0.2f;

            foreach (string handName in new[] { "LeftHand", "RightHand" })
            {
                Transform hand = FindRequiredBone(handName);
                Vector3 local = ToRootSpace(hand.position);
                float planar = new Vector2(local.x - axis.x, local.z - axis.z).magnitude;
                Assert.That(
                    planar,
                    Is.GreaterThan(minimumArmGap),
                    $"{handName} must stay outside the body hurtbox: the arms are not part of " +
                    "the hurtbox volume, so widening the capsule to cover them would make " +
                    "off-line strikes connect.");
            }

            // The mesh is wider than the capsule because the T-pose arms extend past the torso.
            Bounds bounds = MeshBounds();
            Assert.That(
                Mathf.Max(Mathf.Abs(bounds.min.x), Mathf.Abs(bounds.max.x)),
                Is.GreaterThan(_hurtbox.radius),
                "The model must be wider than the body capsule, otherwise the arm-exclusion " +
                "check above would be meaningless.");
        }

        private void AssertBoneInside(string boneName, Vector3 axis, float bottom, float top, bool required)
        {
            Transform bone = FindBone(boneName);
            if (bone == null)
            {
                if (required)
                {
                    Assert.Fail($"Required bone '{boneName}' was not found in the fighter prefab.");
                }

                return;
            }

            Vector3 local = ToRootSpace(bone.position);
            float planar = new Vector2(local.x - axis.x, local.z - axis.z).magnitude;
            Assert.That(
                planar,
                Is.LessThanOrEqualTo(_hurtbox.radius - InsideMargin),
                $"{boneName} must be horizontally inside the body capsule " +
                $"({planar:0.000} m from the axis, radius {_hurtbox.radius:0.000} m).");
            Assert.That(
                local.y,
                Is.InRange(bottom, top),
                $"{boneName} must be vertically inside the body capsule.");
        }

        private Bounds MeshBounds()
        {
            Bounds bounds = default;
            bool first = true;
            foreach (SkinnedMeshRenderer renderer in _root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Mesh mesh = renderer.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                foreach (Vector3 vertex in mesh.vertices)
                {
                    Vector3 local = ToRootSpace(renderer.transform.TransformPoint(vertex));
                    if (first)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        first = false;
                    }
                    else
                    {
                        bounds.Encapsulate(local);
                    }
                }
            }

            Assert.IsFalse(first, "The fighter prefab has no skinned mesh geometry.");
            return bounds;
        }

        private Transform FindRequiredBone(string suffix)
        {
            Transform bone = FindBone(suffix);
            Assert.IsNotNull(bone, $"Bone '{suffix}' was not found in the fighter prefab.");
            return bone;
        }

        /// <summary>
        /// Finds a humanoid bone by its name suffix, tolerating importer prefixes such as
        /// "mixamorig5:". Exact-suffix matching avoids confusing "Head" with "HeadTop_End".
        /// </summary>
        private Transform FindBone(string suffix)
        {
            foreach (Transform transform in _root.GetComponentsInChildren<Transform>(true))
            {
                if (transform.name == suffix || transform.name.EndsWith(":" + suffix))
                {
                    return transform;
                }
            }

            return null;
        }

        private Vector3 ToRootSpace(Vector3 worldPoint)
        {
            return _root.transform.InverseTransformPoint(worldPoint);
        }
    }
}
