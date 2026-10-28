using OctagonFighter.Domain.Combat;
using UnityEngine;

namespace OctagonFighter.Infrastructure.Combat
{
    /// <summary>
    /// Samples the animated fists so hit queries follow the striking hand instead of the torso.
    /// </summary>
    /// <remarks>
    /// The rig is read-only: it never moves a bone, teleports a fist, or repositions a target. Each
    /// sample reports the hand's position on the previous simulation tick and on the current one so
    /// the caller can sweep between them, which is what keeps a fast fist from tunnelling through a
    /// target between 60 Hz ticks. Missing bones are reported instead of throwing, so a rig without
    /// a hand simply cannot resolve a hit.
    /// </remarks>
    [DisallowMultipleComponent]
    public sealed class FighterStrikeRig : MonoBehaviour
    {
        private const int HandCount = 2;

        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _leftHand;
        [SerializeField] private Transform _rightHand;

        private readonly Vector3[] _previousPositions = new Vector3[HandCount];
        private readonly bool[] _hasPreviousPositions = new bool[HandCount];
        private bool _isInitialized;

        /// <summary>
        /// Gets a value indicating whether both fists were resolved.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Resolves the animator and both hand transforms. Safe to call more than once.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator != null && _animator.isHuman)
            {
                if (_leftHand == null)
                {
                    _leftHand = _animator.GetBoneTransform(HumanBodyBones.LeftHand);
                }

                if (_rightHand == null)
                {
                    _rightHand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
                }
            }

            if (_leftHand == null)
            {
                _leftHand = FindBone("LeftHand");
            }

            if (_rightHand == null)
            {
                _rightHand = FindBone("RightHand");
            }

            _isInitialized = _leftHand != null && _rightHand != null;
        }

        /// <summary>
        /// Resolves a hand transform for a strike.
        /// </summary>
        public bool TryGetHand(StrikeHand hand, out Transform handTransform)
        {
            switch (hand)
            {
                case StrikeHand.Left:
                    handTransform = _leftHand;
                    break;

                case StrikeHand.Right:
                    handTransform = _rightHand;
                    break;

                default:
                    handTransform = null;
                    break;
            }

            return handTransform != null;
        }

        /// <summary>
        /// Restarts the sweep for one hand at its current position.
        /// </summary>
        /// <remarks>
        /// Call this whenever a new contact window opens so the first sweep never spans the wind-up
        /// from the guard position to the strike.
        /// </remarks>
        public void BeginSweep(StrikeHand hand)
        {
            if (!TryGetHand(hand, out Transform handTransform))
            {
                return;
            }

            int index = (int)hand;
            _previousPositions[index] = handTransform.position;
            _hasPreviousPositions[index] = true;
        }

        /// <summary>
        /// Samples one hand and returns the segment it travelled since the previous sample.
        /// </summary>
        public bool TrySampleSweep(StrikeHand hand, out Vector3 previous, out Vector3 current)
        {
            previous = default;
            current = default;

            if (!TryGetHand(hand, out Transform handTransform))
            {
                return false;
            }

            int index = (int)hand;
            current = handTransform.position;
            previous = _hasPreviousPositions[index] ? _previousPositions[index] : current;
            _previousPositions[index] = current;
            _hasPreviousPositions[index] = true;
            return true;
        }

        private Transform FindBone(string suffix)
        {
            foreach (Transform candidate in GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name.EndsWith(suffix, System.StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
