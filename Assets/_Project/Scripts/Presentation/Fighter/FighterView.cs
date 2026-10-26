using OctagonFighter.Domain.Fighters;
using OctagonFighter.Domain.StateMachine;
using UnityEngine;

namespace OctagonFighter.Presentation.Fighter
{
    /// <summary>
    /// Presentation adapter that translates fighter simulation state into animator parameters.
    /// </summary>
    public sealed class FighterView : MonoBehaviour
    {
        private static readonly int SpeedParameter = Animator.StringToHash("Speed");
        private static readonly int MoveXParameter = Animator.StringToHash("MoveX");
        private static readonly int MoveYParameter = Animator.StringToHash("MoveY");
        private static readonly int IsBlockingParameter = Animator.StringToHash("IsBlocking");
        private static readonly int IsDefeatedParameter = Animator.StringToHash("IsDefeated");

        [SerializeField] private Animator _animator;
        [SerializeField, Min(0f)] private float _speedDamping = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _walkBlendSpeed = 0.5f;
        [SerializeField, Range(0f, 1f)] private float _runBlendSpeed = 1f;
        [SerializeField, Min(0.01f)] private float _runPlaybackSpeed = 1.818f;

        private bool _hasSpeed;
        private bool _hasMoveX;
        private bool _hasMoveY;
        private bool _hasBlocking;
        private bool _hasDefeated;

        /// <summary>
        /// Resolves the animator reference and caches the supported parameters.
        /// </summary>
        public void Initialize()
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }

            if (_animator == null || _animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter parameter in _animator.parameters)
            {
                if (parameter.nameHash == SpeedParameter)
                {
                    _hasSpeed = true;
                }
                else if (parameter.nameHash == MoveXParameter)
                {
                    _hasMoveX = true;
                }
                else if (parameter.nameHash == MoveYParameter)
                {
                    _hasMoveY = true;
                }
                else if (parameter.nameHash == IsBlockingParameter)
                {
                    _hasBlocking = true;
                }
                else if (parameter.nameHash == IsDefeatedParameter)
                {
                    _hasDefeated = true;
                }
            }
        }

        /// <summary>
        /// Applies a simulation snapshot and the consumed intent to the view.
        /// </summary>
        public void Apply(
            in FighterSnapshot snapshot,
            in FighterIntent intent,
            MovementAuthority authority)
        {
            if (_animator == null)
            {
                return;
            }

            float speed = 0f;
            Vector2 move = Vector2.zero;
            if (authority != MovementAuthority.None && intent.IsMovementRequested)
            {
                float gaitBlend = authority == MovementAuthority.Full &&
                                  intent.Gait == MovementGait.Run
                    ? _runBlendSpeed
                    : _walkBlendSpeed;

                float magnitude = Mathf.Clamp01(intent.Move.Magnitude);
                speed = magnitude * gaitBlend;

                // Input is planar in world space; the blend tree expects it relative to the fighter
                // so the same direction always reads as forward, backward, or strafe.
                Vector3 localDirection = transform.InverseTransformDirection(
                    new Vector3(intent.Move.X, 0f, intent.Move.Y));
                Vector2 planar = new Vector2(localDirection.x, localDirection.z);
                if (planar.sqrMagnitude > 0.0001f)
                {
                    move = planar.normalized * magnitude;
                }
            }

            if (_hasSpeed)
            {
                _animator.SetFloat(SpeedParameter, speed, _speedDamping, Time.deltaTime);
            }

            if (_hasMoveX)
            {
                _animator.SetFloat(MoveXParameter, move.x, _speedDamping, Time.deltaTime);
            }

            if (_hasMoveY)
            {
                _animator.SetFloat(MoveYParameter, move.y, _speedDamping, Time.deltaTime);
            }

            // Every body speed scales by the same walk multiplier, so one global playback rate
            // keeps all clipped directions matched while running.
            _animator.speed = authority == MovementAuthority.Full &&
                              intent.Gait == MovementGait.Run &&
                              intent.IsMovementRequested
                ? _runPlaybackSpeed
                : 1f;

            if (_hasBlocking)
            {
                _animator.SetBool(IsBlockingParameter, snapshot.State == FighterStateId.Blocking);
            }

            if (_hasDefeated)
            {
                _animator.SetBool(IsDefeatedParameter, snapshot.State == FighterStateId.Defeated);
            }
        }
    }
}
