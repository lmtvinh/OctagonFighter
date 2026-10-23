using OctagonFighter.Core.Math;
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
        private static readonly int IsBlockingParameter = Animator.StringToHash("IsBlocking");
        private static readonly int IsDefeatedParameter = Animator.StringToHash("IsDefeated");

        [SerializeField] private Animator _animator;
        [SerializeField, Min(0f)] private float _speedDamping = 0.1f;

        private bool _hasSpeed;
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
        /// Applies a simulation snapshot to the view.
        /// </summary>
        public void Apply(FighterSnapshot snapshot, Float2 move, MovementAuthority authority)
        {
            if (_animator == null)
            {
                return;
            }

            float speed = authority == MovementAuthority.None ? 0f : move.Magnitude;

            if (_hasSpeed)
            {
                _animator.SetFloat(SpeedParameter, speed, _speedDamping, Time.deltaTime);
            }

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
